using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Colossal.IO.AssetDatabase;

namespace C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Simulation;

/// <summary>
/// Handles persistent storage of sync groups to JSON file.
/// Groups are saved per-city based on the save game name.
/// </summary>
public static class SyncGroupPersistence
{
    private static string GetSaveDirectory()
    {
        // Use the mod's local data directory
        var modPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Colossal Order", "Cities Skylines II", "ModsData", "TrafficToolEssentials"
        );
        
        if (!Directory.Exists(modPath))
        {
            Directory.CreateDirectory(modPath);
        }
        
        return modPath;
    }
    
    private static string GetFilePath(string cityName = "default")
    {
        // Sanitize city name for file system
        var safeName = string.Join("_", cityName.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "default";
        
        return Path.Combine(GetSaveDirectory(), $"syncgroups_{safeName}.json");
    }
    
    /// <summary>
    /// Data structure for JSON serialization
    /// </summary>
    [Serializable]
    public class SyncGroupData
    {
        public uint GroupId { get; set; }
        public string GroupName { get; set; }
        public ushort BaseCycleDuration { get; set; }
        
        // Time window settings (added in v2)
        public bool AlwaysActive { get; set; } = true;
        public byte TimeWindow1Start { get; set; } = 255;
        public byte TimeWindow1End { get; set; } = 255;
        public byte TimeWindow2Start { get; set; } = 255;
        public byte TimeWindow2End { get; set; } = 255;
        public byte TimeWindow3Start { get; set; } = 255;
        public byte TimeWindow3End { get; set; } = 255;
    }
    
    [Serializable]
    public class SyncGroupsFile
    {
        public int Version { get; set; } = 2;  // Updated version for time windows
        public uint NextGroupId { get; set; } = 1;
        public List<SyncGroupData> Groups { get; set; } = new List<SyncGroupData>();
        public DateTime LastSaved { get; set; }
    }
    
    /// <summary>
    /// Saves all sync groups to JSON file
    /// </summary>
    public static bool SaveGroups(List<SyncGroupData> groups, uint nextGroupId, string cityName = "default")
    {
        try
        {
            var data = new SyncGroupsFile
            {
                Version = 1,
                NextGroupId = nextGroupId,
                Groups = groups,
                LastSaved = DateTime.Now
            };
            
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            var filePath = GetFilePath(cityName);
            
            File.WriteAllText(filePath, json);
            
            Mod.LogDebug($"[SyncGroupPersistence] Saved {groups.Count} groups to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Mod.LogError($"[SyncGroupPersistence] Failed to save groups: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Loads sync groups from JSON file
    /// </summary>
    public static SyncGroupsFile LoadGroups(string cityName = "default")
    {
        try
        {
            var filePath = GetFilePath(cityName);
            
            if (!File.Exists(filePath))
            {
                Mod.LogDebug($"[SyncGroupPersistence] No saved groups found at {filePath}");
                return new SyncGroupsFile();
            }
            
            var json = File.ReadAllText(filePath);
            var data = JsonConvert.DeserializeObject<SyncGroupsFile>(json);
            
            if (data == null)
            {
                return new SyncGroupsFile();
            }
            
            Mod.LogDebug($"[SyncGroupPersistence] Loaded {data.Groups?.Count ?? 0} groups from {filePath}");
            return data;
        }
        catch (Exception ex)
        {
            Mod.LogError($"[SyncGroupPersistence] Failed to load groups: {ex.Message}");
            return new SyncGroupsFile();
        }
    }
    
    /// <summary>
    /// Deletes the groups file for a city
    /// </summary>
    public static bool DeleteGroupsFile(string cityName = "default")
    {
        try
        {
            var filePath = GetFilePath(cityName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Mod.LogDebug($"[SyncGroupPersistence] Deleted groups file: {filePath}");
            }
            return true;
        }
        catch (Exception ex)
        {
            Mod.LogError($"[SyncGroupPersistence] Failed to delete groups file: {ex.Message}");
            return false;
        }
    }
}
