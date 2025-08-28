using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace InsertNameHere3.Modules
{
    public class SkillSequenceStateMachine : IDisposable
    {
        public delegate bool SkillCastDelegate(uint actionId, ulong targetId);
        
        public class SkillStep
        {
            public uint ActionId { get; set; }
            public ActionType ActionType { get; set; } = ActionType.Action;
            public string? LogMessage { get; set; }
            
            public SkillStep(uint actionId, ActionType actionType = ActionType.Action, string? logMessage = null)
            {
                ActionId = actionId;
                ActionType = actionType;
                LogMessage = logMessage;
            }
        }
        
        private readonly List<SkillStep> _skillSequence;
        private readonly SkillCastDelegate _castMethod;
        private readonly TimeSpan _timeout;
        private readonly ActionTracker _actionTracker;
        
        private int _currentStep;
        private IGameObject? _target;
        private DateTime _startTime = DateTime.MinValue;
        private uint _lastDetectedActionId = 0;
        
        public bool IsActive => _currentStep > 0 && _currentStep <= _skillSequence.Count;
        public IGameObject? CurrentTarget => _target;
        public int CurrentStepIndex => Math.Max(0, _currentStep - 1);
        public SkillStep? CurrentStep => IsActive ? _skillSequence[CurrentStepIndex] : null;
        
        public SkillSequenceStateMachine(List<SkillStep> skillSequence, SkillCastDelegate castMethod, TimeSpan timeout, ActionTracker actionTracker)
        {
            _skillSequence = skillSequence ?? throw new ArgumentNullException(nameof(skillSequence));
            _castMethod = castMethod ?? throw new ArgumentNullException(nameof(castMethod));
            _timeout = timeout;
            _actionTracker = actionTracker ?? throw new ArgumentNullException(nameof(actionTracker));
            
            // Subscribe to action events to detect when actions start executing
            _actionTracker.ActionRequested += OnActionRequested;
        }
        
        public void StartSequence(IGameObject target)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _currentStep = 1;
            _startTime = DateTime.Now;
            _lastDetectedActionId = 0;
            
            Service.Log.Debug($"[SkillSequence] Starting sequence with {_skillSequence.Count} steps on target {target.Name}");
        }
        
        public void Reset()
        {
            _currentStep = 0;
            _target = null;
            _startTime = DateTime.MinValue;
            _lastDetectedActionId = 0;
            
            Service.Log.Debug("[SkillSequence] Sequence reset");
        }
        
        private void OnActionRequested(ActionTracker.ActionRequest request)
        {
            // Check if this action request is for our current pending action
            if (IsActive && CurrentStep != null)
            {
                bool isValidAction = false;
                
                // Check if the action directly matches
                if (CurrentStep.ActionId == request.ActionId)
                {
                    isValidAction = true;
                }
                // Special case for Machinist variable skills
                // When sequence expects Drill, accept Anchor or Chainsaw requests as well
                else if (CurrentStep.ActionId == Service.Action_Drill && 
                        (request.ActionId == Service.Action_Drill || 
                         request.ActionId == Service.Action_Anchor || 
                         request.ActionId == Service.Action_ChainSaw))
                {
                    isValidAction = true;
                    Service.Log.Debug($"[SkillSequence] Machinist variable skill detected: Expected={CurrentStep.ActionId}, Requested={request.ActionId}");
                }
                // Special case for Bard Harmonic Arrow variants
                // When sequence expects Harmonic Arrow, accept any stack variant
                else if (CurrentStep.ActionId == Service.Action_HarmonicArrow && 
                        (request.ActionId == Service.Action_HarmonicArrow || 
                         request.ActionId == Service.Action_HarmonicArrowX2 || 
                         request.ActionId == Service.Action_HarmonicArrowX3 || 
                         request.ActionId == Service.Action_HarmonicArrowX4))
                {
                    isValidAction = true;
                    Service.Log.Debug($"[SkillSequence] Bard Harmonic Arrow variant detected: Expected={CurrentStep.ActionId}, Requested={request.ActionId}");
                }
                
                if (isValidAction)
                {
                    // Only advance if the action used a real sequence number (not a fallback)
                    // Real sequences are incremental, fallback sequences are timestamp-based and much larger
                    var currentSeq = _actionTracker.LastUsedActionSequence;
                    bool isRealSequence = request.SourceSequence == currentSeq;
                    
                    if (isRealSequence)
                    {
                        Service.Log.Debug($"[SkillSequence] Action {request.ActionId} successfully executed (seq: {request.SourceSequence}), advancing to next step");
                        
                        if (!string.IsNullOrEmpty(CurrentStep.LogMessage))
                        {
                            Service.Log.Debug(CurrentStep.LogMessage);
                        }
                        
                        _lastDetectedActionId = request.ActionId;
                        _currentStep++;
                        
                        // Check if sequence is complete
                        if (_currentStep > _skillSequence.Count)
                        {
                            Service.Log.Debug("[SkillSequence] Sequence completed successfully");
                            Reset();
                        }
                    }
                    else
                    {
                        Service.Log.Debug($"[SkillSequence] Action {request.ActionId} attempted but failed (fallback seq: {request.SourceSequence}), continuing to retry");
                    }
                }
            }
        }
        
        public bool Update()
        {
            if (!IsActive) return false;
            
            // Check for reset conditions
            if (ShouldReset())
            {
                Reset();
                Service.Log.Debug("[SkillSequence] reset due to timeout or invalid state");
                return false;
            }
            
            // Check if sequence is complete
            if (_currentStep > _skillSequence.Count)
            {
                Reset();
                Service.Log.Debug("[SkillSequence] Sequence completed successfully");
                return false;
            }

            var step = CurrentStep;
            if (step is null)
            {
                Reset();
                return false;
            }

            // Always try to cast the current action - let the game and ActionTracker handle the timing
            // This creates continuous pressure and immediate response when actions become available
            _castMethod(step.ActionId, _target!.GameObjectId);

            return true;
        }

        private bool ShouldReset()
        {
            return Service.ClientState.LocalPlayer?.IsDead == true ||
                   _target?.IsDead == true ||
                   DateTime.Now.Subtract(_startTime) >= _timeout;
        }
        
        public void Dispose()
        {
            // Unsubscribe from events
            if (_actionTracker != null)
            {
                _actionTracker.ActionRequested -= OnActionRequested;
            }
        }
    }
}
