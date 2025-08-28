using Dalamud.Plugin.Services;
using System;
using System.Linq;
using System.Net.Security;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System.Numerics;
using InsertNameHere3;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using InsertNameHere3.Modules;

namespace InsertNameHere3.Modules.PvP
{
    public unsafe class PvPSelfProtect : IPvPGeneralAction
    {
        private readonly IPvPCombatModule _combatModule;
        private readonly Configuration _configuration;

        // Auto-protection fields
        private bool _autoBubbleTriggered;
        private DateTime _lastTryingBubbleTime = DateTime.MinValue;
        private bool _autoPurifyTriggered;

        // Action hook for detecting actions used on the player
        private Hook<ActionEffectHandler.Delegates.Receive>? _onActionUsedHook;

        // UseAction hook for blocking actions during bubble timing
        private Hook<ActionManager.Delegates.UseAction>? _useActionHook;

        public PvPSelfProtect(IPvPCombatModule combatModule, Configuration configuration)
        {
            _combatModule = combatModule;
            _configuration = configuration;
        }

        public void Initialize()
        {
            // Initialize the action hook similar to Plugin.cs
            _onActionUsedHook = Service.GameInteropProvider.HookFromAddress<ActionEffectHandler.Delegates.Receive>(
                ActionEffectHandler.MemberFunctionPointers.Receive, OnActionHappens);
            _onActionUsedHook.Enable();

            // Initialize the useAction hook for blocking actions during bubble timing
            _useActionHook = Service.GameInteropProvider.HookFromAddress<ActionManager.Delegates.UseAction>(
                ActionManager.MemberFunctionPointers.UseAction, UseActionDetour);
            _useActionHook.Enable();
        }

        public void Update(IFramework framework)
        {
            HandleAction();
        }

        public void Dispose()
        {
            _onActionUsedHook?.Dispose();
            _useActionHook?.Dispose();
        }

        private void OnActionHappens(uint actorId, Character* casterPtr, Vector3* targetPos,
            ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects,
            GameObjectId* targetEntityIds)
        {
            // Call the original function first
            _onActionUsedHook?.Original(actorId, casterPtr, targetPos, header, effects, targetEntityIds);

            // Check if the action targets the local player
            if (header != null && Service.Actions_NeedBubble.Contains(header->ActionId) &&
                Service.ClientState.LocalPlayer != null &&
                header->AnimationTargetId.Id == Service.ClientState.LocalPlayer.GameObjectId)
            {
                switch (header->ActionId)
                {
                    case Service.Action_Blota:
                        if (_configuration.AutoPurifyBlotaReaction)
                            _autoPurifyTriggered = true;
                        break;
                    case Service.Action_WindsReply:
                        if (_configuration.AutoPurifyWindsReplyReaction)
                            _autoPurifyTriggered = true;
                        break;
                    case Service.Action_MarksmansSpite:
                        _autoBubbleTriggered = true;
                        break;
                }
            }
        }

        private unsafe bool UseActionDetour(ActionManager* thisPtr, ActionType actionType, uint actionId,
            ulong targetId, uint extraParam, ActionManager.UseActionMode mode, uint comboRouteId,
            bool* outOptAreaTargeted)
        {
            // Block actions within 2 seconds of trying to use bubble (except bubble itself)
            if (_configuration.AutoBubbleBlock && DateTime.Now - _lastTryingBubbleTime < TimeSpan.FromSeconds(2) &&
                !actionId.Equals(Service.Action_Bubble))
            {
                return false;
            }

            // Call the original function
            var result = _useActionHook!.Original(thisPtr, actionType, actionId, targetId, extraParam, mode,
                comboRouteId, outOptAreaTargeted);
            return result;
        }

        public void HandleAction()
        {
            if (!_combatModule.IsPvPAndEnemiesNearBy || !_configuration.PvPAutoSkillsEnabled)
            {
                return;
            }

            HandleAutoBubble();
            HandleAutoHeal();
            HandleAutoPurify();
        }

        private void HandleAutoBubble()
        {
            if (!_configuration.AutoBubble || !_combatModule.ActionReady(Service.Action_Bubble) ||
                Service.ClientState.LocalPlayer?.IsDead == true)
            {
                _autoBubbleTriggered = false;
                return;
            }

            if (Service.ClientState.LocalPlayer.StatusList.Any(status => status.StatusId == Service.Buff_CanNotBubble))
            {
                _autoBubbleTriggered = false;
                _lastTryingBubbleTime = DateTime.MinValue;
                return;
            }

            if (_autoBubbleTriggered)
            {
                if (Service.ClientState.LocalPlayer.CurrentMount != null)
                {
                    ActionManager.Instance()->UseAction(ActionType.Mount, 0);
                }

                _combatModule.Cast(Service.Action_Bubble);
                _lastTryingBubbleTime = DateTime.Now;
            }
        }

        private void HandleAutoHeal()
        {
            if (!_configuration.AutoElixir || !_combatModule.ActionReady(Service.Action_StandardElixir))
            {
                return;
            }

            if (_configuration.DisableCureWhenSelfGuard)
            {
                foreach (var cc in Service.ClientState.LocalPlayer.StatusList)
                {
                    if (cc.StatusId.Equals(Service.Buff_Bubble))
                    {
                        return;
                    }
                }
            }

            if (Service.ClientState.LocalPlayer.CurrentHp >
                Service.ClientState.LocalPlayer.MaxHp * _configuration.AutoElixirPercentage / 100)
            {
                return;
            }

            _combatModule.Cast(Service.Action_StandardElixir);
        }

        private void HandleAutoPurify()
        {
            // if local player is null or dead already
            if (Service.ClientState.LocalPlayer == null || Service.ClientState.LocalPlayer.IsDead)
            {
                _autoPurifyTriggered = false;
                return;
            }

            if (!_configuration.AutoPurify || !_combatModule.ActionReady(Service.Action_Purify))
            {
                _autoPurifyTriggered = false;
                return;
            }

            if (_autoPurifyTriggered)
            {
                foreach (var cc in Service.ClientState.LocalPlayer.StatusList)
                {
                    if (cc.StatusId.Equals(Service.Buff_Bubble))
                    {
                        _autoPurifyTriggered = false;
                        return;
                    }
                }
                
                _combatModule.Cast(Service.Action_Purify);
            }

            foreach (var cc in Service.ClientState.LocalPlayer.StatusList)
            {
                if (_configuration.AutoPurifyHumanReaction.Contains(cc.StatusId))
                {
                    _autoPurifyTriggered = true;
                    return;
                }
            }
        }
    }
}