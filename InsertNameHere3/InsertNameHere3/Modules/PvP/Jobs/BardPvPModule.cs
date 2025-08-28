using System;
using System.Collections.Generic;
using InsertNameHere3;
using InsertNameHere3.Modules;

namespace InsertNameHere3.Modules.PvP.Jobs
{
    public class BardPvPModule : BasePvPJobModule
    {
        public override uint JobId => Service.JobBard;
        
        private readonly SkillSequenceStateMachine _harmonicEeCombo;

        public BardPvPModule(IPvPCombatModule combatModule, Configuration configuration, ActionTracker actionTracker) 
            : base(combatModule, configuration)
        {
            // Initialize Harmonic Arrow + Eagle Eye combo sequence
            var harmonicEeSequence = new List<SkillSequenceStateMachine.SkillStep>
            {
                new(Service.Action_HarmonicArrow, logMessage: "Harmonic Arrow casted"),
                new(Service.Action_EagleEyeShot, logMessage: "Eagle Eye Shot casted - combo complete")
            };
            _harmonicEeCombo = new SkillSequenceStateMachine(harmonicEeSequence, Cast, TimeSpan.FromSeconds(2), actionTracker);
        }

        public override void ExecuteJobSpecificLogic()
        {
            // Check if any sequence is already active
            if (_harmonicEeCombo.IsActive)
            {
                // Ensure ForceTarget is set to the current combo target
                if (_harmonicEeCombo.CurrentTarget != null && !ReferenceEquals(CombatModule.ForceTarget, _harmonicEeCombo.CurrentTarget))
                {
                    CombatModule.ForceTarget = _harmonicEeCombo.CurrentTarget;
                }

                _harmonicEeCombo.Update();

                // Clear ForceTarget if no combo is active anymore
                if (!_harmonicEeCombo.IsActive)
                {
                    CombatModule.ForceTarget = null;
                }
                
                return;
            }

            // Clear ForceTarget when no combos are running
            if (CombatModule.ForceTarget != null)
            {
                CombatModule.ForceTarget = null;
            }

            // Execute Bard-specific logic
            ExecuteBardLogic();
        }

        private bool ExecuteBardLogic()
        {
            bool useEe = Configuration.BardAutoEagleEye && CombatModule.ActionReady(Service.Action_EagleEyeShot);
            bool useHarmonic = Configuration.BardAutoHarmonicArrow && CombatModule.ActionReady(Service.Action_HarmonicArrow);
            
            if (!useEe && !useHarmonic)
                return false;
            
            foreach (var enemyActor in CombatModule.AllEnemyActors)
            {
                if (!enemyActor.IsSelectableAsTarget)
                {
                    continue;
                }
                var chara = enemyActor.BattleChara;
                
                var predictedDamageEe = CalculateFinalDamage(12000f, JobId, chara.ClassJob.RowId,
                    Service.ClientState.LocalPlayer?.StatusList, chara.StatusList, true);
                var predictedDamageHarmonic = CalculateFinalDamage(18000f, JobId, chara.ClassJob.RowId,
                    Service.ClientState.LocalPlayer?.StatusList, chara.StatusList);
            
                long shield = chara.ShieldPercentage > 0 ? chara.MaxHp * (chara.ShieldPercentage + 1) / 100 : 0;

                // Priority logic based on user requirements:
                // 1. If only autoEE enabled, use autoEE
                // 2. If only auto Harmonic Arrow enabled, do nothing (as requested)
                // 3. If both enabled, use Harmonic Arrow + EE combo, or autoEE if Harmonic is on cooldown
                Service.Log.Debug("shield: " + shield);
                Service.Log.Debug("predictedDamageEe: " + predictedDamageEe);
                Service.Log.Debug("predictedDamageHarmonic: " + predictedDamageHarmonic);
                Service.Log.Debug("currentHp: " + chara.CurrentHp);
                if (useEe && useHarmonic)
                {
                    // Both enabled - check if combined damage can kill the target
                    if (CombatModule.DamageAsExpected(predictedDamageEe + predictedDamageHarmonic, shield + chara.CurrentHp) &&
                        CombatModule.Available_Range(Service.Action_HarmonicArrow, chara, true))
                    {
                        Service.Log.Debug($"Bard PvP: Starting Harmonic Arrow + EE combo on {chara.Name.ToString()}");
                        _harmonicEeCombo.StartSequence(chara);
                        return true;
                    }
                }
                if (useEe)
                {
                    // Only autoEE enabled
                    if (CombatModule.DamageAsExpected(predictedDamageEe, shield + chara.CurrentHp) &&
                        CombatModule.Available_Range(Service.Action_EagleEyeShot, chara))
                    {
                        Cast(Service.Action_EagleEyeShot, chara.GameObjectId);
                        return true;
                    }
                }
            }
            return false;
        }

        public override void Dispose()
        {
            _harmonicEeCombo.Reset();
            base.Dispose();
        }
    }
}
