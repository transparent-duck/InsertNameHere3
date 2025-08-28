using InsertNameHere3;
using System.Collections.Generic;
using System;
using System.Linq;
using InsertNameHere3.Modules;
using InsertNameHere3.Modules.PvP.Jobs;
using Dalamud.Game.ClientState.Objects.Types;

namespace InsertNameHere3.Modules.PvP.Jobs
{
    public class SamuraiPvPModule : BasePvPJobModule
    {
        public override uint JobId => Service.JobSamurai;

        public SamuraiPvPModule(IPvPCombatModule combatModule, Configuration configuration, ActionTracker actionTracker) 
            : base(combatModule, configuration) { }

        public override void ExecuteJobSpecificLogic()
        {
            // Execute Samurai-specific LB logic
            ExecuteZantetsukenLogic();
            
            // Execute smite logic
            ExecuteSmiteLogic(Configuration.SamuraiAutoSmite);
        }

        private void ExecuteZantetsukenLogic()
        {
            if (Service.ClientState.LocalPlayer.ClassJob.RowId != Service.JobSamurai ||!Configuration.SamuraiAutoLB || !CombatModule.ActionReady(Service.Action_Zantetsuken)) return;

            var distances = new Dictionary<(int, int), double>();
            int n = CombatModule.AllEnemyActors.Count;
            
            // Calculate distances between all enemies
            for (int i = 0; i < n; i++)
            {
                for (int j = i; j < n; j++)
                {
                    double distance = Cal2DDistance(CombatModule.AllEnemyActors[i].BattleChara.Position,
                        CombatModule.AllEnemyActors[j].BattleChara.Position);
                    distances[(i, j)] = distance;
                    distances[(j, i)] = distance;
                }
            }

            int maxCount = 0;
            IBattleChara? bestCenter = null;
            
            // Find the best center for AOE attack
            for (int i = 0; i < n; i++)
            {
                if (!CombatModule.AllEnemyActors[i].IsSelectableAsTarget)
                {
                    continue;
                }
                int count = 0;
                var currentEnemy = CombatModule.AllEnemyActors[i].BattleChara;
                bool isWeak = IsWeak(currentEnemy);
                
                // If SamuraiAutoLBAllowNonWeak is false, skip non-weak centers
                if (!Configuration.SamuraiAutoLBAllowNonWeak && !isWeak)
                {
                    continue;
                }

                if (!CombatModule.Available_Range(Service.Action_Zantetsuken, currentEnemy))
                {
                    continue;
                }
                
                // Count all weak enemies within 5m radius (including the center if weak)
                for (int j = 0; j < n; j++)
                {
                    double distance = distances[(i, j)];
                    if (distance < 5 && IsWeak(CombatModule.AllEnemyActors[j].BattleChara))
                    {
                        count++;
                    }
                }

                // Prefer weak centers when counts are equal
                if (isWeak && count == maxCount)
                {
                    bestCenter = currentEnemy;
                }

                if (count > maxCount)
                {
                    maxCount = count;
                    bestCenter = currentEnemy;
                }
            }

            if (maxCount >= Configuration.SamuraiAutoLBMinAmount && 
                bestCenter != null)
            {
                Cast(Service.Action_Zantetsuken, bestCenter.GameObjectId);
            }
        }

        private double Cal2DDistance(System.Numerics.Vector3 pos1, System.Numerics.Vector3 pos2)
        {
            return System.Math.Sqrt((pos1.X - pos2.X) * (pos1.X - pos2.X) + (pos1.Z - pos2.Z) * (pos1.Z - pos2.Z));
        }

        private bool IsWeak(IBattleChara chara)
        {
            bool weaked = false;
            foreach (var cc in chara.StatusList)
            {
                if (Service.Buffs_Hallow.Contains(cc.StatusId))
                {
                    return false;
                }

                if (cc.StatusId.Equals(Service.Buff_Kuzushi) &&
                    cc.SourceId == Service.ClientState.LocalPlayer?.GameObjectId)
                {
                    long shield = 0;
                    if (chara.ShieldPercentage > 0)
                    {
                        shield = chara.MaxHp * (chara.ShieldPercentage + 1) / 100;
                    }

                    if (shield + chara.CurrentHp <= chara.MaxHp)
                    {
                        weaked = true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return weaked;
        }
    }
}
