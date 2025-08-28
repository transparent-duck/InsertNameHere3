using Dalamud.Plugin.Services;
using System;
using InsertNameHere3;
using FFXIVClientStructs.FFXIV.Client.Game;
using InsertNameHere3.Modules;
using Dalamud.Game.ClientState.Statuses;

namespace InsertNameHere3.Modules.PvP.Jobs
{
    public abstract class BasePvPJobModule : IPvPJobModule
    {
        public abstract uint JobId { get; }
        // public bool IsEnabled { get; set; } = true;
        
        protected readonly IPvPCombatModule CombatModule;
        protected readonly Configuration Configuration;

        protected BasePvPJobModule(IPvPCombatModule combatModule, Configuration configuration)
        {
            CombatModule = combatModule;
            Configuration = configuration;
        }

        public virtual void Initialize() { }

        public virtual void Update(IFramework framework)
        {
            // Service.Log.Debug("Currnet Job: " + (Service.ClientState.LocalPlayer?.ClassJob.RowId == JobId));
            // Service.Log.Debug("currnetRowId: " + Service.ClientState.LocalPlayer?.ClassJob.RowId);
            // Service.Log.Debug("JobId: " + JobId);
            // Service.Log.Debug("Monk JobId: " + Service.JobMonk);
            // Service.Log.Debug("PvP Auto Skills Enabled: " + Configuration.PvPAutoSkillsEnabled);
            // Service.Log.Debug("Is PvP and Enemies Nearby: " + CombatModule.IsPvPAndEnemiesNearBy);
            if (!CombatModule.IsPvPAndEnemiesNearBy || 
                Service.ClientState.LocalPlayer?.ClassJob.RowId != JobId ||
                !Configuration.PvPAutoSkillsEnabled) return;
            
            // Execute job-specific logic (including smite for melee DPS jobs)
            ExecuteJobSpecificLogic();
        }

        public abstract void ExecuteJobSpecificLogic();

        public virtual void Dispose() { }

        // Helper methods for job modules
        protected unsafe bool Cast(uint actionId, ulong target = 0xE0000000)
        {
            // Use UseAction with queueing to match manual play behavior
            // This allows actions to be queued before the previous one completes
            Service.Log.Debug($"Casting action {actionId} on target {target:X}");
            return ActionManager.Instance()->UseAction(ActionType.Action, actionId, target);
        }
        
        protected unsafe bool CastInQueue(uint ac, ulong target = 0xE0000000)
        {
            return ActionManager.Instance()->UseAction(ActionType.Action, ac, target, mode: ActionManager.UseActionMode.Queue);
        }

        public float CalculateFinalDamage(float baseDamage, uint selfJob, uint targetJob, StatusList selfBuffs, StatusList targetBuffs, bool ignoreBubble = false)
        {
            float damage = baseDamage;

            if (Configuration.ExpectedDamageBuffCalculation)
            {
                if (!CombatModule.IsInPvP55)
                {
                    damage *= JobDamageCorrection(selfJob, targetJob);
                }
                
                var selfBuffDamageCorrections = Service.SelfBuffDamageCorrections;
                foreach (var buff in selfBuffs)
                {
                    if (selfBuffDamageCorrections.TryGetValue(buff.StatusId, out var factor))
                    {
                        damage *= factor;
                    }
                }

                var targetBuffDamageCorrections = Service.TargetBuffDamageCorrections;
                foreach (var buff in targetBuffs)
                {
                    if (targetBuffDamageCorrections.TryGetValue(buff.StatusId, out var factor))
                    {
                        damage *= factor;
                    }
                }
                
                var specialBubbleStatus = Service.SpecialBubbleStatus;
                if (!ignoreBubble)
                {
                    foreach (var buff in targetBuffs)
                    {
                        if (specialBubbleStatus.TryGetValue(buff.StatusId, out var factor))
                        {
                            damage *= factor;
                        }
                    }
                }
            }

            return damage;
        }
        
        
        private static float JobDamageCorrection(uint selfJob, uint targetJob)
        {
            return 1 + Service.JobCorrections[selfJob].OutputCorrection +
                   Service.JobCorrections[targetJob].DamageTakenCorrection;
        }
        
        private static float CalHpRelatedDamage(float minDamage, float maxDamage, uint MaxHP, uint CurrentHp)
        {
            return Math.Max(18000,MathF.Ceiling((float)(MaxHP-CurrentHp)/MaxHP*100)*4/300*(maxDamage - minDamage) + minDamage);
        }

        // Helper method for melee DPS jobs to implement smite logic
        protected unsafe bool ExecuteSmiteLogic(bool jobSpecificSmiteEnabled)
        {
            if (!jobSpecificSmiteEnabled || !CombatModule.IsPvPAndEnemiesNearBy || !CombatModule.ActionReady(Service.Action_Smite))
                return false;
            
            foreach (var enemyActor in CombatModule.AllEnemyActors)
            {
                if (!enemyActor.IsSelectableAsTarget)
                {
                    continue;
                }
                var chara = enemyActor.BattleChara;
                if (!CombatModule.Available_Range(Service.Action_Smite, chara))
                {
                    continue;
                }
                var predictedDamage = CalculateFinalDamage(CalHpRelatedDamage(6000,18000,chara.MaxHp, chara.CurrentHp), JobId, chara.ClassJob.RowId, Service.ClientState.LocalPlayer.StatusList, chara.StatusList);
                long shield = chara.ShieldPercentage > 0 ? chara.MaxHp * (chara.ShieldPercentage + 1) / 100 : 0;

                if (CombatModule.DamageAsExpected(predictedDamage, shield+chara.CurrentHp))
                {
                    return Cast(Service.Action_Smite, chara.GameObjectId);
                }
            }
            return false;
        }
    }
}
