using InsertNameHere3;
using InsertNameHere3.Modules;
using System;

namespace InsertNameHere3.Modules.PvP.Jobs
{
    public class ReaperPvPModule : BasePvPJobModule
    {
        public override uint JobId => Service.JobReaper;

        public ReaperPvPModule(IPvPCombatModule combatModule, Configuration configuration, ActionTracker actionTracker) 
            : base(combatModule, configuration) { }

        public override void ExecuteJobSpecificLogic()
        {
            // Execute auto perfectio logic
            ExecuteAutoPerfectioLogic();
            
            // Execute smite logic
            ExecuteSmiteLogic(Configuration.ReaperAutoSmite);
        }

        private bool ExecuteAutoPerfectioLogic()
        {
            if (!Configuration.ReaperAutoPerfectio || !CombatModule.IsPvPAndEnemiesNearBy || !CombatModule.ActionReady(Service.Action_Perfectio))
                return false;

            foreach (var enemyActor in CombatModule.AllEnemyActors)
            {
                if (!enemyActor.IsSelectableAsTarget)
                {
                    continue;
                }
                var targetChara = enemyActor.BattleChara;
                
                // Check if target is in range (25m) for Perfectio
                if (!CombatModule.Available_Range(Service.Action_Perfectio, targetChara))
                {
                    continue;
                }
                
                // Check if target itself is killable (< 25% HP)
                float targetHpPercentage = (float)targetChara.CurrentHp / targetChara.MaxHp * 100f;
                bool targetIsKillable = targetHpPercentage < 25f;
                
                // If ReaperAutoPerfectioAllowNonWeak is false, skip non-killable targets
                if (!Configuration.ReaperAutoPerfectioAllowNonWeak && !targetIsKillable)
                {
                    continue;
                }
                
                // Count how many enemies within 5m radius of this target would be killed by Perfectio
                int predictedKills = 0;
                
                // Count the target itself if it's killable
                if (targetIsKillable)
                {
                    predictedKills++;
                }
                
                foreach (var potentialVictim in CombatModule.AllEnemyActors)
                {
                    if (!potentialVictim.IsSelectableAsTarget)
                    {
                        continue;
                    }
                    var victimChara = potentialVictim.BattleChara;
                    
                    // Skip the target itself (already counted above)
                    if (victimChara.GameObjectId == targetChara.GameObjectId)
                    {
                        continue;
                    }
                    
                    // Calculate distance between target position and potential victim
                    var distance = Math.Sqrt(Math.Pow(targetChara.Position.X - victimChara.Position.X, 2) +
                                           Math.Pow(targetChara.Position.Z - victimChara.Position.Z, 2))+targetChara.HitboxRadius;
                    
                    // Check if victim is within 5m radius of Perfectio AOE
                    if (distance < 5.0)
                    {
                        // Check if victim's HP is below 25% (shields don't protect against Perfectio's execute)
                        float actualHpPercentage = (float)victimChara.CurrentHp / victimChara.MaxHp * 100f;
                        if (actualHpPercentage < 25f)
                        {
                            predictedKills++;
                        }
                    }
                }
                
                // Cast Perfectio if predicted kills meet the minimum requirement
                if (predictedKills >= Configuration.ReaperAutoPerfectioMinPredictionKill)
                {
                    Service.Log.Debug($"Reaper PvP: Auto Perfectio on {targetChara.Name} (Predicted kills: {predictedKills})");
                    return Cast(Service.Action_Perfectio, targetChara.GameObjectId);
                }
            }
            return false;
        }
    }
}
