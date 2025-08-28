using InsertNameHere3.Modules;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using InsertNameHere3;
using System;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;

namespace InsertNameHere3.Modules.PvP
{
    public class PvPTargetingModule : IPvPTargetingModule
    {
        // public bool IsEnabled { get; set; } = true;
        private readonly IPvPCombatModule _combatModule;
        private readonly Configuration _configuration;

        public PvPTargetingModule(IPvPCombatModule combatModule, Configuration configuration)
        {
            _combatModule = combatModule;
            _configuration = configuration;
        }

        public void Initialize()
        {
            // Initialization logic
        }

        public void Update(IFramework framework)
        {
            AutoSelectTarget();
        }

        public void AutoSelectTarget()
        {
            
            if (_combatModule.ForceTarget != null && !_combatModule.ForceTarget.IsDead)
            {
                Service.TargetManager.Target = _combatModule.ForceTarget;
                return;
            }
            
            if (!_configuration.EnableAutoSelect || !_combatModule.IsPvPAndEnemiesNearBy) return;
            
            ICharacter currentChara = null;

            foreach (var enemyActor in _combatModule.AllEnemyActors)
            {
                // Only consider enemies that are selectable as targets
                if (!enemyActor.IsSelectableAsTarget) continue;
                
                var chara = enemyActor.BattleChara;
                if (_configuration.OnlyTarget50 && chara.CurrentHp >= chara.MaxHp * 0.5 - 1)
                    continue;
                
                if (_configuration.ExcludeBeingProtected)
                {
                    bool beingProtected = false;
                    foreach (var cc in chara.StatusList)
                    {
                        if (Service.Buffs_Hallow.Contains(cc.StatusId))
                        {
                            beingProtected = true;
                            break;
                        }
                    }
                    if (beingProtected) continue;
                }

                double distance = CalculateDistance(Service.ClientState.LocalPlayer.Position, chara.Position);
                if (distance <= _configuration.TargetingRange && (currentChara == null || chara.CurrentHp < currentChara.CurrentHp) && _combatModule.Available_Range(Service.Action_MarksmansSpite, chara))
                {
                    // If the current character is null or the new character has less HP, select it
                    currentChara = chara;
                }
                else if (currentChara != null && chara.CurrentHp < currentChara.CurrentHp && CalculateDistance(Service.ClientState.LocalPlayer.Position, chara.Position) <= _configuration.TargetingRange)
                {
                    currentChara = chara;
                }
            }

            if (currentChara != null)
            {
                Service.TargetManager.Target = currentChara;
            }
        }

        private double CalculateDistance(System.Numerics.Vector3 a, System.Numerics.Vector3 b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Z - b.Z, 2));
        }

        public void Dispose()
        {
            // Cleanup logic
        }
    }
}
