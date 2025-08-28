using Dalamud.Game.ClientState.Objects.Types;
using InsertNameHere3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using InsertNameHere3.Modules;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Hooking;

namespace InsertNameHere3.Modules.PvP.Jobs
{
    public unsafe class WarriorPvPModule : BasePvPJobModule
    {
        public override uint JobId => Service.JobWarrior;
        
        private Hook<ActionManager.Delegates.UseAction>? _useActionHook;

        public WarriorPvPModule(IPvPCombatModule combatModule, Configuration configuration, ActionTracker actionTracker) 
            : base(combatModule, configuration)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            
            // Hook UseAction to intercept Primal Rend usage
            _useActionHook = Service.GameInteropProvider.HookFromAddress<ActionManager.Delegates.UseAction>(
                ActionManager.MemberFunctionPointers.UseAction, UseActionDetour);
            _useActionHook.Enable();
        }

        public override void ExecuteJobSpecificLogic()
        {
            // Main job logic runs in the base Update method
            // Additional Warrior-specific logic can be added here if needed
        }

        private bool UseActionDetour(ActionManager* actionManager, ActionType actionType, uint actionId, ulong targetId, uint param, ActionManager.UseActionMode mode, uint comboRouteId, bool* outOptAreaTargeted)
        {
            // Only intercept when we're a Warrior in PvP with enemies nearby
            if (Service.ClientState.LocalPlayer?.ClassJob.RowId == JobId &&
                CombatModule.IsPvPAndEnemiesNearBy &&
                Configuration.WarriorPrimalRendTargetCorrection > 0)
            {
                switch (actionId)
                {
                    case Service.Action_PrimalRend:
                        {
                            if (ActionManager.Instance()->GetAdjustedActionId(actionId) != actionId)
                            {
                                break;
                            }
                            var (optimalTarget, predictedHitCount) = FindOptimalPrimalRendTarget();
                            if (optimalTarget != null)
                            {
                                bool shouldRedirect = targetId == Service.EmptyPlayerObjectId 
                                    ? (predictedHitCount >= 1)  // Redirect if at least 1 hit
                                    : (predictedHitCount > 1);  // Redirect only if >1 hits

                                if (shouldRedirect)
                                {
                                    targetId = optimalTarget.GameObjectId;
                                    Service.Log.Debug($"WarriorPrimalRend: Redirecting to optimal target with {predictedHitCount} predicted hits.");
                                }
                            }
                        }
                        Service.Log.Debug($"WarriorPrimalRend: Not Redirecting.");

                        break;

                    case Service.Action_PrimalScream:
                        {
                            var (optimalRotation, predictedHitCount) = FindOptimalPrimalScreamRotation();
                            if (predictedHitCount > 0)
                            {
                                // For Primal Scream, we need to adjust player rotation to hit optimal targets
                                // Since we can't directly change rotation in the hook, we'll find the best target
                                // in the direction that would hit the most enemies
                                var optimalTarget = FindBestTargetForPrimalScream(optimalRotation);
                                if (optimalTarget != null)
                                {
                                    targetId = optimalTarget.GameObjectId;
                                    Service.Log.Debug(
                                        $"WarriorPrimalScream: Redirecting to optimal target with {predictedHitCount} predicted hits.");
                                }
                            }
                        }
                        break;
                }
            }

            // Call the original UseAction
            return _useActionHook!.Original(actionManager, actionType, actionId, targetId, param, mode, comboRouteId, outOptAreaTargeted);
        }

        private (IGameObject? optimalTarget, int predictedHitCount) FindOptimalPrimalRendTarget()
        {
            var localPlayer = Service.ClientState.LocalPlayer;
            if (localPlayer == null) return (null, 0);

            var playerPosition = localPlayer.Position;
            var playerRotation = localPlayer.Rotation;
            
            // Use CombatModule.AllEnemyActors instead of manually querying GameObjects
            var enemies = CombatModule.AllEnemyActors
                .Select(enemyActor => enemyActor.BattleChara)
                .ToList();
            
            if (!enemies.Any()) return (null, 0);

            IGameObject? bestTarget = null;
            int maxHitCount = 0;

            foreach (var candidate in enemies)
            {
                // Check if candidate is within facing constraint if mode 1 is selected (120 degree facing)
                if (Configuration.WarriorPrimalRendTargetCorrection == 1 && !IsWithinFacingAngle(playerPosition, playerRotation, candidate.Position))
                {
                    continue;
                }

                if (!CombatModule.Available_Range(Service.Action_PrimalRend, candidate))
                {
                    continue;
                }
                
                // Count how many enemies would be hit if we target this candidate
                var hitCount = CountEnemiesInPrimalRendRange(candidate.Position, enemies);
                
                if (hitCount > maxHitCount)
                {
                    maxHitCount = hitCount;
                    bestTarget = candidate;
                }
            }
            
            Service.Log.Debug($"WarriorPrimalRend: Found optimal target {bestTarget?.Name} {bestTarget?.GameObjectId} with {maxHitCount} hits in range.");
            
            return (bestTarget, maxHitCount);
        }

        private bool IsWithinFacingAngle(Vector3 playerPos, float playerRotation, Vector3 targetPos)
        {
            // Calculate the angle between player facing direction and target direction
            var directionToTarget = Vector3.Normalize(targetPos - playerPos);
            var playerFacing = new Vector3((float)Math.Sin(playerRotation), 0, (float)Math.Cos(playerRotation));
            
            var dot = Vector3.Dot(playerFacing, directionToTarget);
            var angle = Math.Acos(Math.Clamp(dot, -1f, 1f));
            
            // 120 degrees = 2.094 radians, so half angle is ~1.047 radians
            return angle <= Math.PI * (120.0 / 360.0);
        }

        private int CountEnemiesInPrimalRendRange(Vector3 centerPosition, List<IBattleChara> allEnemies)
        {
            return allEnemies.Count(enemy => Vector3.Distance(centerPosition, enemy.Position) <= Service.Action_PrimalRend_Radius);
        }

        private (float optimalRotation, int predictedHitCount) FindOptimalPrimalScreamRotation()
        {
            var localPlayer = Service.ClientState.LocalPlayer;
            if (localPlayer == null) return (0f, 0);

            var playerPosition = localPlayer.Position;
            
            // Use CombatModule.AllEnemyActors instead of manually querying GameObjects
            var enemies = CombatModule.AllEnemyActors
                .Select(enemyActor => enemyActor.BattleChara)
                .Where(enemy => Vector3.Distance(playerPosition, enemy.Position) <= Service.Action_PrimalScream_Radius)
                .ToList();
            
            if (!enemies.Any()) return (0f, 0);

            float bestRotation = 0f;
            int maxHitCount = 0;

            // Test rotations in 15-degree increments (24 total directions)
            for (int i = 0; i < 24; i++)
            {
                float testRotation = (i * 15f) * (float)Math.PI / 180f; // Convert to radians
                int hitCount = CountEnemiesInPrimalScreamSector(playerPosition, testRotation, enemies);
                
                if (hitCount > maxHitCount)
                {
                    maxHitCount = hitCount;
                    bestRotation = testRotation;
                }
            }
            
            Service.Log.Debug($"WarriorPrimalScream: Found optimal rotation {bestRotation * 180f / Math.PI:F1}° with {maxHitCount} hits.");
            
            return (bestRotation, maxHitCount);
        }

        private int CountEnemiesInPrimalScreamSector(Vector3 playerPosition, float rotation, List<IBattleChara> enemies)
        {
            var facingDirection = new Vector3((float)Math.Sin(rotation), 0, (float)Math.Cos(rotation));
            const float halfSectorAngle = 45f * (float)Math.PI / 180f; // 90 degrees / 2 = 45 degrees in radians
            
            return enemies.Count(enemy =>
            {
                var directionToEnemy = Vector3.Normalize(enemy.Position - playerPosition);
                var dot = Vector3.Dot(facingDirection, directionToEnemy);
                var angle = Math.Acos(Math.Clamp(dot, -1f, 1f));
                
                return angle <= halfSectorAngle;
            });
        }

        private IGameObject? FindBestTargetForPrimalScream(float optimalRotation)
        {
            var localPlayer = Service.ClientState.LocalPlayer;
            if (localPlayer == null) return null;

            var playerPosition = localPlayer.Position;
            var facingDirection = new Vector3((float)Math.Sin(optimalRotation), 0, (float)Math.Cos(optimalRotation));
            
            // Use CombatModule.AllEnemyActors instead of manually querying GameObjects
            var enemies = CombatModule.AllEnemyActors
                .Select(enemyActor => enemyActor.BattleChara)
                .Where(enemy => Vector3.Distance(playerPosition, enemy.Position) <= Service.Action_PrimalScream_Radius)
                .ToList();
            
            if (!enemies.Any()) return null;

            // Find the enemy closest to the center of the optimal sector
            IGameObject? bestTarget = null;
            float smallestAngleDifference = float.MaxValue;
            
            const float halfSectorAngle = 45f * (float)Math.PI / 180f; // 90 degrees / 2
            
            foreach (var enemy in enemies)
            {
                var directionToEnemy = Vector3.Normalize(enemy.Position - playerPosition);
                var dot = Vector3.Dot(facingDirection, directionToEnemy);
                var angle = (float)Math.Acos(Math.Clamp(dot, -1f, 1f));
                
                // Only consider enemies within the 90-degree sector
                if (angle <= halfSectorAngle && angle < smallestAngleDifference)
                {
                    smallestAngleDifference = angle;
                    bestTarget = enemy;
                }
            }
            
            return bestTarget;
        }

        public override void Dispose()
        {
            _useActionHook?.Dispose();
            base.Dispose();
        }
    }
}
