using C2VM.TrafficToolEssentials.Components;
using Game;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using System.Collections.Generic;
using System.Linq;

namespace C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Simulation;

/// <summary>
/// V91 COORDINATED SYNC - State machine for green wave synchronization.
/// </summary>
public enum SyncState
{
    /// <summary>BARRIER: Block P1 for everyone until all are out of P1 (initial alignment)</summary>
    BARRIER = 0,
    
    /// <summary>Not yet synchronized - needs initial sync</summary>
    UNSYNCED = 1,
    
    /// <summary>Reference intersection: Waiting in current phase for others to be ready</summary>
    WAITING_FOR_OTHERS = 2,
    
    /// <summary>Non-reference: Rushing through phases to reach P1</summary>
    RUSHING_TO_P1 = 3,
    
    /// <summary>Ready and waiting at last phase before P1, waiting for GO signal</summary>
    READY_FOR_SYNC = 4,
    
    /// <summary>Synchronized! Running freely, no interventions</summary>
    SYNCED = 5
}

/// <summary>
/// Time conversion constants for Green Wave synchronization.
/// CS2 simulation runs at approximately 60 updates per second at 1x speed.
/// Game time runs ~60x faster than real time (1 real second = 1 game minute).
/// </summary>
public static class GreenWaveTimeConstants
{
    /// <summary>
    /// Simulation updates per real-time second at 1x game speed.
    /// </summary>
    public const float UPDATES_PER_REAL_SECOND = 60f;
    
    /// <summary>
    /// Game time to real time ratio (game runs ~60x faster).
    /// 1 real second = ~60 game seconds = 1 game minute.
    /// </summary>
    public const float GAME_TO_REAL_TIME_RATIO = 60f;
    
    /// <summary>
    /// Traffic light system update interval (from PatchedTrafficLightSystem).
    /// </summary>
    public const int TRAFFIC_LIGHT_UPDATE_INTERVAL = 4;
    
    /// <summary>
    /// Converts real-time seconds to game update frames.
    /// </summary>
    public static uint RealSecondsToFrames(float realSeconds)
    {
        // Real seconds -> game seconds -> frames
        return (uint)(realSeconds * UPDATES_PER_REAL_SECOND);
    }
    
    /// <summary>
    /// Converts game update frames to real-time seconds.
    /// </summary>
    public static float FramesToRealSeconds(uint frames)
    {
        return frames / UPDATES_PER_REAL_SECOND;
    }
}

/// <summary>
/// Manages sync groups for green wave synchronization.
/// 
/// V91 COORDINATED SYNC APPROACH:
/// ==============================
/// 
/// The Problem:
/// - Each intersection runs its own internal cycle (P1→P2→P3→P4→P1...)
/// - We can only control WHEN P1 is allowed, not WHEN it's reached
/// - Pure AVOID can block but not synchronize
/// 
/// The Solution - Coordinated Start:
/// 1. Reference waits in its CURRENT phase (not P1!) until all others are ready
/// 2. Others "rush" to reach P1 by skipping non-P1 phases
/// 3. When all are ready (waiting just before P1): GO! Everyone enters P1
/// 4. After sync: NO MORE INTERVENTIONS - everyone runs freely!
/// 
/// Why this works:
/// - Reference waits in current phase → no P1 "sticking", other phases get time
/// - Others rush only during initial sync → temporary, not permanent
/// - After sync with equal cycle lengths → they stay synchronized naturally
/// - Self-healing: cycle length change triggers re-sync
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(PatchedTrafficLightSystem))]
public partial class SyncGroupSystem : GameSystemBase
{
    private Entity m_ManagerEntity;
    private SimulationSystem m_SimulationSystem;
    private TimeSystem m_TimeSystem;
    private EntityQuery m_SyncedIntersectionsQuery;
    private uint m_FrameCounter = 0;
    private bool m_Initialized = false;
    
    // V91: Coordinated Sync Tracking
    private Dictionary<Entity, SyncState> m_SyncStates = new Dictionary<Entity, SyncState>();
    private Dictionary<Entity, uint> m_LastCycleDurations = new Dictionary<Entity, uint>();
    private Dictionary<uint, Entity> m_GroupReferences = new Dictionary<uint, Entity>(); // GroupId -> Reference Entity
    private HashSet<uint> m_PhaseWarningsIssued = new HashSet<uint>(); // V112: Track groups that have been warned about phase count differences

    // V130: Debug logging controlled by Settings
    private static bool IsDebugLoggingEnabled => Mod.m_Settings?.m_DebugLogging == true;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
        m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
        m_ManagerEntity = Entity.Null;
        m_SyncedIntersectionsQuery = default;
        
        Mod.m_Log.Info("[SyncGroupSystem] OnCreate completed");
    }
    
    /// <summary>
    /// Initialize the system - creates manager entity and sets up RequireForUpdate.
    /// Called from EnsureGroupsLoaded or first OnUpdate.
    /// </summary>
    private void Initialize()
    {
        if (m_Initialized) return;
        
        CreateOrFindManagerEntity();
        
        if (m_ManagerEntity != Entity.Null)
        {
            // Now that manager exists, system should run
            m_Initialized = true;
            Mod.m_Log.Info("[SyncGroupSystem] Initialized successfully");
        }
    }
    
    /// <summary>
    /// Resets the system state when a new game is loaded.
    /// Destroys the old manager entity so the deserialized one can be found.
    /// </summary>
    public void ResetForNewGame()
    {
        Mod.m_Log.Info("[SyncGroupSystem] ResetForNewGame called");
        
        // IMPORTANT: Destroy the old entity so it doesn't persist across game loads
        // The deserialized entity from the savegame (if any) will be found by CreateOrFindManagerEntity
        if (m_ManagerEntity != Entity.Null && EntityManager.Exists(m_ManagerEntity))
        {
            Mod.m_Log.Info("[SyncGroupSystem] Destroying old manager entity");
            EntityManager.DestroyEntity(m_ManagerEntity);
        }
        
        // Reset references
        m_ManagerEntity = Entity.Null;
        m_Initialized = false;
        m_FrameCounter = 0;
        
        // V91: Clear all sync tracking for fresh start
        m_SyncStates.Clear();
        m_LastCycleDurations.Clear();
        m_GroupReferences.Clear();
        m_PhaseWarningsIssued.Clear(); // V112: Reset warnings for new game
        Mod.m_Log.Info("[SyncGroupSystem] V91 sync tracking cleared");
    }
    
    /// <summary>
    /// V118: Removes entries from dictionaries for entities that no longer exist
    /// or are no longer part of any sync group.
    /// Called periodically to prevent memory leaks and stale data.
    /// </summary>
    private void CleanupOrphanedDictionaryEntries(NativeArray<Entity> currentEntities)
    {
        // Build HashSet of valid entities for fast lookup
        HashSet<Entity> validEntities = new HashSet<Entity>();
        for (int i = 0; i < currentEntities.Length; i++)
        {
            var entity = currentEntities[i];
            if (EntityManager.HasComponent<CustomTrafficLights>(entity))
            {
                var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
                if (ctl.m_UseSyncedCycle && ctl.m_SyncGroupId != 0)
                {
                    validEntities.Add(entity);
                }
            }
        }
        
        // Clean m_SyncStates
        List<Entity> toRemoveStates = new List<Entity>();
        foreach (var kvp in m_SyncStates)
        {
            if (!validEntities.Contains(kvp.Key))
            {
                toRemoveStates.Add(kvp.Key);
            }
        }
        foreach (var entity in toRemoveStates)
        {
            m_SyncStates.Remove(entity);
        }
        
        // Clean m_LastCycleDurations
        List<Entity> toRemoveCycles = new List<Entity>();
        foreach (var kvp in m_LastCycleDurations)
        {
            if (!validEntities.Contains(kvp.Key))
            {
                toRemoveCycles.Add(kvp.Key);
            }
        }
        foreach (var entity in toRemoveCycles)
        {
            m_LastCycleDurations.Remove(entity);
        }
        
        // Clean m_GroupReferences - remove refs to entities that don't exist anymore
        List<uint> toRemoveRefs = new List<uint>();
        foreach (var kvp in m_GroupReferences)
        {
            if (!validEntities.Contains(kvp.Value))
            {
                toRemoveRefs.Add(kvp.Key);
            }
        }
        foreach (var groupId in toRemoveRefs)
        {
            m_GroupReferences.Remove(groupId);
        }
        
        // V119.7: Clean m_PhaseWarningsIssued for groups that no longer exist
        // First find all active group IDs
        HashSet<uint> activeGroupIds = new HashSet<uint>();
        foreach (var entity in validEntities)
        {
            if (EntityManager.HasComponent<CustomTrafficLights>(entity))
            {
                var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
                if (ctl.m_UseSyncedCycle && ctl.m_SyncGroupId != 0)
                {
                    activeGroupIds.Add(ctl.m_SyncGroupId);
                }
            }
        }
        
        // Remove warnings for groups that no longer have members
        List<uint> toRemoveWarnings = new List<uint>();
        foreach (var groupId in m_PhaseWarningsIssued)
        {
            if (!activeGroupIds.Contains(groupId))
            {
                toRemoveWarnings.Add(groupId);
            }
        }
        foreach (var groupId in toRemoveWarnings)
        {
            m_PhaseWarningsIssued.Remove(groupId);
        }
        
        if (toRemoveStates.Count > 0 || toRemoveCycles.Count > 0 || toRemoveRefs.Count > 0 || toRemoveWarnings.Count > 0)
        {
            if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] V118/V119: Cleanup removed {toRemoveStates.Count} states, {toRemoveCycles.Count} cycles, {toRemoveRefs.Count} refs, {toRemoveWarnings.Count} warnings");
        }
    }
    
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        Mod.m_Log.Info("[SyncGroupSystem] OnStartRunning called");
    }
    
    protected override void OnStopRunning()
    {
        base.OnStopRunning();
        Mod.m_Log.Info("[SyncGroupSystem] OnStopRunning called");
    }
    
    /// <summary>
    /// Public method called from UISystem.OnUpdate() every frame.
    /// This bypasses the ECS query requirements that prevent OnUpdate from running.
    /// </summary>
    public void UpdateSync()
    {
        m_FrameCounter++;
        
        try
        {
            // Initialize if needed
            if (!m_Initialized)
            {
                Initialize();
            }
            
            // Ensure manager entity exists (will find deserialized entity or create new one)
            if (m_ManagerEntity == Entity.Null || !EntityManager.Exists(m_ManagerEntity))
            {
                CreateOrFindManagerEntity();
            }

            // Update all group timers and apply sync
            if (m_ManagerEntity != Entity.Null && EntityManager.HasBuffer<SyncGroup>(m_ManagerEntity))
            {
                var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity);
                
                // Debug log every 300 frames (~5 seconds)
                if (m_FrameCounter % 300 == 1 && buffer.Length > 0)
                {
                    if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[SyncGroupSystem] UpdateSync: frame {m_FrameCounter}, {buffer.Length} groups, timer={buffer[0].m_GroupTimer}");
                }
                
                // Update timers
                for (int i = 0; i < buffer.Length; i++)
                {
                    var group = buffer[i];
                    group.m_GroupTimer++;
                    if (group.m_GroupTimer > 1000000)
                    {
                        group.m_GroupTimer = 0;
                        
                        // V121: Reset GoTimestamp for this group to prevent underflow
                        // Problem: timeSinceRefP1 = Timer - GoTimestamp würde bei Timer-Reset
                        // zu uint Underflow führen (z.B. 10 - 999990 = ~4 Milliarden!)
                        group.m_GoTimestamp = 0;
                        if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] V121: Timer reset for Group[{group.m_GroupId}] - GoTimestamp also reset to prevent underflow");
                    }
                    buffer[i] = group;
                }
                
                // Apply synchronization
                ApplySyncToIntersections(buffer);
            }
        }
        catch (System.Exception e)
        {
            if (m_FrameCounter % 300 == 1)
            {
                Mod.m_Log.Error($"[SyncGroupSystem] UpdateSync error: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Ensures groups are loaded. On first call after game load, reconstructs groups from intersection data.
    /// This is the key method for savegame persistence - groups are stored in intersections, not separately.
    /// </summary>
    public void EnsureGroupsLoaded()
    {
        // Ensure manager entity exists
        if (m_ManagerEntity == Entity.Null || !EntityManager.Exists(m_ManagerEntity))
        {
            CreateOrFindManagerEntity();
            
            if (m_ManagerEntity != Entity.Null)
            {
                // IMPORTANT: Reconstruct groups from intersection data
                // This is how we restore groups from savegames
                ReconstructGroupsFromIntersections();
                
                var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity, true);
                Mod.m_Log.Info($"[SyncGroupSystem] EnsureGroupsLoaded: Reconstructed {buffer.Length} groups from intersections");
            }
        }
    }
    
    /// <summary>
    /// Reconstructs the SyncGroup buffer by scanning all intersections.
    /// Each intersection stores its group definition, so we can rebuild the group list.
    /// </summary>
    private void ReconstructGroupsFromIntersections()
    {
        if (m_ManagerEntity == Entity.Null) return;
        
        var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity);
        buffer.Clear();
        
        // Reset next ID - will be updated based on found groups
        var manager = EntityManager.GetComponentData<SyncGroupManager>(m_ManagerEntity);
        uint maxGroupId = 0;
        
        // Track which group IDs we've already added
        var addedGroupIds = new HashSet<uint>();
        
        // Query all intersections with CustomTrafficLights
        var query = GetEntityQuery(ComponentType.ReadOnly<CustomTrafficLights>());
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        
        foreach (var entity in entities)
        {
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            // Skip if not in a group
            if (ctl.m_SyncGroupId == 0) continue;
            
            // Track max ID for NextGroupId
            if (ctl.m_SyncGroupId > maxGroupId)
            {
                maxGroupId = ctl.m_SyncGroupId;
            }
            
            // Skip if we already added this group
            if (addedGroupIds.Contains(ctl.m_SyncGroupId)) continue;
            
            // Create group from intersection data
            var group = new SyncGroup(
                ctl.m_SyncGroupId,
                ctl.m_SyncGroupName.ToString(),
                ctl.m_SyncGroupBaseCycle
            );
            
            // Copy time window settings
            group.m_AlwaysActive = ctl.m_SyncGroupAlwaysActive;
            group.m_TimeWindow1Start = ctl.m_SyncGroupTW1Start;
            group.m_TimeWindow1End = ctl.m_SyncGroupTW1End;
            group.m_TimeWindow2Start = ctl.m_SyncGroupTW2Start;
            group.m_TimeWindow2End = ctl.m_SyncGroupTW2End;
            group.m_TimeWindow3Start = ctl.m_SyncGroupTW3Start;
            group.m_TimeWindow3End = ctl.m_SyncGroupTW3End;
            
            buffer.Add(group);
            addedGroupIds.Add(ctl.m_SyncGroupId);
            
            Mod.m_Log.Info($"[SyncGroupSystem] Reconstructed group '{group.GetName()}' (ID={ctl.m_SyncGroupId}) from intersection");
        }
        
        entities.Dispose();
        
        // Update NextGroupId to be higher than any existing group
        manager.m_NextGroupId = maxGroupId + 1;
        EntityManager.SetComponentData(m_ManagerEntity, manager);
        
        Mod.m_Log.Info($"[SyncGroupSystem] Reconstruction complete: {buffer.Length} groups, NextGroupId={manager.m_NextGroupId}");
    }
    
    /// <summary>
    /// Updates BaseCycleDuration for all groups.
    /// Called after loading to ensure correct values.
    /// </summary>
    public void UpdateAllGroupBaseCycles()
    {
        if (m_ManagerEntity == Entity.Null || !EntityManager.Exists(m_ManagerEntity))
            return;
        
        var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity, true);
        for (int i = 0; i < buffer.Length; i++)
        {
            UpdateGroupBaseCycle(buffer[i].m_GroupId);
        }
        Mod.m_Log.Info($"[SyncGroupSystem] Updated BaseCycle for {buffer.Length} groups");
    }

    protected override void OnUpdate()
    {
        // OnUpdate is NOT reliably called due to ECS query requirements.
        // The actual sync logic is called from UISystem.OnUpdate() via UpdateSync().
        // This method is kept as a fallback but should not be relied upon.
    }

    /// <summary>
    /// Applies synchronization to all intersections in sync groups.
    /// 
    /// GREEN WAVE SYNC STRATEGY:
    /// Instead of controlling every phase, we only control WHEN Phase 1 starts.
    /// 
    /// For each intersection:
    /// 1. Let phases run normally through their min/max durations
    /// 2. When the intersection would naturally switch to Phase 1:
    ///    - Check if it's the right time (based on GroupTimer + Offset)
    ///    - If YES: Allow Phase 1 to start
    ///    - If NO: Extend the current phase (or repeat a non-Phase-1 phase) to wait
    /// 
    /// This way:
    /// - Short-cycle intersections wait by repeating phases
    /// - Long-cycle intersections run normally
    /// - All intersections START Phase 1 at synchronized times
    /// </summary>
    private void ApplySyncToIntersections(DynamicBuffer<SyncGroup> groups)
    {
        if (groups.Length == 0) return;
        
        // Create query on-demand if not yet created
        if (m_SyncedIntersectionsQuery == default)
        {
            m_SyncedIntersectionsQuery = GetEntityQuery(ComponentType.ReadWrite<CustomTrafficLights>());
            Mod.m_Log.Info("[SyncGroupSystem] Created m_SyncedIntersectionsQuery on-demand");
        }
        
        var entities = m_SyncedIntersectionsQuery.ToEntityArray(Allocator.Temp);
        
        // V118: Periodic cleanup of orphaned dictionary entries
        // Every 600 frames (~10 seconds), check for dead entities
        uint cleanupFrame = groups.Length > 0 ? groups[0].m_GroupTimer : 0;
        if (cleanupFrame % 600 == 0)
        {
            CleanupOrphanedDictionaryEntries(entities);
        }
        
        // === EXTENSIVE DEBUG LOGGING ===
        // TEMPORARY: Log every frame for detailed debugging
        // TODO: Remove before release!
        uint frameForLog = groups.Length > 0 ? groups[0].m_GroupTimer : 0;
        bool shouldLogDetailed = true;  // TEMPORARY: Always log for debugging
        bool shouldLogSummary = frameForLog % 300 == 0;
        
        int syncedCount = 0;
        int skippedNoGroup = 0;  // Misconfigurations: sync enabled but no group, or orphaned
        int skippedNoPhases = 0;
        int skippedTimeWindow = 0;
        int phasesForced = 0;
        int phasesHeld = 0;
        int phasesReleased = 0;
        int standaloneCount = 0;  // Normal standalone intersections (not in any group)
        
        // Get current game time for logging
        int currentGameHour = GetCurrentGameHour();
        
        if (shouldLogSummary)
        {
            if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] ========== SYNC CYCLE START ==========");
            if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] GameHour={currentGameHour}, Groups={groups.Length}, Entities={entities.Length}");
            for (int g = 0; g < groups.Length; g++)
            {
                var grp = groups[g];
                bool isActive = grp.IsActiveAtHour(currentGameHour);
                if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] Group[{grp.m_GroupId}] '{grp.GetName()}': Timer={grp.m_GroupTimer}, BaseCycle={grp.m_BaseCycleDuration}, AlwaysActive={grp.m_AlwaysActive}, ActiveNow={isActive}");
                if (!grp.m_AlwaysActive)
                {
                    if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG]   TimeWindows: W1={grp.m_TimeWindow1Start}-{grp.m_TimeWindow1End}, W2={grp.m_TimeWindow2Start}-{grp.m_TimeWindow2End}, W3={grp.m_TimeWindow3Start}-{grp.m_TimeWindow3End}");
                }
            }
        }
        
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            
            // V119.5: Wrap entire entity processing in try-catch
            // A single bad entity should not crash the entire sync system
            try
            {
            
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            // Skip if not using synced cycle or no group assigned
            // These are NOT errors - they're intersections that were edited but not added to sync groups
            if (!ctl.m_UseSyncedCycle || ctl.m_SyncGroupId == 0)
            {
                // V118: CRITICAL - Clear any leftover avoid signal!
                // If entity was removed from sync group or sync disabled, 
                // we MUST clear avoidPhase or traffic will be stuck!
                if (ctl.m_AvoidSignalGroup != 0 || ctl.m_ManualSignalGroup != 0)
                {
                    ctl.m_AvoidSignalGroup = 0;
                    ctl.SetManualSignalGroup(0);
                    EntityManager.SetComponentData(entity, ctl);
                    if (shouldLogDetailed)
                    {
                        if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] V118: Cleared leftover overrides for Entity[{entity.Index}] (not in sync group)");
                    }
                }
                
                // Only count as "skipped" if UseSyncedCycle is true but no group (actual misconfiguration)
                // Entities with UseSyncedCycle=false are just standalone intersections, not problems
                if (ctl.m_UseSyncedCycle && ctl.m_SyncGroupId == 0)
                {
                    skippedNoGroup++; // This is a real problem - sync enabled but no group
                }
                else
                {
                    standaloneCount++; // Normal - just not part of any sync group
                }
                continue;
            }
            
            // Find the group
            SyncGroup? foundGroup = null;
            for (int g = 0; g < groups.Length; g++)
            {
                if (groups[g].m_GroupId == ctl.m_SyncGroupId)
                {
                    foundGroup = groups[g];
                    break;
                }
            }
            
            if (!foundGroup.HasValue)
            {
                // V118: Intersection has a group ID but group doesn't exist - orphaned!
                // Clear any leftover avoid signal to prevent stuck traffic!
                if (ctl.m_AvoidSignalGroup != 0 || ctl.m_ManualSignalGroup != 0)
                {
                    ctl.m_AvoidSignalGroup = 0;
                    ctl.SetManualSignalGroup(0);
                    EntityManager.SetComponentData(entity, ctl);
                    if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] V118: Cleared overrides for ORPHAN Entity[{entity.Index}] (group {ctl.m_SyncGroupId} not found)");
                }
                skippedNoGroup++;
                continue;
            }
            
            var group = foundGroup.Value;
            
            // === CHECK TIME WINDOWS ===
            if (!group.IsActiveAtHour(currentGameHour))
            {
                if (ctl.m_ManualSignalGroup != 0 || ctl.m_AvoidSignalGroup != 0)
                {
                    ctl.SetManualSignalGroup(0);
                    ctl.m_AvoidSignalGroup = 0;
                    EntityManager.SetComponentData(entity, ctl);
                    if (shouldLogDetailed)
                    {
                        if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] Entity[{entity.Index}]: Cleared overrides (group inactive at hour {currentGameHour})");
                    }
                }
                skippedTimeWindow++;
                continue;
            }
            
            // Get phase buffer
            if (!EntityManager.HasBuffer<CustomPhaseData>(entity))
            {
                skippedNoPhases++;
                continue;
            }
            
            var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(entity, true);
            
            if (phaseBuffer.Length == 0)
            {
                skippedNoPhases++;
                continue;
            }
            
            // === V119: EDGE CASE PROTECTION ===
            // Protect against configurations that could cause issues
            
            // V119.1: Skip 1-phase intersections - they can't sync properly
            // (phaseBeforeSync would equal syncPhase, causing logic conflicts)
            if (phaseBuffer.Length < 2)
            {
                if (shouldLogDetailed)
                {
                    if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] V119: Entity[{entity.Index}] has only {phaseBuffer.Length} phase(s) - skipping sync (minimum 2 required)");
                }
                skippedNoPhases++;
                continue;
            }
            
            // V119.2: Validate offset doesn't exceed reasonable bounds
            // Max offset should be less than the longest cycle in the group
            ushort entityOffset = ctl.m_CycleOffsetSeconds;
            
            // Get the longest cycle duration in this group (for sync period)
            uint maxCycleDuration = GetMaxCycleDurationInGroup(groups, group.m_GroupId, entities);
            if (maxCycleDuration == 0)
                maxCycleDuration = 600; // Fallback: ~10 seconds
            
            // V119.2: Validate offset - warn if offset exceeds cycle time
            // V120: Apply modulo reduction to prevent excessive wait times
            uint maxCycleSeconds = maxCycleDuration / 60;
            if (entityOffset > 0 && maxCycleSeconds > 0 && entityOffset >= maxCycleSeconds)
            {
                // V120: Modulo-Reduktion - großer Offset wird auf äquivalenten kleineren Wert reduziert
                // Beispiel: 30s Offset bei 8s Zykluszeit → 30 mod 8 = 6s (gleiches Ergebnis, kein Stau!)
                ushort originalOffset = entityOffset;
                entityOffset = (ushort)(entityOffset % maxCycleSeconds);
                
                // Einmalige Info-Nachricht pro Session (nicht jeden Frame!)
                if (shouldLogDetailed)
                {
                    if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-INFO] V120: Entity[{entity.Index}] offset optimized: " +
                        $"{originalOffset}s → {entityOffset}s (modulo {maxCycleSeconds}s cycle). " +
                        $"Green Wave effect identical, but no unnecessary waiting!");
                }
            }
            
            // Get sync phase index (the phase we want to synchronize - usually Phase 1)
            byte syncPhaseIdx = ctl.m_SyncPhaseIndex;
            if (syncPhaseIdx >= phaseBuffer.Length)
                syncPhaseIdx = 0;
            
            // Calculate own cycle duration
            uint ownCycleDuration = 0;
            for (int p = 0; p < phaseBuffer.Length; p++)
            {
                ownCycleDuration += (uint)((phaseBuffer[p].m_MinimumDuration + phaseBuffer[p].m_MaximumDuration) / 2);
            }
            
            if (ownCycleDuration == 0)
                continue;
            
            // === CYCLE LENGTH DRIFT WARNING ===
            // Warn if cycle lengths differ significantly (>10%)
            // Continuous sync can handle small differences, but large ones may cause issues
            if (shouldLogDetailed && maxCycleDuration > 0)
            {
                float cycleDiff = (float)System.Math.Abs((int)ownCycleDuration - (int)maxCycleDuration) / maxCycleDuration;
                if (cycleDiff > 0.10f) // More than 10% difference
                {
                    if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-WARNING] Entity[{entity.Index}] Group[{group.m_GroupId}]: " +
                        $"Cycle length {ownCycleDuration} differs from max {maxCycleDuration} by {cycleDiff:P0}! " +
                        $"Consider matching cycle lengths for better sync.");
                }
            }
            
            byte syncPhase = (byte)(syncPhaseIdx + 1);
            int phaseCount = phaseBuffer.Length;
            
            // Get current phase
            byte currentPhase = 1;
            if (EntityManager.HasComponent<Game.Net.TrafficLights>(entity))
            {
                var trafficLights = EntityManager.GetComponentData<Game.Net.TrafficLights>(entity);
                currentPhase = trafficLights.m_CurrentSignalGroup;
            }
            
            // V119.3: Validate currentPhase is within bounds
            if (currentPhase == 0 || currentPhase > phaseCount)
            {
                if (shouldLogDetailed)
                {
                    if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-WARNING] V119: Entity[{entity.Index}] has invalid currentPhase={currentPhase} (phaseCount={phaseCount}) - resetting to P1");
                }
                currentPhase = 1;
            }
            
            // === V91 COORDINATED SYNC ===
            //
            // State Machine:
            // - SYNCED: Running freely, no interventions needed
            // - UNSYNCED: Needs initial sync
            //   - Reference: Wait in current phase until others are READY
            //   - Others: Rush to P1 by skipping phases
            // - READY_FOR_SYNC: At last phase before P1, waiting for GO
            //
            // Self-Healing: If cycle duration changes, trigger re-sync
            
            // Initialize state if not tracked yet - start with BARRIER!
            if (!m_SyncStates.ContainsKey(entity))
            {
                m_SyncStates[entity] = SyncState.BARRIER;
            }
            
            SyncState currentState = m_SyncStates[entity];
            
            // === SELF-HEALING: Detect cycle duration changes ===
            uint lastKnownCycle = 0;
            m_LastCycleDurations.TryGetValue(entity, out lastKnownCycle);
            
            if (lastKnownCycle != 0 && lastKnownCycle != ownCycleDuration)
            {
                // Cycle duration changed! Trigger re-sync for entire group
                if (currentState == SyncState.SYNCED)
                {
                    TriggerGroupResync(group.m_GroupId, entities);
                    if (shouldLogDetailed)
                    {
                        if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] >>> RESYNC TRIGGERED: Entity[{entity.Index}] cycle changed {lastKnownCycle} -> {ownCycleDuration}");
                    }
                }
                currentState = SyncState.BARRIER; // Go back to BARRIER, not UNSYNCED!
                m_SyncStates[entity] = currentState;
            }
            m_LastCycleDurations[entity] = ownCycleDuration;
            
            // === DETERMINE REFERENCE ===
            // Reference is the FIRST entity we encounter in each group
            // (Only ONE reference per group, not based on offset!)
            bool isReference = false;
            
            if (!m_GroupReferences.ContainsKey(group.m_GroupId))
            {
                // First entity in this group becomes the reference
                m_GroupReferences[group.m_GroupId] = entity;
                isReference = true;
            }
            else
            {
                Entity storedRef = m_GroupReferences[group.m_GroupId];
                
                // V119.4: Validate stored reference still exists and is valid
                if (storedRef == entity)
                {
                    isReference = true;
                }
                else if (!EntityManager.Exists(storedRef) || 
                         !EntityManager.HasComponent<CustomTrafficLights>(storedRef))
                {
                    // Stored reference no longer valid - take over as new reference
                    m_GroupReferences[group.m_GroupId] = entity;
                    isReference = true;
                    if (shouldLogDetailed)
                    {
                        if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] V119: Group[{group.m_GroupId}] reference was invalid - Entity[{entity.Index}] is now REF");
                    }
                }
            }
            
            // Calculate which phase comes before syncPhase
            // V119: phaseCount >= 2 is guaranteed by V119.1 check above
            byte phaseBeforeSync = (byte)(syncPhase == 1 ? phaseCount : syncPhase - 1);
            bool isAtPhaseBeforeSync = (currentPhase == phaseBeforeSync);
            bool isInSyncPhase = (currentPhase == syncPhase);
            
            // Calculate next phase (for rushing)
            // V119: Safe modulo since phaseCount >= 2
            byte nextPhase = (byte)((currentPhase % phaseCount) + 1);
            
            string decisionReason = "";
            byte avoidPhase = 0;
            
            // === V91 CONTINUOUS COORDINATED SYNC ===
            //
            // Key insight: Don't just sync ONCE, sync EVERY cycle!
            // This compensates for slight cycle length differences.
            //
            // Even in SYNCED state, we still coordinate at the phase before P1.
            // This keeps intersections aligned even if they drift slightly.
            //
            // BARRIER Phase: Block P1 for everyone until all are out of P1!
            // This "aligns" all intersections before the real sync begins.
            
            // === STATE MACHINE ===
            
            if (currentState == SyncState.BARRIER)
            {
                // === BARRIER: Block P1 until all are aligned ===
                // This is the "Einnorden" phase - get everyone out of P1 first!
                
                avoidPhase = syncPhase; // Block P1 for everyone!
                
                if (isInSyncPhase)
                {
                    // Still in P1 - let it finish, then we'll be blocked from re-entering
                    decisionReason = "BARRIER_EXITING_P1";
                    phasesHeld++;
                }
                else
                {
                    // Not in P1 - check if ALL in group are out of P1
                    bool allOutOfP1 = AreAllOutOfSyncPhase(group.m_GroupId, syncPhase, entities);
                    
                    if (allOutOfP1)
                    {
                        // Everyone is out of P1! Transition to UNSYNCED to begin real sync
                        m_SyncStates[entity] = SyncState.UNSYNCED;
                        decisionReason = "BARRIER_COMPLETE";
                        
                        if (shouldLogDetailed)
                        {
                            if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] >>> BARRIER COMPLETE: Entity[{entity.Index}] Group[{group.m_GroupId}] - all aligned, starting sync!");
                        }
                    }
                    else
                    {
                        // Still waiting for others to exit P1
                        decisionReason = "BARRIER_WAITING";
                    }
                    phasesReleased++;
                }
            }
            else if (currentState == SyncState.SYNCED)
            {
                // === SYNCED: Still coordinate at phase before P1! ===
                // This is the key to handling slight cycle differences!
                
                if (isInSyncPhase)
                {
                    // In P1 - V114: Control P1 EXIT as well as entry!
                    // Non-REF entities should NOT leave P1 before REF does!
                    if (!isReference)
                    {
                        // V114: Check if REF is still in P1
                        bool refStillInP1 = IsReferenceInSyncPhase(group.m_GroupId);
                        
                        if (refStillInP1)
                        {
                            // REF is still in P1 - block our exit!
                            // avoid = phase after sync (P2 for syncPhase=P1)
                            byte phaseAfterSync = (byte)(syncPhase == phaseCount ? 1 : syncPhase + 1);
                            avoidPhase = phaseAfterSync;
                            decisionReason = "SYNCED_IN_P1_WAIT_REF_EXIT";
                            phasesHeld++;
                        }
                        else
                        {
                            // REF has left P1 - we can leave too!
                            avoidPhase = 0;
                            decisionReason = "SYNCED_IN_P1_REF_LEFT";
                            phasesReleased++;
                        }
                    }
                    else
                    {
                        // Reference in P1 - THIS is when we set GoTimestamp!
                        // V107: Set GoTimestamp when REF actually ENTERS P1, not when GO is given
                        // This ensures offset timing is relative to REF's actual P1 entry!
                        uint currentGoTimestamp = GetGroupGoTimestamp(group.m_GroupId);
                        if (currentGoTimestamp == 0)
                        {
                            SetGroupGoTimestamp(group.m_GroupId, group.m_GroupTimer);
                        }
                        avoidPhase = 0;
                        decisionReason = "SYNCED_REF_IN_P1";
                        phasesHeld++;
                    }
                }
                else if (isAtPhaseBeforeSync)
                {
                    // At phase before P1 - COORDINATE!
                    if (isReference)
                    {
                        // Reference: Check if all others are ready
                        bool allReady = AreAllOthersReady(group.m_GroupId, entity, entities);
                        
                        if (allReady)
                        {
                            // All ready - proceed to P1!
                            // V107: GoTimestamp is set when REF enters P1 (SYNCED_REF_IN_P1), not here!
                            avoidPhase = 0;
                            decisionReason = "SYNCED_REF_GO";
                            phasesForced++;
                        }
                        else
                        {
                            // Others not ready - wait (avoid P1)
                            // V107: Reset GoTimestamp for next GO cycle
                            SetGroupGoTimestamp(group.m_GroupId, 0);
                            avoidPhase = syncPhase;
                            decisionReason = "SYNCED_REF_WAIT";
                            phasesHeld++;
                        }
                    }
                    else
                    {
                        // Non-reference: Check if reference is ready AND all siblings are ready!
                        // This prevents one entity from going to P1 while others are still rushing!
                        bool refReady = IsReferenceReady(group.m_GroupId, entities);
                        bool allSiblingsReady = AreAllOthersReadyForEntity(group.m_GroupId, entity, entities);
                        
                        if (refReady && allSiblingsReady)
                        {
                            // Reference AND all siblings ready - check offset!
                            // V107: Offset determines when this entity can go to P1
                            ushort myOffsetSeconds = ctl.m_CycleOffsetSeconds;
                            
                            // V120: Apply modulo reduction to prevent excessive wait times
                            // Beispiel: 30s Offset bei 8s Zykluszeit → 6s effektiv
                            if (myOffsetSeconds > 0 && maxCycleSeconds > 0 && myOffsetSeconds >= maxCycleSeconds)
                            {
                                myOffsetSeconds = (ushort)(myOffsetSeconds % maxCycleSeconds);
                            }
                            
                            if (myOffsetSeconds == 0)
                            {
                                // No offset - go immediately!
                                avoidPhase = 0;
                                decisionReason = "SYNCED_OTHER_GO";
                                phasesForced++;
                            }
                            else
                            {
                                // Has offset - check if enough time has passed since REF ENTERED P1
                                // V107: GoTimestamp is only set when REF is in P1!
                                uint goTimestamp = GetGroupGoTimestamp(group.m_GroupId);
                                
                                if (goTimestamp == 0)
                                {
                                    // REF hasn't entered P1 yet - wait for REF to enter P1 first!
                                    avoidPhase = syncPhase;
                                    decisionReason = "SYNCED_OTHER_WAIT_REF_P1";
                                    phasesHeld++;
                                }
                                else
                                {
                                    // REF is in P1 - check if our offset time has passed
                                    uint timeSinceRefP1 = group.m_GroupTimer - goTimestamp;
                                    uint offsetInFrames = GreenWaveTimeConstants.RealSecondsToFrames(myOffsetSeconds);
                                    
                                    // V108: Debug logging for offset timing
                                    if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-OFFSET] Entity[{entity.Index}] offset={myOffsetSeconds}s ({offsetInFrames}frames), timer={group.m_GroupTimer}, goTS={goTimestamp}, elapsed={timeSinceRefP1}frames ({timeSinceRefP1/60f:F1}s)");
                                    
                                    if (timeSinceRefP1 >= offsetInFrames)
                                    {
                                        // Offset time reached - GO!
                                        avoidPhase = 0;
                                        decisionReason = "SYNCED_OTHER_GO";
                                        phasesForced++;
                                    }
                                    else
                                    {
                                        // Offset time not reached yet - wait
                                        avoidPhase = syncPhase;
                                        decisionReason = "SYNCED_OTHER_WAIT_OFFSET";
                                        phasesHeld++;
                                    }
                                }
                            }
                        }
                        else if (refReady && !allSiblingsReady)
                        {
                            // Reference ready but siblings still rushing - wait for them!
                            avoidPhase = syncPhase;
                            decisionReason = "SYNCED_OTHER_WAIT_SIBLINGS";
                            phasesHeld++;
                        }
                        else
                        {
                            // Reference not ready - wait (avoid P1)
                            avoidPhase = syncPhase;
                            decisionReason = "SYNCED_OTHER_WAIT";
                            phasesHeld++;
                        }
                    }
                }
                else
                {
                    // Not at our phaseBeforeSync yet
                    if (isReference)
                    {
                        // V116: Only reset GoTimestamp if ALL entities with offset have entered P1!
                        // Otherwise offset entities get stranded waiting for a timestamp that was reset!
                        uint currentGoTimestamp = GetGroupGoTimestamp(group.m_GroupId);
                        if (currentGoTimestamp != 0)
                        {
                            // Check if safe to reset
                            bool allOffsetEntitiesInP1 = HaveAllOffsetEntitiesEnteredP1OrPastP1(group.m_GroupId, entities);
                            
                            if (allOffsetEntitiesInP1)
                            {
                                SetGroupGoTimestamp(group.m_GroupId, 0);
                                if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] V116: Reset GoTimestamp for Group[{group.m_GroupId}] - REF left P1, all offset entities entered P1");
                            }
                            else
                            {
                                // Don't reset yet - some offset entities haven't entered P1!
                                if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] V116: KEEPING GoTimestamp for Group[{group.m_GroupId}] - offset entities still waiting to enter P1");
                            }
                        }
                        
                        // REF: Check if any OTHER is already waiting at their phaseBeforeSync!
                        bool anyOtherWaiting = AreAnyOthersAtPhaseBeforeSync(group.m_GroupId, entity, entities);
                        
                        if (anyOtherWaiting)
                        {
                            // Others are waiting! Rush to our phaseBeforeSync!
                            avoidPhase = syncPhase; // Block P1 so we stop at phaseBeforeSync
                            decisionReason = "SYNCED_REF_RUSH_OTHERS_WAITING";
                            phasesHeld++;
                        }
                        else
                        {
                            // No one waiting yet - run freely
                            avoidPhase = 0;
                            decisionReason = "SYNCED_FREE";
                            phasesReleased++;
                        }
                    }
                    else
                    {
                        // Non-REF: Check if REF is already waiting! If so, we should hurry!
                        bool refAtWaitPoint = IsReferenceAtPhaseBeforeSync(group.m_GroupId, entities);
                        
                        if (refAtWaitPoint)
                        {
                            // REF is waiting - rush to our phaseBeforeSync!
                            avoidPhase = syncPhase; // Block P1 so we stop at phaseBeforeSync
                            decisionReason = "SYNCED_RUSH_REF_WAITING";
                            phasesHeld++;
                        }
                        else
                        {
                            // REF not waiting yet - run freely
                            avoidPhase = 0;
                            decisionReason = "SYNCED_FREE";
                            phasesReleased++;
                        }
                    }
                }
            }
            else if (currentState == SyncState.UNSYNCED || currentState == SyncState.RUSHING_TO_P1 || 
                     currentState == SyncState.WAITING_FOR_OTHERS || currentState == SyncState.READY_FOR_SYNC)
            {
                if (isReference)
                {
                    // === REFERENCE LOGIC ===
                    
                    if (isInSyncPhase)
                    {
                        // Already in sync phase - check if all others are ALSO in sync phase!
                        // Not just "ready" - they must actually BE in P1!
                        bool allInSyncPhase = AreAllOthersInSyncPhase(group.m_GroupId, entity, entities);
                        
                        if (allInSyncPhase)
                        {
                            // GO! Everyone is in P1 together!
                            SetGroupSynced(group.m_GroupId, entities);
                            avoidPhase = 0;
                            decisionReason = "REF_GO_ALL_SYNCED";
                            if (shouldLogDetailed)
                            {
                                if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] >>> SYNC COMPLETE! Group[{group.m_GroupId}] all intersections synchronized!");
                            }
                        }
                        else
                        {
                            // In P1 but others not in P1 yet - wait for them
                            avoidPhase = 0;
                            decisionReason = "REF_IN_P1_WAITING";
                        }
                        phasesHeld++;
                    }
                    else if (isAtPhaseBeforeSync)
                    {
                        // At phase before P1 - check if all others are ready
                        bool allReady = AreAllOthersReady(group.m_GroupId, entity, entities);
                        
                        if (allReady)
                        {
                            // All ready! Allow P1!
                            avoidPhase = 0;
                            m_SyncStates[entity] = SyncState.READY_FOR_SYNC;
                            decisionReason = "REF_ALL_READY_GO";
                            phasesForced++;
                        }
                        else
                        {
                            // Others not ready - WAIT in current phase (avoid P1)
                            avoidPhase = syncPhase;
                            m_SyncStates[entity] = SyncState.WAITING_FOR_OTHERS;
                            decisionReason = "REF_WAITING";
                            phasesHeld++;
                        }
                    }
                    else if (currentState == SyncState.WAITING_FOR_OTHERS)
                    {
                        // CRITICAL: We were waiting but moved to a different phase!
                        // This means we somehow left phaseBeforeSync - need to rush back!
                        // Block P1 until we get back to phaseBeforeSync
                        avoidPhase = syncPhase;
                        decisionReason = "REF_RUSHING_TO_WAIT";
                        phasesHeld++;
                    }
                    else
                    {
                        // Not at phase before P1 yet
                        // CHECK: Are any others already waiting at their phaseBeforeSync?
                        // If so, we should hurry to our own phaseBeforeSync!
                        bool anyOthersWaiting = AreAnyOthersWaiting(group.m_GroupId, entity, entities);
                        
                        if (anyOthersWaiting)
                        {
                            // Others are waiting! Rush to phaseBeforeSync but block P1!
                            avoidPhase = syncPhase;
                            m_SyncStates[entity] = SyncState.RUSHING_TO_P1;
                            decisionReason = "REF_RUSHING_OTHERS_WAITING";
                            phasesHeld++;
                        }
                        else
                        {
                            // No one waiting yet - run normally
                            avoidPhase = 0;
                            decisionReason = "REF_RUNNING";
                            phasesReleased++;
                        }
                    }
                }
                else
                {
                    // === NON-REFERENCE LOGIC ===
                    
                    if (isInSyncPhase)
                    {
                        // Already in sync phase - we're synced!
                        m_SyncStates[entity] = SyncState.READY_FOR_SYNC;
                        avoidPhase = 0;
                        decisionReason = "OTHER_IN_P1";
                        phasesHeld++;
                    }
                    else if (isAtPhaseBeforeSync)
                    {
                        // At phase before P1 - we're READY!
                        m_SyncStates[entity] = SyncState.READY_FOR_SYNC;
                        
                        // Check if EVERYONE (reference AND all others) is ready
                        // Not just the reference! All entities must be at their wait points!
                        bool refReady = IsReferenceReady(group.m_GroupId, entities);
                        bool othersReady = AreAllOthersReadyForEntity(group.m_GroupId, entity, entities);
                        
                        if (refReady && othersReady)
                        {
                            // Everyone is ready - we can proceed to P1!
                            avoidPhase = 0;
                            decisionReason = "OTHER_READY_GO";
                        }
                        else
                        {
                            // Wait for everyone
                            avoidPhase = syncPhase;
                            decisionReason = refReady ? "OTHER_READY_WAIT_OTHERS" : "OTHER_READY_WAIT";
                        }
                        phasesForced++;
                    }
                    else
                    {
                        // Not at phase before P1 - RUSH to get there!
                        m_SyncStates[entity] = SyncState.RUSHING_TO_P1;
                        
                        // Calculate phase after next (to check if we'd skip past the waiting point)
                        byte phaseAfterNext = (byte)((nextPhase % phaseCount) + 1);
                        
                        if (nextPhase == syncPhase)
                        {
                            // Next phase IS P1 - don't skip it, but check if we should wait
                            bool refReady = IsReferenceReady(group.m_GroupId, entities);
                            bool othersReady = AreAllOthersReadyForEntity(group.m_GroupId, entity, entities);
                            if (refReady && othersReady)
                            {
                                avoidPhase = 0;
                                decisionReason = "OTHER_P1_NEXT_GO";
                            }
                            else
                            {
                                avoidPhase = syncPhase;
                                decisionReason = refReady ? "OTHER_P1_NEXT_WAIT_OTHERS" : "OTHER_P1_NEXT_WAIT";
                            }
                        }
                        else if (nextPhase == phaseBeforeSync)
                        {
                            // Next phase is the phase BEFORE P1 - go there and WAIT!
                            // Block P1 so we stay at phaseBeforeSync!
                            avoidPhase = syncPhase; // Block P1 = wait at phaseBeforeSync!
                            decisionReason = $"OTHER_RUSH_ARRIVE_P{nextPhase}";
                        }
                        else if (phaseAfterNext == syncPhase)
                        {
                            // After next phase would be P1 - so nextPhase IS phaseBeforeSync
                            // Go there and wait!
                            avoidPhase = syncPhase; // Block P1 = wait!
                            decisionReason = $"OTHER_RUSH_STOP_BEFORE_P1";
                        }
                        else
                        {
                            // SEQUENTIAL MODE FIX:
                            // In Sequential Mode, we can't "skip" phases - they run in order.
                            // Just let phases progress normally - only block P1 when we get close!
                            // avoidPhase = 0 means "don't block anything, let it progress"
                            avoidPhase = 0;
                            decisionReason = $"OTHER_RUSH_RUNNING_P{nextPhase}";
                        }
                        phasesReleased++;
                    }
                }
            }
            
            // === DETAILED LOGGING ===
            if (shouldLogDetailed)
            {
                string stateStr = currentState.ToString();
                string refStr = isReference ? "REF" : "   ";
                
                if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] Entity[{entity.Index}] Group[{group.m_GroupId}] [{stateStr}] {refStr}: " +
                    $"own={ownCycleDuration}, current=P{currentPhase}, syncPhase=P{syncPhase}, " +
                    $"decision={decisionReason}, avoid={avoidPhase}");
            }
            
            // === APPLY AVOID ===
            byte previousAvoidGroup = ctl.m_AvoidSignalGroup;
            
            // Ensure manual is always 0 (clear any legacy state)
            if (ctl.m_ManualSignalGroup != 0)
            {
                ctl.SetManualSignalGroup(0);
            }
            
            // Apply avoid signal group
            ctl.m_AvoidSignalGroup = avoidPhase;
            
            // Log significant changes
            if (shouldLogDetailed)
            {
                if (previousAvoidGroup != avoidPhase)
                {
                    if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] >>> AVOID CHANGE: Entity[{entity.Index}] Avoid: {previousAvoidGroup} -> {avoidPhase}");
                }
            }
            
            EntityManager.SetComponentData(entity, ctl);
            syncedCount++;
            
            } // end try
            catch (System.Exception ex)
            {
                // V119.5: Log error but continue processing other entities
                Mod.m_Log.Error($"[GW-ERROR] V119: Exception processing Entity[{entity.Index}]: {ex.Message}");
                // Don't re-throw - continue with next entity
            }
        }
        
        // === SUMMARY LOG ===
        if (shouldLogSummary)
        {
            if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] ========== SYNC CYCLE END ==========");
            if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] STATS: synced={syncedCount}, standalone={standaloneCount}, orphaned={skippedNoGroup}, noPhases={skippedNoPhases}, timeWindowSkip={skippedTimeWindow}");
            if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] DECISIONS: forced={phasesForced}, held={phasesHeld}, released={phasesReleased}");
        }
        
        entities.Dispose();
    }
    
    /// <summary>
    /// Gets the maximum cycle duration among all intersections in a group.
    /// This is used as the sync period - all intersections sync to this.
    /// </summary>
    private uint GetMaxCycleDurationInGroup(DynamicBuffer<SyncGroup> groups, uint groupId, NativeArray<Entity> allEntities)
    {
        uint maxDuration = 0;
        
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            if (ctl.m_SyncGroupId != groupId || !ctl.m_UseSyncedCycle)
                continue;
            
            if (!EntityManager.HasBuffer<CustomPhaseData>(entity))
                continue;
            
            var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(entity, true);
            
            uint cycleDuration = 0;
            for (int p = 0; p < phaseBuffer.Length; p++)
            {
                cycleDuration += (uint)((phaseBuffer[p].m_MinimumDuration + phaseBuffer[p].m_MaximumDuration) / 2);
            }
            
            if (cycleDuration > maxDuration)
                maxDuration = cycleDuration;
        }
        
        return maxDuration;
    }
    
    // ==================== V91 COORDINATED SYNC HELPERS ====================
    
    /// <summary>
    /// Triggers a re-sync for all entities in a group.
    /// Called when cycle duration changes (self-healing).
    /// Goes back to BARRIER to ensure proper alignment.
    /// </summary>
    private void TriggerGroupResync(uint groupId, NativeArray<Entity> allEntities)
    {
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            if (ctl.m_SyncGroupId != groupId || !ctl.m_UseSyncedCycle)
                continue;
            
            m_SyncStates[entity] = SyncState.BARRIER; // Back to BARRIER for proper re-alignment!
        }
        
        if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[GW-DEBUG] Group[{groupId}] re-sync triggered - all entities set to BARRIER");
    }
    
    /// <summary>
    /// Checks if all entities in a group are OUT of the sync phase (P1).
    /// Used in BARRIER state to ensure everyone is aligned before starting sync.
    /// </summary>
    private bool AreAllOutOfSyncPhase(uint groupId, byte syncPhase, NativeArray<Entity> allEntities)
    {
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            if (ctl.m_SyncGroupId != groupId || !ctl.m_UseSyncedCycle)
                continue;
            
            // Check current phase
            if (EntityManager.HasComponent<Game.Net.TrafficLights>(entity))
            {
                var trafficLights = EntityManager.GetComponentData<Game.Net.TrafficLights>(entity);
                
                if (trafficLights.m_CurrentSignalGroup == syncPhase)
                {
                    // Someone is still in P1!
                    return false;
                }
            }
        }
        
        return true; // Everyone is out of P1!
    }
    
    /// <summary>
    /// V119.3b: Safely gets the current phase of an entity with bounds checking.
    /// Returns 1 if phase is invalid (0 or > phaseCount).
    /// This prevents issues when phases are deleted while sync is active.
    /// </summary>
    private byte GetSafeCurrentPhase(Entity entity, int phaseCount)
    {
        if (!EntityManager.HasComponent<Game.Net.TrafficLights>(entity))
            return 1;
        
        var trafficLights = EntityManager.GetComponentData<Game.Net.TrafficLights>(entity);
        byte phase = trafficLights.m_CurrentSignalGroup;
        
        // Validate bounds
        if (phase == 0 || phase > phaseCount)
        {
            return 1; // Reset to P1 if invalid
        }
        
        return phase;
    }
    
    /// <summary>
    /// Checks if all non-reference entities in a group are ready for sync.
    /// "Ready" means they are at phaseBeforeSync (waiting to enter P1).
    /// NOT just the state - we check ACTUAL PHASE!
    /// </summary>
    private bool AreAllOthersReady(uint groupId, Entity referenceEntity, NativeArray<Entity> allEntities)
    {
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            
            if (entity == referenceEntity)
                continue; // Skip reference
            
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            if (ctl.m_SyncGroupId != groupId || !ctl.m_UseSyncedCycle)
                continue;
            
            // Check state
            if (!m_SyncStates.ContainsKey(entity))
                return false; // Not tracked yet
            
            SyncState state = m_SyncStates[entity];
            
            // Must be READY_FOR_SYNC or SYNCED
            if (state != SyncState.READY_FOR_SYNC && state != SyncState.SYNCED)
                return false;
            
            // CRITICAL: Also check ACTUAL PHASE
            if (!EntityManager.HasComponent<Game.Net.TrafficLights>(entity) ||
                !EntityManager.HasBuffer<CustomPhaseData>(entity))
                return false;
            
            var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(entity, true);
            
            byte syncPhase = (byte)(ctl.m_SyncPhaseIndex + 1);
            int phaseCount = phaseBuffer.Length;
            byte phaseBeforeSync = (byte)(syncPhase == 1 ? phaseCount : syncPhase - 1);
            byte currentPhase = GetSafeCurrentPhase(entity, phaseCount); // V119.3b: Safe access
            
            // V110: Ready means:
            // 1. At phaseBeforeSync (waiting)
            // 2. In syncPhase/P1
            // 3. Already PAST P1 (P2, P3, etc. - completed this cycle's sync!)
            
            if (currentPhase == phaseBeforeSync || currentPhase == syncPhase)
            {
                continue; // Ready!
            }
            
            // Check if past syncPhase (already went through P1)
            bool isPastSyncPhase = false;
            if (currentPhase > syncPhase && currentPhase < phaseBeforeSync)
            {
                isPastSyncPhase = true;
            }
            
            if (!isPastSyncPhase)
            {
                return false; // Not at the right phase yet!
            }
        }
        
        return true; // Everyone is ready!
    }
    
    /// <summary>
    /// Checks if all non-reference entities are currently IN the sync phase (P1).
    /// Used to verify SYNC COMPLETE - everyone must actually BE in P1!
    /// </summary>
    private bool AreAllOthersInSyncPhase(uint groupId, Entity referenceEntity, NativeArray<Entity> allEntities)
    {
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            
            if (entity == referenceEntity)
                continue; // Skip reference
            
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            if (ctl.m_SyncGroupId != groupId || !ctl.m_UseSyncedCycle)
                continue;
            
            // Check ACTUAL PHASE - must be IN sync phase (P1)!
            if (!EntityManager.HasComponent<Game.Net.TrafficLights>(entity) ||
                !EntityManager.HasBuffer<CustomPhaseData>(entity))
                return false;
            
            var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(entity, true);
            int phaseCount = phaseBuffer.Length;
            byte syncPhase = (byte)(ctl.m_SyncPhaseIndex + 1);
            byte currentPhase = GetSafeCurrentPhase(entity, phaseCount); // V119.3b: Safe access
            
            // Must be IN the sync phase, not just ready for it!
            if (currentPhase != syncPhase)
                return false; // Not in P1 yet!
        }
        
        return true; // Everyone is IN P1!
    }
    
    /// <summary>
    /// Checks if ANY non-reference entity is already waiting at their phaseBeforeSync.
    /// Used by REF to know if it should hurry to its own phaseBeforeSync.
    /// </summary>
    private bool AreAnyOthersWaiting(uint groupId, Entity referenceEntity, NativeArray<Entity> allEntities)
    {
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            
            if (entity == referenceEntity)
                continue; // Skip reference
            
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            if (ctl.m_SyncGroupId != groupId || !ctl.m_UseSyncedCycle)
                continue;
            
            // Check state - is this entity waiting?
            if (!m_SyncStates.ContainsKey(entity))
                continue;
            
            SyncState state = m_SyncStates[entity];
            
            // If READY_FOR_SYNC, they're at their phaseBeforeSync waiting!
            if (state == SyncState.READY_FOR_SYNC)
            {
                // Double-check they're actually at phaseBeforeSync
                if (!EntityManager.HasComponent<Game.Net.TrafficLights>(entity) ||
                    !EntityManager.HasBuffer<CustomPhaseData>(entity))
                    continue;
                
                var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(entity, true);
                
                byte syncPhase = (byte)(ctl.m_SyncPhaseIndex + 1);
                int phaseCount = phaseBuffer.Length;
                byte phaseBeforeSync = (byte)(syncPhase == 1 ? phaseCount : syncPhase - 1);
                byte currentPhase = GetSafeCurrentPhase(entity, phaseCount); // V119.3b: Safe access
                
                if (currentPhase == phaseBeforeSync)
                {
                    return true; // Found someone waiting!
                }
            }
        }
        
        return false; // No one is waiting yet
    }
    
    /// <summary>
    /// Checks if any OTHER entity (non-reference) is currently at their phaseBeforeSync.
    /// Used by REF in SYNCED mode to know if it should rush to its own phaseBeforeSync.
    /// This checks ACTUAL PHASE, not just state - works for SYNCED entities!
    /// </summary>
    private bool AreAnyOthersAtPhaseBeforeSync(uint groupId, Entity referenceEntity, NativeArray<Entity> allEntities)
    {
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            
            if (entity == referenceEntity)
                continue; // Skip reference (that's us!)
            
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            if (ctl.m_SyncGroupId != groupId || !ctl.m_UseSyncedCycle)
                continue;
            
            // Check ACTUAL PHASE - regardless of state!
            if (!EntityManager.HasComponent<Game.Net.TrafficLights>(entity) ||
                !EntityManager.HasBuffer<CustomPhaseData>(entity))
                continue;
            
            var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(entity, true);
            
            byte syncPhase = (byte)(ctl.m_SyncPhaseIndex + 1);
            int phaseCount = phaseBuffer.Length;
            byte phaseBeforeSync = (byte)(syncPhase == 1 ? phaseCount : syncPhase - 1);
            byte currentPhase = GetSafeCurrentPhase(entity, phaseCount); // V119.3b: Safe access
            
            // If this entity is at their phaseBeforeSync, they're waiting!
            if (currentPhase == phaseBeforeSync)
            {
                return true; // Found someone at their wait point!
            }
        }
        
        return false; // No one at their phaseBeforeSync yet
    }
    
    /// <summary>
    /// Checks if all OTHER non-reference entities in a group are ready.
    /// This is called by a non-reference entity to check if its siblings are ready.
    /// </summary>
    private bool AreAllOthersReadyForEntity(uint groupId, Entity callingEntity, NativeArray<Entity> allEntities)
    {
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            
            if (entity == callingEntity)
                continue; // Skip self
            
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            if (ctl.m_SyncGroupId != groupId || !ctl.m_UseSyncedCycle)
                continue;
            
            // Skip reference - we check that separately with IsReferenceReady
            if (m_GroupReferences.ContainsKey(groupId) && m_GroupReferences[groupId] == entity)
                continue;
            
            // Check state - must be READY_FOR_SYNC or SYNCED
            if (!m_SyncStates.ContainsKey(entity))
                return false;
            
            SyncState state = m_SyncStates[entity];
            if (state != SyncState.READY_FOR_SYNC && state != SyncState.SYNCED)
                return false;
            
            // Check actual phase
            if (!EntityManager.HasComponent<Game.Net.TrafficLights>(entity) ||
                !EntityManager.HasBuffer<CustomPhaseData>(entity))
                return false;
            
            var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(entity, true);
            
            byte syncPhase = (byte)(ctl.m_SyncPhaseIndex + 1);
            int phaseCount = phaseBuffer.Length;
            byte phaseBeforeSync = (byte)(syncPhase == 1 ? phaseCount : syncPhase - 1);
            byte currentPhase = GetSafeCurrentPhase(entity, phaseCount); // V119.3b: Safe access
            
            // V110: Ready means:
            // 1. At phaseBeforeSync (waiting to enter P1)
            // 2. In syncPhase/P1 (currently in sync phase)
            // 3. Already PAST P1 (P2, P3, etc. - they've completed this cycle's sync!)
            //
            // NOT ready means: between P2 and phaseBeforeSync (still catching up)
            //
            // For syncPhase=1 (P1):
            //   Ready: P1 (sync), phaseBeforeSync, or any phase > P1 but before phaseBeforeSync
            //   Actually simpler: Ready if at phaseBeforeSync OR anywhere from P1 onwards until phaseBeforeSync
            //
            // Think of it as a cycle: P1 -> P2 -> P3 -> P4 -> P5 -> P1
            // If syncPhase=P1 and phaseBeforeSync=P5:
            //   - P5: ready (waiting)
            //   - P1: ready (in sync)
            //   - P2, P3, P4: ready (already went through P1 this cycle!)
            //
            // The only "not ready" case is if someone is RUSHING to phaseBeforeSync
            // But that's handled by SyncState checks above!
            
            // Simplified: if at phaseBeforeSync or syncPhase, definitely ready
            if (currentPhase == phaseBeforeSync || currentPhase == syncPhase)
            {
                continue; // This sibling is ready!
            }
            
            // V110: Check if they're PAST syncPhase (already went through P1)
            // This is true if they're between syncPhase+1 and phaseBeforeSync-1
            // In a cycle, "past P1" means: P2, P3, P4... up to but not including phaseBeforeSync
            //
            // For 5-phase with syncPhase=1, phaseBeforeSync=5:
            //   P2, P3, P4 are "past P1" = ready
            //
            // For 4-phase with syncPhase=1, phaseBeforeSync=4:
            //   P2, P3 are "past P1" = ready
            
            bool isPastSyncPhase = false;
            if (syncPhase == 1)
            {
                // syncPhase is P1, so "past" means P2 up to phaseBeforeSync-1
                // P2 = 2, P3 = 3, ... up to phaseBeforeSync-1
                if (currentPhase > syncPhase && currentPhase < phaseBeforeSync)
                {
                    isPastSyncPhase = true;
                }
            }
            else
            {
                // syncPhase is not P1 (e.g., P3)
                // "past" means from syncPhase+1 to phaseBeforeSync-1 (wrapping around)
                // This is more complex but for now we assume syncPhase=1 (P1)
                // If needed, we can extend this logic later
                if (currentPhase > syncPhase && currentPhase < phaseBeforeSync)
                {
                    isPastSyncPhase = true;
                }
            }
            
            if (!isPastSyncPhase)
            {
                return false; // This sibling isn't ready yet!
            }
        }
        
        return true; // All siblings are ready!
    }
    
    /// <summary>
    /// Checks if the reference entity for a group is ready (waiting for others).
    /// </summary>
    private bool IsReferenceReady(uint groupId, NativeArray<Entity> allEntities)
    {
        if (!m_GroupReferences.ContainsKey(groupId))
            return false;
        
        Entity refEntity = m_GroupReferences[groupId];
        
        if (!m_SyncStates.ContainsKey(refEntity))
            return false;
        
        SyncState state = m_SyncStates[refEntity];
        
        // Reference is ready if it's waiting or at ready state
        // OR if it's SYNCED and currently at phase before P1 (continuous sync)
        if (state == SyncState.WAITING_FOR_OTHERS || state == SyncState.READY_FOR_SYNC)
            return true;
        
        if (state == SyncState.SYNCED)
        {
            // In SYNCED mode, check if reference is at phase before P1
            if (EntityManager.HasComponent<Game.Net.TrafficLights>(refEntity) &&
                EntityManager.HasBuffer<CustomPhaseData>(refEntity))
            {
                var ctl = EntityManager.GetComponentData<CustomTrafficLights>(refEntity);
                var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(refEntity, true);
                
                byte syncPhase = (byte)(ctl.m_SyncPhaseIndex + 1);
                int phaseCount = phaseBuffer.Length;
                byte phaseBeforeSync = (byte)(syncPhase == 1 ? phaseCount : syncPhase - 1);
                byte currentPhase = GetSafeCurrentPhase(refEntity, phaseCount); // V119.3b: Safe access
                
                // V110: Ready if:
                // 1. At phaseBeforeSync (waiting)
                // 2. In syncPhase/P1
                // 3. Already PAST P1 (P2, P3, etc.)
                if (currentPhase == phaseBeforeSync || currentPhase == syncPhase)
                {
                    return true;
                }
                
                // Check if past syncPhase (already went through P1)
                if (currentPhase > syncPhase && currentPhase < phaseBeforeSync)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Checks if the reference entity is currently AT its phaseBeforeSync (waiting point).
    /// Used by non-reference entities to know if they should rush to their own waiting point.
    /// This is specifically checking if REF is WAITING, not just "ready".
    /// </summary>
    private bool IsReferenceAtPhaseBeforeSync(uint groupId, NativeArray<Entity> allEntities)
    {
        if (!m_GroupReferences.ContainsKey(groupId))
            return false;
        
        Entity refEntity = m_GroupReferences[groupId];
        
        if (!EntityManager.HasComponent<Game.Net.TrafficLights>(refEntity) ||
            !EntityManager.HasBuffer<CustomPhaseData>(refEntity) ||
            !EntityManager.HasComponent<CustomTrafficLights>(refEntity))
            return false;
        
        var ctl = EntityManager.GetComponentData<CustomTrafficLights>(refEntity);
        var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(refEntity, true);
        
        byte syncPhase = (byte)(ctl.m_SyncPhaseIndex + 1);
        int phaseCount = phaseBuffer.Length;
        byte phaseBeforeSync = (byte)(syncPhase == 1 ? phaseCount : syncPhase - 1);
        byte currentPhase = GetSafeCurrentPhase(refEntity, phaseCount); // V119.3b: Safe access
        
        // REF is at waiting point if at phaseBeforeSync
        return currentPhase == phaseBeforeSync;
    }
    
    /// <summary>
    /// V114: Checks if the reference entity is currently IN the sync phase (P1).
    /// Used to coordinate P1 exit - non-REF entities should not leave P1 before REF does!
    /// </summary>
    private bool IsReferenceInSyncPhase(uint groupId)
    {
        if (!m_GroupReferences.ContainsKey(groupId))
            return false;
        
        Entity refEntity = m_GroupReferences[groupId];
        
        if (!EntityManager.HasComponent<Game.Net.TrafficLights>(refEntity) ||
            !EntityManager.HasComponent<CustomTrafficLights>(refEntity) ||
            !EntityManager.HasBuffer<CustomPhaseData>(refEntity))
            return false;
        
        var ctl = EntityManager.GetComponentData<CustomTrafficLights>(refEntity);
        var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(refEntity, true);
        
        byte syncPhase = (byte)(ctl.m_SyncPhaseIndex + 1);
        int phaseCount = phaseBuffer.Length;
        byte currentPhase = GetSafeCurrentPhase(refEntity, phaseCount); // V119.3b: Safe access
        
        // REF is in sync phase if current == syncPhase
        return currentPhase == syncPhase;
    }
    
    /// <summary>
    /// V116: Checks if all entities with offset > 0 have entered P1 in this cycle.
    /// Used to prevent premature GoTimestamp reset that would strand offset entities.
    /// </summary>
    private bool HaveAllOffsetEntitiesEnteredP1OrPastP1(uint groupId, NativeArray<Entity> allEntities)
    {
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity) ||
                !EntityManager.HasComponent<Game.Net.TrafficLights>(entity) ||
                !EntityManager.HasBuffer<CustomPhaseData>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            if (ctl.m_SyncGroupId != groupId || !ctl.m_UseSyncedCycle)
                continue;
            
            // Skip reference - we only care about non-REF entities with offset
            if (m_GroupReferences.ContainsKey(groupId) && m_GroupReferences[groupId] == entity)
                continue;
            
            // Check if this entity has an offset
            ushort offsetSeconds = ctl.m_CycleOffsetSeconds;
            if (offsetSeconds == 0)
                continue; // No offset - doesn't matter where they are
            
            // Entity HAS offset - check if they've entered P1 or are past P1
            var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(entity, true);
            
            byte syncPhase = (byte)(ctl.m_SyncPhaseIndex + 1);
            int phaseCount = phaseBuffer.Length;
            byte phaseBeforeSync = (byte)(syncPhase == 1 ? phaseCount : syncPhase - 1);
            byte currentPhase = GetSafeCurrentPhase(entity, phaseCount); // V119.3b: Safe access
            
            // Are they still at phaseBeforeSync? (waiting to enter P1)
            if (currentPhase == phaseBeforeSync)
            {
                // This entity with offset hasn't entered P1 yet!
                return false;
            }
            
            // They're either in P1 or past P1 - that's fine
        }
        
        // All offset entities have entered P1 (or there are none with offset)
        return true;
    }
    
    /// <summary>
    /// Sets all entities in a group to SYNCED state.
    /// Called when all are ready and sync is complete.
    /// </summary>
    private void SetGroupSynced(uint groupId, NativeArray<Entity> allEntities)
    {
        // V112: Validate phase counts and warn if needed
        ValidateGroupPhaseCountsAndWarn(groupId, allEntities);
        
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            if (ctl.m_SyncGroupId != groupId || !ctl.m_UseSyncedCycle)
                continue;
            
            m_SyncStates[entity] = SyncState.SYNCED;
        }
    }
    
    // ==================== END V91 HELPERS ====================
    
    /// <summary>
    /// Gets the current game hour (0-23) from the TimeSystem.
    /// </summary>
    private int GetCurrentGameHour()
    {
        if (m_TimeSystem == null)
            return 12; // Default to noon if time system not available
        
        // normalizedTime is 0.0 to 1.0 representing a full day
        // 0.0 = midnight, 0.5 = noon, 1.0 = midnight
        float normalizedTime = m_TimeSystem.normalizedTime;
        int hour = (int)(normalizedTime * 24f) % 24;
        return hour;
    }
    
    /// <summary>
    /// Updates the BaseCycleDuration for a group based on its intersections.
    /// Should be called whenever intersections are added or removed from a group.
    /// </summary>
    public void UpdateGroupBaseCycle(uint groupId)
    {
        if (m_ManagerEntity == Entity.Null || !EntityManager.Exists(m_ManagerEntity))
            return;
        
        var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity);
        
        // Find all intersections in this group and calculate max cycle
        var query = GetEntityQuery(ComponentType.ReadOnly<CustomTrafficLights>());
        var entities = query.ToEntityArray(Allocator.Temp);
        
        uint maxCycleDuration = 0;
        int intersectionCount = 0;
        
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            if (ctl.m_SyncGroupId != groupId || !ctl.m_UseSyncedCycle)
                continue;
            
            intersectionCount++;
            
            if (!EntityManager.HasBuffer<CustomPhaseData>(entity))
                continue;
            
            var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(entity, true);
            
            uint cycleDuration = 0;
            for (int p = 0; p < phaseBuffer.Length; p++)
            {
                cycleDuration += (uint)((phaseBuffer[p].m_MinimumDuration + phaseBuffer[p].m_MaximumDuration) / 2);
            }
            
            if (cycleDuration > maxCycleDuration)
                maxCycleDuration = cycleDuration;
        }
        
        entities.Dispose();
        
        // Update the group's BaseCycleDuration (convert frames to seconds: frames / 60)
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].m_GroupId == groupId)
            {
                var group = buffer[i];
                ushort baseCycleSeconds = (ushort)(maxCycleDuration / 60);
                if (baseCycleSeconds < 10) baseCycleSeconds = 10; // Minimum 10 seconds
                group.m_BaseCycleDuration = baseCycleSeconds;
                buffer[i] = group;
                
                Mod.m_Log.Info($"[SyncGroupSystem] Updated Group[{groupId}] BaseCycle to {baseCycleSeconds}s ({maxCycleDuration} frames), {intersectionCount} intersections");
                break;
            }
        }
    }

    /// <summary>
    /// V106: Sets the GoTimestamp for a group (used for offset calculation).
    /// </summary>
    private void SetGroupGoTimestamp(uint groupId, uint timestamp)
    {
        if (m_ManagerEntity == Entity.Null || !EntityManager.Exists(m_ManagerEntity))
            return;
            
        var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity);
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].m_GroupId == groupId)
            {
                var group = buffer[i];
                group.m_GoTimestamp = timestamp;
                buffer[i] = group;
                break;
            }
        }
    }
    
    /// <summary>
    /// V106: Gets the GoTimestamp for a group.
    /// </summary>
    private uint GetGroupGoTimestamp(uint groupId)
    {
        if (m_ManagerEntity == Entity.Null || !EntityManager.Exists(m_ManagerEntity))
            return 0;
            
        var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity, true);
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].m_GroupId == groupId)
            {
                return buffer[i].m_GoTimestamp;
            }
        }
        return 0;
    }

    /// <summary>
    /// V112: Validates phase count differences in a group.
    /// Warns (once per group) if any intersection has more than 2 phases difference from others.
    /// V119: Also warns about 1-phase intersections and empty groups.
    /// This doesn't prevent sync but informs users about potential issues.
    /// </summary>
    private void ValidateGroupPhaseCountsAndWarn(uint groupId, NativeArray<Entity> allEntities)
    {
        // Only warn once per group
        if (m_PhaseWarningsIssued.Contains(groupId))
            return;
        
        int minPhases = int.MaxValue;
        int maxPhases = 0;
        List<(Entity entity, int phases)> members = new List<(Entity, int)>();
        List<Entity> singlePhaseEntities = new List<Entity>();
        
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            if (ctl.m_SyncGroupId != groupId || !ctl.m_UseSyncedCycle)
                continue;
            
            if (!EntityManager.HasBuffer<CustomPhaseData>(entity))
                continue;
            
            var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(entity, true);
            int phaseCount = phaseBuffer.Length;
            
            members.Add((entity, phaseCount));
            
            // V119.6: Track 1-phase entities separately
            if (phaseCount < 2)
            {
                singlePhaseEntities.Add(entity);
            }
            
            if (phaseCount < minPhases) minPhases = phaseCount;
            if (phaseCount > maxPhases) maxPhases = phaseCount;
        }
        
        bool hasWarning = false;
        
        // V119.6: Warn about empty groups
        if (members.Count == 0)
        {
            m_PhaseWarningsIssued.Add(groupId);
            Mod.m_Log.Warn($"[GW-WARNING] V119: Group[{groupId}] has NO active members! Consider removing the group.");
            return;
        }
        
        // V119.6: Warn about single-member groups
        if (members.Count == 1)
        {
            hasWarning = true;
            Mod.m_Log.Warn($"[GW-WARNING] V119: Group[{groupId}] has only 1 member - sync requires at least 2 intersections.");
        }
        
        // V119.6: Warn about 1-phase intersections (they can't sync properly)
        if (singlePhaseEntities.Count > 0)
        {
            hasWarning = true;
            Mod.m_Log.Warn($"[GW-WARNING] V119: Group[{groupId}] contains {singlePhaseEntities.Count} intersection(s) with only 1 phase!");
            Mod.m_Log.Warn($"[GW-WARNING]   1-phase intersections CANNOT be synchronized (minimum 2 phases required).");
            foreach (var entity in singlePhaseEntities)
            {
                Mod.m_Log.Warn($"[GW-WARNING]     Entity[{entity.Index}]: 1 phase (EXCLUDED from sync)");
            }
        }
        
        // Check if difference is > 2
        int difference = maxPhases - minPhases;
        if (difference > 2 && members.Count >= 2)
        {
            hasWarning = true;
            Mod.m_Log.Warn($"[GW-WARNING] Group[{groupId}] has intersections with very different phase counts!");
            Mod.m_Log.Warn($"[GW-WARNING]   Min phases: {minPhases}, Max phases: {maxPhases} (difference: {difference})");
            Mod.m_Log.Warn($"[GW-WARNING]   For best synchronization, intersections should have similar phase counts (within ±2).");
            Mod.m_Log.Warn($"[GW-WARNING]   The sync will still work, but timing may not be optimal.");
            
            foreach (var (entity, phases) in members)
            {
                Mod.m_Log.Warn($"[GW-WARNING]     Entity[{entity.Index}]: {phases} phases");
            }
        }
        
        if (hasWarning)
        {
            m_PhaseWarningsIssued.Add(groupId);
        }
    }
    
    /// <summary>
    /// V112: Resets the warning for a group (call when group composition changes).
    /// </summary>
    private void ResetPhaseWarningForGroup(uint groupId)
    {
        m_PhaseWarningsIssued.Remove(groupId);
    }

    /// <summary>
    /// Creates or finds the singleton manager entity.
    /// </summary>
    private void CreateOrFindManagerEntity()
    {
        // First, try to find existing manager entity
        var query = GetEntityQuery(ComponentType.ReadOnly<SyncGroupManager>());
        if (!query.IsEmpty)
        {
            var entities = query.ToEntityArray(Allocator.Temp);
            if (entities.Length > 0)
            {
                m_ManagerEntity = entities[0];
            }
            entities.Dispose();
            return;
        }

        // Create new manager entity
        m_ManagerEntity = EntityManager.CreateEntity();
        EntityManager.AddComponentData(m_ManagerEntity, new SyncGroupManager());
        EntityManager.AddBuffer<SyncGroup>(m_ManagerEntity);
        
        Mod.m_Log.Info("[SyncGroupSystem] Created manager entity");
    }

    /// <summary>
    /// Creates a new sync group with the given name and cycle duration.
    /// Returns the new group's ID.
    /// </summary>
    public uint CreateGroup(string name, ushort cycleDuration)
    {
        if (m_ManagerEntity == Entity.Null || !EntityManager.Exists(m_ManagerEntity))
        {
            CreateOrFindManagerEntity();
        }

        // Get next ID
        var manager = EntityManager.GetComponentData<SyncGroupManager>(m_ManagerEntity);
        uint newId = manager.m_NextGroupId;
        manager.m_NextGroupId++;
        EntityManager.SetComponentData(m_ManagerEntity, manager);

        // Add new group to buffer
        var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity);
        buffer.Add(new SyncGroup(newId, name, cycleDuration));

        Mod.m_Log.Info($"[SyncGroupSystem] Created group '{name}' with ID {newId}, cycle {cycleDuration}s");
        
        return newId;
    }

    /// <summary>
    /// Deletes a sync group by ID.
    /// Also clears any intersection references to this group.
    /// Returns true if the group was found and deleted.
    /// </summary>
    public bool DeleteGroup(uint groupId)
    {
        if (m_ManagerEntity == Entity.Null || !EntityManager.Exists(m_ManagerEntity))
        {
            return false;
        }

        var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity);
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].m_GroupId == groupId)
            {
                buffer.RemoveAt(i);
                Mod.m_Log.Info($"[SyncGroupSystem] Deleted group {groupId}");
                
                // Clear references from any intersections that were in this group
                ClearIntersectionReferencesToGroup(groupId);
                
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Clears sync group references from all intersections that reference the given group ID.
    /// </summary>
    private void ClearIntersectionReferencesToGroup(uint groupId)
    {
        // Create query on-demand if not yet created
        if (m_SyncedIntersectionsQuery == default)
        {
            m_SyncedIntersectionsQuery = GetEntityQuery(ComponentType.ReadWrite<CustomTrafficLights>());
        }
        
        var entities = m_SyncedIntersectionsQuery.ToEntityArray(Allocator.Temp);
        int clearedCount = 0;
        
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            if (!EntityManager.HasComponent<CustomTrafficLights>(entity))
                continue;
            
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            if (ctl.m_SyncGroupId == groupId)
            {
                // Clear group assignment
                ctl.SetSyncGroupId(0);
                ctl.SetUseSyncedCycle(false);
                ctl.SetCycleOffsetSeconds(0);
                // Clear embedded group data
                ctl.SetSyncGroupName("");
                ctl.SetSyncGroupBaseCycle(60);
                ctl.SetSyncGroupAlwaysActive(true);
                ctl.SetSyncGroupTimeWindows(255, 255, 255, 255, 255, 255);
                EntityManager.SetComponentData(entity, ctl);
                clearedCount++;
            }
        }
        
        entities.Dispose();
        
        if (clearedCount > 0)
        {
            Mod.m_Log.Info($"[SyncGroupSystem] Cleared {clearedCount} intersection references to deleted group {groupId}");
        }
    }

    /// <summary>
    /// Gets a sync group by ID.
    /// Returns null if not found.
    /// </summary>
    public SyncGroup? GetGroup(uint groupId)
    {
        if (groupId == 0 || m_ManagerEntity == Entity.Null || !EntityManager.Exists(m_ManagerEntity))
        {
            return null;
        }

        if (!EntityManager.HasBuffer<SyncGroup>(m_ManagerEntity))
        {
            return null;
        }

        var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity, true);
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].m_GroupId == groupId)
            {
                return buffer[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the current timer value for a group.
    /// Returns 0 if group not found.
    /// </summary>
    public uint GetGroupTimer(uint groupId)
    {
        var group = GetGroup(groupId);
        return group?.m_GroupTimer ?? 0;
    }

    /// <summary>
    /// Gets all sync groups.
    /// Caller must dispose the returned array!
    /// </summary>
    public NativeArray<SyncGroup> GetAllGroups(Allocator allocator)
    {
        if (m_ManagerEntity == Entity.Null || !EntityManager.Exists(m_ManagerEntity))
        {
            return new NativeArray<SyncGroup>(0, allocator);
        }

        if (!EntityManager.HasBuffer<SyncGroup>(m_ManagerEntity))
        {
            return new NativeArray<SyncGroup>(0, allocator);
        }

        var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity, true);
        var result = new NativeArray<SyncGroup>(buffer.Length, allocator);
        for (int i = 0; i < buffer.Length; i++)
        {
            result[i] = buffer[i];
        }
        return result;
    }

    /// <summary>
    /// Renames a sync group.
    /// Returns true if the group was found and renamed.
    /// </summary>
    public bool RenameGroup(uint groupId, string newName)
    {
        if (m_ManagerEntity == Entity.Null || !EntityManager.Exists(m_ManagerEntity))
        {
            return false;
        }

        var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity);
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].m_GroupId == groupId)
            {
                var group = buffer[i];
                group.SetName(newName);
                buffer[i] = group;
                Mod.m_Log.Info($"[SyncGroupSystem] Renamed group {groupId} to '{newName}'");
                
                // Propagate to all intersections in this group
                PropagateGroupSettingsToIntersections(groupId);
                
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Sets time windows for when a sync group should be active.
    /// Returns true if the group was found and updated.
    /// </summary>
    public bool SetGroupTimeWindows(uint groupId, bool alwaysActive,
        byte tw1Start, byte tw1End,
        byte tw2Start, byte tw2End,
        byte tw3Start, byte tw3End)
    {
        if (m_ManagerEntity == Entity.Null || !EntityManager.Exists(m_ManagerEntity))
        {
            return false;
        }

        var buffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity);
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].m_GroupId == groupId)
            {
                var group = buffer[i];
                group.m_AlwaysActive = alwaysActive;
                group.m_TimeWindow1Start = tw1Start;
                group.m_TimeWindow1End = tw1End;
                group.m_TimeWindow2Start = tw2Start;
                group.m_TimeWindow2End = tw2End;
                group.m_TimeWindow3Start = tw3Start;
                group.m_TimeWindow3End = tw3End;
                buffer[i] = group;
                
                if (alwaysActive)
                {
                    Mod.m_Log.Info($"[SyncGroupSystem] Group {groupId} set to always active");
                }
                else
                {
                    Mod.m_Log.Info($"[SyncGroupSystem] Group {groupId} time windows: {tw1Start}-{tw1End}, {tw2Start}-{tw2End}, {tw3Start}-{tw3End}");
                }
                
                // Propagate to all intersections in this group
                PropagateGroupSettingsToIntersections(groupId);
                
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Propagates group settings (name, time windows, etc.) to all intersections in the group.
    /// This ensures the embedded group data in each intersection is kept in sync.
    /// </summary>
    private void PropagateGroupSettingsToIntersections(uint groupId)
    {
        if (groupId == 0) return;
        
        // Get the group settings
        var groupBuffer = EntityManager.GetBuffer<SyncGroup>(m_ManagerEntity, true);
        SyncGroup? foundGroup = null;
        
        for (int i = 0; i < groupBuffer.Length; i++)
        {
            if (groupBuffer[i].m_GroupId == groupId)
            {
                foundGroup = groupBuffer[i];
                break;
            }
        }
        
        if (!foundGroup.HasValue) return;
        var group = foundGroup.Value;
        
        // Update all intersections in this group
        var query = GetEntityQuery(ComponentType.ReadWrite<CustomTrafficLights>());
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        int updateCount = 0;
        
        foreach (var entity in entities)
        {
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            if (ctl.m_SyncGroupId == groupId)
            {
                ctl.SetSyncGroupName(group.GetName());
                ctl.SetSyncGroupBaseCycle(group.m_BaseCycleDuration);
                ctl.SetSyncGroupAlwaysActive(group.m_AlwaysActive);
                ctl.SetSyncGroupTimeWindows(
                    group.m_TimeWindow1Start, group.m_TimeWindow1End,
                    group.m_TimeWindow2Start, group.m_TimeWindow2End,
                    group.m_TimeWindow3Start, group.m_TimeWindow3End
                );
                EntityManager.SetComponentData(entity, ctl);
                updateCount++;
            }
        }
        
        entities.Dispose();
        
        if (updateCount > 0)
        {
            Mod.m_Log.Info($"[SyncGroupSystem] Propagated group {groupId} settings to {updateCount} intersections");
        }
    }

    /// <summary>
    /// Counts how many intersections are using a specific group.
    /// This queries all CustomTrafficLights components.
    /// </summary>
    public int CountIntersectionsInGroup(uint groupId)
    {
        if (groupId == 0) return 0;
        
        int count = 0;
        var query = GetEntityQuery(ComponentType.ReadOnly<CustomTrafficLights>());
        var entities = query.ToEntityArray(Allocator.Temp);
        
        for (int i = 0; i < entities.Length; i++)
        {
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entities[i]);
            if (ctl.m_SyncGroupId == groupId)
            {
                count++;
            }
        }
        
        // Only log when count > 0 or for debugging
        if (count > 0)
        {
            if (IsDebugLoggingEnabled) Mod.m_Log.Info($"[CountIntersections] Group {groupId}: {count} intersections (of {entities.Length} total CTL entities)");
        }
        
        entities.Dispose();
        return count;
    }
    
    /// <summary>
    /// Data structure for intersection details in a sync group
    /// </summary>
    public struct IntersectionInfo
    {
        public Entity Entity;
        public int Index;              // Display index (1-based)
        public ushort CycleOffset;     // Offset in frames (internal)
        public float OffsetRealSeconds; // Offset in real-time seconds (for UI)
        public byte SyncPhaseIndex;
        public bool UseSyncedCycle;
        public uint TotalCycleDuration;
        public int PhaseCount;
        public uint CurrentPhaseProgress; // How far into current cycle (0-100%)
        public int CurrentPhaseIndex;     // Which phase is currently active (0-based)
        public bool IsSyncPhaseActive;    // Is the sync phase (green wave phase) currently active?
        public bool IsReference;          // Is this the reference intersection (position 0)?
        public int Position;              // Position in group for ordering (0 = first/reference)
        
        // Extended stats (V15)
        public int EdgeCount;             // Number of connected roads/edges
        public int TotalLaneCount;        // Total number of car lanes
        public int LeftLaneCount;         // Lanes turning left
        public int StraightLaneCount;     // Lanes going straight
        public int RightLaneCount;        // Lanes turning right
        public int PedestrianLaneCount;   // Pedestrian crossings
    }
    
    /// <summary>
    /// Gets detailed information about all intersections in a specific group.
    /// Returns a list sorted by offset (lowest first = start of green wave).
    /// </summary>
    public List<IntersectionInfo> GetIntersectionsInGroup(uint groupId)
    {
        var result = new List<IntersectionInfo>();
        if (groupId == 0) return result;
        
        var query = GetEntityQuery(ComponentType.ReadOnly<CustomTrafficLights>());
        var entities = query.ToEntityArray(Allocator.Temp);
        
        int index = 1;
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
            
            if (ctl.m_SyncGroupId == groupId)
            {
                // Calculate total cycle duration from phases
                uint totalCycle = 0;
                int phaseCount = 0;
                var phaseMinDurations = new List<uint>();
                var phaseMaxDurations = new List<uint>();
                
                if (EntityManager.HasBuffer<CustomPhaseData>(entity))
                {
                    var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(entity, true);
                    phaseCount = phaseBuffer.Length;
                    for (int p = 0; p < phaseBuffer.Length; p++)
                    {
                        phaseMinDurations.Add(phaseBuffer[p].m_MinimumDuration);
                        phaseMaxDurations.Add(phaseBuffer[p].m_MaximumDuration);
                        // Use average for total cycle estimate
                        totalCycle += (uint)((phaseBuffer[p].m_MinimumDuration + phaseBuffer[p].m_MaximumDuration) / 2);
                    }
                }
                
                // === READ REAL TRAFFIC LIGHT STATE ===
                int currentPhaseIndex = 0;
                uint currentPhaseProgress = 0;
                bool isSyncPhaseActive = false;
                
                // Get actual current phase from Game.Net.TrafficLights
                if (EntityManager.HasComponent<Game.Net.TrafficLights>(entity))
                {
                    var trafficLights = EntityManager.GetComponentData<Game.Net.TrafficLights>(entity);
                    // m_CurrentSignalGroup is 1-indexed, convert to 0-indexed
                    currentPhaseIndex = trafficLights.m_CurrentSignalGroup > 0 
                        ? trafficLights.m_CurrentSignalGroup - 1 
                        : 0;
                    
                    // Check if sync phase is currently active
                    isSyncPhaseActive = (currentPhaseIndex == ctl.m_SyncPhaseIndex);
                    
                    // Calculate progress within current phase using CustomTrafficLights.m_Timer
                    // Progress: 0-50% = towards minimum, 50+ = past minimum (overflow/ready to switch)
                    if (currentPhaseIndex < phaseMinDurations.Count)
                    {
                        uint minDur = phaseMinDurations[currentPhaseIndex];
                        uint timer = ctl.m_Timer;
                        
                        if (timer >= minDur)
                        {
                            // Past minimum - show as 50% (overflow state)
                            currentPhaseProgress = 50;
                        }
                        else
                        {
                            // 0-50%: Progress toward minimum
                            currentPhaseProgress = minDur > 0 ? (timer * 50) / minDur : 0;
                        }
                    }
                }
                
                result.Add(new IntersectionInfo
                {
                    Entity = entity,
                    Index = index++,
                    CycleOffset = ctl.m_CycleOffsetSeconds,
                    OffsetRealSeconds = GreenWaveTimeConstants.FramesToRealSeconds(ctl.m_CycleOffsetSeconds),
                    SyncPhaseIndex = ctl.m_SyncPhaseIndex,
                    UseSyncedCycle = ctl.m_UseSyncedCycle,
                    TotalCycleDuration = totalCycle,
                    PhaseCount = phaseCount,
                    CurrentPhaseProgress = currentPhaseProgress,
                    CurrentPhaseIndex = currentPhaseIndex,
                    IsSyncPhaseActive = isSyncPhaseActive,
                    IsReference = false, // Will be set after sorting
                    Position = 0         // Will be set after sorting
                });
            }
        }
        
        entities.Dispose();
        
        // V122: Stable sort by offset (ascending), then by Entity.Index for consistency
        // This ensures the order is deterministic even when multiple intersections have the same offset
        result.Sort((a, b) => {
            int offsetCompare = a.CycleOffset.CompareTo(b.CycleOffset);
            if (offsetCompare != 0) return offsetCompare;
            // Same offset: sort by Entity.Index for stable, reproducible ordering
            return a.Entity.Index.CompareTo(b.Entity.Index);
        });
        
        // Re-index after sorting and set Position/IsReference
        for (int i = 0; i < result.Count; i++)
        {
            var info = result[i];
            info.Index = i + 1;
            info.Position = i;
            info.IsReference = (i == 0); // First one (lowest offset) is reference
            result[i] = info;
        }
        
        return result;
    }
    
    /// <summary>
    /// Moves an intersection up in the group order (decreases its offset to be before the previous one).
    /// </summary>
    /// <param name="groupId">The sync group ID</param>
    /// <param name="entity">The intersection entity to move</param>
    /// <returns>True if successful</returns>
    public bool MoveIntersectionUp(uint groupId, Entity entity)
    {
        var intersections = GetIntersectionsInGroup(groupId);
        if (intersections.Count < 2) return false;
        
        // Find the intersection and the one before it
        int currentIndex = -1;
        for (int i = 0; i < intersections.Count; i++)
        {
            if (intersections[i].Entity == entity)
            {
                currentIndex = i;
                break;
            }
        }
        
        // Can't move up if already first or not found
        if (currentIndex <= 0) return false;
        
        var current = intersections[currentIndex];
        var previous = intersections[currentIndex - 1];
        
        // Swap offsets: current gets previous's offset, previous gets current's offset
        // But we need to ensure current ends up before previous
        // Strategy: Set current's offset to previous's offset - 1 (or 0 if previous is 0)
        ushort newCurrentOffset = previous.CycleOffset > 0 ? (ushort)(previous.CycleOffset - 1) : (ushort)0;
        ushort newPreviousOffset = (ushort)(previous.CycleOffset + 1);
        
        // If previous was the reference (offset 0), we need to adjust all offsets
        if (previous.CycleOffset == 0)
        {
            // Current becomes new reference (offset 0)
            // Previous and all others shift up
            SetIntersectionOffset(current.Entity, 0);
            
            // Shift all others up by 1
            for (int i = 0; i < intersections.Count; i++)
            {
                if (intersections[i].Entity != entity)
                {
                    var ctl = EntityManager.GetComponentData<CustomTrafficLights>(intersections[i].Entity);
                    SetIntersectionOffset(intersections[i].Entity, (ushort)(ctl.m_CycleOffsetSeconds + 1));
                }
            }
        }
        else
        {
            // Simple swap
            SetIntersectionOffset(current.Entity, newCurrentOffset);
            SetIntersectionOffset(previous.Entity, newPreviousOffset);
        }
        
        Mod.m_Log.Info($"[SyncGroupSystem] Moved intersection {entity.Index} up in group {groupId}");
        return true;
    }
    
    /// <summary>
    /// Moves an intersection down in the group order (increases its offset to be after the next one).
    /// </summary>
    /// <param name="groupId">The sync group ID</param>
    /// <param name="entity">The intersection entity to move</param>
    /// <returns>True if successful</returns>
    public bool MoveIntersectionDown(uint groupId, Entity entity)
    {
        var intersections = GetIntersectionsInGroup(groupId);
        if (intersections.Count < 2) return false;
        
        // Find the intersection and the one after it
        int currentIndex = -1;
        for (int i = 0; i < intersections.Count; i++)
        {
            if (intersections[i].Entity == entity)
            {
                currentIndex = i;
                break;
            }
        }
        
        // Can't move down if already last or not found
        if (currentIndex < 0 || currentIndex >= intersections.Count - 1) return false;
        
        var current = intersections[currentIndex];
        var next = intersections[currentIndex + 1];
        
        // Swap: current gets next's offset + 1, next gets current's offset
        ushort newCurrentOffset = (ushort)(next.CycleOffset + 1);
        ushort newNextOffset = current.CycleOffset;
        
        // If current was the reference (offset 0), next becomes new reference
        if (current.CycleOffset == 0)
        {
            // Next becomes reference (offset 0)
            // Current shifts to next's old offset + some delta
            SetIntersectionOffset(next.Entity, 0);
            SetIntersectionOffset(current.Entity, (ushort)(next.CycleOffset + 1));
            
            // All others after next also need adjustment
            for (int i = currentIndex + 2; i < intersections.Count; i++)
            {
                var ctl = EntityManager.GetComponentData<CustomTrafficLights>(intersections[i].Entity);
                // Recalculate relative to new reference
                SetIntersectionOffset(intersections[i].Entity, (ushort)(ctl.m_CycleOffsetSeconds - next.CycleOffset));
            }
        }
        else
        {
            // Simple swap
            SetIntersectionOffset(current.Entity, newCurrentOffset);
            SetIntersectionOffset(next.Entity, newNextOffset);
        }
        
        Mod.m_Log.Info($"[SyncGroupSystem] Moved intersection {entity.Index} down in group {groupId}");
        return true;
    }
    
    /// <summary>
    /// Sets the offset for an intersection in frames.
    /// </summary>
    private void SetIntersectionOffset(Entity entity, ushort offsetFrames)
    {
        if (!EntityManager.HasComponent<CustomTrafficLights>(entity)) return;
        
        var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
        ctl.SetCycleOffsetSeconds(offsetFrames);
        EntityManager.SetComponentData(entity, ctl);
    }
    
    /// <summary>
    /// Sets the offset for an intersection using real-time seconds.
    /// Converts to frames internally.
    /// </summary>
    public void SetIntersectionOffsetRealSeconds(Entity entity, float realSeconds)
    {
        ushort frames = (ushort)GreenWaveTimeConstants.RealSecondsToFrames(realSeconds);
        SetIntersectionOffset(entity, frames);
        Mod.m_Log.Info($"[SyncGroupSystem] Set intersection {entity.Index} offset to {realSeconds}s ({frames} frames)");
    }
    
    /// <summary>
    /// Removes an intersection from its sync group and resets it to standalone operation.
    /// </summary>
    /// <param name="entity">The intersection entity to remove</param>
    /// <returns>True if successful</returns>
    public bool RemoveIntersectionFromGroup(Entity entity)
    {
        if (!EntityManager.HasComponent<CustomTrafficLights>(entity)) return false;
        
        var ctl = EntityManager.GetComponentData<CustomTrafficLights>(entity);
        uint oldGroupId = ctl.m_SyncGroupId;
        
        // V112: Reset phase warning for this group so it can be re-evaluated
        ResetPhaseWarningForGroup(oldGroupId);
        
        // Reset sync settings
        ctl.SetSyncGroupId(0);
        ctl.SetUseSyncedCycle(false);
        ctl.SetCycleOffsetSeconds(0);
        ctl.SetSyncPhaseIndex(0);
        
        EntityManager.SetComponentData(entity, ctl);
        
        Mod.m_Log.Info($"[SyncGroupSystem] Removed intersection {entity.Index} from group {oldGroupId}");
        return true;
    }
}
