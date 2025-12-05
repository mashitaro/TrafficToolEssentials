using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficToolEssentials.Components;

public struct CustomTrafficLights : IComponentData, IQueryTypeParameter, ISerializable
{
    public enum Patterns : uint
    {
        Vanilla = 0,

        SplitPhasing = 1,

        ProtectedCentreTurn = 2,

        SplitPhasingAdvancedObsolete = 3,

        ModDefault = 4,

        CustomPhase = 5,

        ExclusivePedestrian = 1 << 16,

        AlwaysGreenKerbsideTurn = 1 << 17,

        CentreTurnGiveWay = 1 << 18,
    }

    private int m_SchemaVersion;

    // Schema 1
    private const int DefaultSelectedPatternLength = 16;

    // Schema 2
    private Patterns m_Pattern;

    // Schema 3
    public float m_PedestrianPhaseDurationMultiplier { get; private set; }

    public int m_PedestrianPhaseGroupMask { get; private set; }

    // Schema 4
    public uint m_Timer;

    public byte m_ManualSignalGroup;
    
    /// <summary>
    /// Signal group to avoid transitioning TO. Used by Green Wave to prevent
    /// fast-cycling intersections from drifting into sync phase too early.
    /// Unlike m_ManualSignalGroup which FORCES a phase, this PREVENTS a phase.
    /// Not serialized - runtime only.
    /// </summary>
    public byte m_AvoidSignalGroup;
    
    /// <summary>
    /// Whether this intersection has completed its initial synchronization.
    /// In the two-stage sync system:
    /// - Stage 1 (InitialSyncDone=false): FORCE to sync phase once
    /// - Stage 2 (InitialSyncDone=true): AVOID-only mode
    /// Not serialized - resets on game load (which is correct, we want to re-sync).
    /// </summary>
    public bool m_InitialSyncDone;

    // Schema 5 - Green Wave Sync
    /// <summary>
    /// ID of the sync group this intersection belongs to. 0 = no group.
    /// </summary>
    public uint m_SyncGroupId { get; private set; }

    /// <summary>
    /// Whether this intersection uses synced cycle timing.
    /// </summary>
    public bool m_UseSyncedCycle { get; private set; }

    /// <summary>
    /// Offset in seconds from the group's base timer. Used to create green wave effect.
    /// </summary>
    public ushort m_CycleOffsetSeconds { get; private set; }

    // Schema 6 - Sync Phase Selection
    /// <summary>
    /// Index of the phase to synchronize (0 = first phase, 1 = second, etc.)
    /// This allows syncing specific phases for complex routes (e.g., turn lanes).
    /// </summary>
    public byte m_SyncPhaseIndex { get; private set; }

    // Schema 7 - Embedded Sync Group Definition
    // Each intersection stores the full group definition for robust savegame persistence
    /// <summary>
    /// Name of the sync group (embedded for savegame persistence)
    /// </summary>
    public Unity.Collections.FixedString64Bytes m_SyncGroupName { get; private set; }
    
    /// <summary>
    /// Base cycle duration for the sync group
    /// </summary>
    public ushort m_SyncGroupBaseCycle { get; private set; }
    
    /// <summary>
    /// Whether the sync group is always active
    /// </summary>
    public bool m_SyncGroupAlwaysActive { get; private set; }
    
    /// <summary>
    /// Time window settings for the sync group (start/end hours, 255=disabled)
    /// </summary>
    public byte m_SyncGroupTW1Start { get; private set; }
    public byte m_SyncGroupTW1End { get; private set; }
    public byte m_SyncGroupTW2Start { get; private set; }
    public byte m_SyncGroupTW2End { get; private set; }
    public byte m_SyncGroupTW3Start { get; private set; }
    public byte m_SyncGroupTW3End { get; private set; }

    // Schema 8 - Sequential Phase Mode
    /// <summary>
    /// Whether to use sequential phase ordering (1→2→3→4→1) instead of priority-based.
    /// When true, phases always progress in order regardless of traffic.
    /// Required for Green Wave synchronization to work correctly.
    /// </summary>
    public bool m_UseSequentialPhases { get; private set; }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        m_SchemaVersion = 8;
        writer.Write(uint.MaxValue);
        writer.Write(m_SchemaVersion);
        writer.Write((uint)m_Pattern);
        writer.Write(m_PedestrianPhaseDurationMultiplier);
        writer.Write(m_PedestrianPhaseGroupMask);
        writer.Write(m_Timer);
        writer.Write(m_ManualSignalGroup);
        // Schema 5 - Green Wave
        writer.Write(m_SyncGroupId);
        writer.Write(m_UseSyncedCycle);
        writer.Write(m_CycleOffsetSeconds);
        // Schema 6 - Sync Phase Selection
        writer.Write(m_SyncPhaseIndex);
        // Schema 7 - Embedded Sync Group Definition
        writer.Write(m_SyncGroupName.ToString());
        writer.Write(m_SyncGroupBaseCycle);
        writer.Write(m_SyncGroupAlwaysActive);
        writer.Write(m_SyncGroupTW1Start);
        writer.Write(m_SyncGroupTW1End);
        writer.Write(m_SyncGroupTW2Start);
        writer.Write(m_SyncGroupTW2End);
        writer.Write(m_SyncGroupTW3Start);
        writer.Write(m_SyncGroupTW3End);
        // Schema 8 - Sequential Phase Mode
        writer.Write(m_UseSequentialPhases);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        // Defaults
        m_PedestrianPhaseDurationMultiplier = 1;
        m_PedestrianPhaseGroupMask = 0;
        m_SyncGroupId = 0;
        m_UseSyncedCycle = false;
        m_CycleOffsetSeconds = 0;
        m_SyncPhaseIndex = 0;
        // Schema 7 defaults
        m_SyncGroupName = default;
        m_SyncGroupBaseCycle = 60;
        m_SyncGroupAlwaysActive = true;
        m_SyncGroupTW1Start = 255;
        m_SyncGroupTW1End = 255;
        m_SyncGroupTW2Start = 255;
        m_SyncGroupTW2End = 255;
        m_SyncGroupTW3Start = 255;
        m_SyncGroupTW3End = 255;
        // Schema 8 defaults
        m_UseSequentialPhases = false;

        reader.Read(out uint uint1);
        if (uint1 == uint.MaxValue)
        {
            reader.Read(out m_SchemaVersion);
        }
        else
        {
            m_SchemaVersion = 1;
        }
        if (m_SchemaVersion == 1)
        {
            for (int i = 1; i < DefaultSelectedPatternLength; i++)
            {
                reader.Read(out uint pattern);
            }
            m_Pattern = Patterns.Vanilla;
        }
        if (m_SchemaVersion >= 2)
        {
            reader.Read(out uint pattern);
            m_Pattern = (Patterns)pattern;
        }
        if (m_SchemaVersion >= 3)
        {
            reader.Read(out float pedestrianPhaseDurationMultiplier);
            reader.Read(out int pedestrianPhaseGroupMask);
            m_PedestrianPhaseDurationMultiplier = pedestrianPhaseDurationMultiplier;
            m_PedestrianPhaseGroupMask = pedestrianPhaseGroupMask;
        }
        if (m_SchemaVersion >= 4)
        {
            reader.Read(out m_Timer);
            reader.Read(out m_ManualSignalGroup);
        }
        if (m_SchemaVersion >= 5)
        {
            reader.Read(out uint syncGroupId);
            reader.Read(out bool useSyncedCycle);
            reader.Read(out ushort cycleOffsetSeconds);
            m_SyncGroupId = syncGroupId;
            m_UseSyncedCycle = useSyncedCycle;
            m_CycleOffsetSeconds = cycleOffsetSeconds;
        }
        if (m_SchemaVersion >= 6)
        {
            reader.Read(out byte syncPhaseIndex);
            m_SyncPhaseIndex = syncPhaseIndex;
        }
        if (m_SchemaVersion >= 7)
        {
            reader.Read(out string groupName);
            m_SyncGroupName = new Unity.Collections.FixedString64Bytes(groupName ?? "");
            reader.Read(out ushort baseCycle);
            m_SyncGroupBaseCycle = baseCycle;
            reader.Read(out bool alwaysActive);
            m_SyncGroupAlwaysActive = alwaysActive;
            reader.Read(out byte tw1Start);
            reader.Read(out byte tw1End);
            reader.Read(out byte tw2Start);
            reader.Read(out byte tw2End);
            reader.Read(out byte tw3Start);
            reader.Read(out byte tw3End);
            m_SyncGroupTW1Start = tw1Start;
            m_SyncGroupTW1End = tw1End;
            m_SyncGroupTW2Start = tw2Start;
            m_SyncGroupTW2End = tw2End;
            m_SyncGroupTW3Start = tw3Start;
            m_SyncGroupTW3End = tw3End;
        }
        if (m_SchemaVersion >= 8)
        {
            reader.Read(out bool useSequentialPhases);
            m_UseSequentialPhases = useSequentialPhases;
        }
        if (GetPatternOnly() == Patterns.SplitPhasingAdvancedObsolete)
        {
            SetPatternOnly(Patterns.SplitPhasing);
        }
        m_ManualSignalGroup = 0;
    }

    public CustomTrafficLights()
    {
        m_SchemaVersion = 7;
        m_Pattern = Patterns.Vanilla;
        m_PedestrianPhaseDurationMultiplier = 1;
        m_PedestrianPhaseGroupMask = 0;
        m_Timer = 0;
        m_ManualSignalGroup = 0;
        // Schema 5 defaults
        m_SyncGroupId = 0;
        m_UseSyncedCycle = false;
        m_CycleOffsetSeconds = 0;
        // Schema 6 defaults
        m_SyncPhaseIndex = 0;
        // Schema 7 defaults
        m_SyncGroupName = default;
        m_SyncGroupBaseCycle = 60;
        m_SyncGroupAlwaysActive = true;
        m_SyncGroupTW1Start = 255;
        m_SyncGroupTW1End = 255;
        m_SyncGroupTW2Start = 255;
        m_SyncGroupTW2End = 255;
        m_SyncGroupTW3Start = 255;
        m_SyncGroupTW3End = 255;
        // Schema 8 defaults
        m_UseSequentialPhases = false;
    }

    public CustomTrafficLights(Patterns pattern)
    {
        m_SchemaVersion = 8;
        m_Pattern = pattern;
        m_PedestrianPhaseDurationMultiplier = 1;
        m_PedestrianPhaseGroupMask = 0;
        m_Timer = 0;
        m_ManualSignalGroup = 0;
        // Schema 5 defaults
        m_SyncGroupId = 0;
        m_UseSyncedCycle = false;
        m_CycleOffsetSeconds = 0;
        // Schema 6 defaults
        m_SyncPhaseIndex = 0;
        // Schema 7 defaults
        m_SyncGroupName = default;
        m_SyncGroupBaseCycle = 60;
        m_SyncGroupAlwaysActive = true;
        m_SyncGroupTW1Start = 255;
        m_SyncGroupTW1End = 255;
        m_SyncGroupTW2Start = 255;
        m_SyncGroupTW2End = 255;
        m_SyncGroupTW3Start = 255;
        m_SyncGroupTW3End = 255;
        // Schema 8 defaults
        m_UseSequentialPhases = false;
    }

    public Patterns GetPattern()
    {
        return m_Pattern;
    }

    public Patterns GetPatternOnly()
    {
        return (Patterns)((uint)GetPattern() & 0xFFFF);
    }

    public void SetPattern(uint pattern)
    {
        SetPattern((Patterns)pattern);
    }

    public void SetPattern(Patterns pattern)
    {
        m_Pattern = pattern;
    }

    public void SetPatternOnly(Patterns pattern)
    {
        m_Pattern = (Patterns)(((uint)m_Pattern & 0xFFFF0000) | ((uint)pattern & 0xFFFF));
    }

    public void SetPedestrianPhaseDurationMultiplier(float durationMultiplier)
    {
        m_PedestrianPhaseDurationMultiplier = durationMultiplier;
    }

    public void SetPedestrianPhaseGroupMask(int groupMask)
    {
        m_PedestrianPhaseGroupMask = groupMask;
    }

    // Schema 5 Setters - Green Wave
    public void SetSyncGroupId(uint groupId)
    {
        m_SyncGroupId = groupId;
    }

    public void SetUseSyncedCycle(bool useSynced)
    {
        m_UseSyncedCycle = useSynced;
    }

    public void SetCycleOffsetSeconds(ushort offsetSeconds)
    {
        m_CycleOffsetSeconds = offsetSeconds;
    }

    // Schema 6 Setter - Sync Phase Selection
    public void SetSyncPhaseIndex(byte phaseIndex)
    {
        m_SyncPhaseIndex = phaseIndex;
    }

    // Schema 7 Setters - Embedded Sync Group Definition
    public void SetSyncGroupName(string name)
    {
        m_SyncGroupName = new Unity.Collections.FixedString64Bytes(name ?? "");
    }

    public void SetSyncGroupBaseCycle(ushort baseCycle)
    {
        m_SyncGroupBaseCycle = baseCycle;
    }

    public void SetSyncGroupAlwaysActive(bool alwaysActive)
    {
        m_SyncGroupAlwaysActive = alwaysActive;
    }

    public void SetSyncGroupTimeWindows(byte tw1Start, byte tw1End, byte tw2Start, byte tw2End, byte tw3Start, byte tw3End)
    {
        m_SyncGroupTW1Start = tw1Start;
        m_SyncGroupTW1End = tw1End;
        m_SyncGroupTW2Start = tw2Start;
        m_SyncGroupTW2End = tw2End;
        m_SyncGroupTW3Start = tw3Start;
        m_SyncGroupTW3End = tw3End;
    }

    /// <summary>
    /// Copies all sync group definition fields from another intersection.
    /// Used to propagate group settings when adding intersections to a group.
    /// </summary>
    public void CopySyncGroupDefinitionFrom(CustomTrafficLights source)
    {
        m_SyncGroupName = source.m_SyncGroupName;
        m_SyncGroupBaseCycle = source.m_SyncGroupBaseCycle;
        m_SyncGroupAlwaysActive = source.m_SyncGroupAlwaysActive;
        m_SyncGroupTW1Start = source.m_SyncGroupTW1Start;
        m_SyncGroupTW1End = source.m_SyncGroupTW1End;
        m_SyncGroupTW2Start = source.m_SyncGroupTW2Start;
        m_SyncGroupTW2End = source.m_SyncGroupTW2End;
        m_SyncGroupTW3Start = source.m_SyncGroupTW3Start;
        m_SyncGroupTW3End = source.m_SyncGroupTW3End;
    }

    // Runtime Setter - Used by SyncGroupSystem for green wave
    public void SetManualSignalGroup(byte signalGroup)
    {
        m_ManualSignalGroup = signalGroup;
    }

    // Schema 8 Setter - Sequential Phase Mode
    public void SetUseSequentialPhases(bool useSequential)
    {
        m_UseSequentialPhases = useSequential;
    }
}
