using System;
using System.Linq;
using Lumina.Excel.Sheets;

namespace InsertNameHere3.utils;

public static class LuminaDebug
{
    /// <summary>
    /// Debug method to print all job information from the ClassJob sheet
    /// </summary>
    public static void PrintAllJobsInfo()
    {
        try
        {
            Service.Log.Information("=== Lumina Debug: All Jobs Information ===");
            
            var jobSheet = LuminaReader.Get<ClassJob>();
            if (jobSheet == null)
            {
                Service.Log.Error("Failed to get ClassJob sheet");
                return;
            }

            Service.Log.Information($"ClassJob sheet has {jobSheet.Count()} entries");

            foreach (var job in jobSheet)
            {
                if (job.RowId == 0) continue; // Skip empty row
                
                Service.Log.Information($"Job ID: {job.RowId}");
                Service.Log.Information($"  Name: {job.Name}");
                Service.Log.Information($"  Abbreviation: {job.Abbreviation}");
                Service.Log.Information($"  Category: {job.ClassJobCategory.Value.Name.ToString()}");
                Service.Log.Information($"  Role: {job.Role}");
                Service.Log.Information($"  Job Index: {job.JobIndex}");
                Service.Log.Information($"  Primary Stat: {job.PrimaryStat}");
                Service.Log.Information($"  Modifier HP: {job.ModifierHitPoints}");
                Service.Log.Information($"  Modifier MP: {job.ModifierManaPoints}");
                Service.Log.Information($"  Can Queue for Duty: {job.CanQueueForDuty}");
                Service.Log.Information($"  Is Limited Job: {job.IsLimitedJob}");
                Service.Log.Information("  ---");
            }
            
            Service.Log.Information("=== End Jobs Information ===");
        }
        catch (Exception ex)
        {
            Service.Log.Error($"Error in PrintAllJobsInfo: {ex}");
        }
    }

    /// <summary>
    /// Debug method to demonstrate how to get specific job by ID
    /// </summary>
    public static void PrintJobById(uint jobId)
    {
        try
        {
            Service.Log.Information($"=== Lumina Debug: Job ID {jobId} ===");
            
            var job = LuminaReader.GetRow<ClassJob>(jobId);
            if (job == null)
            {
                Service.Log.Warning($"Job with ID {jobId} not found");
                return;
            }

            Service.Log.Information($"Found job: {job.Value.Name} ({job.Value.Abbreviation})");
            Service.Log.Information($"Category: {job.Value.ClassJobCategory.Value.Name.ToString()}");
            Service.Log.Information($"Role: {job.Value.Role}");
        }
        catch (Exception ex)
        {
            Service.Log.Error($"Error in PrintJobById: {ex}");
        }
    }

    /// <summary>
    /// Debug method to show how sheet filtering works
    /// </summary>
    public static void PrintCombatJobs()
    {
        try
        {
            Service.Log.Information("=== Lumina Debug: Combat Jobs Only ===");
            
            var jobSheet = LuminaReader.Get<ClassJob>();
            var combatJobs = jobSheet.Where(job => job.RowId > 0 && job.CanQueueForDuty);

            foreach (var job in combatJobs)
            {
                Service.Log.Information($"{job.Name} (ID: {job.RowId}) - Role: {job.Role}");
            }
            
            Service.Log.Information("=== End Combat Jobs ===");
        }
        catch (Exception ex)
        {
            Service.Log.Error($"Error in PrintCombatJobs: {ex}");
        }
    }

    /// <summary>
    /// Debug method to demonstrate subrow sheets (example with Quest)
    /// </summary>
    public static void PrintQuestExample(uint questId = 65536)
    {
        try
        {
            Service.Log.Information($"=== Lumina Debug: Quest Example (ID: {questId}) ===");
            
            var quest = LuminaReader.GetRow<Quest>(questId);
            if (quest == null)
            {
                Service.Log.Warning($"Quest with ID {questId} not found");
                return;
            }

            Service.Log.Information($"Quest Name: {quest.Value.Name}");
            Service.Log.Information($"Quest ID: {quest.Value.Id}");
            Service.Log.Information($"Level: {quest.Value.ClassJobLevel}");
            Service.Log.Information($"Required Job: {quest.Value.ClassJobRequired.Value.Name.ToString()}");
            
        }
        catch (Exception ex)
        {
            Service.Log.Error($"Error in PrintQuestExample: {ex}");
        }
    }

    /// <summary>
    /// Debug method to show sheet statistics
    /// </summary>
    public static void PrintSheetStats()
    {
        try
        {
            Service.Log.Information("=== Lumina Debug: Sheet Statistics ===");
            
            // ClassJob sheet
            var jobSheet = LuminaReader.Get<ClassJob>();
            var validJobs = jobSheet.Where(j => j.RowId > 0).Count();
            Service.Log.Information($"ClassJob: {jobSheet.Count()} total, {validJobs} valid entries");

            // Action sheet
            var actionSheet = LuminaReader.Get<Lumina.Excel.Sheets.Action>();
            var validActions = actionSheet.Where(a => a.RowId > 0 && !string.IsNullOrEmpty(a.Name.ToString())).Count();
            Service.Log.Information($"Action: {actionSheet.Count()} total, {validActions} named actions");

            // Item sheet
            var itemSheet = LuminaReader.Get<Item>();
            var validItems = itemSheet.Where(i => i.RowId > 0 && !string.IsNullOrEmpty(i.Name.ToString())).Count();
            Service.Log.Information($"Item: {itemSheet.Count()} total, {validItems} named items");

            Service.Log.Information("=== End Sheet Statistics ===");
        }
        catch (Exception ex)
        {
            Service.Log.Error($"Error in PrintSheetStats: {ex}");
        }
    }

    /// <summary>
    /// Comprehensive debug method that runs all debug functions
    /// </summary>
    public static void RunAllDebugMethods()
    {
        Service.Log.Information("Starting comprehensive Lumina debug session...");
        
        PrintSheetStats();
        PrintAllJobsInfo();
        PrintCombatJobs();
        PrintJobById(1); // Gladiator
        PrintJobById(19); // Paladin  
        PrintJobById(25); // Arcanist
        PrintQuestExample();
        
        Service.Log.Information("Lumina debug session completed!");
    }
}
