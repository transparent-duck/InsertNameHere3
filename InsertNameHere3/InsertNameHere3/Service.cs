using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;

namespace InsertNameHere3
{
    internal class Service
    {
        internal static PluginAddressResolver Address { get; set; }
        internal static uint GetActionInRangeOrLoSStatus_OutOfRange = 566;
        internal static uint GetActionInRangeOrLoSStatus_WallInMiddle = 562;
        internal static uint GetActionInRangeOrLoSStatus_Normal = 0;
        internal static uint GetActionInRangeOrLoSStatus_NotFacing = 565;

        internal static uint[] Available_GetActinoInRangeOrLoSStatus =
        [
            GetActionInRangeOrLoSStatus_Normal, GetActionInRangeOrLoSStatus_NotFacing
        ];

        internal static ulong EmptyPlayerObjectId = 3758096384;

        internal static uint ActionStatus_CoolingDown = 582;
        internal const uint ActionStatus_Ready = 0;
        internal const uint ActionStatus_Mounted = 579; //mounted ??
        internal const uint Buff_3039 = 3039; // 死而不僵
        internal const uint Buff_1301 = 1301; // 被保护
        internal const uint Buff_1302 = 1302; // 神圣领域
        
        internal static uint[] Buffs_Hallow =
        [
            Buff_1301, // 被保护
            Buff_1302, // 神圣领域
            Buff_3039, // 死而不僵
            Buff_3119, // 中庸之道
            Buff_immortal
        ];

        internal const uint Buff_test = 365;
        internal const uint Buff_Stun = 1343;
        internal const uint Buff_Heavy = 1344;
        internal const uint Buff_Bind = 1345;
        internal const uint Buff_Silence = 1347;
        internal const uint Buff_Sleep = 1348;
        internal const uint Buff_HalfAsleep = 3022;
        internal const uint Buff_DeepFreeze = 3219;
        internal const uint Buff_AmazingNature = 3085;
        internal const uint Buff_Bubble = 3054;
        internal const uint Buff_BrokenBubble = 3673;
        internal const uint Buff_Kuzushi = 3202;
        internal const uint Buff_Reviving = 148;
        internal const uint Buff_CanNotBubble = 3021;
        internal const uint Buff_3171 = 3171; // 金刚神髓


        internal static uint[] PurifiableBuffs =
        [
            Buff_Stun, Buff_Heavy, Buff_Bind, Buff_Silence, Buff_Sleep,
            Buff_HalfAsleep,
            Buff_DeepFreeze, Buff_AmazingNature
        ];

        internal const uint Action_Purify = 29056; // 淨化
        internal const uint Action_Bubble = 29054; // 泡泡
        internal const uint Action_SeitonTenchu = 29515; // 星遁天誅
        internal const uint Action_Shukuchi = 29513; // 缩地

        internal const uint Action_Perfectio = 41458; // 完人
        internal const uint Action_TenebraeLemurum = 29553; // 暗夜游魂(LB)
        
        internal const uint Action_HarmonicArrow = 41464; // 和弦箭
        internal const uint Action_HarmonicArrowX4 = 41964; // 和弦箭 4 charges
        internal const uint Action_HarmonicArrowX3 = 41466; // 和弦箭 3 charges
        internal const uint Action_HarmonicArrowX2 = 41465; // 和弦箭 2 charges
        
        internal const uint Action_WardensPaean = 29400; // 光阴神的礼赞凯歌

        internal const uint Action_StandardElixir = 29711; // pvp自愈
        internal const uint Action_Zantetsuken = 29537; // 斬鐵劍
        internal const uint Action_MarksmansSpite = 29415; // 魔彈射手
        internal const uint Action_EagleEyeShot = 43251; // 銳眼追擊
        internal const uint Action_Fire1 = 141; // 火1
        internal const uint Action_Recitation = 29236; // 秘策
        internal const uint Action_Biolysis = 29233; // 蛊毒法
        internal const uint Action_Biolysis_Radius = 15; // 蛊毒法 15m 擴散
        internal const uint Action_Deployment_Tactics = 29234; // 展开战术
        internal const uint Action_MulDungeonGuard = 29733;
        internal const uint Action_MulDungeonSeiso = 29732;
        internal const uint Action_29729 = 29729; // 多变治疗
        internal const uint Action_Wildfire = 29409; // 野火
        internal const uint Action_FullMetal = 41469; // 全金属
        internal const uint Action_Analyze = 29414; // 分析
        internal const uint Action_Drill = 29405; // 钻头
        internal const uint Action_Poison = 29406; // 喷毒
        internal const uint Action_Anchor = 29407; // 空气锚
        internal const uint Action_ChainSaw = 29408; // 链锯
        internal static uint[] Actions_NeedBubble = [
            Action_MarksmansSpite, Action_Blota, Action_WindsReply
        ];
        internal static uint[] Actions_checkReady = [
            Action_MarksmansSpite, 
            Action_Zantetsuken, 
            Action_EagleEyeShot
            // Action_Smite
        ];
        
        internal static Dictionary<uint, uint> Actions_checkCharge =
        new()
        {
            { Action_FullMetal, 1 },
            { Action_HarmonicArrow, 4 }
        };

        internal static Dictionary<uint, uint> Actions_checkAdjustedId =
            new()
            {
                { Action_Drill, Action_Drill }, 
                { Action_Anchor, Action_Drill }, 
                { Action_ChainSaw, Action_Drill },
                { Action_Smite, Action_PvPAction },
                { Action_EarthsReply, Action_RiddleOfEarth },
                { Action_Perfectio, Action_TenebraeLemurum }
            };
        
        internal static Dictionary<uint, uint[]> Actions_checkIfAdjusted =
            new()
            {
                { Action_SerpentsTail, new uint[] {} }, 
            };

        
        // default, seems most skill can be checked by Coolingdown.
        // this also checks if an action is pushable for the skill queue.
        internal static uint[] Actions_checkCooldown = [];
        
        internal const uint Action_41631 = 41631;  // 魔恢复药
        internal const uint Action_41633 = 41631;  // 魔以太药
        internal const uint Action_41634 = 41634;  // 苏生(新月岛)
        
        internal const uint Action_PrimalRend = 29084;  // 蛮荒崩裂
        internal const uint Action_PrimalRend_Radius = 5; // 蛮荒崩裂范围
        internal const uint Action_PrimalScream = 29083;  // 原初的怒号 LB
        internal const uint Action_PrimalScream_Radius = 12; // 原初的怒号范围
        internal const uint Action_Plunge = 29092; // 跳斩
        internal const uint Action_Plunge_Radius = 10; // 跳斩

        internal const uint Action_EarthsReply = 29483; // 金刚转轮
        internal const uint Action_RiddleOfEarth = 29482; // 金刚极意
        internal const uint Action_ElusiveJump = 29494; // 回避跳跃
        internal const uint Action_SerpentsTail = 39183; // 追击之牙
        

        // Create a list contains Drill, Anchor and ChainSaw
        internal static readonly List<uint> MachinistActions = new() { Action_Drill, Action_Anchor, Action_ChainSaw };
        internal const uint Action_Blota = 29081; // 献身/战士死斗拉
        internal const uint Action_WindsReply = 41509; // 绝空拳/武僧推
        internal const uint Action_PvPAction = 43259; // pvp专有技能
        internal const uint Action_43246 = 43246; // 浴血
        internal const uint Action_43247 = 43247; // 敏捷
        internal const uint Action_Smite = 43248; // 猛击
        internal const uint Buff_ReuseSeitonTenchu = 3192;
        internal const uint JobPaladin = 19;
        internal const uint JobWarrior = 21;
        internal const uint JobDarkKnight = 32;
        internal const uint JobGunBlade = 37;
        internal const uint JobWhiteMage = 24;
        internal const uint JobScholar = 28;
        internal const uint JobAstrologian = 33;
        internal const uint JobSage = 40;
        internal const uint JobMonk = 20;
        internal const uint JobDragoon = 22;
        internal const uint JobNinja = 30;
        internal const uint JobSamurai = 34;
        internal const uint JobReaper = 39;
        internal const uint JobViper = 41;
        internal const uint JobBard = 23;
        internal const uint JobMachinist = 31;
        internal const uint JobDancer = 38;
        internal const uint JobBlackMage = 25;
        internal const uint JobSummoner = 27;
        internal const uint JobRedMage = 35;
        internal const uint JobPictoMancer = 42;
        internal static uint[] JobsWhoHaveHollowGuards = [JobPaladin, JobDarkKnight];
        internal static float BaseSpeed = 6f;
        internal const uint Buff_3154 = 3154; // 回转飞锯 受伤+20%
        internal const uint Buff_1978 = 1978; // 铁壁 受伤-50%
        internal const uint Buff_4476 = 4476; // 暴怒 受伤+25%
        internal const uint Buff_3256 = 3256; // 群山隆起 输出-10%
        internal const uint Buff_4283 = 4283; // 强盾猛击 受伤+10% next is 3188
        internal const uint Buff_3188 = 3188; // 忠义之盾 受伤-10%
        internal const uint Buff_3210 = 3210; // 列阵 受伤-33%
        
        internal const uint Buff_immortal = 895; // 无敌

        // internal const uint Buff_3155 = 3155; // 盾阵 输出-10%  // 这是谁的技能?
        internal const uint Buff_3037 = 3037; // 大地 受伤-20%
        internal const uint Buff_4295 = 4295; // 刚玉 受伤-10%
        internal const uint Buff_3052 = 3052; // 连续剑 受伤-25%     // 可以适配层数
        internal const uint Buff_3088 = 3088; // 激励 受伤-10%
        internal const uint Buff_1406 = 1406; // 连环计 受伤+10% 
        internal const uint Buff_3093 = 3093; // 怒涛 输出+10% 
        internal const uint Buff_3113 = 3113; // 箭毒 受伤+10% 
        internal const uint Buff_3119 = 3119; // 中庸之道 无敌 
        internal const uint Buff_1415 = 1415; // 护盾 受伤-8% 
        internal const uint Buff_1452 = 1452; // 王冠贵妇 受伤-10% 
        internal const uint Buff_1451 = 1451; // 王冠领主 受伤+10% 
        internal const uint Buff_3105 = 3105; // 星河漫天 输出+10%    // 可以适配层数
        internal const uint Buff_3106 = 3106; // 漫天星光 输出-30% 
        internal const uint Buff_4096 = 4096; // 蛇鳞 受伤-50% 
        internal const uint Buff_3177 = 3177; // 红莲龙血 受伤+25% 
        internal const uint Buff_3179 = 3179; // 恐惧咆哮 受伤-50%    // 仅对来源生效
        internal const uint Buff_4304 = 4304; // 土遁 受伤-15% 
        internal const uint Buff_1986 = 1986; // 幻影弹 受伤+25% 
        internal const uint Buff_4480 = 4480; // 武装削弱 输出-33% 
        internal const uint Buff_3224 = 3224; // 守护之光 受伤-25% 
        internal const uint Buff_4333 = 4333; // 昏沉 输出-33% 
        internal const uint Buff_4316 = 4316; // 寒冰环 受伤-20% 
        internal const uint Buff_2282 = 2282; // 鼓励 输出+8% 受伤-8%
        internal const uint Buff_4111 = 4111; // 兽抓构想 受伤+10% 
        internal const uint Buff_4117 = 4117; // 胖胖之壁 受伤-25% 
        internal const uint Buff_3139 = 3139; // 冲锋的进行曲 输出+5% 
        internal const uint Buff_3145 = 3145; // 英豪的幻想曲 输出+10% 
        internal const uint Buff_4479 = 4479; // 勇气 输出+25% 受伤-25% 
        internal const uint Buff_2052 = 2052; // 扇舞 受伤-20%
        // 拂晓之谊 - 舞者 - 未知

        internal const uint Buff_2131 = 2131; // 志氣高揚I 輸出+10%
        internal const uint Buff_2132 = 2132; // 志氣高揚II 輸出+20%
        internal const uint Buff_2133 = 2133; // 志氣高揚III 輸出+30%
        internal const uint Buff_2134 = 2134; // 志氣高揚IV 輸出+40%
        internal const uint Buff_2135 = 2135; // 志氣高揚V 輸出+50%
        internal const uint Buff_1729 = 1729; // 坚定不移 輸出+50%


        internal static uint Buff_Recitation = 3094; // 秘策

        
        // FIXME: 兼容層數或時間或限定來源的 buff
        internal static readonly Dictionary<uint, float> TargetBuffDamageCorrections = new()
        {
            { Buff_immortal, 0f},
            { Buff_1301, 0f },
            { Buff_1302, 0f },
            { Buff_3039, 0f },
            { Buff_3154, 1.2f }, // 回转飞锯 受伤+20%
            { Buff_1978, 0.5f }, // 铁壁 受伤-50%
            { Buff_4476, 1.25f }, // 暴怒 受伤+25%
            // { Buff_3256, 0.9f },    // 群山隆起 输出-10%
            { Buff_4283, 1.1f }, // 强盾猛击 受伤+10%
            { Buff_3188, 0.9f }, // 忠义之盾 受伤-10%
            { Buff_3210, 0.67f }, // 列阵 受伤-33%
            //{ Buff_3155, 0.9f },  // 盾阵 输出-10%  // 这是谁的技能?
            { Buff_3037, 0.8f }, // 大地 受伤-20%
            { Buff_4295, 0.9f }, // 刚玉 受伤-10%
            { Buff_3052, 0.75f }, // 连续剑 受伤-25%
            { Buff_3088, 0.9f }, // 激励 受伤-10%
            { Buff_1406, 1.1f }, // 连环计 受伤+10%
            // { Buff_3093, 1.1f },    // 怒涛 输出+10%
            { Buff_3113, 1.1f }, // 箭毒 受伤+10%
            { Buff_3119, 0f }, // 中庸之道 无敌
            { Buff_1415, 0.92f }, // 护盾 受伤-8%
            { Buff_1452, 0.9f }, // 王冠贵妇 受伤-10%
            { Buff_1451, 1.1f }, // 王冠领主 受伤+10%
            // { Buff_3105, 1.1f },    // 星河漫天 输出+10%
            // { Buff_3106, 0.7f },    // 漫天星光 输出-30%
            { Buff_4096, 0.5f }, // 蛇鳞 受伤-50%
            { Buff_3177, 1.25f }, // 红莲龙血 受伤+25%
            { Buff_3179, 0.5f }, // 恐惧咆哮 受伤-50%    // 仅对来源生效
            { Buff_4304, 0.85f }, // 土遁 受伤-15%
            { Buff_1986, 1.25f }, // 幻影弹 受伤+25%
            // { Buff_4480, 0.67f },   // 武装削弱 输出-33%
            { Buff_3224, 0.75f }, // 守护之光 受伤-25%
            // { Buff_4333, 0.67f },   // 昏沉 输出-33%
            { Buff_4316, 0.8f }, // 寒冰环 受伤-20%
            { Buff_2282, 0.92f }, // 鼓励 输出+8% 受伤-8%
            { Buff_4111, 1.1f }, // 兽抓构想 受伤+10%
            { Buff_4117, 0.75f }, // 胖胖之壁 受伤-25%
            // { Buff_3139, 1.05f },   // 冲锋的进行曲 输出+5%
            // { Buff_3145, 1.1f },    // 英豪的幻想曲 输出+10%
            { Buff_4479, 0.75f }, // 勇气 输出+25% 受伤-25%
            { Buff_2052, 0.8f }, // 扇舞 受伤-20%
        };

        internal static readonly Dictionary<uint, float> SpecialBubbleStatus = new()
        {
            { Buff_Bubble, 0.1f }, { Buff_BrokenBubble, 0.55f }
        };

        internal static readonly Dictionary<uint, float> SelfBuffDamageCorrections = new()
        {
            { Buff_3256, 0.9f }, // 群山隆起 输出-10%
            // { Buff_3155, 0.9f },    // 盾阵 输出-10%  // 这是谁的技能?
            { Buff_3093, 1.1f }, // 怒涛 输出+10%
            { Buff_3105, 1.1f }, // 星河漫天 输出+10%
            { Buff_3106, 0.7f }, // 漫天星光 输出-30%
            { Buff_4480, 0.67f }, // 武装削弱 输出-33%
            { Buff_4333, 0.67f }, // 昏沉 输出-33%
            { Buff_3139, 1.05f }, // 冲锋的进行曲 输出+5%
            { Buff_3145, 1.1f }, // 英豪的幻想曲 输出+10%
            { Buff_2282, 1.08f }, // 鼓励 输出+8% 受伤-8%
            { Buff_4479, 1.25f }, // 勇气 输出+25% 受伤-25%
            { Buff_2131, 1.1f }, // 志氣高揚I 輸出+10%
            { Buff_2132, 1.2f }, // 志氣高揚II 輸出+20%
            { Buff_2133, 1.3f }, // 志氣高揚III 輸出+30%
            { Buff_2134, 1.4f }, // 志氣高揚IV 輸出+40%
            { Buff_2135, 1.5f }, // 志氣高揚V 輸出+50%
            { Buff_1729, 1.5f }, // 坚定不移 輸出+50%
        };

        internal static readonly Dictionary<uint, (float OutputCorrection, float DamageTakenCorrection)>
            JobCorrections = new()
            {
                { 0, (0, 0) },
                { JobPaladin, (-0.1f, -0.5f) },
                { JobWarrior, (-0.1f, -0.55f) },
                { JobDarkKnight, (-0.15f, -0.4f) },
                { JobGunBlade, (0, -0.55f) },
                { JobWhiteMage, (-0.1f, -0.25f) },
                { JobScholar, (-0.1f, -0.3f) },
                { JobAstrologian, (-0.15f, -0.25f) },
                { JobSage, (0, -0.35f) },
                { JobMonk, (0, -0.5f) },
                { JobDragoon, (-0.15f, -0.5f) },
                { JobNinja, (0, -0.45f) },
                { JobSamurai, (-0.1f, -0.5f) },
                { JobReaper, (0, -0.5f) },
                { JobViper, (0, -0.6f) },
                { JobBard, (0, -0.3f) },
                { JobMachinist, (0, -0.3f) },
                { JobDancer, (0, -0.35f) },
                { JobBlackMage, (-0.05f, -0.3f) },
                { JobSummoner, (-0.1f, -0.3f) },
                { JobRedMage, (0, -0.38f) },
                { JobPictoMancer, (-0.1f, -0.3f) },
            };


        internal static readonly List<ushort> Pvp55TerritoryType = new()
        {
            // 斗技学校, 九霄云上, 火山之心
            1032,1033,1034, 
            1058,1059,1060, 
            // 机关大殿
            1116,
            1117,
            // 赤土红沙
            1138,
            1139
        };
        
        internal static readonly List<uint> ForcedMoveInputArgs = new()
        {
            // 0x60 - 0x63
            96, 
            // 97, 沈默, 詠唱依然會被伺服器阻止
            98, 
            99,
            0x3E9, // 1001
            0x3EE, // 1006
            0x3EF, // 1007
            0x3F0 // 1008

        };
        
        [PluginService] internal static ISigScanner Scanner { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IObjectTable GameObjects { get; private set; } = null!;
        [PluginService] internal static IPluginLog Log { get; private set; } = null!;
        [PluginService] public static IChatGui Chat { get; private set; }
        [PluginService] public static IGameGui GameGui { get; private set; } = null!;
        [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
        [PluginService] public static ICondition Condition { get; private set; }
        
        [PluginService] public static IPartyList PartyList { get; private set; }
        

        [PluginService] public static ITargetManager TargetManager { get; private set; } = null!;
        [PluginService] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService] public static IGameNetwork GameNetwork { get; private set; } = null!;
    }
}