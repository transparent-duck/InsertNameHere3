using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using InsertNameHere3.Modules.PvP;

namespace InsertNameHere3.Modules
{
    public interface IPvPModule
    {
        void Initialize();
        void Update(IFramework framework);
        void Dispose();
    }

    public interface IPvPCombatModule : IPvPModule
    {
        List<EnemyActor> AllEnemyActors { get; }
        // void GenerateEnemiesList();
        bool IsPvPAndEnemiesNearBy { get; }
        bool IsInPvP55 { get; }
        IGameObject? ForceTarget { get; set; }
        bool ActionReady(uint actionId);
        bool Available_Range(uint actionId, IBattleChara target, bool beginCombo = false);
        bool DamageAsExpected(float damage, long currentHp);
        bool CanAttack(IBattleChara target);

        bool Cast(uint actionId, ulong target = 0xE0000000);

        // Cooldown tracking methods
        bool IsEnemyBubbleOnCooldown(IBattleChara? enemy);
        float GetEnemySkillCooldown(IBattleChara? enemy, uint skillId);
        Dictionary<uint, SkillCooldownInfo> GetEnemyCooldowns(IBattleChara? enemy);
    }

    public interface IPvPTargetingModule : IPvPModule
    {
        void AutoSelectTarget();
    }

    public interface IPvPJobModule : IPvPModule
    {
        uint JobId { get; }
        void ExecuteJobSpecificLogic();
    }

    public interface IPvPGeneralAction : IPvPModule
    {
        void HandleAction();
    }
}


