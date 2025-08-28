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
    public unsafe class DarkKnightPvPModule : BasePvPJobModule
    {
        public override uint JobId => Service.JobDarkKnight;
        
        private Hook<ActionManager.Delegates.UseAction>? _useActionHook;

        public DarkKnightPvPModule(IPvPCombatModule combatModule, Configuration configuration, ActionTracker actionTracker) 
            : base(combatModule, configuration)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            
            // Hook UseAction to intercept Plunge usage
            _useActionHook = Service.GameInteropProvider.HookFromAddress<ActionManager.Delegates.UseAction>(
                ActionManager.MemberFunctionPointers.UseAction, UseActionDetour);
            _useActionHook.Enable();
        }

        public override void ExecuteJobSpecificLogic()
        {
            // Main job logic runs in the base Update method
            // Additional DarkKnight-specific logic can be added here if needed
        }

        private bool UseActionDetour(ActionManager* actionManager, ActionType actionType, uint actionId, ulong targetId, uint param, ActionManager.UseActionMode mode, uint comboRouteId, bool* outOptAreaTargeted)
        {
            // Only intercept when we're a Dark Knight in PvP with enemies nearby
            if (Service.ClientState.LocalPlayer?.ClassJob.RowId == JobId &&
                CombatModule.IsPvPAndEnemiesNearBy &&
                Configuration.DarkKnightPlungeTargetCorrection > 0)
            {
                switch (actionId)
                {
                    case Service.Action_Plunge:
                        {
                            if (ActionManager.Instance()->GetAdjustedActionId(actionId) != actionId)
                            {
                                break;
                            }
                            var (optimalTarget, predictedHitCount) = FindOptimalPlungeTarget();
                            if (optimalTarget != null)
                            {
                                bool shouldRedirect = targetId == Service.EmptyPlayerObjectId 
                                    ? (predictedHitCount >= 1)  // Redirect if at least 1 hit
                                    : (predictedHitCount > 1);  // Redirect only if >1 hits

                                if (shouldRedirect)
                                {
                                    targetId = optimalTarget.GameObjectId;
                                    Service.Log.Debug($"DarkKnightPlunge: Redirecting to optimal target with {predictedHitCount} predicted hits.");
                                }
                            }
                        }
                        Service.Log.Debug($"DarkKnightPlunge: Not Redirecting.");
                        break;
                }
            }

            // Call the original UseAction
            return _useActionHook!.Original(actionManager, actionType, actionId, targetId, param, mode, comboRouteId, outOptAreaTargeted);
        }

        private (IGameObject? optimalTarget, int predictedHitCount) FindOptimalPlungeTarget()
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
                if (Configuration.DarkKnightPlungeTargetCorrection == 1 && !IsWithinFacingAngle(playerPosition, playerRotation, candidate.Position))
                {
                    continue;
                }

                if (!CombatModule.Available_Range(Service.Action_Plunge, candidate))
                {
                    continue;
                }
                
                // Count how many enemies would be hit if we target this candidate
                var hitCount = CountEnemiesInPlungeRange(candidate.Position, enemies);
                
                if (hitCount > maxHitCount)
                {
                    maxHitCount = hitCount;
                    bestTarget = candidate;
                }
            }
            
            Service.Log.Debug($"DarkKnightPlunge: Found optimal target {bestTarget?.Name} {bestTarget?.GameObjectId} with {maxHitCount} hits in range.");
            
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

        private int CountEnemiesInPlungeRange(Vector3 centerPosition, List<IBattleChara> allEnemies)
        {
            return allEnemies.Count(enemy => Vector3.Distance(centerPosition, enemy.Position) <= Service.Action_Plunge_Radius);
        }

        public override void Dispose()
        {
            _useActionHook?.Dispose();
            base.Dispose();
        }
    }
}
