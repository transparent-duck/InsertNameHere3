// using Catnip.Modules.PvP.Jobs;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using InsertNameHere3;
using InsertNameHere3.Modules;
using InsertNameHere3.Modules.Debug;
using InsertNameHere3.Modules.Movement;
using InsertNameHere3.Modules.MulDungeon;
using InsertNameHere3.Modules.PvP;
using InsertNameHere3.Modules.PvP.Jobs;

namespace InsertNameHere3.Modules
{
    public class ModuleManager : IDisposable
    {
        private readonly List<IPvPModule> _pvpModules = new();
        private readonly List<object> _allModules = new();
        
        // Core modules
        public PvPCombatModule PvPCombat { get; private set; }
        public PvPTargetingModule PvPTargeting { get; private set; }
        public PvPSelfProtect PvPAutoProtection { get; private set; }
        public MovementModule Movement { get; private set; }
        public MulDungeonModule MulDungeon { get; private set; }
        public DebugModule Debug { get; private set; }
        public TargetIconModule TargetIcon { get; private set; }
        public EnemyWatcherModule EnemyWatcher { get; private set; }
        public OccultCrescentModule OccultCrescent { get; private set; }
        
        // Job-specific modules
        public MachinistPvPModule MachinistPvP { get; private set; }
        public NinjaPvPModule NinjaPvP { get; private set; }
        public SamuraiPvPModule SamuraiPvP { get; private set; }
        public ScholarPvPModule ScholarPvP { get; private set; }
        public WarriorPvPModule WarriorPvP { get; private set; }
        public DarkKnightPvPModule DarkKnightPvP { get; private set; }
        public MonkPvPModule MonkPvP { get; private set; }
        public DragoonPvPModule DragoonPvP { get; private set; }
        public ReaperPvPModule ReaperPvP { get; private set; }
        public ViperPvPModule ViperPvP { get; private set; }
        public BardPvPModule BardPvP { get; private set; }

        private readonly Configuration _configuration;
        private readonly ActionTracker _actionTracker;

        public ModuleManager(Configuration configuration, ActionTracker actionTracker)
        {
            _configuration = configuration;
            _actionTracker = actionTracker;
            InitializeModules();
        }

        private void InitializeModules()
        {
            // Initialize core modules
            PvPCombat = new PvPCombatModule(_configuration);
            PvPTargeting = new PvPTargetingModule(PvPCombat, _configuration);
            PvPAutoProtection = new PvPSelfProtect(PvPCombat, _configuration);
            Movement = new MovementModule(_configuration);
            MulDungeon = new MulDungeonModule(_configuration);
            Debug = new DebugModule(_configuration, PvPCombat);
            TargetIcon = new TargetIconModule(_configuration, PvPCombat);
            EnemyWatcher = new EnemyWatcherModule(_configuration, PvPCombat);
            OccultCrescent = new OccultCrescentModule(_configuration);

            // Initialize job-specific modules with ActionTracker
            MachinistPvP = new MachinistPvPModule(PvPCombat, _configuration, _actionTracker);
            NinjaPvP = new NinjaPvPModule(PvPCombat, _configuration, _actionTracker);
            SamuraiPvP = new SamuraiPvPModule(PvPCombat, _configuration, _actionTracker);
            ScholarPvP = new ScholarPvPModule(PvPCombat, _configuration, _actionTracker);
            WarriorPvP = new WarriorPvPModule(PvPCombat, _configuration, _actionTracker);
            DarkKnightPvP = new DarkKnightPvPModule(PvPCombat, _configuration, _actionTracker);
            MonkPvP = new MonkPvPModule(PvPCombat, _configuration, _actionTracker);
            DragoonPvP = new DragoonPvPModule(PvPCombat, _configuration, _actionTracker);
            ReaperPvP = new ReaperPvPModule(PvPCombat, _configuration, _actionTracker);
            ViperPvP = new ViperPvPModule(PvPCombat, _configuration, _actionTracker);
            BardPvP = new BardPvPModule(PvPCombat, _configuration, _actionTracker);

            // Add to collections for management
            _pvpModules.AddRange(new IPvPModule[] 
            { 
                PvPCombat, PvPTargeting, PvPAutoProtection, TargetIcon, EnemyWatcher,
                MachinistPvP, NinjaPvP, SamuraiPvP, ScholarPvP, WarriorPvP, DarkKnightPvP,
                MonkPvP, DragoonPvP, ReaperPvP, ViperPvP, BardPvP
            });
            
            _allModules.AddRange(new object[] 
            { 
                PvPCombat, PvPTargeting, PvPAutoProtection, Movement, MulDungeon, Debug, TargetIcon, EnemyWatcher, OccultCrescent,
                MachinistPvP, NinjaPvP, SamuraiPvP, ScholarPvP, WarriorPvP, DarkKnightPvP,
                MonkPvP, DragoonPvP, ReaperPvP, ViperPvP, BardPvP
            });

            // Initialize all modules
            foreach (var module in _pvpModules)
            {
                module.Initialize();
            }
            
            Movement.Initialize();
            MulDungeon.Initialize();
            Debug.Initialize();
            OccultCrescent.Initialize();
        }

        public void Update(IFramework framework)
        {
            try
            {
                // Update PvP modules
                foreach (var module in _pvpModules)
                {
                    module.Update(framework);
                }
                
                // Update other core modules
                Movement.Update(framework);
                // MulDungeon.Update(framework);
                Debug.Update(framework);
                OccultCrescent.Update(framework);
            }
            catch (Exception ex)
            {
                Service.Log.Error($"Error in ModuleManager.Update: {ex}");
            }
        }
        
        public void Dispose()
        {
            foreach (var module in _pvpModules)
            {
                try
                {
                    module.Dispose();
                }
                catch (Exception ex)
                {
                    Service.Log.Error($"Error disposing PvP module: {ex}");
                }
            }

            try
            {
                Movement?.Dispose();
                MulDungeon?.Dispose();
                Debug?.Dispose();
                OccultCrescent?.Dispose();
                _actionTracker?.Dispose();
            }
            catch (Exception ex)
            {
                Service.Log.Error($"Error disposing core modules: {ex}");
            }

            _pvpModules.Clear();
            _allModules.Clear();
        }
    }
}
