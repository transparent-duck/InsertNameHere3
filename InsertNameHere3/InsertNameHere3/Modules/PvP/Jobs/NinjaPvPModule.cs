using InsertNameHere3;
using System;
using System.Linq;
using InsertNameHere3.Modules;
using InsertNameHere3.Modules.PvP.Jobs;

namespace InsertNameHere3.Modules.PvP.Jobs
{
    public class NinjaPvPModule : BasePvPJobModule
    {
        public override uint JobId => Service.JobNinja;

        public NinjaPvPModule(IPvPCombatModule combatModule, Configuration configuration, ActionTracker actionTracker) 
            : base(combatModule, configuration) { }

        public override void ExecuteJobSpecificLogic()
        {
            // Execute Ninja-specific LB logic
            if (Service.ClientState.LocalPlayer.ClassJob.RowId == Service.JobNinja && Configuration.NinjaAutoLB && SeitonReady())
            {
                foreach (var enemyActor in CombatModule.AllEnemyActors)
                {
                    if (!enemyActor.IsSelectableAsTarget)
                    {
                        continue;
                    }
                    var chara = enemyActor.BattleChara;
                    if (chara.CurrentHp >= chara.MaxHp * 0.5 - 1) continue;

                    // Check for protective buffs
                    bool havingProtectiveBuff = false;
                    foreach (var cc in chara.StatusList)
                    {
                        if (cc.StatusId.Equals(Service.Buff_3039) &&
                            (cc.RemainingTime < 0 || cc.RemainingTime >= 1.0))
                        {
                            havingProtectiveBuff = true;
                            break;
                        }

                        if (Service.Buffs_Hallow.Contains(cc.StatusId))
                        {
                            havingProtectiveBuff = true;
                            break;
                        }
                    }

                    if (havingProtectiveBuff) continue;

                    if (!CombatModule.Available_Range(Service.Action_SeitonTenchu, chara))
                    {
                        continue;
                    }

                    Cast(Service.Action_SeitonTenchu, chara.GameObjectId);
                    return;
                }
            }
            
            // Execute smite logic
            ExecuteSmiteLogic(Configuration.NinjaAutoSmite);
        }
        
        private bool SeitonReady()
        {
            if (CombatModule.ActionReady(Service.Action_SeitonTenchu))
            {
                return true;
            }
            foreach (var cc in Service.ClientState.LocalPlayer.StatusList)
            {
                if (cc.StatusId.Equals(Service.Buff_ReuseSeitonTenchu))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
