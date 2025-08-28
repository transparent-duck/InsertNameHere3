using System;
using System.Collections.Generic;
using System.Numerics;
using InsertNameHere3;
using InsertNameHere3.Modules;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ImGuiNET;

namespace InsertNameHere3.Modules.PvP
{
    public unsafe class TargetIconModule : IPvPModule
    {
        private readonly Configuration _configuration;
        private readonly PvPCombatModule _combatModule;
        private IGameObject? _currentTarget;
        
        // Debug cooldown data for testing
        private readonly Dictionary<ulong, Dictionary<uint, SkillCooldownInfo>> _debugActorSkillCooldowns = new();
        
        // Icon settings (now using configuration colors)
        public bool IsEnabled => _configuration.ShowTargetIcon && _combatModule.IsInPvP55;
        
        public TargetIconModule(Configuration configuration, PvPCombatModule combatModule)
        {
            _configuration = configuration;
            _combatModule = combatModule;
        }

        public void Initialize()
        {
            // Initialize debug cooldown data
            InitializeDebugCooldowns();
            
            Service.Log.Information("[TargetIconModule] Initialized - using PvPCombatModule for cooldown tracking");
        }
        
        private void InitializeDebugCooldowns()
        {
            // Clear any existing debug data
            _debugActorSkillCooldowns.Clear();
            
            // We'll populate this with the local player's ID when debug mode is active
            // For now, just ensure it's initialized as empty
        }

        public void Update(IFramework framework)
        {
            if (_configuration.Debug)
            {
                // Update debug cooldowns
                UpdateDebugCooldowns();
            }
            if (!IsEnabled) return;
            
            try
            {
                var currentTarget = Service.TargetManager.Target;
                _currentTarget = currentTarget;
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[TargetIconModule] Error in Update: {ex}");
            }
        }

        private void UpdateDebugCooldowns()
        {
            if (!_configuration.Debug || Service.ClientState.LocalPlayer == null) return;
            
            var localPlayerId = Service.ClientState.LocalPlayer.GameObjectId;
            
            // Create fake cooldown data for debug display
            _debugActorSkillCooldowns[localPlayerId] = new Dictionary<uint, SkillCooldownInfo>
            {
                [Service.Action_Bubble] = new SkillCooldownInfo
                (
                    DateTime.Now.AddSeconds(-15), // 15 seconds ago
                    Service.Action_Bubble,
                    30f,
                    "防禦"
                ),
                [Service.Action_Purify] = new SkillCooldownInfo
                (
                    DateTime.Now.AddSeconds(-16), // 16 seconds ago  
                    Service.Action_Purify,
                    24f,
                    "淨化"
                )
            };
        }
        
        public void Draw()
        {
            // Debug: Draw sample cooldown tooltip on local player
            if (_configuration.Debug && Service.ClientState.LocalPlayer != null)
            {
                DrawCooldownTooltip(Service.ClientState.LocalPlayer, _debugActorSkillCooldowns);
            }
            
            // Movement Y-offset active tip (always visible when enabled)
            try
            {
                if (Service.ClientState.LocalPlayer != null && Math.Abs(_configuration.MovementYSubtract) != 0f)
                {
                    var me = Service.ClientState.LocalPlayer;
                    var tipWorldPosition = new Vector3(me.Position.X, me.Position.Y + 2.2f, me.Position.Z);
                    if (Service.GameGui.WorldToScreen(tipWorldPosition, out var screenPos))
                    {
                        var drawList = ImGui.GetBackgroundDrawList();
                        var center = new Vector2(screenPos.X, screenPos.Y);
                        var displayText = $"Y軸偏移\n{_configuration.MovementYSubtract:+0.0;-0.0}m";
                        var textSize = ImGui.CalcTextSize(displayText);
                        var textPos = center - textSize / 2;
                        var padding = new Vector2(4.0f, 2.0f);
                        var rectMin = textPos - padding;
                        var rectMax = textPos + textSize + padding;
                        var bg = new Vector4(0f, 0f, 0f, 0.7f);
                        var fg = new Vector4(1f, 0.95f, 0.5f, 1f);
                        drawList.AddRectFilled(rectMin, rectMax, ImGui.ColorConvertFloat4ToU32(bg));
                        drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(fg), displayText);
                    }
                }
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[TargetIconModule] Error drawing Y-offset tip: {ex}");
            }
            
            if (!IsEnabled) return;
            
            try
            {
                // Draw cooldown tooltips for all enemy actors with tracked cooldowns
                foreach (var enemyActor in _combatModule.AllEnemyActors)
                {
                    var character = enemyActor.BattleChara;
                    if (character != null)
                    {
                        DrawCooldownTooltipFromCombatModule(character, false);
                    }
                }
                
                // Draw cooldown tooltips for teammates if enabled
                if (_configuration.ShowTeammateCooldowns)
                {
                    foreach (var partyMember in Service.PartyList)
                    {
                        var teammateObject = partyMember.GameObject;
                        if (teammateObject != null && teammateObject is IBattleChara teammateBattleChara)
                        {
                            // Skip local player
                            if (Service.ClientState.LocalPlayer != null && 
                                teammateBattleChara.GameObjectId == Service.ClientState.LocalPlayer.GameObjectId)
                                continue;
                                
                            DrawCooldownTooltipFromCombatModule(teammateBattleChara, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[TargetIconModule] Error in Draw: {ex}");
            }
        }
        
        private void DrawCooldownTooltipFromCombatModule(IBattleChara target, bool isTeammate = false)
        {
            try
            {
                // Get cooldown data from PvPCombatModule
                var cooldowns = isTeammate 
                    ? _combatModule.GetActorCooldowns(target)
                    : _combatModule.GetEnemyCooldowns(target);
                if (cooldowns.Count == 0) return;
                
                // Get target position and convert to screen coordinates
                var targetPosition = target.Position;
                var iconWorldPosition = new Vector3(targetPosition.X, targetPosition.Y + _configuration.TargetIconHeight, targetPosition.Z);
                
                if (Service.GameGui.WorldToScreen(iconWorldPosition, out var screenPos))
                {
                    var drawList = ImGui.GetBackgroundDrawList();
                    var iconCenter = new Vector2(screenPos.X, screenPos.Y);
                    
                    // Build cooldown text
                    var cooldownTexts = new List<string>();
                    var now = DateTime.Now;
                    
                    foreach (var skillInfo in cooldowns.Values)
                    {
                        var elapsed = (now - skillInfo.LastCastTime).TotalSeconds;
                        var remaining = Math.Max(0, skillInfo.CooldownDuration - elapsed);
                        
                        if (remaining > 0)
                        {
                            cooldownTexts.Add($"{skillInfo.SkillName} {remaining:F0}");
                        }
                    }
                    
                    if (cooldownTexts.Count == 0) return;
                    
                    // Combine all cooldown texts
                    var displayText = string.Join("\n", cooldownTexts);
                    
                    // Calculate text properties
                    var textSize = ImGui.CalcTextSize(displayText);
                    var textPos = iconCenter - textSize / 2;
                    
                    // Apply configuration settings with different colors for teammates
                    var iconColorWithAlpha = isTeammate 
                        ? new Vector4(_configuration.TargetIconTeammateColor.X, _configuration.TargetIconTeammateColor.Y, _configuration.TargetIconTeammateColor.Z, _configuration.TargetIconAlpha)
                        : new Vector4(_configuration.TargetIconEnemyColor.X, _configuration.TargetIconEnemyColor.Y, _configuration.TargetIconEnemyColor.Z, _configuration.TargetIconAlpha);
                    var backgroundColorWithAlpha = new Vector4(0, 0, 0, 0.7f * _configuration.TargetIconAlpha);
                    
                    // Draw background rectangle
                    var padding = new Vector2(4.0f, 2.0f);
                    var rectMin = textPos - padding;
                    var rectMax = textPos + textSize + padding;
                    drawList.AddRectFilled(rectMin, rectMax, ImGui.ColorConvertFloat4ToU32(backgroundColorWithAlpha));
                    
                    // Draw text
                    drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(iconColorWithAlpha), displayText);
                }
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[TargetIconModule] Error in DrawCooldownTooltipFromCombatModule: {ex}");
            }
        }
        
        private void DrawCooldownTooltip(IBattleChara target, Dictionary<ulong, Dictionary<uint, SkillCooldownInfo>> actorSkillCooldowns)
        {
            try
            {
                var actorId = target.GameObjectId;
                
                // Check if we have cooldown data for this actor
                if (!actorSkillCooldowns.ContainsKey(actorId) || actorSkillCooldowns[actorId].Count == 0)
                    return;
                
                // Get target position and convert to screen coordinates
                var targetPosition = target.Position;
                var iconWorldPosition = new Vector3(targetPosition.X, targetPosition.Y + _configuration.TargetIconHeight, targetPosition.Z);
                
                if (Service.GameGui.WorldToScreen(iconWorldPosition, out var screenPos))
                {
                    var drawList = ImGui.GetBackgroundDrawList();
                    var iconCenter = new Vector2(screenPos.X, screenPos.Y);
                    
                    // Build cooldown text
                    var cooldownTexts = new List<string>();
                    var now = DateTime.Now;
                    
                    foreach (var skillInfo in actorSkillCooldowns[actorId].Values)
                    {
                        var elapsed = (now - skillInfo.LastCastTime).TotalSeconds;
                        var remaining = Math.Max(0, skillInfo.CooldownDuration - elapsed);
                        
                        if (remaining > 0)
                        {
                            cooldownTexts.Add($"{skillInfo.SkillName} {remaining:F0}");
                        }
                    }
                    
                    if (cooldownTexts.Count == 0) return;
                    
                    // Combine all cooldown texts
                    var displayText = string.Join("\n", cooldownTexts);
                    
                    // Calculate text properties
                    var textSize = ImGui.CalcTextSize(displayText);
                    var textPos = iconCenter - textSize / 2;
                    
                    // Apply configuration settings
                    var iconColorWithAlpha = new Vector4(_configuration.TargetIconEnemyColor.X, _configuration.TargetIconEnemyColor.Y, _configuration.TargetIconEnemyColor.Z, _configuration.TargetIconAlpha);
                    var backgroundColorWithAlpha = new Vector4(0, 0, 0, 0.7f * _configuration.TargetIconAlpha);
                    
                    // Draw background rectangle
                    var padding = new Vector2(4.0f, 2.0f);
                    var rectMin = textPos - padding;
                    var rectMax = textPos + textSize + padding;
                    drawList.AddRectFilled(rectMin, rectMax, ImGui.ColorConvertFloat4ToU32(backgroundColorWithAlpha));
                    
                    // Draw text
                    drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(iconColorWithAlpha), displayText);
                }
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[TargetIconModule] Error in DrawCooldownTooltip: {ex}");
            }
        }
        
        public void Dispose()
        {
            _debugActorSkillCooldowns.Clear();
        }
    }
}
