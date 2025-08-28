using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace InsertNameHere3.Modules
{
	public class ActionTracker : IDisposable
	{
		public class ActionRequest
		{
			public ActionType ActionType { get; set; }
			public uint ActionId { get; set; }
			public ulong TargetId { get; set; }
			public Vector3 TargetPos { get; set; }
			public uint SourceSequence { get; set; }
			public DateTime RequestTime { get; set; }
			public float InitialAnimationLock { get; set; }
			public bool IsCompleted { get; set; }
		}

		public event Action<ActionRequest>? ActionRequested;
		public event Action<ActionRequest>? ActionCompleted;

		private readonly Dictionary<uint, ActionRequest> _pendingActions = new();
		private Hook<UseActionLocationDelegate>? _useActionLocationHook;
		private Hook<ActionEffectHandler.Delegates.Receive>? _receiveActionEffectHook;
		private uint _lastSequence = 0;

		private unsafe delegate bool UseActionLocationDelegate(ActionManager* self, ActionType actionType, uint actionID, ulong targetID, Vector3* targetPos, uint itemLocation);

		public unsafe uint LastUsedActionSequence
		{
			get
			{
				var actionManager = ActionManager.Instance();
				return actionManager != null ? (uint)actionManager->LastUsedActionSequence : 0;
			}
		}

		public unsafe float AnimationLock
		{
			get
			{
				var actionManager = ActionManager.Instance();
				return actionManager != null ? actionManager->AnimationLock : 0f;
			}
		}

		public unsafe ActionTracker()
		{
			_useActionLocationHook = Service.GameInteropProvider.HookFromAddress<UseActionLocationDelegate>(
				ActionManager.MemberFunctionPointers.UseActionLocation, UseActionLocationDetour);
			_useActionLocationHook?.Enable();

			// Use the provided ActionEffectHandler delegate instead of manual signature scanning
			_receiveActionEffectHook = Service.GameInteropProvider.HookFromAddress<ActionEffectHandler.Delegates.Receive>(
				ActionEffectHandler.MemberFunctionPointers.Receive, ReceiveActionEffectDetour);
			_receiveActionEffectHook?.Enable();
			
			_lastSequence = LastUsedActionSequence;
		}

		private unsafe void ReceiveActionEffectDetour(uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects, GameObjectId* targetEntityIds)
		{
			_receiveActionEffectHook!.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);

			// Check if the caster is the local player
			if (casterPtr == null || casterEntityId != Service.ClientState.LocalPlayer?.GameObjectId)
			{
				return;
			}

			var sourceSequence = header->SourceSequence;
			Service.Log.Debug($"[ActionTracker] ReceiveActionEffect: ActionId={header->ActionId}, SourceSequence={sourceSequence}, CasterEntityId={casterEntityId}");
			
			if (sourceSequence > 0 && _pendingActions.TryGetValue(sourceSequence, out var request))
			{
				if (!request.IsCompleted)
				{
					request.IsCompleted = true;
					Service.Log.Debug($"[ActionTracker] Action #{sourceSequence} completed via ReceiveActionEffect: {request.ActionId}");
					ActionCompleted?.Invoke(request);
					_pendingActions.Remove(sourceSequence);
				}
			}
		}

		private unsafe bool UseActionLocationDetour(ActionManager* self, ActionType actionType, uint actionID, ulong targetID, Vector3* targetPos, uint itemLocation)
		{
			var prevSeq = LastUsedActionSequence;
			
			// Call original function
			bool ret = _useActionLocationHook!.Original(self, actionType, actionID, targetID, targetPos, itemLocation);
			
			var currSeq = LastUsedActionSequence;
			
			Service.Log.Debug($"[ActionTracker] UseActionLocationDetour called: ActionType={actionType}, ActionID={actionID}, TargetID={targetID:X},  ItemLocation={itemLocation}");
			Service.Log.Debug($"[ActionTracker] UseActionLocationDetour: Result={ret}, PreviousSeq={prevSeq}, CurrentSeq={currSeq}, AnimationLock={AnimationLock:f3}");
			
			// Track new action request - handle both sequence increment and same sequence cases
			if (ret && (currSeq != prevSeq || currSeq > 0))
			{
				// For actions that don't increment sequence, use a fallback sequence number
				var trackingSequence = currSeq != prevSeq ? currSeq : (uint)(DateTime.Now.Ticks & 0xFFFFFFFF);
				
				var request = new ActionRequest
				{
					ActionType = actionType,
					ActionId = actionID,
					TargetId = targetID,
					TargetPos = targetPos != null ? *targetPos : Vector3.Zero,
					SourceSequence = trackingSequence,
					RequestTime = DateTime.Now,
					InitialAnimationLock = AnimationLock,
					IsCompleted = false
				};

				_pendingActions[trackingSequence] = request;
				
				Service.Log.Debug($"[ActionTracker] Action #{trackingSequence} requested: {actionID} @ {targetID:X}, ALock={AnimationLock:f3}");
				
				ActionRequested?.Invoke(request);
			}

			return ret;
		}

		public void Update()
		{
			// This method is now primarily for cleanup and timeout handling
			var currentSeq = LastUsedActionSequence;
			var now = DateTime.Now;
			var completedActions = new List<uint>();

			foreach (var kvp in _pendingActions)
			{
				var sequence = kvp.Key;
				var request = kvp.Value;

				if (request.IsCompleted) continue;

				// Timeout-based completion as fallback (should rarely be needed now)
				if ((now - request.RequestTime).TotalSeconds > 2.0)
				{
					request.IsCompleted = true;
					completedActions.Add(sequence);
					
					Service.Log.Debug($"[ActionTracker] Action #{sequence} completed by timeout: {request.ActionId}");
					ActionCompleted?.Invoke(request);
				}
			}

			// Clean up
			foreach (var sequence in completedActions)
			{
				_pendingActions.Remove(sequence);
			}
		}

		public bool IsActionPending(uint actionId)
		{
			foreach (var request in _pendingActions.Values)
			{
				if (!request.IsCompleted && request.ActionId == actionId)
					return true;
			}
			return false;
		}

		public ActionRequest? GetPendingAction(uint actionId)
		{
			foreach (var request in _pendingActions.Values)
			{
				if (!request.IsCompleted && request.ActionId == actionId)
					return request;
			}
			return null;
		}

		public void ClearPendingActions()
		{
			_pendingActions.Clear();
		}

		public void Dispose()
		{
			_useActionLocationHook?.Dispose();
			_receiveActionEffectHook?.Dispose();
		}
	}
}


