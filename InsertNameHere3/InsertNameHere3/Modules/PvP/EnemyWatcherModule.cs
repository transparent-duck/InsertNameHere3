using System;
using System.Collections.Generic;
using System.Numerics;
using InsertNameHere3.Modules;
using InsertNameHere3;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ImGuiNET;
using InsertNameHere3.Modules.PvP;

namespace InsertNameHere3.Modules
{
    public class EnemyWatcherModule : IPvPModule
    {
        private readonly Configuration _configuration;
        private readonly PvPCombatModule _combatModule;
        private readonly List<IGameObject> _watchingEnemies = new();
        private readonly List<IGameObject> _targetedEnemies = new();
        private readonly Dictionary<IGameObject, List<IGameObject>> _teammateWatchers = new(); // Teammates being watched by enemies
        
        // Caching for performance
        private ulong _lastPlayerTargetId;
        
        // Drawing settings (now using configuration colors)
        private const float LineThickness = 2.0f;
        private const float RedLineThickness = 4.0f; // Thicker red line so it shows over green
        private const float TeammateLineThickness = 3.0f; // Medium thickness for teammate lines
        
        public bool IsEnabled => _configuration.ShowEnemyWatchers;
        
        public EnemyWatcherModule(Configuration configuration, PvPCombatModule combatModule)
        {
            _configuration = configuration;
            _combatModule = combatModule;
        }

        public void Initialize()
        {
            Service.Log.Information("[EnemyWatcherModule] Initialized - monitoring enemies watching local player and teammates");
        }

        public void Update(IFramework framework)
        {
            if (!IsEnabled || !_combatModule.IsInPvP55 || Service.ClientState.LocalPlayer == null)
                return;
            UpdateWatchingEnemies();
            UpdateWatchingTeammates();
        }

        private void UpdateWatchingEnemies()
        {
            var localPlayer = Service.ClientState.LocalPlayer;
            if (localPlayer == null) return;

            // Clear and rebuild watching enemies list
            _watchingEnemies.Clear();

            // Use the combat module's enemy list to find enemies watching the player
            foreach (var enemyActor in _combatModule.AllEnemyActors)
            {
                var obj = enemyActor.BattleChara;

                // Validate object is still alive and valid
                if (obj == null || !IsValidGameObject(obj))
                    continue;
 
                // Check if the enemy is targeting the local player
                if (IsWatchingPlayer(obj, localPlayer))
                {
                    _watchingEnemies.Add(obj);
                }
            }

            // Handle player targeting with real-time target manager
            ulong currentTargetId = 0;
            if (Service.TargetManager.Target != null)
            {
                currentTargetId = Service.TargetManager.Target.GameObjectId;
            }
            
            // Only update targeted enemies if target changed
            if (currentTargetId != _lastPlayerTargetId)
            {
                _targetedEnemies.Clear();
                _lastPlayerTargetId = currentTargetId;
                
                if (currentTargetId != 0)
                {
                    // Find the target in the object table
                    foreach (var enemyActor in _combatModule.AllEnemyActors)
                    {
                        var obj = enemyActor.BattleChara;
                        if (obj?.GameObjectId == currentTargetId && IsValidGameObject(obj))
                        {
                            _targetedEnemies.Add(obj);
                            break;
                        }
                    }
                }
            }
            else if (currentTargetId == 0)
            {
                // Target was cleared, clear the list immediately
                _targetedEnemies.Clear();
            }
            else
            {
                // Target unchanged, validate existing targets are still valid
                _targetedEnemies.RemoveAll(enemy => !IsValidGameObject(enemy));
            }

        }

        private bool IsValidGameObject(IGameObject gameObject)
        {
            if (gameObject == null)
                return false;

            try
            {
                // Check if object is still in the object table and has valid properties
                return gameObject.IsValid() && 
                       gameObject.GameObjectId != 0;
                       // Note: Removed IsDead check as it may not be available on all game objects
            }
            catch
            {
                // Object might be disposed or invalid
                return false;
            }
        }

        private bool IsWatchingPlayer(IGameObject enemy, IPlayerCharacter localPlayer)
        {
            // Check if the enemy is targeting the local player
            if (enemy is IBattleChara battleChara)
            {
                return battleChara.TargetObjectId == localPlayer.GameObjectId;
            }
            
            return false;
        }
        
        private void UpdateWatchingTeammates()
        {
            var localPlayer = Service.ClientState.LocalPlayer;
            if (localPlayer == null || !_configuration.ShowTeammateWatchers) return;
            
            // Clear previous teammate watchers
            _teammateWatchers.Clear();
            
            // Get party members from Dalamud's PartyList service
            foreach (var partyMember in Service.PartyList)
            {
                // Skip if it's the local player
                if (partyMember.ObjectId == localPlayer.GameObjectId)
                    continue;
                    
                // Find the party member's game object
                var teammateObject = partyMember.GameObject;
                if (teammateObject == null || !IsValidGameObject(teammateObject))
                    continue;
                
                // Find all enemies targeting this teammate
                var watchingEnemies = new List<IGameObject>();
                foreach (var enemyActor in _combatModule.AllEnemyActors)
                {
                    var enemy = enemyActor.BattleChara;
                    if (enemy == null || !IsValidGameObject(enemy))
                        continue;
                        
                    // Check if this enemy is targeting the teammate
                    if (enemy is IBattleChara battleChara && battleChara.TargetObjectId == teammateObject.GameObjectId)
                    {
                        watchingEnemies.Add(enemy);
                    }
                }
                
                // Add to dictionary if any enemies are watching this teammate
                if (watchingEnemies.Count > 0)
                {
                    _teammateWatchers[teammateObject] = watchingEnemies;
                }
            }
        }

        public void Draw()
        {
            if (!IsEnabled || Service.ClientState.LocalPlayer == null)
                return;

            var localPlayer = Service.ClientState.LocalPlayer;
            var drawList = ImGui.GetBackgroundDrawList();

            // Draw red lines for enemies watching the player
            foreach (var enemy in _watchingEnemies)
            {
                if (enemy == null || !IsValidGameObject(enemy)) 
                    continue;

                try
                {
                    // Convert world positions to screen coordinates
                    if (Service.GameGui.WorldToScreen(enemy.Position, out var enemyScreenPos) &&
                        Service.GameGui.WorldToScreen(localPlayer.Position, out var playerScreenPos))
                    {
                        // Draw thicker red line from enemy to player
                        drawList.AddLine(
                            enemyScreenPos,
                            playerScreenPos,
                            ImGui.ColorConvertFloat4ToU32(_configuration.EnemyWatcherLineColor),
                            RedLineThickness
                        );

                        // Draw a small red circle at the enemy position
                        drawList.AddCircleFilled(
                            enemyScreenPos,
                            5f,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.0f, 0.6f))
                        );
                    }
                }
                catch
                {
                    // Skip this enemy if there's an error (might be disposed)
                    continue;
                }
            }

            // Draw green lines for enemies the player is targeting
            foreach (var enemy in _targetedEnemies)
            {
                if (enemy == null || !IsValidGameObject(enemy)) 
                    continue;

                try
                {
                    // Convert world positions to screen coordinates
                    if (Service.GameGui.WorldToScreen(enemy.Position, out var enemyScreenPos) &&
                        Service.GameGui.WorldToScreen(localPlayer.Position, out var playerScreenPos))
                    {
                        // Draw green line from player to enemy
                        drawList.AddLine(
                            playerScreenPos,
                            enemyScreenPos,
                            ImGui.ColorConvertFloat4ToU32(_configuration.PlayerTargetLineColor),
                            LineThickness
                        );

                        // Draw a small green circle at the enemy position
                        drawList.AddCircleFilled(
                            enemyScreenPos,
                            5f,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 0.0f, 0.6f))
                        );
                    }
                }
                catch
                {
                    // Skip this enemy if there's an error (might be disposed)
                    continue;
                }
            }
            
            // Draw lines for teammates being watched by enemies
            if (_configuration.ShowTeammateWatchers)
            {
                foreach (var (teammate, watchers) in _teammateWatchers)
                {
                if (teammate == null || !IsValidGameObject(teammate))
                    continue;
                    
                foreach (var enemy in watchers)
                {
                    if (enemy == null || !IsValidGameObject(enemy))
                        continue;
                        
                    try
                    {
                        // Convert world positions to screen coordinates
                        if (Service.GameGui.WorldToScreen(enemy.Position, out var enemyScreenPos) &&
                            Service.GameGui.WorldToScreen(teammate.Position, out var teammateScreenPos))
                        {
                            // Draw orange line from enemy to teammate
                            drawList.AddLine(
                                enemyScreenPos,
                                teammateScreenPos,
                                ImGui.ColorConvertFloat4ToU32(_configuration.TeammateWatchedLineColor),
                                TeammateLineThickness
                            );

                            // Draw a small orange circle at the enemy position
                            drawList.AddCircleFilled(
                                enemyScreenPos,
                                4f,
                                ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.5f, 0.0f, 0.5f))
                            );
                        }
                    }
                    catch
                    {
                        // Skip if there's an error
                        continue;
                    }
                }
            }
            }
        }

        public void Dispose()
        {
            _watchingEnemies.Clear();
            _targetedEnemies.Clear();
            _teammateWatchers.Clear();
            Service.Log.Information("[EnemyWatcherModule] Disposed");
        }
    }
}


