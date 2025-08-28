using InsertNameHere3;
using InsertNameHere3.utils;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
//using ECommons;
//using ECommons.Automation.NeoTaskManager;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using InsertNameHere3.Modules;
using Dalamud;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Text;
using ImGuiNET;
using InsertNameHere3.Windows;
using GameObject = Dalamud.Game.ClientState.Objects.Types.IGameObject;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using Lumina.Excel.Sheets;
using Action = System.Action;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows10.0")]

namespace InsertNameHere3
{
    public unsafe sealed class Plugin : IDalamudPlugin, IDisposable
    {
        public string Name => "InsertNameHere3";
        private const string CommandName = "/insertnamehere3";
        private IDalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("InsertNameHere3");
        private ConfigWindow ConfigWindow { get; init; }
        // private MainWindow MainWindow { get; init; }

        public ModuleManager ModuleManager { get; set; }

        private IFramework Framework { get; init; }
        private ITargetManager TargetManager { get; init; }
        internal PluginAddressResolver Address { get; set; }
        internal DateTime LastSelectTime { get; set; }
        public GameObject Target { get; set; }
        public float LastRunFasterMtp { get; set; }
        private static readonly ConcurrentDictionary<string, int> _playerPosOffsetCache = new();
        private Hook<ActionEffectHandler.Delegates.Receive>? _onActionUsedHook;
        private bool autoBubbleTriggered { get; set; }
        private bool autoPurifyTriggered { get; set; }
        private readonly IGameInteropProvider gameInterop;
        private Hook<StartCooldownDelegate>? startCooldownHook;

        private delegate void StartCooldownDelegate(ActionManager* thisPtr, ActionType actionType, uint actionId);

        private Hook<ActionManager.Delegates.CanUseActionOnTarget>? canUseActionOnTargetHook;
        private DateTime lastTryingBubbleTime = DateTime.MinValue;
        private int beginWildFireCombo = 0;
        public GameObject wildFireTarget { get; set; }

        private DateTime wildFireBeginTime = DateTime.MinValue;

        // Add ActionTracker
        public ActionTracker ActionTracker { get; private set; }
        
        // PvP Auto Skills Toggle
        private bool _lastKeyState = false;

        public unsafe Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager,
            IFramework framework, ITargetManager targetManager, IGameInteropProvider gameInterop)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.gameInterop = gameInterop;

            // Initialize Service first before using any of its properties
            pluginInterface.Create<Service>(Array.Empty<object>());
            Service.Address = new PluginAddressResolver();
            ((BaseAddressResolver)Service.Address).Setup(Service.Scanner);
            
            // Initialize JobHelper to load Lumina job data
            JobHelper.Initialize();
            
            Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(this.PluginInterface);
            Configuration.RunFasterMtp = 1;
            LastRunFasterMtp = 1;
            ConfigWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(ConfigWindow);
            this.CommandManager.AddHandler(CommandName,
                new CommandInfo(OnCommand) { HelpMessage = "Open config window." });
            autoBubbleTriggered = false;
            this.PluginInterface.UiBuilder.Draw += this.WindowSystem.Draw;
            this.PluginInterface.UiBuilder.Draw += this.DrawTargetIcon;
            this.PluginInterface.UiBuilder.Draw += this.DrawEnemyWatchers;
            this.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
            this.startCooldownHook = this.gameInterop.HookFromAddress<StartCooldownDelegate>(
                ActionManager.MemberFunctionPointers.StartCooldown, this.StartCooldownDetour);

            // Enable the hook
            this.startCooldownHook.Enable();
            this.canUseActionOnTargetHook =
                this.gameInterop.HookFromAddress<ActionManager.Delegates.CanUseActionOnTarget>(
                    ActionManager.MemberFunctionPointers.CanUseActionOnTarget, this.CanUseActionOnTargetDetour);
            this.canUseActionOnTargetHook.Enable();

            this.canUseActionOnTargetHook?.Dispose();
            this.LastSelectTime = DateTime.Now;
            this.TargetManager = targetManager;
            this.Framework = framework;

            ActionTracker = new ActionTracker();

            ModuleManager = new ModuleManager(Configuration, ActionTracker);

            this.Framework.Update += ModuleManager.Update;
            this.Framework.Update += (framework) => ActionTracker.Update();
            this.Framework.Update += CheckPvPToggleHotkey;

        }

        public void ToggleConfigUI()
        {
            ConfigWindow.IsOpen = true;
            Service.Log.Debug("ConfigWindow Toggle");
        }
        
        private void DrawTargetIcon()
        {
            try
            {
                ModuleManager?.TargetIcon?.Draw();
            }
            catch (Exception ex)
            {
                Service.Log.Error($"Error in DrawTargetIcon: {ex}");
            }
        }
        
        private void DrawEnemyWatchers()
        {
            try
            {
                ModuleManager?.EnemyWatcher?.Draw();
            }
            catch (Exception ex)
            {
                Service.Log.Error($"Error in DrawEnemyWatchers: {ex}");
            }
        }
        // public void ToggleMainUI() => MainWindow.IsOpen = true;
        
        
        private void CheckPvPToggleHotkey(IFramework framework)
        {
            // If disabled mode, ensure auto skills are always enabled
            if (Configuration.PvPToggleMode == PvPToggleMode.Disabled)
            {
                if (!Configuration.PvPAutoSkillsEnabled)
                {
                    Configuration.PvPAutoSkillsEnabled = true;
                }
                return;
            }
            
            // Skip if no key set
            if (Configuration.PvPAutoSkillsToggleKey == 0) return;
            
            var keyPressed = ECommons.GenericHelpers.IsKeyPressed((ECommons.Interop.LimitedKeys)Configuration.PvPAutoSkillsToggleKey);
            
            switch (Configuration.PvPToggleMode)
            {
                case PvPToggleMode.Toggle:
                    // Original toggle behavior - detect key press (transition from not pressed to pressed)
                    if (keyPressed && !_lastKeyState)
                    {
                        Configuration.PvPAutoSkillsEnabled = !Configuration.PvPAutoSkillsEnabled;
                        Service.Log.Information($"PvP Auto Skills: {(Configuration.PvPAutoSkillsEnabled ? "Enabled" : "Disabled")}");
                    }
                    break;
                    
                case PvPToggleMode.EnableOnPress:
                    // Enable while pressed, disable when released
                    if (keyPressed != Configuration.PvPAutoSkillsEnabled)
                    {
                        Configuration.PvPAutoSkillsEnabled = keyPressed;
                        if (keyPressed && !_lastKeyState) // Only log on initial press
                        {
                            Service.Log.Information("PvP Auto Skills: Enabled (while key pressed)");
                        }
                        else if (!keyPressed && _lastKeyState) // Only log on release
                        {
                            Service.Log.Information("PvP Auto Skills: Disabled (key released)");
                        }
                    }
                    break;
                    
                case PvPToggleMode.DisableOnPress:
                    // Disable while pressed, enable when released
                    if (keyPressed == Configuration.PvPAutoSkillsEnabled)
                    {
                        Configuration.PvPAutoSkillsEnabled = !keyPressed;
                        if (keyPressed && !_lastKeyState) // Only log on initial press
                        {
                            Service.Log.Information("PvP Auto Skills: Disabled (while key pressed)");
                        }
                        else if (!keyPressed && _lastKeyState) // Only log on release
                        {
                            Service.Log.Information("PvP Auto Skills: Enabled (key released)");
                        }
                    }
                    break;
            }
            
            _lastKeyState = keyPressed;
        }

        private void StartCooldownDetour(ActionManager* thisPtr, ActionType actionType, uint actionId)
        {
            Service.Log.Information($"Action {actionId} of type {actionType} is being put on cooldown");

            this.startCooldownHook!.Original(thisPtr, actionType, actionId);
        }

        // Add this detour method
        private unsafe bool CanUseActionOnTargetDetour(uint actionId,
            FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* target)
        {
            Service.Log.Debug($"Checking if action {actionId} can be used on target {target->GetGameObjectId()}");

            var result = this.canUseActionOnTargetHook!.Original(actionId, target);

            return result;
        }
        
        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            this.CommandManager.RemoveHandler(CommandName);
            _onActionUsedHook?.Dispose();
            this.startCooldownHook?.Dispose();
                        
            // Unsubscribe from framework update to prevent further module updates
            this.Framework.Update -= ModuleManager.Update;
            this.Framework.Update -= CheckPvPToggleHotkey;
            
            // Dispose ModuleManager to clean up hooks and prevent leaks
            ModuleManager?.Dispose();


            // Dispose ActionTracker and cleanup
            ActionTracker?.ClearPendingActions();
        }

        private unsafe void OnCommand(string command, string args)
        {
            var sumyu = args.Split(" ");
            // in response to the slash command, just display our main ui
            //MainWindow.IsOpen = true;
            if (sumyu[0] == "cZ")
            {
            }
            else if (sumyu[0] == "cX")
            {
            }
            else if (sumyu[0] == "cP")
            {
            }
            else if (sumyu[0] == "debug")
            {
                ModuleManager.Debug.HandleDebugCommand(sumyu);
            }
            else if (sumyu[0] == "cT")
            {
            }
            else
            {
                Service.Log.Debug("unknown sumyu: " + sumyu[0]);
                ConfigWindow.IsOpen = true;
            }
        }

    }
}
