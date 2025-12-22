using System.Reflection;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficToolEssentials;

public class Mod : IMod
{
    public static readonly string m_Id = typeof(Mod).Assembly.GetName().Name;

    public static readonly string m_InformationalVersion = ((AssemblyInformationalVersionAttribute)System.Attribute.GetCustomAttribute(Assembly.GetAssembly(typeof(Mod)), typeof(AssemblyInformationalVersionAttribute))).InformationalVersion;

    // V283: Logger renamed from "C2VM.TrafficToolEssentials.Mod" to "TrafficToolEssentials"
    // Log file will now be: TrafficToolEssentials.log
    public static readonly ILog m_Log = LogManager.GetLogger("TrafficToolEssentials").SetShowsErrorsInUI(false);

    public static C2VM.TrafficToolEssentials.Settings m_Settings;

    public static World m_World;

    // V283: Debug logging helper - only logs when debug setting is enabled
    // Use this for frequent/verbose logging that would impact performance
    public static bool IsDebugLoggingEnabled => m_Settings?.m_DebugLogging == true;
    
    /// <summary>
    /// V283: Log debug message - only when debug logging is enabled in settings.
    /// Use for: sync details, state changes, frame-by-frame info, verbose diagnostics
    /// </summary>
    public static void LogDebug(string message)
    {
        if (IsDebugLoggingEnabled)
            m_Log.Info(message);
    }
    
    /// <summary>
    /// V283: Log info message - always logs regardless of debug setting.
    /// Use for: important lifecycle events, user actions, one-time initializations
    /// </summary>
    public static void LogInfo(string message)
    {
        m_Log.Info(message);
    }
    
    /// <summary>
    /// V283: Log warning message - always logs.
    /// Use for: recoverable errors, unexpected but handled situations
    /// </summary>
    public static void LogWarn(string message)
    {
        m_Log.Warn(message);
    }
    
    /// <summary>
    /// V283: Log error message - always logs.
    /// Use for: errors, exceptions, failed operations
    /// </summary>
    public static void LogError(string message)
    {
        m_Log.Error(message);
    }

    private static Game.Net.TrafficLightInitializationSystem m_TrafficLightInitializationSystem;

    private static Game.Simulation.TrafficLightSystem m_TrafficLightSystem;

    private static C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem m_PatchedTrafficLightInitializationSystem;

    private static C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Simulation.PatchedTrafficLightSystem m_PatchedTrafficLightSystem;

    /// <summary>
    /// Static constructor - runs as early as possible when the assembly is loaded.
    /// This attempts to migrate/fix the old .coc file BEFORE the game tries to parse it.
    /// </summary>
    static Mod()
    {
        RepairCorruptCocFile();
        DeleteLegacyCocFile();
    }

    /// <summary>
    /// V236: Repairs corrupt .coc files by OVERWRITING them with valid defaults.
    /// This runs in static constructor - but the game may have already cached the file.
    /// After this fix, a RESTART will load the valid file.
    /// </summary>
    private static void RepairCorruptCocFile()
    {
        string logPath = null;
        try
        {
            string localLowPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            localLowPath = System.IO.Path.Combine(System.IO.Directory.GetParent(localLowPath).FullName, "LocalLow");
            string cs2Path = System.IO.Path.Combine(localLowPath, "Colossal Order", "Cities Skylines II");
            
            // V236 DEBUG: Write a log file to verify this code runs
            logPath = System.IO.Path.Combine(cs2Path, "TTE_COC_Recovery_Debug.log");
            System.IO.File.AppendAllText(logPath, $"\n[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] === RepairCorruptCocFile() V236 START ===\n");

            // Main settings file
            string settingsDir = System.IO.Path.Combine(cs2Path, "ModsSettings", "C2VM.TrafficToolEssentials");
            string mainCocFile = System.IO.Path.Combine(settingsDir, "Settings.coc");
            
            System.IO.File.AppendAllText(logPath, $"  Checking: {mainCocFile}\n");
            
            if (System.IO.File.Exists(mainCocFile))
            {
                System.IO.File.AppendAllText(logPath, $"    EXISTS - checking if corrupt...\n");
                
                bool isCorrupt = IsFilePotentiallyCorrupt(mainCocFile, logPath);
                
                if (isCorrupt)
                {
                    System.IO.File.AppendAllText(logPath, $"    CORRUPT! Overwriting with valid default...\n");
                    
                    // V236: OVERWRITE with valid COC content (not just delete!)
                    // This ensures the file is valid for the next game start
                    string validCocContent = GetValidDefaultCocContent();
                    
                    try 
                    { 
                        System.IO.File.WriteAllText(mainCocFile, validCocContent);
                        System.IO.File.AppendAllText(logPath, $"    OVERWRITTEN with valid content ({validCocContent.Length} bytes)\n");
                        System.IO.File.AppendAllText(logPath, $"    NOTE: Game may still show error THIS session (cached old version)\n");
                        System.IO.File.AppendAllText(logPath, $"    RESTART the game and it should work!\n");
                    }
                    catch (System.Exception ex)
                    {
                        System.IO.File.AppendAllText(logPath, $"    OVERWRITE FAILED: {ex.Message}\n");
                    }
                }
                else
                {
                    System.IO.File.AppendAllText(logPath, $"    OK - file appears valid\n");
                }
            }
            else
            {
                System.IO.File.AppendAllText(logPath, $"    Does not exist - creating default...\n");
                
                // V236: Create directory and file if it doesn't exist
                try
                {
                    if (!System.IO.Directory.Exists(settingsDir))
                    {
                        System.IO.Directory.CreateDirectory(settingsDir);
                    }
                    System.IO.File.WriteAllText(mainCocFile, GetValidDefaultCocContent());
                    System.IO.File.AppendAllText(logPath, $"    Created default settings file\n");
                }
                catch (System.Exception ex)
                {
                    System.IO.File.AppendAllText(logPath, $"    CREATE FAILED: {ex.Message}\n");
                }
            }

            // Delete legacy files
            string[] legacyCocFiles = new string[]
            {
                System.IO.Path.Combine(cs2Path, "C2VM-TrafficToolEssentials.coc"),
                System.IO.Path.Combine(cs2Path, "C2VM-TrafficLightsEnhancement.coc"),
                System.IO.Path.Combine(cs2Path, "ModsSettings", "C2VM.TrafficLightsEnhancement", "Settings.coc")
            };

            foreach (string cocFile in legacyCocFiles)
            {
                if (System.IO.File.Exists(cocFile))
                {
                    try { System.IO.File.Delete(cocFile); } catch { }
                }
            }
            
            System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] === RepairCorruptCocFile() V236 END ===\n");
        }
        catch (System.Exception ex)
        {
            if (logPath != null)
            {
                try { System.IO.File.AppendAllText(logPath, $"EXCEPTION: {ex}\n"); } catch { }
            }
        }
    }
    
    /// <summary>
    /// V236: Returns valid default COC content for Settings.
    /// This is the minimal valid format that the game can parse.
    /// </summary>
    private static string GetValidDefaultCocContent()
    {
        // COC format is similar to JSON but with some differences
        // The $type field tells the game which class to deserialize to
        return @"{
	""$type"": ""C2VM.TrafficToolEssentials.Settings, C2VM.TrafficToolEssentials"",
	""m_Locale"": ""auto"",
	""m_CompatibilityMode"": false,
	""m_DefaultSplitPhasing"": false,
	""m_DefaultAlwaysGreenKerbsideTurn"": false,
	""m_DefaultExclusivePedestrian"": false,
	""m_DebugLogging"": false,
	""m_SuppressCanaryWarningVersion"": """"
}";
    }
    
    /// <summary>
    /// V236: Checks if a .coc file is potentially corrupt.
    /// </summary>
    private static bool IsFilePotentiallyCorrupt(string filePath, string logPath = null)
    {
        try
        {
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(filePath);
            
            // Check 1: File too small (valid .coc files are typically 200+ bytes)
            if (fileInfo.Length < 50)
            {
                if (logPath != null) System.IO.File.AppendAllText(logPath, $"      Reason: File too small ({fileInfo.Length} bytes)\n");
                return true;
            }
            
            // Check 2: Read and validate content
            string content = System.IO.File.ReadAllText(filePath);
            
            // Check 2a: Content too short
            if (content.Length < 30)
            {
                if (logPath != null) System.IO.File.AppendAllText(logPath, $"      Reason: Content too short ({content.Length} chars)\n");
                return true;
            }
            
            // Check 2b: Doesn't contain expected structure
            if (!content.Contains("{") || !content.Contains("}"))
            {
                if (logPath != null) System.IO.File.AppendAllText(logPath, $"      Reason: No braces found\n");
                return true;
            }
            
            // Check 2c: Unbalanced braces (most common sign of truncation/corruption)
            int openBraces = 0;
            int closeBraces = 0;
            foreach (char c in content)
            {
                if (c == '{') openBraces++;
                if (c == '}') closeBraces++;
            }
            if (openBraces != closeBraces)
            {
                if (logPath != null) System.IO.File.AppendAllText(logPath, $"      Reason: Unbalanced braces (open: {openBraces}, close: {closeBraces})\n");
                return true;
            }
            
            // Check 2d: Doesn't end with closing brace (truncated file)
            string trimmed = content.TrimEnd();
            if (!trimmed.EndsWith("}"))
            {
                if (logPath != null) System.IO.File.AppendAllText(logPath, $"      Reason: File doesn't end with }}\n");
                return true;
            }
            
            if (logPath != null) System.IO.File.AppendAllText(logPath, $"      All checks passed - file is valid\n");
            return false;
        }
        catch (System.Exception ex)
        {
            if (logPath != null) System.IO.File.AppendAllText(logPath, $"      Reason: Exception reading file: {ex.Message}\n");
            // If we can't read it, assume it's corrupt
            return true;
        }
    }

    /// <summary>
    /// Deletes the old C2VM-TrafficLightsEnhancement.coc file.
    /// IMPORTANT: We do NOT migrate the old file! The old TLE format is incompatible
    /// with TTE and causes issues (especially with hotkeys). Just delete and start fresh.
    /// </summary>
    private static void DeleteLegacyCocFile()
    {
        try
        {
            string localLowPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            localLowPath = System.IO.Path.Combine(System.IO.Directory.GetParent(localLowPath).FullName, "LocalLow");
            string cs2Path = System.IO.Path.Combine(localLowPath, "Colossal Order", "Cities Skylines II");

            // Just delete the old TLE .coc file - don't try to migrate it!
            string oldCocFile = System.IO.Path.Combine(cs2Path, "C2VM-TrafficLightsEnhancement.coc");
            if (System.IO.File.Exists(oldCocFile))
            {
                System.IO.File.Delete(oldCocFile);
            }
        }
        catch
        {
            // Silent fail - we don't want to crash the game
        }
    }

    public void OnLoad(UpdateSystem updateSystem)
    {
        LogInfo($"Loading {m_Id} v{m_InformationalVersion}");

        // Legacy cleanup (for any remaining old files) - this is now a backup
        CleanupLegacyFiles();

        // V282: Removed Type.GetType check for outdated versions - was triggering AV false positives
        // The old BepInEx-based "Traffic Lights Enhancement Alpha" is no longer in circulation

        if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
        {
            LogDebug($"Current mod asset at {asset.path}");
        }

        m_World = updateSystem.World;

        m_TrafficLightInitializationSystem = m_World.GetOrCreateSystemManaged<Game.Net.TrafficLightInitializationSystem>();
        m_TrafficLightSystem = m_World.GetOrCreateSystemManaged<Game.Simulation.TrafficLightSystem>();
        m_PatchedTrafficLightInitializationSystem = m_World.GetOrCreateSystemManaged<C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem>();
        m_PatchedTrafficLightSystem = m_World.GetOrCreateSystemManaged<C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Simulation.PatchedTrafficLightSystem>();

        // Green Wave Sync Group System
        m_World.GetOrCreateSystemManaged<C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Simulation.SyncGroupSystem>();

        m_Settings = new Settings(this);

        SystemSetup(updateSystem);

        string netToolSystemToolID = m_World.GetOrCreateSystemManaged<Game.Tools.NetToolSystem>().toolID;
        Assert(netToolSystemToolID == "Net Tool", $"netToolSystemToolID: {netToolSystemToolID}");
    }

    public void OnDispose()
    {
        LogInfo(nameof(OnDispose));
    }

    /// <summary>
    /// Cleans up legacy files from the old mod name (C2VM.TrafficLightsEnhancement).
    /// This is now a backup - the static constructor should have already handled migration.
    /// </summary>
    private void CleanupLegacyFiles()
    {
        
        try
        {
            string localLowPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            localLowPath = System.IO.Path.Combine(System.IO.Directory.GetParent(localLowPath).FullName, "LocalLow");
            string cs2Path = System.IO.Path.Combine(localLowPath, "Colossal Order", "Cities Skylines II");

            // Delete old .coc file if it still exists (migration in static constructor should have handled this)
            string oldCocFile = System.IO.Path.Combine(cs2Path, "C2VM-TrafficLightsEnhancement.coc");
            if (System.IO.File.Exists(oldCocFile))
            {
                System.IO.File.Delete(oldCocFile);
                LogDebug($"Deleted legacy cache file: {oldCocFile}");
            }

            // Delete old ModsSettings folder if it exists
            string oldSettingsFolder = System.IO.Path.Combine(cs2Path, "ModsSettings", "C2VM.TrafficLightsEnhancement");
            if (System.IO.Directory.Exists(oldSettingsFolder))
            {
                System.IO.Directory.Delete(oldSettingsFolder, true);
                LogDebug($"Deleted legacy settings folder: {oldSettingsFolder}");
            }
        }
        catch (System.Exception ex)
        {
            LogWarn($"Failed to clean up legacy files: {ex.Message}");
        }
    }

    public void SystemSetup(UpdateSystem updateSystem)
    {
        m_World.GetOrCreateSystemManaged<Game.Tools.NetToolSystem>(); // Ensure NetToolSystem is created before our tool

        var noneList = new NativeList<ComponentType>(1, Allocator.Temp);
        noneList.Add(ComponentType.ReadOnly<Components.CustomTrafficLights>());

        Utils.EntityQueryUtils.UpdateEntityQuery(m_TrafficLightInitializationSystem, "m_TrafficLightsQuery", noneList);
        Utils.EntityQueryUtils.UpdateEntityQuery(m_TrafficLightSystem, "m_TrafficLightQuery", noneList);
        
        // V205: Dispose noneList after use
        noneList.Dispose();

        updateSystem.UpdateBefore<C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem, Game.Net.TrafficLightInitializationSystem>(SystemUpdatePhase.Modification4B);
        updateSystem.UpdateBefore<C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Simulation.PatchedTrafficLightSystem, Game.Simulation.TrafficLightSystem>(SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<C2VM.TrafficToolEssentials.Systems.UI.TooltipSystem>(SystemUpdatePhase.UITooltip);
        updateSystem.UpdateAt<C2VM.TrafficToolEssentials.Systems.UI.UISystem>(SystemUpdatePhase.UIUpdate);
        updateSystem.UpdateAt<C2VM.TrafficToolEssentials.Systems.Tool.ToolSystem>(SystemUpdatePhase.ToolUpdate);
        updateSystem.UpdateAt<C2VM.TrafficToolEssentials.Systems.Update.ModificationUpdateSystem>(SystemUpdatePhase.ModificationEnd);
        updateSystem.UpdateAfter<C2VM.TrafficToolEssentials.Systems.Update.SimulationUpdateSystem>(SystemUpdatePhase.GameSimulation);

        SetCompatibilityMode(m_Settings != null && m_Settings.m_CompatibilityMode);
    }

    public static void SetCompatibilityMode(bool enable)
    {
        m_TrafficLightInitializationSystem.Enabled = enable;
        m_TrafficLightSystem.Enabled = enable;

        m_PatchedTrafficLightInitializationSystem.SetCompatibilityMode(enable);
        m_PatchedTrafficLightSystem.SetCompatibilityMode(enable);

        LogDebug($"Compatibility mode is set to {enable}.");
    }

    public static bool IsCanary()
    {
        #if SHOW_CANARY_BUILD_WARNING
        return true;
        #else
        return false;
        #endif
    }

    public static void Assert(bool condition, string message = "", bool showInUI = false, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(condition))] string expression = "")
    {
        if (condition == true)
        {
            return;
        }
        bool showsErrorsInUI = m_Log.showsErrorsInUI;
        m_Log.SetShowsErrorsInUI(showInUI);
        m_Log.Error($"Assertion failed!\n{message}\nExpression: {expression}");
        m_Log.SetShowsErrorsInUI(showsErrorsInUI);
    }
}

