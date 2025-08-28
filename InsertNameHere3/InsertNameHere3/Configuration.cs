using Dalamud.Configuration;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Threading;

namespace InsertNameHere3
{
    public enum PvPToggleMode
    {
        Disabled = 0,        // 禁用快速切換功能
        EnableOnPress = 1,   // 按住時啟用自動技能
        DisableOnPress = 2,  // 按住時禁用自動技能
        Toggle = 3           // 切換模式（當前功能）
    }
    
    public enum OccultCrescentBombardmentTrigger
    {
        EnemyCount = 0,         // 怪物數量達到目標
        PartyMemberCast = 1,    // 隊友施放炮擊（不包括自己）
        LocalPlayerCast = 2     // 本地玩家施放任意炮擊技能
    }
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool EnableAutoSelect { get; set; } = false;
        public bool OnlyTarget50 { get; set; } = false;
        public int TargetingRange { get; set; } = 20;
        // public bool ExcludeDrkAndKnight { get; set; } = false;
        public bool ExcludeBeingProtected { get; set; } = false;
        // public bool ExcludeSelfGuard { get; set; } = true;
        public bool AutoBubble { get; set; } = false;
        public bool AutoBubbleBlock { get; set; } = false;
        public bool AutoPurify { get; set; } = false;
        
        // Auto Purify Configuration - Human reaction debuffs (normal speed)
        public HashSet<uint> AutoPurifyHumanReaction { get; set; } = new HashSet<uint>
        {
            InsertNameHere3.Service.Buff_Stun,        // 暈眩
            InsertNameHere3.Service.Buff_Heavy,       // 沉重
            InsertNameHere3.Service.Buff_Bind,        // 束縛
            InsertNameHere3.Service.Buff_Silence,     // 沉默
            InsertNameHere3.Service.Buff_Sleep,       // 睡眠
            InsertNameHere3.Service.Buff_HalfAsleep,  // 半睡眠
            InsertNameHere3.Service.Buff_DeepFreeze,  // 深度凍結
            InsertNameHere3.Service.Buff_AmazingNature // 奇妙自然
        };
        
        // Auto Purify Configuration - Enable instant reaction to Blota
        public bool AutoPurifyBlotaReaction { get; set; } = false;
        
        // Auto Purify Configuration - Enable instant reaction to WindsReply
        public bool AutoPurifyWindsReplyReaction { get; set; } = false;
        
        public bool AutoElixir { get; set; } = false;
        public int AutoElixirPercentage { get; set; } = 50;
        
        public int AutoSequenceOverCap { get; set; } = 100;
        public int AutoSequenceMinFold { get; set; } = 25;
        public bool MachinistAutoLB { get; set; } = false;
        public bool MachinistAutoEagleEye { get; set; } = false;
        public bool MachinistAutoWildFire { get; set; } = false;
        public bool MachinistAutoWildFireMayUseLB { get; set; } = false;
        public bool BardAutoEagleEye { get; set; } = false;
        public bool BardAutoHarmonicArrow { get; set; } = false;
        public bool NinjaAutoLB { get; set; } = false;
        public bool SamuraiAutoLB { get; set; } = false;
        public bool SamuraiAutoLBAllowNonWeak { get; set; } = false;
        public int SamuraiAutoLBMinAmount { get; set; } = 1;
        
        // Job-specific smite settings
        public bool MonkAutoSmite { get; set; } = false;
        
        // Monk Earth's Reply settings
        public bool MonkAutoEarthsReply { get; set; } = false;
        public float MonkAutoEarthsReplyTiming { get; set; } = 1.0f;
        public bool DragoonAutoSmite { get; set; } = false;
        public bool DragoonForwardJump { get; set; } = false;
        public bool NinjaAutoSmite { get; set; } = false;
        public bool SamuraiAutoSmite { get; set; } = false;
        public bool ReaperAutoSmite { get; set; } = false;
        public bool ReaperAutoPerfectio { get; set; } = false;
        public bool ReaperAutoPerfectioAllowNonWeak { get; set; } = false;
        public int ReaperAutoPerfectioMinPredictionKill { get; set; } = 1;
        public bool ViperAutoSmite { get; set; } = false;
        public bool ViperAutoSerpentsTail { get; set; } = false; 
        public bool DisableCureWhenSelfGuard { get; set; } = false;
        
        // Auto LB Bubble Avoidance
        public bool AvoidBubbleEnemiesForAutoLB { get; set; } = false;
        
        // Scholar Settings
        public bool ScholarAutoSpreadPoison { get; set; } = false;
        public int ScholarSecretTacticsMode { get; set; } = 0; // 0: 自動祕策 - 僅祕策時擴毒 1: 動祕策 - 僅祕策時擴毒 2: 毒可用即擴 - 嘗試自動祕策 3: 毒可用即擴
        public int ScholarSpreadPoisonTargetCount { get; set; } = 3;
        
        // Warrior Settings - Combined targeting correction
        public int WarriorPrimalRendTargetCorrection { get; set; } = 0; // 0: 不使用, 1: 選擇面向120度內最多目標, 2: 選擇最多目標
        
        // Dark Knight Settings - Plunge targeting correction
        public int DarkKnightPlungeTargetCorrection { get; set; } = 0; // 0: 不使用, 1: 選擇面向120度內最多目標, 2: 選擇最多目標
        
        public bool WarriorPrimalScreamTargetCorrection {get; set; } = false; 
        public float RunFasterMtp { get; set; } = 1f;
        public bool RemoveDashDistanceLimit { get; set; } = false;
        
        // PvP Settings
        public bool CompatibleDistanceCalculation { get; set; } = true;
        public int ComboStartDistanceCorrection { get; set; } = -5;
        public int ComboFollowUpAndSingleSkillDistanceCorrection { get; set; } = 0;
        public Dictionary<uint, bool> AllowedTargetJobs { get; set; } = new Dictionary<uint, bool>
        {
            // Tank jobs
            { 19, true }, // Paladin
            { 21, true }, // Warrior  
            { 32, true }, // Dark Knight
            { 37, true }, // Gunbreaker
            
            // Healer jobs
            { 24, true }, // White Mage
            { 28, true }, // Scholar
            { 33, true }, // Astrologian
            { 40, true }, // Sage
            
            // Melee DPS jobs
            { 20, true }, // Monk
            { 22, true }, // Dragoon
            { 30, true }, // Ninja
            { 34, true }, // Samurai
            { 39, true }, // Reaper
            { 41, true }, // Viper
            
            // Ranged DPS jobs
            { 23, true }, // Bard
            { 31, true }, // Machinist
            { 38, true }, // Dancer
            
            // Caster DPS jobs
            { 25, true }, // Black Mage
            { 27, true }, // Summoner
            { 35, true }, // Red Mage
            { 42, true }  // Pictomancer
        };
        public bool ExpectedDamageBuffCalculation { get; set; } = true;
        
        // Movement Settings
        public bool IgnoreMovementAbnormality { get; set; } = false;
        public bool MovementExtendCastDistance { get; set; } = false;
        
        public HashSet<uint> MulDungeonMapList = new HashSet<uint>();
        
        public HashSet<uint> MulDungeonTmList = new HashSet<uint>();
        
        public bool MulDungeonAutoShield { get; set; } = false;
        public bool MulDungeonAutoDart { get; set; } = false;
        public bool MulDungeonAutoHeal { get; set; } = false;
        public int MulDungeonAutoHealPercentage { get; set; } = 50;
        
        public bool FurnitureThreeAxisMovement { get; set; } = false;
        public bool SelfThreeAxisMovement { get; set; } = false;
        public float MovementStep { get; set; } = 0.01f;
        
        // Movement Y-axis adjustment when sending movement packets (meters to subtract)
        public float MovementYSubtract { get; set; } = 0.0f;
        
        public bool FurnitureRotation { get; set; } = false;
        public bool DeploymentLock { get; set; } = false;
        
        public bool ReduceAdhesion { get; set; } = false;
        public bool StrongAdhesion { get; set; } = false;
        public bool StrongAdhesionOnSide { get; set; } = false;
        
        public bool InstantCopyFurnitureName { get; set; } = false;
        
        public bool NecromancerBossInvulnPalaceOfDead { get; set; } = false;
        public bool NecromancerMobInvulnPalaceOfDead { get; set; } = false;
        
        public bool KisskissWaterWalk { get; set; } = false;
        public bool KisskissLandSwim { get; set; } = false;
        
        // Poser Settings
        public float PoserO { get; set; } = 0f;
        public float PoserR { get; set; } = 0f;
        public float PoserSg { get; set; } = 0f;
        public uint PoserAction { get; set; } = 0;
        public bool PoserAutoApply { get; set; } = false;

        // Debugging settings
        public bool Debug { get; set; } = false;
        public bool FrameDebug { get; set; } = false;

        // Target Icon Settings
        public bool ShowTargetIcon { get; set; } = false;
        public bool ShowTeammateCooldowns { get; set; } = true;
        public float TargetIconAlpha { get; set; } = 1.0f;
        public float TargetIconHeight { get; set; } = 2.50f;
        
        // Enemy Watcher Settings
        public bool ShowEnemyWatchers { get; set; } = false;
        public bool ShowTeammateWatchers { get; set; } = true;
        
        // Status Tracking Color Settings
        public System.Numerics.Vector4 TargetIconEnemyColor { get; set; } = new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White for enemy cooldowns
        public System.Numerics.Vector4 TargetIconTeammateColor { get; set; } = new System.Numerics.Vector4(0.5f, 1.0f, 0.5f, 1.0f); // Green for teammate cooldowns
        public System.Numerics.Vector4 EnemyWatcherLineColor { get; set; } = new System.Numerics.Vector4(1.0f, 0.2f, 0.2f, 0.8f); // Red for enemies watching player
        public System.Numerics.Vector4 PlayerTargetLineColor { get; set; } = new System.Numerics.Vector4(0.2f, 1.0f, 0.2f, 0.8f); // Green for player targeting enemy
        public System.Numerics.Vector4 TeammateWatchedLineColor { get; set; } = new System.Numerics.Vector4(1.0f, 0.5f, 0.0f, 0.8f); // Orange for enemies watching teammates
        
        public bool ShowOnlyWoodDummies { get; set; } = false;
        public bool ShowOnlyAttackable { get; set; } = false;

        // Occult Crescent
        public bool OccultCrescentAutoRevive { get; set; } = false;
        
        // Enlightenment Gold Farming Settings (十二城邦金幣)
        public bool OccultCrescentAutoFollow { get; set; } = false;
        public bool OccultCrescentAutoBombardment { get; set; } = false;
        public int OccultCrescentMinEnemyCount { get; set; } = 5;
        public OccultCrescentBombardmentTrigger OccultCrescentBombardmentTrigger { get; set; } = OccultCrescentBombardmentTrigger.EnemyCount;

        // PvP Auto Skills Toggle
        public int PvPAutoSkillsToggleKey { get; set; } = 0; // Default: no key assigned
        public PvPToggleMode PvPToggleMode { get; set; } = PvPToggleMode.Toggle; // Default: toggle mode
         
        [NonSerialized]
        public bool PvPAutoSkillsEnabled = true; // Runtime state
        [NonSerialized]
        public bool PvPAutoSkillsDefaultState = true; // Default state for press modes

        // the below exist just to make saving less cumbersome
        // params with NonSerialized attribute will not be saved
        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;
        
        [NonSerialized]
        public bool cing;

        [NonSerialized]
        public DateTime cingT;

        [NonSerialized]
        public float cingR;

        [NonSerialized]
        public float cingD;
        
        [NonSerialized]
        public float cingSg;

        [NonSerialized]
        public IGameObject cingTarget;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
