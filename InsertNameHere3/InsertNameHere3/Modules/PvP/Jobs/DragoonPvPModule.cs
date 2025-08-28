using InsertNameHere3;
using InsertNameHere3.Modules;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Numerics;

namespace InsertNameHere3.Modules.PvP.Jobs
{
    public class DragoonPvPModule : BasePvPJobModule
    {
        public override uint JobId => Service.JobDragoon;
        
        private Hook<UseActionLocationDelegate>? _useActionLocationHook;
        
        private unsafe delegate bool UseActionLocationDelegate(ActionManager* self, ActionType actionType, uint actionID, ulong targetID, Vector3* targetPos, uint itemLocation);

        public DragoonPvPModule(IPvPCombatModule combatModule, Configuration configuration, ActionTracker actionTracker) 
            : base(combatModule, configuration) { }

        public unsafe override void Initialize()
        {
            base.Initialize();
            
            // Hook UseActionLocation to intercept Elusive Jump at execution time
            _useActionLocationHook = Service.GameInteropProvider.HookFromAddress<UseActionLocationDelegate>(
                ActionManager.MemberFunctionPointers.UseActionLocation, UseActionLocationDetour);
            _useActionLocationHook.Enable();
        }

        public override void ExecuteJobSpecificLogic()
        {
            // Execute smite logic
            ExecuteSmiteLogic(Configuration.DragoonAutoSmite);
        }

        private unsafe bool UseActionLocationDetour(ActionManager* self, ActionType actionType, uint actionID, ulong targetID, Vector3* targetPos, uint itemLocation)
        {
            // Only intercept when we're a Dragoon in PvP with forward jump enabled
            if (actionID == Service.Action_ElusiveJump &&
                CombatModule.IsPvPAndEnemiesNearBy &&
                Configuration.DragoonForwardJump)
            {
                // Get the local player
                var localPlayer = Service.ClientState.LocalPlayer;
                if (localPlayer != null)
                {
                    // Store the original rotation
                    var originalRotation = localPlayer.Rotation;
                    
                    // Rotate 180 degrees (π radians) to face the opposite direction
                    var newRotation = originalRotation + (float)Math.PI;
                    
                    // Normalize the rotation to stay within [0, 2π] range
                    if (newRotation > 2 * Math.PI)
                        newRotation -= 2 * (float)Math.PI;
                    
                    // Set the new rotation immediately before action execution
                    var gameObject = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)localPlayer.Address;
                    if (gameObject != null)
                    {
                        gameObject->Rotation = newRotation;
                        Service.Log.Debug($"DragoonForwardJump: Set rotation to {newRotation:F2} radians at UseActionLocation timing");
                    }
                }
            }

            // Call the original UseActionLocation
            return _useActionLocationHook!.Original(self, actionType, actionID, targetID, targetPos, itemLocation);
        }

        public override void Dispose()
        {
            _useActionLocationHook?.Dispose();
            base.Dispose();
        }
    }
}
