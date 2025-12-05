using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficToolEssentials.Components;

/// <summary>
/// Represents a sync group for green wave synchronization.
/// Multiple intersections can reference the same group to synchronize their traffic light cycles.
/// Stored as a buffer on a singleton entity managed by SyncGroupSystem.
/// </summary>
public struct SyncGroup : IBufferElementData, ISerializable
{
    private ushort m_SchemaVersion;

    /// <summary>
    /// Unique identifier for this group. 0 means "no group".
    /// </summary>
    public uint m_GroupId;

    /// <summary>
    /// Display name for this group (max 64 bytes / ~32 chars).
    /// </summary>
    public FixedString64Bytes m_GroupName;

    /// <summary>
    /// Base cycle duration in seconds for this group.
    /// All intersections in the group should ideally have similar cycle durations.
    /// </summary>
    public ushort m_BaseCycleDuration;

    /// <summary>
    /// Runtime timer that increments each frame. Not serialized.
    /// Used to synchronize all intersections in this group.
    /// </summary>
    public uint m_GroupTimer;
    
    /// <summary>
    /// Timestamp (GroupTimer value) when REF gave the GO signal.
    /// Used for offset calculation - entities with offset wait until
    /// (m_GroupTimer - m_GoTimestamp) >= their offset.
    /// Reset to 0 when not in GO phase. Not serialized.
    /// </summary>
    public uint m_GoTimestamp;
    
    // === TIME WINDOWS FOR SCHEDULED ACTIVATION ===
    // Each time window has a start and end hour (0-23), or 255 for "always on"
    // Up to 3 time windows supported (e.g., morning rush, evening rush, night)
    
    /// <summary>
    /// If true, the group is always active. If false, only active during time windows.
    /// </summary>
    public bool m_AlwaysActive;
    
    /// <summary>
    /// Time window 1: Start hour (0-23). 255 = disabled.
    /// </summary>
    public byte m_TimeWindow1Start;
    
    /// <summary>
    /// Time window 1: End hour (0-23). 255 = disabled.
    /// </summary>
    public byte m_TimeWindow1End;
    
    /// <summary>
    /// Time window 2: Start hour (0-23). 255 = disabled.
    /// </summary>
    public byte m_TimeWindow2Start;
    
    /// <summary>
    /// Time window 2: End hour (0-23). 255 = disabled.
    /// </summary>
    public byte m_TimeWindow2End;
    
    /// <summary>
    /// Time window 3: Start hour (0-23). 255 = disabled.
    /// </summary>
    public byte m_TimeWindow3Start;
    
    /// <summary>
    /// Time window 3: End hour (0-23). 255 = disabled.
    /// </summary>
    public byte m_TimeWindow3End;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        m_SchemaVersion = 2;  // Updated schema version for time windows
        writer.Write(m_SchemaVersion);
        writer.Write(m_GroupId);
        // FixedString64Bytes can't be serialized directly - convert to string
        writer.Write(m_GroupName.ToString());
        writer.Write(m_BaseCycleDuration);
        // Time window fields (new in schema v2)
        writer.Write(m_AlwaysActive);
        writer.Write(m_TimeWindow1Start);
        writer.Write(m_TimeWindow1End);
        writer.Write(m_TimeWindow2Start);
        writer.Write(m_TimeWindow2End);
        writer.Write(m_TimeWindow3Start);
        writer.Write(m_TimeWindow3End);
        // m_GroupTimer is NOT serialized - it resets on load
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_SchemaVersion);
        reader.Read(out m_GroupId);
        // Read string and convert to FixedString64Bytes
        reader.Read(out string groupName);
        m_GroupName = new FixedString64Bytes(groupName ?? "");
        reader.Read(out m_BaseCycleDuration);
        
        // Time window fields (schema v2+)
        if (m_SchemaVersion >= 2)
        {
            reader.Read(out m_AlwaysActive);
            reader.Read(out m_TimeWindow1Start);
            reader.Read(out m_TimeWindow1End);
            reader.Read(out m_TimeWindow2Start);
            reader.Read(out m_TimeWindow2End);
            reader.Read(out m_TimeWindow3Start);
            reader.Read(out m_TimeWindow3End);
        }
        else
        {
            // Default for old data: always active
            m_AlwaysActive = true;
            m_TimeWindow1Start = 255;
            m_TimeWindow1End = 255;
            m_TimeWindow2Start = 255;
            m_TimeWindow2End = 255;
            m_TimeWindow3Start = 255;
            m_TimeWindow3End = 255;
        }
        
        // Reset timer on load
        m_GroupTimer = 0;
        m_GoTimestamp = 0;
    }

    public SyncGroup()
    {
        m_SchemaVersion = 2;
        m_GroupId = 0;
        m_GroupName = default;
        m_BaseCycleDuration = 60;
        m_GroupTimer = 0;
        m_GoTimestamp = 0;
        // Default: always active
        m_AlwaysActive = true;
        m_TimeWindow1Start = 255;
        m_TimeWindow1End = 255;
        m_TimeWindow2Start = 255;
        m_TimeWindow2End = 255;
        m_TimeWindow3Start = 255;
        m_TimeWindow3End = 255;
    }

    public SyncGroup(uint groupId, string name, ushort cycleDuration)
    {
        m_SchemaVersion = 2;
        m_GroupId = groupId;
        m_GroupName = new FixedString64Bytes(name);
        m_BaseCycleDuration = cycleDuration;
        m_GroupTimer = 0;
        m_GoTimestamp = 0;
        // Default: always active
        m_AlwaysActive = true;
        m_TimeWindow1Start = 255;
        m_TimeWindow1End = 255;
        m_TimeWindow2Start = 255;
        m_TimeWindow2End = 255;
        m_TimeWindow3Start = 255;
        m_TimeWindow3End = 255;
    }
    
    /// <summary>
    /// Checks if the group is currently active based on the game hour.
    /// </summary>
    /// <param name="gameHour">Current game hour (0-23)</param>
    /// <returns>True if the group should be active</returns>
    public readonly bool IsActiveAtHour(int gameHour)
    {
        if (m_AlwaysActive) return true;
        
        // Check each time window
        if (IsHourInWindow(gameHour, m_TimeWindow1Start, m_TimeWindow1End)) return true;
        if (IsHourInWindow(gameHour, m_TimeWindow2Start, m_TimeWindow2End)) return true;
        if (IsHourInWindow(gameHour, m_TimeWindow3Start, m_TimeWindow3End)) return true;
        
        return false;
    }
    
    /// <summary>
    /// Checks if an hour falls within a time window.
    /// Handles windows that span midnight (e.g., 22:00 - 02:00).
    /// </summary>
    private static bool IsHourInWindow(int hour, byte windowStart, byte windowEnd)
    {
        // 255 = disabled window
        if (windowStart == 255 || windowEnd == 255) return false;
        
        if (windowStart <= windowEnd)
        {
            // Normal window: e.g., 07:00 - 09:00
            return hour >= windowStart && hour < windowEnd;
        }
        else
        {
            // Window spans midnight: e.g., 22:00 - 02:00
            return hour >= windowStart || hour < windowEnd;
        }
    }

    /// <summary>
    /// Gets the group name as a regular string.
    /// </summary>
    public readonly string GetName()
    {
        return m_GroupName.ToString();
    }

    /// <summary>
    /// Sets the group name from a string.
    /// </summary>
    public void SetName(string name)
    {
        m_GroupName = new FixedString64Bytes(name);
    }
}

/// <summary>
/// Singleton component that manages sync group ID generation.
/// Attached to the same entity that holds the SyncGroup buffer.
/// </summary>
public struct SyncGroupManager : IComponentData, ISerializable
{
    private ushort m_SchemaVersion;

    /// <summary>
    /// Counter for generating unique group IDs.
    /// </summary>
    public uint m_NextGroupId;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        m_SchemaVersion = 1;
        writer.Write(m_SchemaVersion);
        writer.Write(m_NextGroupId);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_SchemaVersion);
        reader.Read(out m_NextGroupId);
    }

    public SyncGroupManager()
    {
        m_SchemaVersion = 1;
        m_NextGroupId = 1; // Start at 1, 0 means "no group"
    }
}
