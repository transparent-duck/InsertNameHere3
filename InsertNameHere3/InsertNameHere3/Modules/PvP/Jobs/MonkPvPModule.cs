using InsertNameHere3;
using InsertNameHere3.Modules;
using System;

namespace InsertNameHere3.Modules.PvP.Jobs
{
    public class MonkPvPModule : BasePvPJobModule
    {
        public override uint JobId => Service.JobMonk;

        public MonkPvPModule(IPvPCombatModule combatModule, Configuration configuration, ActionTracker actionTracker) 
            : base(combatModule, configuration) { }

        public override void ExecuteJobSpecificLogic()
        {
            // Execute Earth's Reply logic
            ExecuteEarthsReplyLogic();
            
            // Execute smite logic
            ExecuteSmiteLogic(Configuration.MonkAutoSmite);
        }

        private void ExecuteEarthsReplyLogic()
        {
            // Check if the function is enabled
            if (!Configuration.MonkAutoEarthsReply)
                return;

            // Check if Earth's Reply action is ready
            if (!CombatModule.ActionReady(Service.Action_EarthsReply))
            {
                return;
            }
            

            var localPlayer = Service.ClientState.LocalPlayer;
            if (localPlayer == null)
                return;

            // Check for Buff_3171 (Diamond Body/Golden Essence) on local player
            foreach (var status in localPlayer.StatusList)
            {
                if (status.StatusId == Service.Buff_3171)
                {
                    // Calculate remaining time in seconds
                    float remainingTime = status.RemainingTime;
                    // Check if remaining time matches our configured threshold (within 0.1s tolerance)
                    if (Math.Abs(remainingTime - Configuration.MonkAutoEarthsReplyTiming) <= 0.1f)
                    {
                        // Use Earth's Reply on self
                        Cast(Service.Action_EarthsReply);
                        return;
                    }
                }
            }
        }
    }
}
