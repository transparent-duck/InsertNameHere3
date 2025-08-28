using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using InsertNameHere3;
using InsertNameHere3.Modules;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace InsertNameHere3.Modules.PvP
{
    public readonly struct SkillCooldownInfo
    {
        public DateTime LastCastTime { get; }
        public uint SkillId { get; }
        public float CooldownDuration { get; }
        public string SkillName { get; }

        public SkillCooldownInfo(DateTime lastCastTime, uint skillId, float cooldownDuration, string skillName)
        {
            LastCastTime = lastCastTime;
            SkillId = skillId;
            CooldownDuration = cooldownDuration;
            SkillName = skillName;
        }
    }

    public class DebugItemInfo
    {
        public string Name { get; set; } = "";
        public float Distance { get; set; }
        public bool CanAttack { get; set; }
        public string ObjectKind { get; set; } = "";
        public uint ObjectId { get; set; }
        public bool IsWoodDummy { get; set; }
        public Vector3 Position { get; set; }
    }

    public class EnemyActor
    {
        public IBattleChara BattleChara { get; set; }
        public bool IsSelectableAsTarget { get; set; }

        // FIXME: should be more precise
        // Selectable as central target
        // Selectable as direct damage target
        public EnemyActor(IBattleChara battleChara, bool isSelectableAsTarget)
        {
            BattleChara = battleChara;
            IsSelectableAsTarget = isSelectableAsTarget;
        }
    }

    public unsafe class PvPCombatModule : IPvPCombatModule
    {
        // public bool IsEnabled { get; set; } = true;
        public List<EnemyActor> AllEnemyActors { get; private set; } = new();
        public bool IsPvPAndEnemiesNearBy { get; private set; }
        public bool IsInPvP55 { get; private set; }

        private readonly Configuration _configuration;

        public IGameObject? ForceTarget { get; set; }

        public List<DebugItemInfo> DebugItems { get; private set; } = new();

        // Cooldown tracking
        private readonly Dictionary<ulong, Dictionary<uint, SkillCooldownInfo>> _actorSkillCooldowns = new();
        private Hook<ActionEffectHandler.Delegates.Receive>? _onActionUsedHook;
        
        // Skill database - maps skill ID to cooldown duration and name
        private readonly Dictionary<uint, (float cooldown, string name)> _skillDatabase = new()
        {
            { Service.Action_Bubble, (30f, "防禦") },           // Guard
            { Service.Action_Purify, (24f, "淨化") },           // Purify  
            // Add more skills as needed
        };

        public PvPCombatModule(Configuration configuration)
        {
            _configuration = configuration;
        }

        public void Initialize()
        {
            // Listen to territory changes
            Service.ClientState.TerritoryChanged += OnTerritoryChanged;
            
            // Check initial territory
            CheckPvPTerritory();
            
            // Initialize the action hook to track skill usage
            _onActionUsedHook = Service.GameInteropProvider.HookFromAddress<ActionEffectHandler.Delegates.Receive>(
                ActionEffectHandler.MemberFunctionPointers.Receive, OnActionHappens);
            _onActionUsedHook.Enable();
            
            Service.Log.Information("[PvPCombatModule] Initialized with territory monitoring and cooldown tracking");
        }
        
        private void OnTerritoryChanged(ushort territoryId)
        {
            try
            {
                var wasPvP = IsInPvP55;
                IsInPvP55 = Service.Pvp55TerritoryType.Contains(territoryId);
                
                if (wasPvP && !IsInPvP55)
                {
                    Service.Log.Information($"[PvPCombatModule] Left PvP territory ({territoryId})");
                }
                else if (!wasPvP && IsInPvP55)
                {
                    Service.Log.Information($"[PvPCombatModule] Entered PvP territory ({territoryId})");
                }
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[PvPCombatModule] Error in OnTerritoryChanged: {ex}");
            }
        }

        private void CheckPvPTerritory()
        {
            try
            {
                var currentTerritory = Service.ClientState.TerritoryType;
                IsInPvP55 = Service.Pvp55TerritoryType.Contains(currentTerritory);
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[PvPCombatModule] Error checking PvP territory: {ex}");
            }
        }

        public void Update(IFramework framework)
        {
            GenerateEnemiesList();
            
            // Clean up old cooldowns when in PvP
            if (IsInPvP55)
            {
                CleanupOldCooldowns();
            }
        }

        public void GenerateEnemiesList()
        {
            if (_configuration.Debug)
            {
                IsPvPAndEnemiesNearBy = false;
                AllEnemyActors.Clear();
                DebugItems.Clear();
                
                var debugGameObjects = Service.GameObjects;
                if (debugGameObjects != null)
                {
                    foreach (var item in debugGameObjects)
                    {
                        if (item is IBattleChara battleChara && 
                            CanAttack(battleChara) && 
                            item.Name.ToString().Contains("木人"))
                        {
                            AllEnemyActors.Add(new EnemyActor(battleChara, true));
                            IsPvPAndEnemiesNearBy = true;
                        }

                        // Collect debug info for all items
                        var debugInfo = new DebugItemInfo
                        {
                            Name = item.Name.ToString(),
                            Distance = Vector3.Distance(item.Position, Service.ClientState.LocalPlayer?.Position ?? Vector3.Zero),
                            CanAttack = item is IBattleChara battleCharaCheck && CanAttack(battleCharaCheck),
                            ObjectKind = item.ObjectKind.ToString(),
                            ObjectId = (uint)(item.GameObjectId & 0xFFFFFFFF),
                            IsWoodDummy = item.Name.ToString().Contains("木人"),
                            Position = item.Position
                        };
                        DebugItems.Add(debugInfo);
                    }
                }

                return;
            }

            if (!Service.ClientState.IsPvP || Service.ClientState.LocalPlayer == null)
            {
                IsPvPAndEnemiesNearBy = false;
                return;
            }

            IsPvPAndEnemiesNearBy = true;
            AllEnemyActors.Clear();
            var pvpGameObjects = Service.GameObjects;
            if (pvpGameObjects == null) return;

            foreach (var item in pvpGameObjects)
            {
                if (item.ObjectKind == ObjectKind.Player &&
                    item.GameObjectId != Service.ClientState.LocalPlayer.GameObjectId &&
                    item is IBattleChara battleChara &&
                    CanAttack(battleChara))
                {
                    if (item is IPlayerCharacter playerCharacter)
                    {
                        uint jobId = playerCharacter.ClassJob.RowId;
                        bool isJobAllowed = _configuration.AllowedTargetJobs.ContainsKey(jobId) &&
                                          _configuration.AllowedTargetJobs[jobId];
                        
                        // Add to AllEnemyActors regardless of job preference for AOE calculations
                        AllEnemyActors.Add(new EnemyActor(battleChara, isJobAllowed));
                    }
                }
            }

            // Check if there are any selectable enemies for primary targeting
            if (!AllEnemyActors.Any(e => e.IsSelectableAsTarget))
            {
                IsPvPAndEnemiesNearBy = false;
            }
        }

        private void OnActionHappens(uint actorId, Character* casterPtr, Vector3* targetPos,
            ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects,
            GameObjectId* targetEntityIds)
        {
            // Call the original function first
            _onActionUsedHook?.Original(actorId, casterPtr, targetPos, header, effects, targetEntityIds);

            // Only track cooldowns in PvP territories
            if (!IsInPvP55) return;

            try
            {
                if (header != null && casterPtr != null)
                {
                    var casterId = casterPtr->GameObject.GetGameObjectId().Id;
                    var skillId = header->ActionId;
                    
                    // Check if this is a skill we want to track
                    if (_skillDatabase.ContainsKey(skillId))
                    {
                        var skillInfo = _skillDatabase[skillId];
                        
                        // Initialize actor tracking if not exists
                        if (!_actorSkillCooldowns.ContainsKey(casterId))
                        {
                            _actorSkillCooldowns[casterId] = new Dictionary<uint, SkillCooldownInfo>();
                        }
                        
                        // Update skill cooldown info
                        _actorSkillCooldowns[casterId][skillId] = new SkillCooldownInfo(
                            DateTime.Now,
                            skillId,
                            skillInfo.cooldown,
                            skillInfo.name
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[PvPCombatModule] Error in OnActionHappens: {ex}");
            }
        }

        private void CleanupOldCooldowns()
        {
            var cutoffTime = DateTime.Now.AddMinutes(-2);
            var actorsToRemove = new List<ulong>();
            
            foreach (var actorPair in _actorSkillCooldowns)
            {
                var skillsToRemove = new List<uint>();
                
                foreach (var skillPair in actorPair.Value)
                {
                    var elapsed = (DateTime.Now - skillPair.Value.LastCastTime).TotalSeconds;
                    var isExpired = elapsed > skillPair.Value.CooldownDuration;
                    
                    if (skillPair.Value.LastCastTime < cutoffTime || isExpired)
                    {
                        skillsToRemove.Add(skillPair.Key);
                    }
                }
                
                foreach (var skillId in skillsToRemove)
                {
                    actorPair.Value.Remove(skillId);
                }
                
                if (actorPair.Value.Count == 0)
                {
                    actorsToRemove.Add(actorPair.Key);
                }
            }
            
            foreach (var actorId in actorsToRemove)
            {
                _actorSkillCooldowns.Remove(actorId);
            }
        }

        /// <summary>
        /// Check if an enemy has bubble (guard) on cooldown, indicating they recently used it
        /// </summary>
        /// <param name="enemy">The enemy to check</param>
        /// <returns>True if the enemy has bubble on cooldown (recently used), false otherwise</returns>
        public bool IsEnemyBubbleOnCooldown(IBattleChara? enemy)
        {
            if (enemy == null) return false;
            
            var actorId = enemy.GameObjectId;
            
            if (!_actorSkillCooldowns.ContainsKey(actorId) || 
                !_actorSkillCooldowns[actorId].ContainsKey(Service.Action_Bubble))
            {
                return false;
            }
            
            var bubbleInfo = _actorSkillCooldowns[actorId][Service.Action_Bubble];
            var elapsed = (DateTime.Now - bubbleInfo.LastCastTime).TotalSeconds;
            var remaining = Math.Max(0, bubbleInfo.CooldownDuration - elapsed);
            
            return remaining > 0;
        }

        /// <summary>
        /// Get remaining cooldown time for a specific skill on an enemy
        /// </summary>
        /// <param name="enemy">The enemy to check</param>
        /// <param name="skillId">The skill ID to check</param>
        /// <returns>Remaining cooldown time in seconds, 0 if not on cooldown</returns>
        public float GetEnemySkillCooldown(IBattleChara? enemy, uint skillId)
        {
            if (enemy == null) return 0f;
            
            var actorId = enemy.GameObjectId;
            
            if (!_actorSkillCooldowns.ContainsKey(actorId) || 
                !_actorSkillCooldowns[actorId].ContainsKey(skillId))
            {
                return 0f;
            }
            
            var skillInfo = _actorSkillCooldowns[actorId][skillId];
            var elapsed = (DateTime.Now - skillInfo.LastCastTime).TotalSeconds;
            var remaining = Math.Max(0, skillInfo.CooldownDuration - elapsed);
            
            return (float)remaining;
        }

        /// <summary>
        /// Get all tracked cooldowns for an enemy
        /// </summary>
        /// <param name="enemy">The enemy to check</param>
        /// <returns>Dictionary of skill cooldowns for the enemy</returns>
        public Dictionary<uint, SkillCooldownInfo> GetEnemyCooldowns(IBattleChara? enemy)
        {
            if (enemy == null) return new Dictionary<uint, SkillCooldownInfo>();
            
            var actorId = enemy.GameObjectId;
            
            if (!_actorSkillCooldowns.ContainsKey(actorId))
            {
                return new Dictionary<uint, SkillCooldownInfo>();
            }
            
            return new Dictionary<uint, SkillCooldownInfo>(_actorSkillCooldowns[actorId]);
        }
        
        /// <summary>
        /// Get all tracked cooldowns for any actor (including teammates)
        /// </summary>
        /// <param name="actor">The actor to check</param>
        /// <returns>Dictionary of skill cooldowns for the actor</returns>
        public Dictionary<uint, SkillCooldownInfo> GetActorCooldowns(IBattleChara? actor)
        {
            if (actor == null) return new Dictionary<uint, SkillCooldownInfo>();
            
            var actorId = actor.GameObjectId;
            
            if (!_actorSkillCooldowns.ContainsKey(actorId))
            {
                return new Dictionary<uint, SkillCooldownInfo>();
            }
            
            return new Dictionary<uint, SkillCooldownInfo>(_actorSkillCooldowns[actorId]);
        }

        public void Dispose()
        {
            // Unsubscribe from territory changes
            Service.ClientState.TerritoryChanged -= OnTerritoryChanged;
            
            // Dispose action hook
            _onActionUsedHook?.Dispose();
            
            // Clear cooldown data
            _actorSkillCooldowns.Clear();
            
            AllEnemyActors.Clear();
        }

        public bool CanAttack(IBattleChara target)
        {
            if (target == null || target.Address == 0)
            {
                return false;
            }

            var gameObject = (GameObject*)target.Address;
            if (ActionManager.CanUseActionOnTarget(Service.Action_Fire1, gameObject))
            {
                return true;
            }

            return false;
        }

        public bool ActionReady(uint actionId)
        {
            if (Service.Actions_checkReady.Contains(actionId))
            {
                return ActionManager.Instance()->GetActionStatus(ActionType.Action, actionId)
                    .Equals(Service.ActionStatus_Ready);
            }

            if (Service.Actions_checkCharge.ContainsKey(actionId))
            {
                return ActionManager.Instance()->GetCurrentCharges(actionId) >= Service.Actions_checkCharge[actionId];
            }

            if (Service.Actions_checkAdjustedId.ContainsKey(actionId))
            {
                var baseId = Service.Actions_checkAdjustedId[actionId];
                return ActionManager.Instance()->GetAdjustedActionId(baseId) == actionId &&
                       ActionManager.Instance()->IsActionOffCooldown(ActionType.Action, actionId);
            }

            if (Service.Actions_checkIfAdjusted.ContainsKey(actionId))
            {
                var currentAdjustedId = ActionManager.Instance()->GetAdjustedActionId(actionId);
                if (actionId == currentAdjustedId)
                {
                    return false;
                }
                var acceptedAdjustedIds = Service.Actions_checkIfAdjusted[actionId];
                if (acceptedAdjustedIds.Length != 0 && !acceptedAdjustedIds.Contains(currentAdjustedId))
                {
                    return false;
                } 
            }

            return ActionManager.Instance()->IsActionOffCooldown(ActionType.Action, actionId);
        }

        public bool Cast(uint actionId, ulong target = 0xE0000000)
        {
            return ActionManager.Instance()->UseAction(ActionType.Action, actionId, target);
        }

        public bool Available_RangeOrLos(uint actionId, IBattleChara target)
        {
            if (target == null || target.Address == 0 || Service.ClientState.LocalPlayer == null ||
                Service.ClientState.LocalPlayer.Address == 0)
            {
                return false;
            }

            var gameObject = (GameObject*)target.Address;
            var status = ActionManager.GetActionInRangeOrLoS(actionId,
                (GameObject*)Service.ClientState.LocalPlayer.Address,
                gameObject);
            return Service.Available_GetActinoInRangeOrLoSStatus.Contains(status);
        }

        public bool Available_Range(uint actionId, IBattleChara target, bool comboBegin = false)
        {
            // Read config to decide which method to use
            if (_configuration.CompatibleDistanceCalculation)
            {
                return Available_DistanceCalculation(actionId, target, comboBegin) && 
                       // 檢查障礙物.
                       Available_RangeOrLos(actionId, target);
            }

            return Available_RangeOrLos(actionId, target);

        }

        private bool Available_DistanceCalculation(uint actionId, IBattleChara target, bool comboBegin = false)
        {
            if (Service.ClientState.LocalPlayer == null)
            {
                return false;
            }

            var localPlayer = Service.ClientState.LocalPlayer;
            var distance = System.Math.Sqrt(
                System.Math.Pow(target.Position.X - localPlayer.Position.X, 2) +
                System.Math.Pow(target.Position.Y - localPlayer.Position.Y, 2) +
                System.Math.Pow(target.Position.Z - localPlayer.Position.Z, 2));
            
            var baseRange = GetActionRange(actionId);
            
            // Apply distance correction based on action type
            var distanceCorrection = comboBegin 
                ? _configuration.ComboStartDistanceCorrection 
                : _configuration.ComboFollowUpAndSingleSkillDistanceCorrection;
            
            var adjustedRange = baseRange + distanceCorrection;
            
            // Ensure range doesn't go below 0
            adjustedRange = System.Math.Max(0, adjustedRange);

            return distance <= adjustedRange;
        }

        private static float GetActionRange(uint actionId) => actionId switch
        {
            Service.Action_Smite => 10f, 
            Service.Action_MarksmansSpite => 50f, 
            Service.Action_EagleEyeShot => 40f,
            Service.Action_Anchor => 25f, 
            Service.Action_SeitonTenchu => 20f, 
            Service.Action_Zantetsuken => 20f,
            Service.Action_Biolysis => 25f,
            Service.Action_PrimalRend => 20f,
            Service.Action_Plunge => 20f,
            Service.Action_Perfectio => 25f,
            Service.Action_HarmonicArrow => 25f,
            _ => 0f
        };


        public bool DamageAsExpected(float damage, long currentHp)
        {
            return currentHp * 100 <= damage * _configuration.AutoSequenceOverCap &&
                   currentHp * 100 >= damage * _configuration.AutoSequenceMinFold;
        }
    }
}
