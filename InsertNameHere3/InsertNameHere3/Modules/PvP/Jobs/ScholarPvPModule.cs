using Dalamud.Game.ClientState.Objects.Types;
using InsertNameHere3;
using System;
using System.Collections.Generic;
using System.Linq;
using InsertNameHere3.Modules;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace InsertNameHere3.Modules.PvP.Jobs
{
    public class ScholarPvPModule : BasePvPJobModule
    {
        public override uint JobId => Service.JobScholar;
        
        private readonly SkillSequenceStateMachine _spreadPoisonCombo;

        public ScholarPvPModule(IPvPCombatModule combatModule, Configuration configuration, ActionTracker actionTracker) 
            : base(combatModule, configuration)
        {
            // Initialize spread poison combo sequence
            var spreadPoisonSequence = new List<SkillSequenceStateMachine.SkillStep>
            {
                new(Service.Action_Biolysis, logMessage: "Biolysis casted"), // Apply poison first
                new(Service.Action_Deployment_Tactics, logMessage: "Deployment Tactics casted - poison spread complete")
            };
            _spreadPoisonCombo = new SkillSequenceStateMachine(spreadPoisonSequence, Cast, TimeSpan.FromSeconds(1), actionTracker);
        }

        public override void ExecuteJobSpecificLogic()
        {
            // Update active combo if running
            if (_spreadPoisonCombo.IsActive)
            {
                _spreadPoisonCombo.Update();
                return;
            }

            // Execute auto spread poison if enabled
            if (Configuration.ScholarAutoSpreadPoison)
            {
                ExecuteAutoSpreadPoison();
            }
        }

        private bool ExecuteAutoSpreadPoison()
        {
            // Check if we can start the spread poison combo
            if (!CanExecuteSpreadPoison())
                return false;

            // Find the best target for spreading poison
            var target = FindBestSpreadPoisonTarget();
            
            if (target == null)
                return false;
            
            // Check secret tactics mode requirements
            if (!CheckSecretTacticsRequirement())
                return false;

            // Start the spread poison combo
            _spreadPoisonCombo.StartSequence(target);
            Service.Log.Debug($"[Scholar] Starting spread poison combo on {target.Name}");
            return true;
        }

        private bool CanExecuteSpreadPoison()
        {
            // Check if both skills are available
            return CombatModule.ActionReady(Service.Action_Biolysis) && 
                   CombatModule.ActionReady(Service.Action_Deployment_Tactics);
        }

        private IGameObject? FindBestSpreadPoisonTarget()
        {
            if (!CombatModule.IsPvPAndEnemiesNearBy)
                return null;

            var targetCandidates = new List<(IGameObject target, float totalDamage, int spreadCount, float mainTargetDamage)>();

            // Find enemies in range for Biolysis
            foreach (var enemyActor in CombatModule.AllEnemyActors)
            {
                if (!enemyActor.IsSelectableAsTarget)
                {
                    continue;
                }

                var enemy = enemyActor.BattleChara;
                if (!CombatModule.Available_Range(Service.Action_Biolysis, enemy))
                {
                    continue;
                }

                // Count nearby enemies that would be affected by spread using proper range
                int nearbyEnemyCount = CountNearbyEnemies(enemy, Service.Action_Biolysis_Radius);
                
                if (nearbyEnemyCount >= Configuration.ScholarSpreadPoisonTargetCount)
                {
                    // Calculate the damage to the main target with job corrections and buff calculations
                    // Base Biolysis damage - adjust this value based on actual skill data if needed
                    float baseDamage = 4000f;
                    
                    float mainTargetDamage = CalculateFinalDamage(
                        baseDamage, 
                        JobId, 
                        enemy.ClassJob.RowId, 
                        Service.ClientState.LocalPlayer?.StatusList, 
                        enemy.StatusList
                    );
                    
                    // Total damage = main target damage × number of targets hit by spread
                    // The spread targets receive the same damage as the main target regardless of their job/buffs
                    float totalDamage = mainTargetDamage * nearbyEnemyCount;
                    
                    targetCandidates.Add((enemy, totalDamage, nearbyEnemyCount, mainTargetDamage));
                    
                    Service.Log.Debug($"[Scholar] Target {enemy.Name}: Main damage={mainTargetDamage:F0}, Spread count={nearbyEnemyCount}, Total damage={totalDamage:F0}");
                }
            }

            if (targetCandidates.Count == 0)
                return null;

            // Return the target that maximizes total damage output
            var bestTarget = targetCandidates
                .OrderByDescending(candidate => candidate.totalDamage)
                .First();
                
            Service.Log.Debug($"[Scholar] Best spread target: {bestTarget.target.Name} with total damage {bestTarget.totalDamage:F0} across {bestTarget.spreadCount} targets (main target damage: {bestTarget.mainTargetDamage:F0})");
            
            return bestTarget.target;
        }

        private int CountNearbyEnemies(IGameObject center, float range)
        {
            int count = 0;
            var centerPos = center.Position;

            foreach (var enemyActor in CombatModule.AllEnemyActors)
            {
                var enemy = enemyActor.BattleChara;
                var distance = (enemy.Position - centerPos).Length();
                if (distance <= range)
                {
                    count++;
                }
            }

            return count;
        }

        private bool CheckSecretTacticsRequirement()
        {
            var mode = Configuration.ScholarSecretTacticsMode;
            var player = Service.ClientState.LocalPlayer;

            if (player == null)
                return false;

            switch (mode)
            {
                case 0: // 自動祕策 - 僅祕策時擴毒 (Auto secret tactics - only spread with secret tactics)
                    // Check if we have Recitation buff, if not try to cast it
                    bool hasRecitationBuff = player.StatusList.Any(status => status.StatusId == Service.Buff_Recitation);
                    if (!hasRecitationBuff)
                    {
                        if (CombatModule.ActionReady(Service.Action_Recitation))
                        {
                            Cast(Service.Action_Recitation);
                        }
                        
                        return false; // Recitation not ready, can't proceed
                    }
                    return true;

                case 1: // 手動祕策 - 僅祕策時擴毒 (Manual secret tactics - only spread with secret tactics)
                    // Check if we already have Recitation buff
                    bool hasManualRecitationBuff = player.StatusList.Any(status => status.StatusId == Service.Buff_Recitation);
                    if (hasManualRecitationBuff)
                    {
                        return true;
                    }
                    
                    return false;

                case 2: // 毒可用即擴 - 嘗試自動祕策 (Spread when poison available - try auto secret tactics)
                    // Try to use auto secret tactics if available, but allow spreading even without it
                    bool hasRecitationBuff2 = player.StatusList.Any(status => status.StatusId == Service.Buff_Recitation);
                    if (!hasRecitationBuff2 && CombatModule.ActionReady(Service.Action_Recitation))
                    {
                        Cast(Service.Action_Recitation);
                        return false; // Wait for recitation to be cast
                    }
                    return true; // Always allow spreading regardless of recitation status

                case 3: // 毒可用即擴 (Spread when poison available)
                    return true; // Always allow spreading regardless of secret tactics

                default:
                    return false;
            }
        }

        public override void Dispose()
        {
            _spreadPoisonCombo?.Dispose();
            base.Dispose();
        }
    }
}
