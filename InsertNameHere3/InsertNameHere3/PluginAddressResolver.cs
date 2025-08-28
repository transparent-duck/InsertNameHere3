using Dalamud.Game;
using System;
using System.Runtime.InteropServices;

namespace InsertNameHere3
{
    internal class PluginAddressResolver : BaseAddressResolver
    {
        internal delegate int CanAttackDelegate(int arg, IntPtr objectAddress);

        internal CanAttackDelegate CanAttack;

        internal IntPtr TargetPtr;
        internal IntPtr TargetIdPtr;
        internal IntPtr SpeedBasePtr;
        internal IntPtr ControlData;

        protected unsafe override void Setup64Bit(ISigScanner scanner)
        {
            Service.Log.Debug("----------------- inited! -----------------");
            CanAttack = Marshal.GetDelegateForFunctionPointer<CanAttackDelegate>(scanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 8B F9 E8 ?? ?? ?? ?? 4C 8B C3"));
            TargetPtr = scanner.GetStaticAddressFromSig("75 17 48 83 3D ?? ?? ?? ?? ??", 0) + 1;
            TargetIdPtr = scanner.GetStaticAddressFromSig("F3 0F 11 05 ?? ?? ?? ?? EB 27", 0) + 4;
            SpeedBasePtr = scanner.GetStaticAddressFromSig("E8 ?? ?? ?? ?? 48 ?? ?? 74 ?? 83 ?? ?? 75 ?? 0F ?? ?? ?? 66");
            Service.Log.Debug(((int*)SpeedBasePtr)->ToString());
            SpeedBasePtr = SpeedBasePtr + 4 + Marshal.ReadInt32(SpeedBasePtr + 4) + 4;
            Service.Log.Debug(((int*)SpeedBasePtr)->ToString());
            ControlData = scanner.GetStaticAddressFromSig("E8 ?? ?? ?? ?? 48 ?? ?? 74 ?? 83 ?? ?? 75 ?? 0F ?? ?? ?? 66");
            scanner.ScanText("48 89 5c 24 ?? 48 89 74 24 ?? 57 41 ?? 41 ?? 48 ?? ?? ?? 48 ?? ?? ?? ?? ?? ?? 48 ?? ?? 48 ?? ?? ?? ?? ?? ?? 49");
        }
    }
}
