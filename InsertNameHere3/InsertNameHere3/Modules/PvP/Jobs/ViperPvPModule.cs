using InsertNameHere3;
using InsertNameHere3.Modules;
using System;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace InsertNameHere3.Modules.PvP.Jobs
{
    public class ViperPvPModule : BasePvPJobModule
    {
        public override uint JobId => Service.JobViper;
        
        private DateTime _lastSerpentsTailAttempt = DateTime.MinValue;
        private uint _lastSerpentsTailAdjustedId = Service.Action_SerpentsTail;

        public ViperPvPModule(IPvPCombatModule combatModule, Configuration configuration, ActionTracker actionTracker) 
            : base(combatModule, configuration) { }

        public override void ExecuteJobSpecificLogic()
        {
            // Execute Serpent's Tail logic
            ExecuteSerpentsTailLogic();
            
            // Execute smite logic
            ExecuteSmiteLogic(Configuration.ViperAutoSmite);
        }

        private unsafe void ExecuteSerpentsTailLogic()
        {
            // Check if the function is enabled
            if (!Configuration.ViperAutoSerpentsTail)
            {
                return;
            }

            // Check if previous cast was successful by comparing adjusted action IDs
            var currentAdjustedId = ActionManager.Instance()->GetAdjustedActionId(Service.Action_SerpentsTail);
            if (_lastSerpentsTailAdjustedId != currentAdjustedId)
            {
                // Previous cast was successful (adjusted ID changed), update timeout
                _lastSerpentsTailAttempt = DateTime.Now;
                _lastSerpentsTailAdjustedId = currentAdjustedId; // Reset for next attempt
            }
            
            // Service.Log.Debug("Current Adjusted ID: " + currentAdjustedId);
            // Service.Log.Debug("Last Serpent's Tail Adjusted ID: " + _lastSerpentsTailAdjustedId);
            // Service.Log.Debug("Time since last Serpent's Tail attempt: " + (DateTime.Now - _lastSerpentsTailAttempt).TotalSeconds + " seconds");

            // Check if we're still in cooldown from last successful attempt (1 second timeout)
            if (DateTime.Now - _lastSerpentsTailAttempt > TimeSpan.FromSeconds(1))
            {
                return;
            }

            // Check if Serpent's Tail action is ready
            if (!CombatModule.ActionReady(Service.Action_SerpentsTail))
            {
                return;
            }

            // Get the local player
            var localPlayer = Service.ClientState.LocalPlayer;
            if (localPlayer == null)
                return;

            // Check if player has an enemy target
            var target = Service.TargetManager.Target;
            if (target == null)
            {
                return;
            }

            // Check if target is a valid battle character
            if (target is not Dalamud.Game.ClientState.Objects.Types.IBattleChara battleTarget)
            {
                return;
            }

            // Check if we can attack the target using CombatModule
            if (!CombatModule.CanAttack(battleTarget))
            {
                return;
            }


            // Check if target is in range for Serpent's Tail
            // if (!CombatModule.Available_Range(Service.Action_SerpentsTail, battleTarget))
            //     return;

            // Store the current adjusted action ID before casting to check in next frame
            // _lastSerpentsTailAdjustedId = ActionManager.Instance()->GetAdjustedActionId(Service.Action_SerpentsTail);
            
            // Cast Serpent's Tail on the target
            Cast(Service.Action_SerpentsTail, target.GameObjectId);
        }
    }
}
