using System;
using System.Collections.Generic;
using InsertNameHere3;
using InsertNameHere3.Modules;

namespace InsertNameHere3.Modules.PvP.Jobs
{
    public class MachinistPvPModule : BasePvPJobModule
    {
        public override uint JobId => Service.JobMachinist;
        
        private readonly SkillSequenceStateMachine _wildfireLbCombo;
        private readonly SkillSequenceStateMachine _eagleEyeLbCombo;
        private readonly SkillSequenceStateMachine _normalWildfireCombo;

        public MachinistPvPModule(IPvPCombatModule combatModule, Configuration configuration, ActionTracker actionTracker) 
            : base(combatModule, configuration)
        {
            // Initialize wildfire combo sequence
            var wildfireSequence = new List<SkillSequenceStateMachine.SkillStep>
            {
                new(Service.Action_Wildfire, logMessage: "Wildfire casted"),
                new(Service.Action_FullMetal, logMessage: "FullMetal casted"),
                new(Service.Action_EagleEyeShot, logMessage: "EagleEyeShot casted"),
                new(Service.Action_MarksmansSpite, logMessage: "MarksmansSpite casted - combo complete")
            };
            _wildfireLbCombo = new SkillSequenceStateMachine(wildfireSequence, Cast, TimeSpan.FromSeconds(3), actionTracker);

            // Initialize normal wildfire combo sequence (without LB)
            var normalWildfireSequence = new List<SkillSequenceStateMachine.SkillStep>
            {
                new(Service.Action_Analyze, logMessage: "Analyze casted"),
                new(Service.Action_Wildfire, logMessage: "Wildfire casted"),
                new(Service.Action_Drill, logMessage: "Drill casted"), // Will be replaced by available skill
                new(Service.Action_EagleEyeShot, logMessage: "EagleEyeShot casted"),
                new(Service.Action_FullMetal, logMessage: "FullMetal casted - combo complete")
            };
            _normalWildfireCombo = new SkillSequenceStateMachine(normalWildfireSequence, Cast, TimeSpan.FromSeconds(5), actionTracker);

            // Initialize LB + EE combo sequence
            var lbeeSequence = new List<SkillSequenceStateMachine.SkillStep>
            {
                new(Service.Action_EagleEyeShot, logMessage: "EagleEyeShot casted"),
                new(Service.Action_MarksmansSpite, logMessage: "MarksmansSpite casted - combo complete")
            };
            _eagleEyeLbCombo = new SkillSequenceStateMachine(lbeeSequence, Cast, TimeSpan.FromSeconds(2), actionTracker);
        }

        public override void ExecuteJobSpecificLogic()
        {
            // Only execute one sequence at a time - check if any sequence is already active
            if (_wildfireLbCombo.IsActive || _eagleEyeLbCombo.IsActive || _normalWildfireCombo.IsActive)
            {
                // Ensure ForceTarget is set to the current combo target
                var activeCombo = GetActiveCombo();
                if (activeCombo?.CurrentTarget != null && !ReferenceEquals(CombatModule.ForceTarget, activeCombo.CurrentTarget))
                {
                    CombatModule.ForceTarget = activeCombo.CurrentTarget;
                }

                _wildfireLbCombo.Update();
                _eagleEyeLbCombo.Update();
                _normalWildfireCombo.Update();

                // Clear ForceTarget if no combos are active anymore
                if (!_wildfireLbCombo.IsActive && !_eagleEyeLbCombo.IsActive && !_normalWildfireCombo.IsActive)
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

            // Execute in priority order: LB+EE (highest) -> LB Wildfire -> Normal Wildfire (lowest)
            if (ExecuteLbEe())
                return;
            ExecuteWildfire();

            // // Update any active sequences
            // _wildfireLbCombo.Update();
            // _eagleEyeLbCombo.Update();
            // _normalWildfireCombo.Update();
        }

        private SkillSequenceStateMachine? GetActiveCombo()
        {
            if (_wildfireLbCombo.IsActive) return _wildfireLbCombo;
            if (_eagleEyeLbCombo.IsActive) return _eagleEyeLbCombo;
            if (_normalWildfireCombo.IsActive) return _normalWildfireCombo;
            return null;
        }

        private bool ExecuteLbEe()
        {
            bool useLb = Configuration.MachinistAutoLB && CombatModule.ActionReady(Service.Action_MarksmansSpite);
            bool useEe = Configuration.MachinistAutoEagleEye && CombatModule.ActionReady(Service.Action_EagleEyeShot);
            if (!useLb && !useEe)
                return false;
            
            foreach (var enemyActor in CombatModule.AllEnemyActors)
            {
                if (!enemyActor.IsSelectableAsTarget)
                {
                    continue;
                }
                var chara = enemyActor.BattleChara;
                
                // Skip enemies who can use bubble (bubble is available) - they can defend against our LB
                if (Configuration.AvoidBubbleEnemiesForAutoLB && !CombatModule.IsEnemyBubbleOnCooldown(chara))
                {
                    continue;
                }
                
                var predictedDamageLb = CalculateFinalDamage(40000f, JobId, chara.ClassJob.RowId, 
                    Service.ClientState.LocalPlayer?.StatusList, chara.StatusList);
                var predictedDamageEe = CalculateFinalDamage(12000f, JobId, chara.ClassJob.RowId,
                    Service.ClientState.LocalPlayer?.StatusList, chara.StatusList, true);
            
                long shield = chara.ShieldPercentage > 0 ? chara.MaxHp * (chara.ShieldPercentage + 1) / 100 : 0;

                if (useLb && 
                    CombatModule.DamageAsExpected(predictedDamageLb, shield+chara.CurrentHp) &&
                    CombatModule.Available_Range(Service.Action_MarksmansSpite, chara))
                {
                    Cast(Service.Action_MarksmansSpite, chara.GameObjectId);
                    return false;
                }
                
                // Individual EE check
                if (useEe && 
                    CombatModule.DamageAsExpected(predictedDamageEe, shield+chara.CurrentHp) &&
                    CombatModule.Available_Range(Service.Action_EagleEyeShot, chara))
                {
                    Cast(Service.Action_EagleEyeShot, chara.GameObjectId);
                    return false;
                }
                
                // Combined EE + LB check
                if (useEe && useLb && 
                    CombatModule.DamageAsExpected(predictedDamageEe+predictedDamageLb, shield+chara.CurrentHp) &&
                    CombatModule.Available_Range(Service.Action_EagleEyeShot, chara, true))
                {
                    Service.Log.Debug($"Machinist PvP: Starting LB + EE combo on {chara.Name.ToString()}");
                    _eagleEyeLbCombo.StartSequence(chara);
                    return true;
                }
            }
            return false;
        }
        
        private void ExecuteWildfire()
        {
            if (!Configuration.MachinistAutoWildFire || _wildfireLbCombo.IsActive || _normalWildfireCombo.IsActive)
                return;
            
            float wildfireDamage = 0;
            bool useLbWildfire = false;
            bool useNormalWildfire = false;
            
            // Check if LB wildfire combo is available and viable
            if (Configuration.MachinistAutoWildFireMayUseLB && ValidateLbWildfireRequirements())
            {
                wildfireDamage = 83000f; // LB wildfire damage
                useLbWildfire = true;
            }
            // Check if normal wildfire combo is available and viable
            else if (ValidateNormalWildfireRequirements())
            {
                // Determine damage based on available finisher skill
                if (CombatModule.ActionReady(Service.Action_ChainSaw))
                {
                    wildfireDamage = 63600f;
                    useNormalWildfire = true;
                }
                else if (CombatModule.ActionReady(Service.Action_Drill))
                {
                    wildfireDamage = 61000f;
                    useNormalWildfire = true;
                }
                else if (CombatModule.ActionReady(Service.Action_Anchor))
                {
                    wildfireDamage = 55000f;
                    useNormalWildfire = true;
                }
            }
            
            if (!useLbWildfire && !useNormalWildfire)
                return;
            
            foreach (var enemyActor in CombatModule.AllEnemyActors)
            {
                if (!enemyActor.IsSelectableAsTarget)
                {
                    continue;
                }
                var chara = enemyActor.BattleChara;
                if (!CombatModule.Available_Range(Service.Action_Anchor, chara, true))
                {
                    continue;
                }
                
                var predictedDamage = CalculateFinalDamage(wildfireDamage, JobId, chara.ClassJob.RowId,
                    Service.ClientState.LocalPlayer?.StatusList, chara.StatusList);
                long shield = chara.ShieldPercentage > 0 ? chara.MaxHp * (chara.ShieldPercentage + 1) / 100 : 0;

                if (CombatModule.DamageAsExpected(predictedDamage, shield + chara.CurrentHp))
                {
                    if (useLbWildfire)
                    {
                        Service.Log.Debug("Machinist PvP: Starting LB Wildfire combo on " + chara.Name);
                        _wildfireLbCombo.StartSequence(chara);
                    }
                    else if (useNormalWildfire)
                    {
                        Service.Log.Debug("Machinist PvP: Starting Normal Wildfire combo on " + chara.Name);
                        // Update the normal wildfire sequence with the appropriate finisher skill
                        UpdateNormalWildfireSequence();
                        _normalWildfireCombo.StartSequence(chara);
                    }
                    return;
                }
            }
        }

        private bool ValidateLbWildfireRequirements()
        {
            return CombatModule.ActionReady(Service.Action_Wildfire) && 
                   CombatModule.ActionReady(Service.Action_FullMetal) &&
                   CombatModule.ActionReady(Service.Action_EagleEyeShot) &&
                   CombatModule.ActionReady(Service.Action_MarksmansSpite);
        }

        private bool ValidateNormalWildfireRequirements()
        {
            return CombatModule.ActionReady(Service.Action_Analyze) && 
                   CombatModule.ActionReady(Service.Action_Wildfire) && 
                   CombatModule.ActionReady(Service.Action_FullMetal) &&
                   CombatModule.ActionReady(Service.Action_EagleEyeShot) &&
                   (CombatModule.ActionReady(Service.Action_Drill) || 
                    CombatModule.ActionReady(Service.Action_ChainSaw) || 
                    CombatModule.ActionReady(Service.Action_Anchor));
        }

        private void UpdateNormalWildfireSequence()
        {
            // This method is kept for potential future use
            // Currently the normal wildfire sequence uses a fixed drill skill
        }

        public override void Dispose()
        {
            _wildfireLbCombo.Reset();
            _eagleEyeLbCombo.Reset();
            _normalWildfireCombo.Reset();
            base.Dispose();
        }
    }
}
