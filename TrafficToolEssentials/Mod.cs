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

    public static readonly ILog m_Log = LogManager.GetLogger($"{m_Id}.{nameof(Mod)}").SetShowsErrorsInUI(false);

    public static C2VM.TrafficToolEssentials.Settings m_Settings;

    public static World m_World;

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
    /// Repairs corrupt .coc files by detecting and deleting them.
    /// The game will recreate them with default values on next save.
    /// A .coc file is considered corrupt if:
    /// - It's smaller than 50 bytes (minimum valid size)
    /// - It doesn't start with a valid identifier
    /// - It contains invalid characters suggesting truncation
    /// </summary>
    private static void RepairCorruptCocFile()
    {
        try
        {
            string localLowPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            localLowPath = System.IO.Path.Combine(System.IO.Directory.GetParent(localLowPath).FullName, "LocalLow");
            string cs2Path = System.IO.Path.Combine(localLowPath, "Colossal Order", "Cities Skylines II");

            // Check both possible .coc file locations
            string[] cocFiles = new string[]
            {
                System.IO.Path.Combine(cs2Path, "C2VM-TrafficToolEssentials.coc"),
                System.IO.Path.Combine(cs2Path, "C2VM-TrafficLightsEnhancement.coc"),
                System.IO.Path.Combine(cs2Path, "ModsSettings", "C2VM.TrafficToolEssentials", "Settings.coc"),
                System.IO.Path.Combine(cs2Path, "ModsSettings", "C2VM.TrafficLightsEnhancement", "Settings.coc")
            };

            foreach (string cocFile in cocFiles)
            {
                if (System.IO.File.Exists(cocFile))
                {
                    try
                    {
                        System.IO.FileInfo fileInfo = new System.IO.FileInfo(cocFile);
                        
                        // Check 1: File too small (corrupt/truncated)
                        // A valid .coc file should be at least 50 bytes
                        if (fileInfo.Length < 50)
                        {
                            System.IO.File.Delete(cocFile);
                            continue;
                        }

                        // Check 2: Read and validate content
                        string content = System.IO.File.ReadAllText(cocFile);
                        
                        // Check 2a: Content too short
                        if (content.Length < 30)
                        {
                            System.IO.File.Delete(cocFile);
                            continue;
                        }

                        // Check 2b: Doesn't contain expected structure
                        // Valid .coc files should have braces {} somewhere
                        if (!content.Contains("{") || !content.Contains("}"))
                        {
                            System.IO.File.Delete(cocFile);
                            continue;
                        }

                        // Check 2c: Truncated content (ends abruptly without closing brace)
                        string trimmed = content.TrimEnd();
                        if (!trimmed.EndsWith("}") && !trimmed.EndsWith("\"") && !trimmed.EndsWith(";"))
                        {
                            // File appears truncated - might be corrupt
                            // Only delete if it's very short or clearly broken
                            if (content.Length < 100 || content.Split('{').Length != content.Split('}').Length)
                            {
                                System.IO.File.Delete(cocFile);
                                continue;
                            }
                        }
                    }
                    catch
                    {
                        // If we can't read the file, it's probably corrupt - delete it
                        try { System.IO.File.Delete(cocFile); } catch { }
                    }
                }
            }
        }
        catch
        {
            // Silent fail - we don't want to crash the game
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
        m_Log.Info($"Loading {m_Id} v{m_InformationalVersion}");

        // Legacy cleanup (for any remaining old files) - this is now a backup
        CleanupLegacyFiles();

        var outdatedType = System.Type.GetType("C2VM.TrafficToolEssentials.Plugin, C2VM.TrafficToolEssentials") ?? System.Type.GetType("C2VM.CommonLibraries.LaneSystem.Plugin, C2VM.CommonLibraries.LaneSystem");
        if (outdatedType != null)
        {
            throw new System.Exception($"An outdated version of Traffic Lights Enhancement has been detected at {outdatedType.Assembly.Location}");
        }

        if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
        {
            m_Log.Info($"Current mod asset at {asset.path}");
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
        m_Log.Info(nameof(OnDispose));
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
                m_Log.Info($"Deleted legacy cache file: {oldCocFile}");
            }

            // Delete old ModsSettings folder if it exists
            string oldSettingsFolder = System.IO.Path.Combine(cs2Path, "ModsSettings", "C2VM.TrafficLightsEnhancement");
            if (System.IO.Directory.Exists(oldSettingsFolder))
            {
                System.IO.Directory.Delete(oldSettingsFolder, true);
                m_Log.Info($"Deleted legacy settings folder: {oldSettingsFolder}");
            }
        }
        catch (System.Exception ex)
        {
            m_Log.Warn($"Failed to clean up legacy files: {ex.Message}");
        }
    }

    public void SystemSetup(UpdateSystem updateSystem)
    {
        m_World.GetOrCreateSystemManaged<Game.Tools.NetToolSystem>(); // Ensure NetToolSystem is created before our tool

        var noneList = new NativeList<ComponentType>(1, Allocator.Temp);
        noneList.Add(ComponentType.ReadOnly<Components.CustomTrafficLights>());

        Utils.EntityQueryUtils.UpdateEntityQuery(m_TrafficLightInitializationSystem, "m_TrafficLightsQuery", noneList);
        Utils.EntityQueryUtils.UpdateEntityQuery(m_TrafficLightSystem, "m_TrafficLightQuery", noneList);

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

        m_Log.Info($"Compatibility mode is set to {enable}.");
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

