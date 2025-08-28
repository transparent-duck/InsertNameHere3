using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;

namespace InsertNameHere3.utils
{
    /// <summary>
    /// Helper class to manage job data using Lumina instead of hardcoded constants
    /// </summary>
    public static class JobHelper
    {
        private static Dictionary<string, uint>? _jobIdByAbbreviation;
        private static Dictionary<uint, ClassJob>? _jobDataCache;

        // Lazy-initialized job sets built from GetJobIdByAbbreviation
        private static HashSet<uint>? _tankJobIds;
        private static HashSet<uint>? _healerJobIds;
        private static HashSet<uint>? _meleeDpsJobIds;
        private static HashSet<uint>? _rangedDpsJobIds;
        private static HashSet<uint>? _casterJobIds;
        private static HashSet<uint>? _crafterJobIds;
        private static HashSet<uint>? _gathererJobIds;

        private static HashSet<uint> TankJobIds => _tankJobIds ??= new HashSet<uint>
        {
            GetJobIdByAbbreviation("PLD"), // Paladin  
            GetJobIdByAbbreviation("WAR"), // Warrior
            GetJobIdByAbbreviation("DRK"), // Dark Knight
            GetJobIdByAbbreviation("GNB")  // Gunbreaker
        };

        private static HashSet<uint> HealerJobIds => _healerJobIds ??= new HashSet<uint>
        {
            GetJobIdByAbbreviation("WHM"), // White Mage
            GetJobIdByAbbreviation("SCH"), // Scholar
            GetJobIdByAbbreviation("AST"), // Astrologian
            GetJobIdByAbbreviation("SGE")  // Sage
        };

        private static HashSet<uint> MeleeDpsJobIds => _meleeDpsJobIds ??= new HashSet<uint>
        {
            GetJobIdByAbbreviation("PGL"), // Pugilist
            GetJobIdByAbbreviation("MNK"), // Monk
            GetJobIdByAbbreviation("LNC"), // Lancer
            GetJobIdByAbbreviation("DRG"), // Dragoon
            GetJobIdByAbbreviation("ROG"), // Rogue
            GetJobIdByAbbreviation("NIN"), // Ninja
            GetJobIdByAbbreviation("SAM"), // Samurai
            GetJobIdByAbbreviation("RPR"), // Reaper
            GetJobIdByAbbreviation("VPR")  // Viper
        };

        private static HashSet<uint> RangedDpsJobIds => _rangedDpsJobIds ??= new HashSet<uint>
        {
            GetJobIdByAbbreviation("ARC"), // Archer (base class)
            GetJobIdByAbbreviation("BRD"), // Bard
            GetJobIdByAbbreviation("MCH"), // Machinist
            GetJobIdByAbbreviation("DNC")  // Dancer
        };

        private static HashSet<uint> CasterJobIds => _casterJobIds ??= new HashSet<uint>
        {
            GetJobIdByAbbreviation("THM"), // Thaumaturge
            GetJobIdByAbbreviation("BLM"), // Black Mage
            GetJobIdByAbbreviation("ACN"), // Arcanist
            GetJobIdByAbbreviation("SMN"), // Summoner
            GetJobIdByAbbreviation("RDM"), // Red Mage
            GetJobIdByAbbreviation("PCT")  // Pictomancer
        };

        private static HashSet<uint> CrafterJobIds => _crafterJobIds ??= new HashSet<uint>
        {
            GetJobIdByAbbreviation("CRP"), // Carpenter
            GetJobIdByAbbreviation("BSM"), // Blacksmith
            GetJobIdByAbbreviation("ARM"), // Armorer
            GetJobIdByAbbreviation("GSM"), // Goldsmith
            GetJobIdByAbbreviation("LTW"), // Leatherworker
            GetJobIdByAbbreviation("WVR"), // Weaver
            GetJobIdByAbbreviation("ALC"), // Alchemist
            GetJobIdByAbbreviation("CUL")  // Culinarian
        };

        private static HashSet<uint> GathererJobIds => _gathererJobIds ??= new HashSet<uint>
        {
            GetJobIdByAbbreviation("MIN"), // Miner
            GetJobIdByAbbreviation("BTN"), // Botanist
            GetJobIdByAbbreviation("FSH")  // Fisher
        };

        /// <summary>
        /// Initialize job data cache from Lumina (only for abbreviation lookups)
        /// </summary>
        public static void Initialize()
        {
            var jobSheet = LuminaReader.Get<ClassJob>();
            
            _jobDataCache = new Dictionary<uint, ClassJob>();
            _jobIdByAbbreviation = new Dictionary<string, uint>();

            foreach (var job in jobSheet.Where(j => j.RowId > 0))
            {
                _jobDataCache[job.RowId] = job;
                _jobIdByAbbreviation[job.Abbreviation.ToString()] = job.RowId;
            }
        }

        /// <summary>
        /// Get job ID by abbreviation (e.g., "PLD" -> 19)
        /// </summary>
        public static uint GetJobIdByAbbreviation(string abbreviation)
        {
            if (_jobIdByAbbreviation == null) Initialize();
            return _jobIdByAbbreviation!.TryGetValue(abbreviation, out var jobId) ? jobId : 0;
        }

        /// <summary>
        /// Get job data by ID
        /// </summary>
        public static ClassJob? GetJobData(uint jobId)
        {
            if (_jobDataCache == null) Initialize();
            return _jobDataCache!.TryGetValue(jobId, out var job) ? job : null;
        }

        /// <summary>
        /// Check if a job is a tank
        /// </summary>
        public static bool IsTank(uint jobId)
        {
            return TankJobIds.Contains(jobId);
        }

        /// <summary>
        /// Check if a job is a healer
        /// </summary>
        public static bool IsHealer(uint jobId)
        {
            return HealerJobIds.Contains(jobId);
        }

        /// <summary>
        /// Check if a job is a melee DPS
        /// </summary>
        public static bool IsMeleeDps(uint jobId)
        {
            return MeleeDpsJobIds.Contains(jobId);
        }

        /// <summary>
        /// Check if a job is a ranged DPS
        /// </summary>
        public static bool IsRangedDps(uint jobId)
        {
            return RangedDpsJobIds.Contains(jobId);
        }

        /// <summary>
        /// Check if a job is a caster
        /// </summary>
        public static bool IsCaster(uint jobId)
        {
            return CasterJobIds.Contains(jobId);
        }

        /// <summary>
        /// Check if a job is any type of DPS (melee, ranged, or caster)
        /// </summary>
        public static bool IsDps(uint jobId)
        {
            return MeleeDpsJobIds.Contains(jobId) || 
                   RangedDpsJobIds.Contains(jobId) || 
                   CasterJobIds.Contains(jobId);
        }

        /// <summary>
        /// Check if a job is a crafter
        /// </summary>
        public static bool IsCrafter(uint jobId)
        {
            return CrafterJobIds.Contains(jobId);
        }

        /// <summary>
        /// Check if a job is a gatherer
        /// </summary>
        public static bool IsGatherer(uint jobId)
        {
            return GathererJobIds.Contains(jobId);
        }

        /// <summary>
        /// Get all tank job IDs
        /// </summary>
        public static IEnumerable<uint> GetTankJobs()
        {
            return TankJobIds;
        }

        /// <summary>
        /// Get all healer job IDs
        /// </summary>
        public static IEnumerable<uint> GetHealerJobs()
        {
            return HealerJobIds;
        }

        /// <summary>
        /// Get all melee DPS job IDs
        /// </summary>
        public static IEnumerable<uint> GetMeleeDpsJobs()
        {
            return MeleeDpsJobIds;
        }

        /// <summary>
        /// Get all ranged DPS job IDs
        /// </summary>
        public static IEnumerable<uint> GetRangedDpsJobs()
        {
            return RangedDpsJobIds;
        }

        /// <summary>
        /// Get all caster job IDs
        /// </summary>
        public static IEnumerable<uint> GetCasterJobs()
        {
            return CasterJobIds;
        }

        /// <summary>
        /// Get all DPS job IDs (melee, ranged, and caster combined)
        /// </summary>
        public static IEnumerable<uint> GetDpsJobs()
        {
            return MeleeDpsJobIds.Concat(RangedDpsJobIds).Concat(CasterJobIds);
        }

        /// <summary>
        /// Get all combat job IDs
        /// </summary>
        public static IEnumerable<uint> GetCombatJobs()
        {
            return TankJobIds.Concat(HealerJobIds).Concat(GetDpsJobs());
        }

        // Static properties for commonly used job IDs (backwards compatibility)
        public static uint JobPaladin => GetJobIdByAbbreviation("PLD");
        public static uint JobWarrior => GetJobIdByAbbreviation("WAR");
        public static uint JobDarkKnight => GetJobIdByAbbreviation("DRK");
        public static uint JobGunBlade => GetJobIdByAbbreviation("GNB");
        public static uint JobWhiteMage => GetJobIdByAbbreviation("WHM");
        public static uint JobScholar => GetJobIdByAbbreviation("SCH");
        public static uint JobAstrologian => GetJobIdByAbbreviation("AST");
        public static uint JobSage => GetJobIdByAbbreviation("SGE");
        public static uint JobMonk => GetJobIdByAbbreviation("MNK");
        public static uint JobDragoon => GetJobIdByAbbreviation("DRG");
        public static uint JobNinja => GetJobIdByAbbreviation("NIN");
        public static uint JobSamurai => GetJobIdByAbbreviation("SAM");
        public static uint JobReaper => GetJobIdByAbbreviation("RPR");
        public static uint JobViper => GetJobIdByAbbreviation("VPR");
        public static uint JobBard => GetJobIdByAbbreviation("BRD");
        public static uint JobMachinist => GetJobIdByAbbreviation("MCH");
        public static uint JobDancer => GetJobIdByAbbreviation("DNC");
        public static uint JobBlackMage => GetJobIdByAbbreviation("BLM");
        public static uint JobSummoner => GetJobIdByAbbreviation("SMN");
        public static uint JobRedMage => GetJobIdByAbbreviation("RDM");
        public static uint JobPictoMancer => GetJobIdByAbbreviation("PCT");

        /// <summary>
        /// Jobs that have hollow/invulnerability guards (tanks with invulns)
        /// </summary>
        public static uint[] JobsWhoHaveHollowGuards => [JobPaladin, JobDarkKnight];
    }
}
