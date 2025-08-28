using System;
using System.Linq;
using Dalamud.Hooking;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using Dalamud.Game.Network;
using System.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game;
using InsertNameHere3.Modules.PvP;

namespace InsertNameHere3.Modules.Debug
{
    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    public struct PlayerMoveControllerFlyInput
    {
        [FieldOffset(0x0)] public float Forward;
        [FieldOffset(0x4)] public float Left;
        [FieldOffset(0x8)] public float Up;
        [FieldOffset(0xC)] public float Turn;
        [FieldOffset(0x10)] public float u10;
        [FieldOffset(0x14)] public byte DirMode;
        [FieldOffset(0x15)] public byte HaveBackwardOrStrafe;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x140)]
    public struct MoveControllerSubMemberForMine
    {
        [FieldOffset(0x94)] public byte Spinning;
    }

    public class DebugModule
    {
        private readonly InsertNameHere3.Configuration _configuration;
        private readonly PvPCombatModule? _combatModule;
        private const double MovementLogIntervalSeconds = 0.5; // Log movement every 0.5 seconds

        // Movement tracking variables
        private Vector2 _lastMovementInput = Vector2.Zero;
        private DateTime _lastMovementLogTime = DateTime.MinValue;

        // Hook delegates for movement tracking
        private unsafe delegate void RmiWalkDelegate(MoveControllerSubMemberForMine* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk);
        private Hook<RmiWalkDelegate>? _rmiWalkHook;

        private unsafe delegate void RmiFlyDelegate(void* self, PlayerMoveControllerFlyInput* result);
        private Hook<RmiFlyDelegate>? _rmiFlyHook;

        // input source flags: 1 = kb/mouse, 2 = gamepad
        private unsafe delegate byte MoveControlIsInputActiveDelegate(void* self, byte inputSourceFlags);
        private Hook<MoveControlIsInputActiveDelegate>? _mcIsInputActiveHook;

        // Hook for ProcessZonePacketUp to intercept outgoing network packets
        private delegate byte ProcessZonePacketUpDelegate(IntPtr a1, IntPtr dataPtr, IntPtr a3, byte a4);
        private Hook<ProcessZonePacketUpDelegate>? _processZonePacketUpHook;

        // Hook for condition flags to override jumping condition
        private unsafe delegate bool GetConditionDelegate(void* self, ConditionFlag flag);
        private Hook<GetConditionDelegate>? _getConditionHook;

        public DebugModule(InsertNameHere3.Configuration configuration, PvPCombatModule? combatModule = null)
        {
            _configuration = configuration;
            _combatModule = combatModule;
        }

        public void Initialize()
        {
            Service.Log.Info("[DebugModule] Debug module initialized");
            InitializeHooks();

        }

        public void Update(Dalamud.Plugin.Services.IFramework framework)
        {
            ExecuteFrameDebug();
        }

        /// <summary>
        /// Initialize the hooks for debugging purposes
        /// </summary>
        private void InitializeHooks()
        {
            try
            {
                InitializeMovementHooks();
                Service.Log.Info("[DebugModule] Hooks initialization completed");
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[DebugModule] Error initializing hooks: {ex}");
            }
        }

        private unsafe void InitializeMovementHooks()
        {
            try
            {
                // Hook the walk movement function
                var rmiWalkAddress = Service.Scanner.ScanText("E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D");
                _rmiWalkHook = Service.GameInteropProvider.HookFromAddress<RmiWalkDelegate>(rmiWalkAddress, RmiWalkDetour);
                _rmiWalkHook?.Enable();
                Service.Log.Info($"[DebugModule] Walk movement hook enabled at 0x{rmiWalkAddress.ToInt64():X}");

                // Hook the fly movement function
                var rmiFlyAddress = Service.Scanner.ScanText("E8 ?? ?? ?? ?? 0F B6 0D ?? ?? ?? ?? B8");
                _rmiFlyHook = Service.GameInteropProvider.HookFromAddress<RmiFlyDelegate>(rmiFlyAddress, RmiFlyDetour);
                _rmiFlyHook?.Enable();
                Service.Log.Info($"[DebugModule] Fly movement hook enabled at 0x{rmiFlyAddress.ToInt64():X}");

                // Hook the input active check function
                var mcIsInputActiveAddress = Service.Scanner.ScanText("E8 ?? ?? ?? ?? 84 C0 74 09 84 DB 74 1A");
                _mcIsInputActiveHook = Service.GameInteropProvider.HookFromAddress<MoveControlIsInputActiveDelegate>(mcIsInputActiveAddress, MoveControlIsInputActiveDetour);
                _mcIsInputActiveHook?.Enable();
                Service.Log.Info($"[DebugModule] Input active check hook enabled at 0x{mcIsInputActiveAddress.ToInt64():X}");

                // Moved ProcessZonePacketUp handling to MovementModule

                // Hook the condition flag getter to override jumping condition
                try
                {
                    var conditionAddress = Service.Scanner.ScanText("E8 ?? ?? ?? ?? 84 C0 74 ?? 80 BB ?? ?? ?? ?? ??");
                    _getConditionHook = Service.GameInteropProvider.HookFromAddress<GetConditionDelegate>(conditionAddress, GetConditionDetour);
                    _getConditionHook?.Enable();
                    Service.Log.Info($"[DebugModule] Condition flag hook enabled at 0x{conditionAddress.ToInt64():X}");
                }
                catch (Exception ex)
                {
                    Service.Log.Warning($"[DebugModule] Could not hook condition flags, using alternative method: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[DebugModule] Error initializing movement hooks: {ex}");
            }
        }

        private unsafe void RmiWalkDetour(MoveControllerSubMemberForMine* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk)
        {
            // Call original function first
            _rmiWalkHook?.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk);

            // Log movement input if debug is enabled
            if (_configuration.FrameDebug)
            {
                var currentInput = new Vector2(*sumLeft, *sumForward);
                var now = DateTime.Now;

                // Log movement changes or periodically if moving
                LogMovementInput("Walk", currentInput, *sumTurnLeft, self->Spinning, bAdditiveUnk);
                _lastMovementInput = currentInput;
                _lastMovementLogTime = now;
            }
        }

        private unsafe void RmiFlyDetour(void* self, PlayerMoveControllerFlyInput* result)
        {
            // Call original function first
            _rmiFlyHook?.Original(self, result);

            // Log fly movement input if debug is enabled
            if (_configuration.FrameDebug)
            {
                var currentInput = new Vector2(result->Left, result->Forward);
                var now = DateTime.Now;

                // Log movement changes or periodically if moving
                LogFlyMovementInput(result);
                _lastMovementInput = currentInput;
                _lastMovementLogTime = now;
            }
        }

        private unsafe byte MoveControlIsInputActiveDetour(void* self, byte inputSourceFlags)
        {
            // Call original function
            var result = _mcIsInputActiveHook?.Original(self, inputSourceFlags) ?? 0;

            // Log input active status if debug is enabled
            if (_configuration.FrameDebug)
            {
                Service.Log.Debug($"[DebugModule] Input Active Check - SourceFlags: {inputSourceFlags}, Result: {result}");
            }

            return result;
        }

        private unsafe bool GetConditionDetour(void* self, ConditionFlag flag)
        {
            // Override jumping condition to always return false
            // if (flag == ConditionFlag.Jumping)
            // {
            //     Service.Log.Debug("[DebugModule] Overriding ConditionFlag.Jumping to false");
            //     return false;
            // }

            // Call original function for all other conditions
            return _getConditionHook?.Original(self, flag) ?? false;
        }



        private static bool IsFiniteFloat(float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }

        private static bool IsPlausiblePosition(Vector3 v)
        {
            if (!(IsFiniteFloat(v.X) && IsFiniteFloat(v.Y) && IsFiniteFloat(v.Z)))
                return false;

            if (Math.Abs(v.X) >= 100000 || Math.Abs(v.Y) >= 100000 || Math.Abs(v.Z) >= 100000)
                return false;

            var player = Service.ClientState.LocalPlayer;
            if (player == null)
                return true; // If we can't read player pos, fall back to base plausibility only

            var p = player.Position;
            var dx = v.X - p.X;
            var dy = v.Y - p.Y;
            var dz = v.Z - p.Z;
            var distSq = dx * dx + dy * dy + dz * dz;
            return distSq < (50f * 50f);
        }

        private static IntPtr Add(IntPtr ptr, int offset)
        {
            return new IntPtr(ptr.ToInt64() + offset);
        }

        private static bool TryReadVector3(IntPtr basePtr, int offset, out Vector3 vec)
        {
            try
            {
                var bytes = new byte[12];
                Marshal.Copy(Add(basePtr, offset), bytes, 0, 12);
                vec = new Vector3(BitConverter.ToSingle(bytes, 0), BitConverter.ToSingle(bytes, 4), BitConverter.ToSingle(bytes, 8));
                return true;
            }
            catch
            {
                vec = default;
                return false;
            }
        }

        private static void TryWriteFloat(IntPtr basePtr, int offset, float value)
        {
            try
            {
                var bytes = BitConverter.GetBytes(value);
                Marshal.Copy(bytes, 0, Add(basePtr, offset), 4);
            }
            catch
            {
                // ignore write failures
            }
        }

        // moved to MovementModule

        // network debug printing removed
        
        private bool IsValidPosition(float x, float y, float z)
        {
            // Basic validation for reasonable position values in FFXIV
            // Typically positions are within reasonable world bounds
            return !float.IsNaN(x) && !float.IsNaN(y) && !float.IsNaN(z) &&
                   !float.IsInfinity(x) && !float.IsInfinity(y) && !float.IsInfinity(z) &&
                   Math.Abs(x) < 10000 && Math.Abs(y) < 10000 && Math.Abs(z) < 10000 &&
                   x != 0 || y != 0 || z != 0; // Not all zeros
        }

        private void LogMovementInput(string movementType, Vector2 input, float turn, byte spinning, byte additiveUnk)
        {
            return;
            var magnitude = input.Length();
            var angle = magnitude > 0 ? Math.Atan2(input.Y, input.X) * 180.0 / Math.PI : 0;
            
            Service.Log.Debug($"[DebugModule] {movementType} Movement - " +
                             $"Input: ({input.X:F2}, {input.Y:F2}), " +
                             $"Magnitude: {magnitude:F2}, " +
                             $"Angle: {angle:F1}°, " +
                             $"Turn: {turn:F2}, " +
                             $"Spinning: {spinning}, " +
                             $"AdditiveUnk: {additiveUnk}");
        }

        private unsafe void LogFlyMovementInput(PlayerMoveControllerFlyInput* input)
        {
            var horizontal = new Vector2(input->Left, input->Forward);
            var magnitude = horizontal.Length();
            var angle = magnitude > 0 ? Math.Atan2(input->Forward, input->Left) * 180.0 / Math.PI : 0;
            
            Service.Log.Debug($"[DebugModule] Fly Movement - " +
                             $"Horizontal: ({input->Left:F2}, {input->Forward:F2}), " +
                             $"Up: {input->Up:F2}, " +
                             $"Turn: {input->Turn:F2}, " +
                             $"Magnitude: {magnitude:F2}, " +
                             $"Angle: {angle:F1}°, " +
                             $"DirMode: {input->DirMode}");
        }

        /// <summary>
        /// Handles the /cc debug command
        /// </summary>
        public void HandleDebugCommand(string[] args)
        {
            try
            {
                Service.Log.Debug("[DebugModule] Debug command executed");
                var target = Service.TargetManager.Target;
                if (target is IBattleChara battleChara)
                {
                    for (int i = 0; i < battleChara.StatusList.Length; i++)
                    {
                        var status = battleChara.StatusList[i];
                        if (status.StatusId != 0)
                        {
                            var remainingTime = status.RemainingTime;
                            var stackCount = status.Param;
                            Service.Log.Debug($"[DebugModule] Buff {i}: NAME={status.GameData.Value.Name} ID={status.StatusId}, Remaining={remainingTime:F1}s, Stacks={stackCount}, Source={status.SourceId}");
                        }
                    }
                }
                else
                {
                    Service.Log.Debug($"[DebugModule] Target is not a BattleChara, cannot read buffs");
                }
                // utils.LuminaDebug.RunAllDebugMethods();
                Service.Log.Debug("[DebugModule] Debug command completed");
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[DebugModule] Error in HandleDebugCommand: {ex}");
            }
        }
        
        /// <summary>
        /// Framework function that executes debug logic every frame (with throttling)
        /// </summary>
        private unsafe void ExecuteFrameDebug()
        {
            if (!_configuration.FrameDebug)
            {
                return;
            }

            try
            {
                // // position keep 1 digit after decimal point
                // // if digit is 0, also display 0
                // var presentingPosition = Service.ClientState.LocalPlayer?.Position ?? Vector3.Zero;
                // presentingPosition.X = (float)Math.Round(presentingPosition.X, 1);
                // presentingPosition.Y = (float)Math.Round(presentingPosition.Y, 1);
                // presentingPosition.Z = (float)Math.Round(presentingPosition.Z, 1);
                // Service.Log.Debug($"[DebugModule] Player Position: ({presentingPosition.X:F1}, {presentingPosition.Y:F1}, {presentingPosition.Z:F1})");

                // uint BOMBARDMENT_SKILL_2 = 41626;
                // var actionId = BOMBARDMENT_SKILL_2;
                // var canUseBombardment = _combatModule.ActionReady(BOMBARDMENT_SKILL_2);            
                //
                
                printCooldownInfo(Service.Action_HarmonicArrow);

            }
            catch (Exception ex)
            {
                Service.Log.Error($"[DebugModule] Error in ExecuteFrameDebug: {ex}");
            }
        }

        /// <summary>
        /// Alternative method to override jumping condition using reflection/unsafe access
        /// </summary>
        private unsafe void OverrideJumpingCondition()
        {
            try
            {
                // This is a fallback method if the hook doesn't work
                // We can try to directly modify the condition service's internal state
                if (Service.Condition[ConditionFlag.Jumping])
                {
                    Service.Log.Debug("[DebugModule] Detected jumping condition, attempting to override");
                    // Note: This approach may require additional implementation depending on Dalamud's internal structure
                }
            }
            catch (Exception ex)
            {
                Service.Log.Debug($"[DebugModule] Alternative jumping override method failed: {ex.Message}");
            }
        }
        
        private unsafe void printCooldownInfo(uint actionId)
        {
            Service.Log.Debug("[DebugModule] ActionId: " + actionId);
            var directReady = ActionManager.Instance()->GetActionStatus(ActionType.Action, actionId)
                .Equals(Service.ActionStatus_Ready);
            Service.Log.Debug($"[DebugModule] {actionId} directReady: {directReady}");
            
            var currentCharge = ActionManager.Instance()->GetCurrentCharges(actionId);
            Service.Log.Debug($"[DebugModule] {actionId} currentCharge: {currentCharge}");
            
            if (Service.Actions_checkAdjustedId.ContainsKey(actionId))
            {
                var baseId = Service.Actions_checkAdjustedId[actionId];
                var adjustedStatus = ActionManager.Instance()->GetAdjustedActionId(baseId) == actionId &&
                       ActionManager.Instance()->IsActionOffCooldown(ActionType.Action, actionId);
                Service.Log.Debug($"[DebugModule] {actionId} adjustedStatus: {adjustedStatus}");
            }
            
            if (Service.Actions_checkIfAdjusted.ContainsKey(actionId))
            {
                var currentAdjustedId = ActionManager.Instance()->GetAdjustedActionId(actionId);
                Service.Log.Debug($"[DebugModule] {actionId} currentAdjustedId: {currentAdjustedId}");
                if (actionId == currentAdjustedId)
                {
                    Service.Log.Debug($"[DebugModule] {actionId} is not adjusted, skipping adjusted ID check");
                    return;
                }
                var acceptedAdjustedIds = Service.Actions_checkIfAdjusted[actionId];
                if (acceptedAdjustedIds.Length != 0 && !acceptedAdjustedIds.Contains(currentAdjustedId))
                {
                    Service.Log.Debug($"[DebugModule] {actionId} adjusted ID {currentAdjustedId} not in accepted list, skipping adjusted ID check");
                    return;
                } 
            }
            
            var maxCharge = ActionManager.Instance()->IsActionOffCooldown(ActionType.Action,actionId);
            Service.Log.Debug($"[DebugModule] {actionId} offCooldown: {maxCharge}");
        }

        public void Dispose()
        {
            try
            {
                Service.Log.Info("[DebugModule] Disposing debug module hooks");
                
                // Unsubscribe from network messages
                // try { Service.GameNetwork.NetworkMessage -= OnNetworkMessage; } catch { }
                
                // Dispose movement hooks
                _rmiWalkHook?.Disable();
                _rmiWalkHook?.Dispose();
                _rmiFlyHook?.Disable();
                _rmiFlyHook?.Dispose();
                _mcIsInputActiveHook?.Disable();
                _mcIsInputActiveHook?.Dispose();
                
                // Dispose ProcessZonePacketUp hook
                _processZonePacketUpHook?.Disable();
                _processZonePacketUpHook?.Dispose();
                
                // Dispose condition hook
                _getConditionHook?.Disable();
                _getConditionHook?.Dispose();
                
                Service.Log.Info("[DebugModule] Debug module disposed successfully");
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[DebugModule] Error during disposal: {ex}");
            }
        }

        /// <summary>
        /// Check if the player is falling or in the air.
        /// Modified to force the player to be grounded by setting the memory value to 0.
        /// </summary>
        /// <returns>Always returns false to indicate player is grounded</returns>
        public unsafe static bool IsPlayerFalling()
        {
            var p = Service.ClientState.LocalPlayer;
            if(p == null)
                return false; // Changed from true to false - player is grounded when null
            
            var isJumping = *(byte*)(p.Address + 496 + 208); // This will now always be 0
            Service.Log.Debug($"[DebugModule] IsPlayerFalling: Jumping state set to {isJumping}");
            // Force the jumping state to 0 (grounded) by writing to memory
            // 0 if grounded
            // 1 = "jumpsquat"
            // 3 = going up
            // 4 = stopped
            // 5 = going down
            // *(byte*)(p.Address + 496 + 208) = 0; // Set jumping state to 0 (grounded)

            
            // The condition flag will now always be false due to our hook
            var conditionalFlagJumping = Service.Condition[ConditionFlag.Jumping];
            Service.Log.Debug($"[DebugModule] IsPlayerFalling: ConditionFlag.Jumping is {conditionalFlagJumping} (should always be false)");
            
            // 1 iff dismounting and haven't hit the ground yet
            var isAirDismount = **(byte**)(p.Address + 496 + 904) == 1;
            
            return isJumping > 0 || isAirDismount;
        }

        /// <summary>
        /// Uses reflection to get the ProcessZonePacketUp address from GameNetwork's internal address resolver
        /// </summary>
        /// <returns>The ProcessZonePacketUp address, or IntPtr.Zero if reflection fails</returns>
        private IntPtr GetProcessZonePacketUpAddressFromGameNetwork()
        {
            try
            {
                Service.Log.Debug("[DebugModule] Attempting to get ProcessZonePacketUp address via reflection");
                
                // Get the GameNetwork instance type (since we can't use typeof on internal classes)
                var gameNetworkInstance = Service.GameNetwork;
                var gameNetworkType = gameNetworkInstance.GetType();
                
                Service.Log.Debug($"[DebugModule] GameNetwork type: {gameNetworkType.FullName}");
                
                // The Service.GameNetwork returns a GameNetworkPluginScoped wrapper
                // We need to get the actual GameNetwork instance from the gameNetworkService field
                var gameNetworkServiceField = gameNetworkType.GetField("gameNetworkService", BindingFlags.NonPublic | BindingFlags.Instance);
                if (gameNetworkServiceField == null)
                {
                    Service.Log.Warning("[DebugModule] Could not find 'gameNetworkService' field in GameNetworkPluginScoped");
                    return IntPtr.Zero;
                }
                
                var actualGameNetwork = gameNetworkServiceField.GetValue(gameNetworkInstance);
                if (actualGameNetwork == null)
                {
                    Service.Log.Warning("[DebugModule] GameNetworkService instance is null");
                    return IntPtr.Zero;
                }
                
                Service.Log.Debug($"[DebugModule] Actual GameNetwork type: {actualGameNetwork.GetType().FullName}");
                
                // Now get the address resolver from the actual GameNetwork instance
                var actualGameNetworkType = actualGameNetwork.GetType();
                
                // Try multiple possible field names for the address resolver
                string[] possibleFieldNames = { "address", "addresses", "addressResolver", "Address", "Addresses", "AddressResolver" };
                object? addressResolver = null;
                
                foreach (var fieldName in possibleFieldNames)
                {
                    var addressField = actualGameNetworkType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (addressField != null)
                    {
                        Service.Log.Debug($"[DebugModule] Found address field: {fieldName}");
                        addressResolver = addressField.GetValue(actualGameNetwork);
                        break;
                    }
                }
                
                if (addressResolver == null)
                {
                    Service.Log.Warning("[DebugModule] Could not find address resolver field in actual GameNetwork");
                    
                    // Debug: List all fields in the actual GameNetwork to see what's available
                    var actualFields = actualGameNetworkType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    Service.Log.Debug($"[DebugModule] Available fields in actual GameNetwork:");
                    foreach (var field in actualFields)
                    {
                        Service.Log.Debug($"[DebugModule] - Field: {field.Name} (Type: {field.FieldType.Name})");
                    }
                    
                    return IntPtr.Zero;
                }
                
                Service.Log.Debug($"[DebugModule] Address resolver type: {addressResolver.GetType().FullName}");
                
                // Get the ProcessZonePacketUp property from the address resolver
                var addressResolverType = addressResolver.GetType();
                var processZonePacketUpProperty = addressResolverType.GetProperty("ProcessZonePacketUp", BindingFlags.Public | BindingFlags.Instance);
                
                if (processZonePacketUpProperty == null)
                {
                    // Debug: List all properties in the address resolver
                    var resolverProperties = addressResolverType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    Service.Log.Debug($"[DebugModule] Available properties in address resolver:");
                    foreach (var prop in resolverProperties)
                    {
                        Service.Log.Debug($"[DebugModule] - Property: {prop.Name} (Type: {prop.PropertyType.Name})");
                    }
                    
                    Service.Log.Warning("[DebugModule] Could not find 'ProcessZonePacketUp' property in address resolver");
                    return IntPtr.Zero;
                }
                
                // Get the address value
                var addressValue = processZonePacketUpProperty.GetValue(addressResolver);
                if (addressValue is IntPtr address && address != IntPtr.Zero)
                {
                    Service.Log.Debug($"[DebugModule] Successfully retrieved ProcessZonePacketUp address: 0x{address.ToInt64():X}");
                    return address;
                }
                else
                {
                    Service.Log.Warning($"[DebugModule] ProcessZonePacketUp address is invalid: {addressValue}");
                    return IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                Service.Log.Warning($"[DebugModule] Reflection failed to get ProcessZonePacketUp address: {ex.Message}");
                return IntPtr.Zero;
            }
        }
    }
}
