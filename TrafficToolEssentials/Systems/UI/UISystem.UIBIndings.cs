using System.Collections;
using System.Collections.Generic;
using System.Linq;
using C2VM.TrafficToolEssentials.Components;
using C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Initialisation;
using C2VM.TrafficToolEssentials.Utils;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Net;
using Game.UI;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace C2VM.TrafficToolEssentials.Systems.UI;

public partial class UISystem : UISystemBase
{
    public static GetterValueBinding<string> m_MainPanelBinding { get; private set; }

    private static GetterValueBinding<string> m_LocaleBinding;

    private GetterValueBinding<string> m_CityConfigurationBinding;

    private GetterValueBinding<Dictionary<string, UITypes.ScreenPoint>> m_ScreenPointBinding;

    private GetterValueBinding<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>> m_EdgeInfoBinding;

    private ValueBinding<int> m_ActiveEditingCustomPhaseIndexBinding;

    private ValueBinding<int> m_ActiveViewingCustomPhaseIndexBinding;

    internal ValueBinding<UITypes.ToolTooltipMessage[]> m_ToolTooltipMessageBinding;
    // === COPY/PASTE FEATURE ===
    private static List<CopiedPhaseData> m_PhaseClipboard = new List<CopiedPhaseData>();
    private ValueBinding<int> m_ClipboardCountBinding;
    private static int m_SourceEdgeCount = 0;

    private void AddUIBindings()
    {
        AddBinding(m_MainPanelBinding = new GetterValueBinding<string>("C2VM.TLE", "GetMainPanel", GetMainPanel));
        AddBinding(m_LocaleBinding = new GetterValueBinding<string>("C2VM.TLE", "GetLocale", GetLocale));
        AddBinding(m_CityConfigurationBinding = new GetterValueBinding<string>("C2VM.TLE", "GetCityConfiguration", GetCityConfiguration));
        AddBinding(m_ScreenPointBinding = new GetterValueBinding<Dictionary<string, UITypes.ScreenPoint>>("C2VM.TLE", "GetScreenPoint", GetScreenPoint, new DictionaryWriter<string, UITypes.ScreenPoint>(null, new ValueWriter<UITypes.ScreenPoint>()), new JsonWriter.FalseEqualityComparer<Dictionary<string, UITypes.ScreenPoint>>()));
        AddBinding(m_EdgeInfoBinding = new GetterValueBinding<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>>("C2VM.TLE", "GetEdgeInfo", GetEdgeInfo, new JsonWriter.EdgeInfoWriter(), new JsonWriter.FalseEqualityComparer<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>>()));
        AddBinding(m_ActiveEditingCustomPhaseIndexBinding = new ValueBinding<int>("C2VM.TLE", "GetActiveEditingCustomPhaseIndex", -1));
        AddBinding(m_ActiveViewingCustomPhaseIndexBinding = new ValueBinding<int>("C2VM.TLE", "GetActiveViewingCustomPhaseIndex", -1));
        AddBinding(m_ToolTooltipMessageBinding = new ValueBinding<UITypes.ToolTooltipMessage[]>("C2VM.TLE", "GetToolTooltipMessage", [], new ListWriter<UITypes.ToolTooltipMessage>(new ValueWriter<UITypes.ToolTooltipMessage>())));


        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdatePattern", CallMainPanelUpdatePattern));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdateOption", CallMainPanelUpdateOption));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdateValue", CallMainPanelUpdateValue));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelUpdatePosition", CallMainPanelUpdatePosition));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMainPanelSave", CallMainPanelSave));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallLaneDirectionToolReset", CallLaneDirectionToolReset));

        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallSetMainPanelState", CallSetMainPanelState));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallOpenGreenWave", CallOpenGreenWave));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallAddCustomPhase", CallAddCustomPhase));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallRemoveCustomPhase", CallRemoveCustomPhase));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallSwapCustomPhase", CallSwapCustomPhase));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallSetActiveCustomPhaseIndex", CallSetActiveCustomPhaseIndex));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallUpdateEdgeGroupMask", CallUpdateEdgeGroupMask));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallUpdateSubLaneGroupMask", CallUpdateSubLaneGroupMask));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallUpdateCustomPhaseData", CallUpdateCustomPhaseData));

        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallKeyPress", CallKeyPress));

        // Clipboard Bindings
        AddBinding(m_ClipboardCountBinding = new ValueBinding<int>("C2VM.TLE", "GetClipboardCount", 0));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallCopyPhases", CallCopyPhases));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallPastePhases", CallPastePhases));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallDeletePhases", CallDeletePhases));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallClearPhaseClipboard", CallClearPhaseClipboard));

        // Green Wave Sync Bindings
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallUpdateSyncSettings", CallUpdateSyncSettings));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallCreateSyncGroup", CallCreateSyncGroup));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallDeleteSyncGroup", CallDeleteSyncGroup));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallRenameSyncGroup", CallRenameSyncGroup));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallSetGroupTimeWindows", CallSetGroupTimeWindows));
        
        // Green Wave Phase 2 - Intersection ordering
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMoveIntersectionUp", CallMoveIntersectionUp));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallMoveIntersectionDown", CallMoveIntersectionDown));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallRemoveIntersectionFromGroup", CallRemoveIntersectionFromGroup));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallSetIntersectionOffset", CallSetIntersectionOffset));
        
        // Dashboard live updates
        AddBinding(new TriggerBinding<bool>("C2VM.TLE", "SetDashboardActive", (active) => { m_GreenWaveDashboardActive = active; }));

        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallAddWorldPosition", CallAddWorldPosition));
        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallRemoveWorldPosition", CallRemoveWorldPosition));

        AddBinding(new CallBinding<string, string>("C2VM.TLE", "CallOpenBrowser", CallOpenBrowser));

        AddBinding(new TriggerBinding<int>("C2VM.TLE", "SetDebugDisplayGroup", (group) => { m_DebugDisplayGroup = group; RedrawGizmo(); }));
    }

    protected string GetMainPanel()
    {
        // Prepare sync data
        object syncSettings = null;
        object[] syncGroups = null;
        
        if (m_MainPanelState == MainPanelState.CustomPhase && m_SelectedEntity != Entity.Null)
        {
            // Safety: only proceed if entity has CustomTrafficLights
            if (EntityManager.TryGetComponent(m_SelectedEntity, out CustomTrafficLights ctl))
            {
                // Get total cycle duration from phases
                uint totalCycleDuration = 0;
                if (EntityManager.TryGetBuffer(m_SelectedEntity, true, out DynamicBuffer<CustomPhaseData> phaseBuffer))
                {
                    for (int i = 0; i < phaseBuffer.Length; i++)
                    {
                        totalCycleDuration += (uint)((phaseBuffer[i].m_MinimumDuration + phaseBuffer[i].m_MaximumDuration) / 2);
                    }
                }
                
                // Build phase info for dropdown
                int phaseCount = 0;
                if (EntityManager.TryGetBuffer(m_SelectedEntity, true, out DynamicBuffer<CustomPhaseData> phaseBufferForCount))
                {
                    phaseCount = phaseBufferForCount.Length;
                }
                
                syncSettings = new
                {
                    syncGroupId = ctl.m_SyncGroupId,
                    useSyncedCycle = ctl.m_UseSyncedCycle,
                    cycleOffsetSeconds = ctl.m_CycleOffsetSeconds,
                    syncPhaseIndex = ctl.m_SyncPhaseIndex,
                    totalCycleDuration = totalCycleDuration,
                    phaseCount = phaseCount,
                    selectedEntityIndex = m_SelectedEntity.Index,  // Node ID for UI display
                    useSequentialPhases = ctl.m_UseSequentialPhases
                };
                
                // Get all sync groups (with safety check)
                if (m_SyncGroupSystem != null)
                {
                    // Ensure groups are loaded from file (fixes groups not showing after game load)
                    m_SyncGroupSystem.EnsureGroupsLoaded();
                    
                    // V199.3: Use try-finally to ALWAYS dispose NativeArray
                    Unity.Collections.NativeArray<SyncGroup> groups = default;
                    try
                    {
                        groups = m_SyncGroupSystem.GetAllGroups(Allocator.Temp);
                        syncGroups = new object[groups.Length];
                        for (int i = 0; i < groups.Length; i++)
                        {
                        // Get intersection details for this group
                        var intersections = m_SyncGroupSystem.GetIntersectionsInGroup(groups[i].m_GroupId);
                        var intersectionData = new object[intersections.Count];
                        
                        // V211: Find reference offset for relative calculation
                        // Reference is the first after sorting (lowest offset)
                        ushort referenceOffset = 0;
                        for (int j = 0; j < intersections.Count; j++)
                        {
                            if (intersections[j].IsReference)
                            {
                                referenceOffset = intersections[j].CycleOffset;
                                break;
                            }
                        }
                        
                        for (int j = 0; j < intersections.Count; j++)
                        {
                            // Get edge statistics for this intersection
                            int edgeCount = 0;
                            int totalLanes = 0;
                            int leftLanes = 0;
                            int straightLanes = 0;
                            int rightLanes = 0;
                            int pedestrianLanes = 0;
                            
                            // V199.3: Use try-finally to ALWAYS dispose NativeList, even on exception
                            Unity.Collections.NativeList<NodeUtils.EdgeInfo> edgeInfoList = default;
                            try
                            {
                                edgeInfoList = NodeUtils.GetEdgeInfoList(Allocator.Temp, intersections[j].Entity, this);
                                edgeCount = edgeInfoList.Length;
                                for (int e = 0; e < edgeInfoList.Length; e++)
                                {
                                    var edge = edgeInfoList[e];
                                    leftLanes += edge.m_CarLaneLeftCount + edge.m_PublicCarLaneLeftCount;
                                    straightLanes += edge.m_CarLaneStraightCount + edge.m_PublicCarLaneStraightCount;
                                    rightLanes += edge.m_CarLaneRightCount + edge.m_PublicCarLaneRightCount;
                                    pedestrianLanes += edge.m_PedestrianLaneStopLineCount;
                                }
                                totalLanes = leftLanes + straightLanes + rightLanes;
                            }
                            catch { /* Ignore errors, use defaults */ }
                            finally
                            {
                                // V206: CRITICAL FIX - Use NodeUtils.Dispose to also dispose inner m_SubLaneInfoList
                                // edgeInfoList.Dispose() only disposes the list, not the nested SubLaneInfoLists!
                                if (edgeInfoList.IsCreated)
                                    NodeUtils.Dispose(edgeInfoList);
                            }
                            
                            // V211: Calculate RELATIVE offset (my offset - reference offset)
                            // This matches what the backend actually does:
                            // - Reference: 0s (goes immediately)
                            // - Others: (their offset - reference offset) = actual wait time
                            ushort absOffset = intersections[j].CycleOffset;
                            int relativeOffset = absOffset - referenceOffset;
                            if (relativeOffset < 0) relativeOffset = 0; // Shouldn't happen, but safety
                            
                            intersectionData[j] = new
                            {
                                index = intersections[j].Index,
                                entity = intersections[j].Entity.Index,  // Entity index for backend calls
                                offset = relativeOffset,  // V211: RELATIVE offset (what they actually wait)
                                syncPhase = intersections[j].SyncPhaseIndex,
                                isSynced = intersections[j].UseSyncedCycle,
                                cycleDuration = intersections[j].TotalCycleDuration,
                                phaseCount = intersections[j].PhaseCount,
                                progress = intersections[j].CurrentPhaseProgress,
                                currentPhase = intersections[j].CurrentPhaseIndex,
                                isSyncPhaseActive = intersections[j].IsSyncPhaseActive,
                                isReference = intersections[j].IsReference,
                                position = intersections[j].Position,
                                // Extended stats (V15)
                                edgeCount = edgeCount,
                                totalLanes = totalLanes,
                                leftLanes = leftLanes,
                                straightLanes = straightLanes,
                                rightLanes = rightLanes,
                                pedestrianLanes = pedestrianLanes
                            };
                        }
                        
                        syncGroups[i] = new
                        {
                            groupId = groups[i].m_GroupId,
                            groupName = groups[i].GetName(),
                            baseCycleDuration = groups[i].m_BaseCycleDuration,
                            intersectionCount = m_SyncGroupSystem.CountIntersectionsInGroup(groups[i].m_GroupId),
                            groupTimer = groups[i].m_GroupTimer,
                            intersections = intersectionData,
                            // Time window settings
                            alwaysActive = groups[i].m_AlwaysActive,
                            timeWindow1Start = groups[i].m_TimeWindow1Start,
                            timeWindow1End = groups[i].m_TimeWindow1End,
                            timeWindow2Start = groups[i].m_TimeWindow2Start,
                            timeWindow2End = groups[i].m_TimeWindow2End,
                            timeWindow3Start = groups[i].m_TimeWindow3Start,
                            timeWindow3End = groups[i].m_TimeWindow3End
                        };
                    }
                    }
                    finally
                    {
                        if (groups.IsCreated)
                            groups.Dispose();
                    }
                }
            }
        }
        
        var menu = new
        {
            title = Mod.IsCanary() ? "TLE Canary" : "Traffic Tool Essentials",
            image = "Media/Game/Icons/TrafficLights.svg",
            position = m_MainPanelPosition,
            showPanel = m_MainPanelState != MainPanelState.Hidden,
            showFloatingButton = true,
            state = m_MainPanelState,
            items = new ArrayList(),
            syncSettings = syncSettings,
            syncGroups = syncGroups
        };
        if (m_MainPanelState == MainPanelState.Main && m_SelectedEntity != Entity.Null)
        {
            // V170: Title moved below TrafficSummary, before dropdown
            
            // V138: Traffic Summary with corrected lane counting
            // V163: Improved to distinguish physical lanes vs directions
            // V164: Count UNIQUE SubLane entities for true physical lane count
            // V165: Only count CAR lanes, exclude pedestrian and other lane types
            {
                int totalDirections = 0;      // Fahrrichtungen (Links+Geradeaus+Rechts)
                int totalPhysicalLanes = 0;   // Actual physical incoming CAR lanes only
                int approaches = 0;
                float totalCarFlow = 0f;
                int maxOccupiedInPhase = 0;   // Max occupied in any single phase (not sum!)
                int occupiedPedestrianLanes = 0;
                bool hasFlowData = false;
                int waitingVehicles = 0;  // V179: Count vehicles on incoming lanes
                
                // Count lanes from EdgeInfo - only unique CAR lane entities
                if (m_EdgeInfoDictionary.TryGetValue(m_SelectedEntity, out var edgeInfoArray))
                {
                    approaches = edgeInfoArray.Length;
                    var uniqueCarLanes = new System.Collections.Generic.HashSet<Entity>();
                    
                    // V179: Update TypeHandle for LaneObject access
                    m_TypeHandle.Update(this);
                    
                    foreach (var edgeInfo in edgeInfoArray)
                    {
                        totalDirections += edgeInfo.m_CarLaneLeftCount + 
                                           edgeInfo.m_CarLaneStraightCount + 
                                           edgeInfo.m_CarLaneRightCount;
                        
                        // V179: Count vehicles on the INCOMING edge lanes (not node sublanes)
                        // Get sublanes of the edge (road segment), not the node (intersection)
                        if (m_TypeHandle.m_SubLane.TryGetBuffer(edgeInfo.m_Edge, out var edgeSubLanes))
                        {
                            // V180: Determine if this edge connects at Start or End to our intersection
                            bool edgeEndsAtIntersection = false;
                            if (m_TypeHandle.m_Edge.TryGetComponent(edgeInfo.m_Edge, out var edge))
                            {
                                // Edge.m_End is the node at the end of the edge
                                edgeEndsAtIntersection = edge.m_End.Equals(m_SelectedEntity);
                            }
                            
                            foreach (var edgeSubLane in edgeSubLanes)
                            {
                                // Only count car lanes
                                if (!m_TypeHandle.m_CarLane.HasComponent(edgeSubLane.m_SubLane))
                                    continue;
                                
                                // Skip master lanes
                                if (m_TypeHandle.m_MasterLane.HasComponent(edgeSubLane.m_SubLane))
                                    continue;
                                
                                // V180: Use CarLane flags to determine direction
                                // CarLaneFlags.Invert means the lane goes opposite to edge direction
                                if (m_TypeHandle.m_CarLane.TryGetComponent(edgeSubLane.m_SubLane, out var carLane))
                                {
                                    bool laneGoesForward = (carLane.m_Flags & Game.Net.CarLaneFlags.Invert) == 0;
                                    // If intersection at edge.m_End: forward lanes are incoming
                                    // If intersection at edge.m_Start: inverted lanes are incoming
                                    bool isIncomingLane = edgeEndsAtIntersection ? laneGoesForward : !laneGoesForward;
                                    
                                    if (!isIncomingLane)
                                        continue; // Skip outgoing lanes
                                }
                                
                                // Count vehicles (LaneObjects) on this lane
                                // V181: Filter out trailers (they have a Controller component)
                                if (m_TypeHandle.m_LaneObject.TryGetBuffer(edgeSubLane.m_SubLane, out var laneObjects))
                                {
                                    foreach (var laneObj in laneObjects)
                                    {
                                        // V181: Skip trailers - they have a Controller pointing to the truck
                                        if (m_TypeHandle.m_Controller.HasComponent(laneObj.m_LaneObject))
                                            continue;
                                        
                                        waitingVehicles++;
                                    }
                                }
                            }
                        }
                        
                        foreach (var subLane in edgeInfo.m_SubLaneInfoList)
                        {
                            int carDirections = subLane.m_CarLaneLeftCount + 
                                               subLane.m_CarLaneStraightCount + 
                                               subLane.m_CarLaneRightCount +
                                               subLane.m_CarLaneUTurnCount;
                            
                            if (carDirections > 0)
                            {
                                uniqueCarLanes.Add(subLane.m_SubLane);
                            }
                        }
                    }
                    
                    totalPhysicalLanes = uniqueCarLanes.Count;
                }
                
                // V168: Sum occupied lanes across ALL phases (each lane typically belongs to only one phase)
                // Previously took MAX which undercounted when vehicles are spread across different phases
                if (EntityManager.TryGetBuffer(m_SelectedEntity, true, out DynamicBuffer<CustomPhaseData> phaseDataBuffer) && phaseDataBuffer.Length > 0)
                {
                    int maxPedOccupied = 0;
                    int totalOccupied = 0;  // V168: Sum instead of max
                    for (int i = 0; i < phaseDataBuffer.Length; i++)
                    {
                        var phase = phaseDataBuffer[i];
                        totalCarFlow += phase.m_CarFlow.x;
                        
                        // V168: Sum all occupied lanes across phases
                        totalOccupied += phase.m_CarLaneOccupied + phase.m_PublicCarLaneOccupied + phase.m_TrackLaneOccupied;
                        
                        if (phase.m_PedestrianLaneOccupied > maxPedOccupied)
                            maxPedOccupied = phase.m_PedestrianLaneOccupied;
                    }
                    maxOccupiedInPhase = totalOccupied;  // V168: Now contains sum, not max
                    occupiedPedestrianLanes = maxPedOccupied;
                    hasFlowData = totalCarFlow > 0.1f;
                }
                
                // V163: Use physical lanes for VPH estimation
                float vehiclesPerHour;
                
                float basePerLane = 80f;
                float baseEstimate = totalPhysicalLanes * basePerLane;
                
                // V163: Occupancy ratio based on physical incoming lanes
                float occupancyRatio = totalPhysicalLanes > 0 
                    ? (float)maxOccupiedInPhase / totalPhysicalLanes 
                    : 0f;
                occupancyRatio = System.Math.Min(occupancyRatio, 1.0f); // Cap at 100%
                
                // Occupancy factor: scales from 0.5 (empty) to 1.5 (full)
                float occupancyFactor = 0.5f + occupancyRatio;  // Range: 0.5 - 1.5
                
                // Approach adjustment: 3-way intersections have ~75% of 4-way throughput
                float approachFactor = approaches switch
                {
                    2 => 0.5f,   // T-junction
                    3 => 0.75f,  // 3-way
                    4 => 1.0f,   // Standard 4-way
                    _ => 0.9f    // 5+ way - complex, slightly less efficient
                };
                
                vehiclesPerHour = baseEstimate * occupancyFactor * approachFactor;
                
                // V156: For the history chart, use the estimated VPH directly
                float currentVPH = vehiclesPerHour;
                
                // V163: Flow status based on occupancy of physical incoming lanes
                // This is much more accurate than before!
                // Low: < 40% of incoming lanes occupied
                // Medium: 40-70% of incoming lanes occupied  
                // High: > 70% of incoming lanes occupied
                string flowStatus = "low";
                if (occupancyRatio > 0.7f) 
                    flowStatus = "high";
                else if (occupancyRatio > 0.4f) 
                    flowStatus = "medium";
                
                // V156: Read history data from ECS component (now using real vehicle counts!)
                float[] historyData = System.Array.Empty<float>();
                bool hasLiveVPH = false;
                // V179: waitingVehicles now counted above from edge lanes directly
                if (EntityManager.TryGetComponent<PersistentTrafficHistory>(m_SelectedEntity, out var flowHistory))
                {
                    historyData = flowHistory.GetOrderedHistory();
                    // V156: Use live VPH from real vehicle counting
                    if (flowHistory.m_CurrentVPH > 0)
                    {
                        currentVPH = flowHistory.m_CurrentVPH;
                        hasLiveVPH = true;
                    }
                }
                
                // V147: Check if CustomPhase mode is active
                bool isCustomPhase = m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase;
                
                // V157: Use real VPH if available, otherwise fall back to estimate
                float displayVPH = hasLiveVPH ? currentVPH : vehiclesPerHour;
                
                menu.items.Add(new UITypes.ItemTrafficSummary{
                    vehiclesPerHour = displayVPH,
                    vehiclesPerMinute = displayVPH / 60f,
                    incomingLanes = totalPhysicalLanes,  // V163: Now shows physical lanes
                    totalDirections = totalDirections,   // V163: New field for directions count
                    approaches = approaches,
                    flowStatus = flowStatus,
                    occupiedLanes = maxOccupiedInPhase,  // V163: Max from single phase, not sum
                    occupiedPedestrianLanes = occupiedPedestrianLanes,
                    hasFlowData = historyData.Length > 0 || hasLiveVPH,
                    isCustomPhase = isCustomPhase,
                    currentVPH = currentVPH,
                    currentGameTime = m_TimeSystem?.normalizedTime ?? 0f,  // V172: For chart time labels
                    waitingVehicles = waitingVehicles,  // V179: Current vehicle queue
                    historyData = historyData
                });
            }
            
            // V170: Title as label for dropdown (moved from top)
            menu.items.Add(new UITypes.ItemTitle{title = "TrafficSignal"});
            
            // V166: Phase mode dropdown instead of radio buttons
            var dropdownOptions = new System.Collections.Generic.List<UITypes.DropdownOption>();
            dropdownOptions.Add(new UITypes.DropdownOption { value = ((uint)CustomTrafficLights.Patterns.Vanilla).ToString(), label = "Vanilla" });
            
            if (PredefinedPatternsProcessor.IsValidPattern(m_EdgeInfoDictionary[m_SelectedEntity], CustomTrafficLights.Patterns.SplitPhasing))
            {
                dropdownOptions.Add(new UITypes.DropdownOption { value = ((uint)CustomTrafficLights.Patterns.SplitPhasing).ToString(), label = "SplitPhasing" });
            }
            if (PredefinedPatternsProcessor.IsValidPattern(m_EdgeInfoDictionary[m_SelectedEntity], CustomTrafficLights.Patterns.ProtectedCentreTurn))
            {
                if (m_CityConfigurationSystem.leftHandTraffic)
                {
                    dropdownOptions.Add(new UITypes.DropdownOption { value = ((uint)CustomTrafficLights.Patterns.ProtectedCentreTurn).ToString(), label = "ProtectedRightTurns" });
                }
                else
                {
                    dropdownOptions.Add(new UITypes.DropdownOption { value = ((uint)CustomTrafficLights.Patterns.ProtectedCentreTurn).ToString(), label = "ProtectedLeftTurns" });
                }
            }
            dropdownOptions.Add(new UITypes.DropdownOption { value = ((uint)CustomTrafficLights.Patterns.CustomPhase).ToString(), label = "CustomPhase" });
            
            menu.items.Add(new UITypes.ItemDropdown {
                key = "pattern",
                selectedValue = ((uint)m_CustomTrafficLights.GetPattern() & 0xFFFF).ToString(),
                engineEventName = "C2VM.TLE.CallMainPanelUpdatePattern",
                options = dropdownOptions.ToArray()
            });
            
            // V172: Divider under dropdown for visual separation (3 sections: Traffic Summary | Phase Selection | Buttons)
            menu.items.Add(default(UITypes.ItemDivider));
            
            if (m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase)
            {
                menu.items.Add(new UITypes.ItemButton{label = "CustomPhaseEditor", key = "state", value = $"{(int)MainPanelState.CustomPhase}", engineEventName = "C2VM.TLE.CallSetMainPanelState"});
            }
            // Green Wave Button - always visible, but disabled if CustomPhase not active
            bool isCustomPhaseActive = m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase;
            menu.items.Add(new UITypes.ItemButton{
                label = "GreenWaveButton", 
                key = "greenwave", 
                value = isCustomPhaseActive ? "open" : "disabled", 
                engineEventName = "C2VM.TLE.CallOpenGreenWave"
            });
            
            if (m_CustomTrafficLights.GetPatternOnly() < CustomTrafficLights.Patterns.ModDefault && !NodeUtils.HasTrainTrack(m_EdgeInfoDictionary[m_SelectedEntity]))
            {
                menu.items.Add(default(UITypes.ItemDivider));
                menu.items.Add(new UITypes.ItemTitle{title = "Options"});
                menu.items.Add(UITypes.MainPanelItemOption("AllowTurningOnRed", (uint)CustomTrafficLights.Patterns.AlwaysGreenKerbsideTurn, (uint)m_CustomTrafficLights.GetPattern()));
                if (m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.Vanilla)
                {
                    menu.items.Add(UITypes.MainPanelItemOption("GiveWayToOncomingVehicles", (uint)CustomTrafficLights.Patterns.CentreTurnGiveWay, (uint)m_CustomTrafficLights.GetPattern()));
                }
                menu.items.Add(UITypes.MainPanelItemOption("ExclusivePedestrianPhase", (uint)CustomTrafficLights.Patterns.ExclusivePedestrian, (uint)m_CustomTrafficLights.GetPattern()));
                if (((uint)m_CustomTrafficLights.GetPattern() & (uint)CustomTrafficLights.Patterns.ExclusivePedestrian) != 0)
                {
                    menu.items.Add(default(UITypes.ItemDivider));
                    menu.items.Add(new UITypes.ItemTitle{title = "Adjustments"});
                    menu.items.Add(new UITypes.ItemRange
                    {
                        key = "CustomPedestrianDurationMultiplier",
                        label = "CustomPedestrianDurationMultiplier",
                        valuePrefix = "",
                        valueSuffix = "CustomPedestrianDurationMultiplierSuffix",
                        min = 0.5f,
                        max = 10,
                        step = 0.5f,
                        defaultValue = 1f,
                        enableTextField = false,
                        value = m_CustomTrafficLights.m_PedestrianPhaseDurationMultiplier,
                        engineEventName = "C2VM.TLE.CallMainPanelUpdateValue"
                    });
                }
            }
            menu.items.Add(default(UITypes.ItemDivider));
            if (EntityManager.HasBuffer<C2VM.CommonLibraries.LaneSystem.CustomLaneDirection>(m_SelectedEntity))
            {
                menu.items.Add(new UITypes.ItemTitle{title = "LaneDirectionTool"});
                menu.items.Add(new UITypes.ItemButton{label = "Reset", key = "status", value = "0", engineEventName = "C2VM.TLE.CallLaneDirectionToolReset"});
                menu.items.Add(default(UITypes.ItemDivider));
            }
            menu.items.Add(new UITypes.ItemButton{label = "Save", key = "save", value = "1", engineEventName = "C2VM.TLE.CallMainPanelSave"});
            if (m_ShowNotificationUnsaved)
            {
                menu.items.Add(default(UITypes.ItemDivider));
                menu.items.Add(new UITypes.ItemNotification{label = "PleaseSave", notificationType = "warning"});
            }
        }
        else if (m_MainPanelState == MainPanelState.CustomPhase)
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, true, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            EntityManager.TryGetComponent(m_SelectedEntity, out TrafficLights trafficLights);
            EntityManager.TryGetComponent(m_SelectedEntity, out CustomTrafficLights customTrafficLights);
            for (int i = 0; i < customPhaseDataBuffer.Length; i++)
            {
                menu.items.Add(new UITypes.ItemCustomPhase
                {
                    activeIndex = m_ActiveEditingCustomPhaseIndexBinding.value,
                    activeViewingIndex = m_ActiveViewingCustomPhaseIndexBinding.value,
                    currentSignalGroup = trafficLights.m_CurrentSignalGroup,
                    manualSignalGroup = customTrafficLights.m_ManualSignalGroup,
                    index = i,
                    length = customPhaseDataBuffer.Length,
                    timer = trafficLights.m_CurrentSignalGroup == i + 1 ? customTrafficLights.m_Timer : 0,
                    turnsSinceLastRun = customPhaseDataBuffer[i].m_TurnsSinceLastRun,
                    lowFlowTimer = customPhaseDataBuffer[i].m_LowFlowTimer,
                    carFlow = customPhaseDataBuffer[i].AverageCarFlow(),
                    carLaneOccupied = customPhaseDataBuffer[i].m_CarLaneOccupied,
                    publicCarLaneOccupied = customPhaseDataBuffer[i].m_PublicCarLaneOccupied,
                    trackLaneOccupied = customPhaseDataBuffer[i].m_TrackLaneOccupied,
                    pedestrianLaneOccupied = customPhaseDataBuffer[i].m_PedestrianLaneOccupied,
                    bicycleLaneOccupied = customPhaseDataBuffer[i].m_BicycleLaneOccupied,
                    weightedWaiting = customPhaseDataBuffer[i].m_WeightedWaiting,
                    targetDuration = customPhaseDataBuffer[i].m_TargetDuration,
                    priority = customPhaseDataBuffer[i].m_Priority,
                    minimumDuration = customPhaseDataBuffer[i].m_MinimumDuration,
                    maximumDuration = customPhaseDataBuffer[i].m_MaximumDuration,
                    targetDurationMultiplier = customPhaseDataBuffer[i].m_TargetDurationMultiplier,
                    laneOccupiedMultiplier = customPhaseDataBuffer[i].m_LaneOccupiedMultiplier,
                    intervalExponent = customPhaseDataBuffer[i].m_IntervalExponent,
                    prioritiseTrack = (customPhaseDataBuffer[i].m_Options & CustomPhaseData.Options.PrioritiseTrack) != 0,
                    prioritisePublicCar = (customPhaseDataBuffer[i].m_Options & CustomPhaseData.Options.PrioritisePublicCar) != 0,
                    prioritisePedestrian = (customPhaseDataBuffer[i].m_Options & CustomPhaseData.Options.PrioritisePedestrian) != 0,
                    prioritiseBicycle = (customPhaseDataBuffer[i].m_Options & CustomPhaseData.Options.PrioritiseBicycle) != 0,
                    linkedWithNextPhase = (customPhaseDataBuffer[i].m_Options & CustomPhaseData.Options.LinkedWithNextPhase) != 0,
                    endPhasePrematurely = (customPhaseDataBuffer[i].m_Options & CustomPhaseData.Options.EndPhasePrematurely) != 0,
                });
            }
        }
        else if (m_MainPanelState == MainPanelState.FunctionSelection)
        {
            menu.items.Add(new UITypes.ItemTitle{title = "SelectFunction"});
            menu.items.Add(new UITypes.ItemButton{label = "EditIntersectionPhases", key = "state", value = $"{(int)MainPanelState.Empty}", engineEventName = "C2VM.TLE.CallSetMainPanelState"});
            menu.items.Add(new UITypes.ItemButton{label = "IntersectionMonitor", key = "state", value = $"{(int)MainPanelState.IntersectionMonitor}", engineEventName = "C2VM.TLE.CallSetMainPanelState"});
        }
        else if (m_MainPanelState == MainPanelState.Empty)
        {
            menu.items.Add(new UITypes.ItemMessage{message = "PleaseSelectJunction"});
        }
        else if (m_MainPanelState == MainPanelState.IntersectionMonitor)
        {
            menu.items.Add(new UITypes.ItemTitle{title = "IntersectionMonitor"});
            menu.items.Add(new UITypes.ItemMessage{message = "DashboardComingSoon"});
        }
        if (Mod.IsCanary() && Mod.m_Settings != null && Mod.m_Settings.m_SuppressCanaryWarningVersion != Mod.m_InformationalVersion)
        {
            menu.items.Add(default(UITypes.ItemDivider));
            menu.items.Add(new UITypes.ItemNotification{label = "CanaryBuildWarning", notificationType = "warning"});
        }
        string result = JsonConvert.SerializeObject(menu);
        return result;
    }

    public static string GetLocale()
    {
        var result = new
        {
            locale = GetLocaleCode(),
        };

        return JsonConvert.SerializeObject(result);
    }

    public string GetCityConfiguration()
    {
        var result = new
        {
            leftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
        };

        return JsonConvert.SerializeObject(result);
    }

    protected Dictionary<string, UITypes.ScreenPoint> GetScreenPoint()
    {
        Dictionary<string, UITypes.ScreenPoint> screenPointDictionary = [];
        m_Camera = Camera.main;
        m_ScreenHeight = Screen.height;
        foreach (var wp in m_WorldPositionList)
        {
            if (!screenPointDictionary.ContainsKey(wp))
            {
                screenPointDictionary[wp] = new UITypes.ScreenPoint(m_Camera.WorldToScreenPoint(wp), m_ScreenHeight);
            }
        }
        return screenPointDictionary;
    }

    protected Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>> GetEdgeInfo()
    {
        return m_EdgeInfoDictionary;
    }

    protected string CallMainPanelUpdatePattern(string input)
    {
        UITypes.ItemRadio pattern = JsonConvert.DeserializeObject<UITypes.ItemRadio>(input);
        m_CustomTrafficLights.SetPattern(((uint)m_CustomTrafficLights.GetPattern() & 0xFFFF0000) | uint.Parse(pattern.value));
        if (m_CustomTrafficLights.GetPatternOnly() != CustomTrafficLights.Patterns.Vanilla)
        {
            m_CustomTrafficLights.SetPattern(m_CustomTrafficLights.GetPattern() & ~CustomTrafficLights.Patterns.CentreTurnGiveWay);
        }
        if (m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase)
        {
            if (!EntityManager.HasBuffer<CustomPhaseData>(m_SelectedEntity))
            {
                EntityManager.AddComponent<CustomPhaseData>(m_SelectedEntity);
            }
            if (!EntityManager.HasBuffer<EdgeGroupMask>(m_SelectedEntity))
            {
                EntityManager.AddComponent<EdgeGroupMask>(m_SelectedEntity);
            }
            if (!EntityManager.HasBuffer<SubLaneGroupMask>(m_SelectedEntity))
            {
                EntityManager.AddComponent<SubLaneGroupMask>(m_SelectedEntity);
            }
            m_CustomTrafficLights.SetPattern(CustomTrafficLights.Patterns.CustomPhase);
        }
        UpdateEntity();
        m_MainPanelBinding.Update();
        return "";
    }

    protected string CallMainPanelUpdateOption(string input)
    {
        UITypes.ItemCheckbox option = JsonConvert.DeserializeObject<UITypes.ItemCheckbox>(input);
        foreach (CustomTrafficLights.Patterns pattern in System.Enum.GetValues(typeof(CustomTrafficLights.Patterns)))
        {
            if (((uint) pattern & 0xFFFF0000) != 0)
            {
                if (uint.Parse(option.key) == (uint)pattern)
                {
                    // Toggle the option
                    m_CustomTrafficLights.SetPattern(m_CustomTrafficLights.GetPattern() ^ pattern);
                }
            }
        }
        UpdateEntity();
        m_MainPanelBinding.Update();
        return "";
    }

    protected string CallMainPanelUpdateValue(string jsonString)
    {
        var keyDefinition = new { key = "" };
        var parsedKey = JsonConvert.DeserializeAnonymousType(jsonString, keyDefinition);
        if (parsedKey.key == "CustomPedestrianDurationMultiplier")
        {
            var valueDefinition = new { value = 0.0f };
            var parsedValue = JsonConvert.DeserializeAnonymousType(jsonString, valueDefinition);
            m_CustomTrafficLights.SetPedestrianPhaseDurationMultiplier(parsedValue.value);
        }
        UpdateEntity();
        m_MainPanelBinding.Update();
        return "";
    }

    protected string CallMainPanelUpdatePosition(string jsonString)
    {
        m_MainPanelPosition = JsonConvert.DeserializeObject<UITypes.ScreenPoint>(jsonString);
        m_MainPanelBinding.Update();
        return "";
    }

    protected string CallMainPanelSave(string value)
    {
        SaveSelectedEntity();
        return "";
    }

    protected string CallLaneDirectionToolReset(string input)
    {
        if (m_SelectedEntity != Entity.Null)
        {
            EntityManager.RemoveComponent<CommonLibraries.LaneSystem.CustomLaneDirection>(m_SelectedEntity);
            m_MainPanelBinding.Update();
        }
        return "";
    }

    protected string CallSetMainPanelState(string input)
    {
        var definition = new { key = "", value = "" };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        MainPanelState state = (MainPanelState)System.Int32.Parse(value.value);
        SetMainPanelState(state);
        return "";
    }

    /// <summary>
    /// Called from the main menu to open Green Wave panel directly.
    /// This switches to CustomPhase state and signals the frontend to show GreenWave panel.
    /// </summary>
    protected string CallOpenGreenWave(string input)
    {
        // Switch to CustomPhase state (this loads the sync data)
        SetMainPanelState(MainPanelState.CustomPhase);
        
        // Return a signal that frontend can use to auto-open GreenWave panel
        return JsonConvert.SerializeObject(new { openGreenWave = true });
    }

    protected string CallAddCustomPhase(string input)
    {
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            customPhaseDataBuffer.Add(new CustomPhaseData());
            UpdateActiveEditingCustomPhaseIndex(customPhaseDataBuffer.Length - 1);
            m_MainPanelBinding.Update();
            UpdateEdgeInfo(m_SelectedEntity);
            UpdateEntity();
        }
        return "";
    }

    protected string CallRemoveCustomPhase(string input)
    {
        var definition = new { index = 0 };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            customPhaseDataBuffer.RemoveAt(value.index);

            DynamicBuffer<EdgeGroupMask> edgeGroupMaskBuffer;
            DynamicBuffer<SubLaneGroupMask> subLaneGroupMaskBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out edgeGroupMaskBuffer))
            {
                edgeGroupMaskBuffer = EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
            }
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out subLaneGroupMaskBuffer))
            {
                subLaneGroupMaskBuffer = EntityManager.AddBuffer<SubLaneGroupMask>(m_SelectedEntity);
            }
            for (int i = value.index; i < 16; i++)
            {
                CustomPhaseUtils.SwapBit(subLaneGroupMaskBuffer, i, i + 1);
                CustomPhaseUtils.SwapBit(edgeGroupMaskBuffer, i, i + 1);
            }

            if (m_ActiveEditingCustomPhaseIndexBinding.value >= customPhaseDataBuffer.Length)
            {
                UpdateActiveEditingCustomPhaseIndex(customPhaseDataBuffer.Length - 1);
            }

            m_MainPanelBinding.Update();
            UpdateEdgeInfo(m_SelectedEntity);

            UpdateEntity();
        }
        return "";
    }

    protected string CallSwapCustomPhase(string input)
    {
        var definition = new { index1 = 0, index2 = 0 };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            (customPhaseDataBuffer[value.index2], customPhaseDataBuffer[value.index1]) = (customPhaseDataBuffer[value.index1], customPhaseDataBuffer[value.index2]);

            DynamicBuffer<EdgeGroupMask> edgeGroupMaskBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out edgeGroupMaskBuffer))
            {
                edgeGroupMaskBuffer = EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
            }
            CustomPhaseUtils.SwapBit(edgeGroupMaskBuffer, value.index1, value.index2);

            DynamicBuffer<SubLaneGroupMask> subLaneGroupMaskBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out subLaneGroupMaskBuffer))
            {
                subLaneGroupMaskBuffer = EntityManager.AddBuffer<SubLaneGroupMask>(m_SelectedEntity);
            }
            CustomPhaseUtils.SwapBit(subLaneGroupMaskBuffer, value.index1, value.index2);

            UpdateActiveEditingCustomPhaseIndex(value.index2);
            m_MainPanelBinding.Update();
            UpdateEdgeInfo(m_SelectedEntity);

            UpdateEntity();
        }
        return "";
    }

    protected string CallSetActiveCustomPhaseIndex(string input)
    {
        var definition = new { key = "", value = 0 };
        var result = JsonConvert.DeserializeAnonymousType(input, definition);
        if (result.key == "ActiveEditingCustomPhaseIndex")
        {
            UpdateActiveEditingCustomPhaseIndex(result.value);
            UpdateEntity();
        }
        else if (result.key == "ActiveViewingCustomPhaseIndex")
        {
            UpdateActiveViewingCustomPhaseIndex(result.value);
            RedrawGizmo();
        }
        else if (result.key == "ManualSignalGroup")
        {
            UpdateManualSignalGroup(result.value);
            RedrawGizmo();
        }
        m_MainPanelBinding.Update();
        return "";
    }

    protected string CallUpdateEdgeGroupMask(string input)
    {
        if (m_SelectedEntity.Equals(Entity.Null))
        {
            return "";
        }

        EdgeGroupMask[] groupMaskArray = JsonConvert.DeserializeObject<EdgeGroupMask[]>(input);
        DynamicBuffer<EdgeGroupMask> groupMaskBuffer;
        if (EntityManager.HasBuffer<EdgeGroupMask>(m_SelectedEntity))
        {
            groupMaskBuffer = EntityManager.GetBuffer<EdgeGroupMask>(m_SelectedEntity, false);
        }
        else
        {
            groupMaskBuffer = EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
        }

        foreach (var newValue in groupMaskArray)
        {
            int index = CustomPhaseUtils.TryGet(groupMaskBuffer, newValue, out EdgeGroupMask oldValue);
            if (index >= 0)
            {
                groupMaskBuffer[index] = new EdgeGroupMask(oldValue, newValue);
            }
            else
            {
                groupMaskBuffer.Add(new EdgeGroupMask(oldValue, newValue));
            }
        }

        UpdateEdgeInfo(m_SelectedEntity);
        UpdateEntity();

        return "";
    }

    protected string CallUpdateSubLaneGroupMask(string input)
    {
        if (m_SelectedEntity.Equals(Entity.Null))
        {
            return "";
        }

        SubLaneGroupMask[] groupMaskArray = JsonConvert.DeserializeObject<SubLaneGroupMask[]>(input);
        DynamicBuffer<SubLaneGroupMask> groupMaskBuffer;
        if (EntityManager.HasBuffer<SubLaneGroupMask>(m_SelectedEntity))
        {
            groupMaskBuffer = EntityManager.GetBuffer<SubLaneGroupMask>(m_SelectedEntity, false);
        }
        else
        {
            groupMaskBuffer = EntityManager.AddBuffer<SubLaneGroupMask>(m_SelectedEntity);
        }

        foreach (var newValue in groupMaskArray)
        {
            int index = CustomPhaseUtils.TryGet(groupMaskBuffer, newValue, out SubLaneGroupMask oldValue);
            if (index >= 0)
            {
                groupMaskBuffer[index] = new SubLaneGroupMask(oldValue, newValue);
            }
            else
            {
                groupMaskBuffer.Add(new SubLaneGroupMask(oldValue, newValue));
            }
        }

        UpdateEdgeInfo(m_SelectedEntity);
        UpdateEntity();

        return "";
    }

    protected string CallUpdateCustomPhaseData(string jsonString)
    {
        var input = JsonConvert.DeserializeObject<UITypes.UpdateCustomPhaseData>(jsonString);
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }

            int index = input.index >= 0 ? input.index : m_ActiveEditingCustomPhaseIndexBinding.value;
            if (index < 0 || index >= customPhaseDataBuffer.Length)
            {
                return "";
            }
            var newValue = customPhaseDataBuffer[index];

            if (input.key == "MinimumDuration")
            {
                newValue.m_MinimumDuration = (ushort)input.value;
                if (newValue.m_MinimumDuration > newValue.m_MaximumDuration)
                {
                    newValue.m_MaximumDuration = newValue.m_MinimumDuration;
                }
            }
            else if (input.key == "MaximumDuration")
            {
                newValue.m_MaximumDuration = (ushort)input.value;
                if (newValue.m_MinimumDuration > newValue.m_MaximumDuration)
                {
                    newValue.m_MinimumDuration = newValue.m_MaximumDuration;
                }
            }
            else if (input.key == "TargetDurationMultiplier")
            {
                newValue.m_TargetDurationMultiplier = (float)input.value;
            }
            else if (input.key == "LaneOccupiedMultiplier")
            {
                newValue.m_LaneOccupiedMultiplier = (float)input.value;
            }
            else if (input.key == "IntervalExponent")
            {
                newValue.m_IntervalExponent = (float)input.value;
            }
            else if (input.key == "PrioritiseTrack")
            {
                newValue.m_Options ^= CustomPhaseData.Options.PrioritiseTrack;
            }
            else if (input.key == "PrioritisePublicCar")
            {
                newValue.m_Options ^= CustomPhaseData.Options.PrioritisePublicCar;
            }
            else if (input.key == "PrioritisePedestrian")
            {
                newValue.m_Options ^= CustomPhaseData.Options.PrioritisePedestrian;
            }
            else if (input.key == "PrioritiseBicycle")
            {
                newValue.m_Options ^= CustomPhaseData.Options.PrioritiseBicycle;
            }
            else if (input.key == "LinkedWithNextPhase")
            {
                newValue.m_Options ^= CustomPhaseData.Options.LinkedWithNextPhase;
            }
            else if (input.key == "EndPhasePrematurely")
            {
                newValue.m_Options ^= CustomPhaseData.Options.EndPhasePrematurely;
            }
            customPhaseDataBuffer[index] = newValue;

            m_MainPanelBinding.Update();
            UpdateEdgeInfo(m_SelectedEntity);
            UpdateEntity(addUpdated: false);
        }
        return "";
    }

    protected string CallKeyPress(string value)
    {
        var definition = new { ctrlKey = false, key = "" };
        var keyPressEvent = JsonConvert.DeserializeAnonymousType(value, definition);
        if (keyPressEvent.ctrlKey && keyPressEvent.key == "S")
        {
            if (!m_SelectedEntity.Equals(Entity.Null))
            {
                SaveSelectedEntity();
            }
        }
        return "";
    }

    protected string CallAddWorldPosition(string input)
    {
        UITypes.WorldPosition[] posArray = JsonConvert.DeserializeObject<UITypes.WorldPosition[]>(input);
        foreach (var pos in posArray)
        {
            m_WorldPositionList.Add(pos);
        }
        m_CameraPosition = float.MaxValue; // Trigger binding update
        return "";
    }

    protected string CallRemoveWorldPosition(string input)
    {
        UITypes.WorldPosition[] posArray = JsonConvert.DeserializeObject<UITypes.WorldPosition[]>(input);
        foreach (var pos in posArray)
        {
            m_WorldPositionList.Remove(pos);
        }
        m_CameraPosition = float.MaxValue; // Trigger binding update
        return "";
    }

    protected string CallOpenBrowser(string jsonString)
    {
        var keyDefinition = new { key = "", value = "" };
        var parsedKey = JsonConvert.DeserializeAnonymousType(jsonString, keyDefinition);
        
        // V282: Security fix - only allow HTTP/HTTPS URLs to prevent arbitrary command execution
        if (System.Uri.TryCreate(parsedKey.value, System.UriKind.Absolute, out System.Uri uri) && 
            (uri.Scheme == System.Uri.UriSchemeHttp || uri.Scheme == System.Uri.UriSchemeHttps))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = parsedKey.value,
                UseShellExecute = true
            });
        }
        return "";
    }

    protected void UpdateActiveEditingCustomPhaseIndex(int index)
    {
        m_ActiveEditingCustomPhaseIndexBinding.Update(index);
        if (index >= 0)
        {
            m_ActiveViewingCustomPhaseIndexBinding.Update(-1);
        }
    }

    protected void UpdateActiveViewingCustomPhaseIndex(int index)
    {
        m_ActiveViewingCustomPhaseIndexBinding.Update(index);
        if (index >= 0)
        {
            m_ActiveEditingCustomPhaseIndexBinding.Update(-1);
        }
    }

    protected void UpdateManualSignalGroup(int group)
    {
        if (m_SelectedEntity != Entity.Null)
        {
            m_CustomTrafficLights.m_ManualSignalGroup = (byte)group;
            if (group > 0 && EntityManager.TryGetComponent<TrafficLights>(m_SelectedEntity, out var trafficLights))
            {
                trafficLights.m_NextSignalGroup = (byte)group;
                EntityManager.SetComponentData(m_SelectedEntity, trafficLights);
            }
            UpdateEntity(addUpdated: false);
        }
        if (group > 0)
        {
            m_ActiveViewingCustomPhaseIndexBinding.Update(-1);
            m_ActiveEditingCustomPhaseIndexBinding.Update(-1);
        }
    }
    // =====================================================
    // COPY/PASTE FEATURE - Data Structures
    // =====================================================
    
    private class CopiedPhaseData
    {
        public CustomPhaseData PhaseData;
        public int OriginalPhaseIndex;  // Der Index der Phase beim Kopieren
        public List<CopiedEdgeMask> EdgeMasks = new List<CopiedEdgeMask>();
        public List<CopiedSubLaneMask> SubLaneMasks = new List<CopiedSubLaneMask>();
    }
    
    private class CopiedEdgeMask
    {
        public Entity Edge;
        public EdgeGroupMask Mask;
    }
    
    private class CopiedSubLaneMask
    {
        public Entity SubLane;
        public SubLaneGroupMask Mask;
    }
    
    private class PhaseIndicesInput
    {
        public int[] indices { get; set; }
    }

    // =====================================================
    // COPY/PASTE FEATURE - Methods
    // =====================================================
    
    protected string CallCopyPhases(string jsonString)
    {
        var input = JsonConvert.DeserializeObject<PhaseIndicesInput>(jsonString);
        
        if (m_SelectedEntity.Equals(Entity.Null) || input?.indices == null || input.indices.Length == 0)
        {
            return JsonConvert.SerializeObject(new { success = false, error = "No phases selected" });
        }
        
        m_PhaseClipboard.Clear();
        
        if (!EntityManager.TryGetBuffer<CustomPhaseData>(m_SelectedEntity, true, out var phaseBuffer))
        {
            return JsonConvert.SerializeObject(new { success = false, error = "No phase data found" });
        }
        
        EntityManager.TryGetBuffer<EdgeGroupMask>(m_SelectedEntity, true, out var edgeGroupMasks);
        EntityManager.TryGetBuffer<SubLaneGroupMask>(m_SelectedEntity, true, out var subLaneGroupMasks);
        
        foreach (int index in input.indices)
        {
            if (index < 0 || index >= phaseBuffer.Length) continue;
            
            var copied = new CopiedPhaseData { PhaseData = phaseBuffer[index], OriginalPhaseIndex = index };
            
            // Copy edge masks for this phase (bit index = phase index)
            if (edgeGroupMasks.IsCreated)
            {
                for (int i = 0; i < edgeGroupMasks.Length; i++)
                {
                    var mask = edgeGroupMasks[i];
                    copied.EdgeMasks.Add(new CopiedEdgeMask { Edge = mask.m_Edge, Mask = mask });
                }
            }
            
            // Copy sublane masks for this phase
            if (subLaneGroupMasks.IsCreated)
            {
                for (int i = 0; i < subLaneGroupMasks.Length; i++)
                {
                    var mask = subLaneGroupMasks[i];
                    copied.SubLaneMasks.Add(new CopiedSubLaneMask { SubLane = mask.m_SubLane, Mask = mask });
                }
            }
            
            m_PhaseClipboard.Add(copied);
        }
        
        m_ClipboardCountBinding.Update(m_PhaseClipboard.Count);
        Mod.LogDebug($"[CopyPhases] Copied {m_PhaseClipboard.Count} phases to clipboard");
        return JsonConvert.SerializeObject(new { success = true, count = m_PhaseClipboard.Count });
    }
    
    protected string CallPastePhases(string jsonString)
    {
        if (m_SelectedEntity.Equals(Entity.Null))
        {
            return JsonConvert.SerializeObject(new { success = false, error = "No intersection selected" });
        }
        
        if (m_PhaseClipboard.Count == 0)
        {
            return JsonConvert.SerializeObject(new { success = false, error = "Clipboard is empty" });
        }
        
        if (!EntityManager.TryGetBuffer<CustomPhaseData>(m_SelectedEntity, false, out var phaseBuffer))
        {
            phaseBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
        }
        
        // Get mask buffers
        if (!EntityManager.TryGetBuffer<EdgeGroupMask>(m_SelectedEntity, false, out var targetEdgeMasks))
        {
            targetEdgeMasks = EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
        }
        if (!EntityManager.TryGetBuffer<SubLaneGroupMask>(m_SelectedEntity, false, out var targetSubLaneMasks))
        {
            targetSubLaneMasks = EntityManager.AddBuffer<SubLaneGroupMask>(m_SelectedEntity);
        }
        
        int availableSlots = 16 - phaseBuffer.Length;
        int phasesToPaste = System.Math.Min(m_PhaseClipboard.Count, availableSlots);
        
        if (phasesToPaste == 0)
        {
            return JsonConvert.SerializeObject(new { success = false, error = "Maximum 16 phases reached" });
        }
        
        for (int i = 0; i < phasesToPaste; i++)
        {
            var copied = m_PhaseClipboard[i];
            int newPhaseIndex = phaseBuffer.Length;
            int originalPhaseIndex = copied.OriginalPhaseIndex;
            
            // Add the phase data
            phaseBuffer.Add(copied.PhaseData);
            
            // Kopiere Edge Masks
            int edgeCount = System.Math.Min(copied.EdgeMasks.Count, targetEdgeMasks.Length);
            for (int edgeIdx = 0; edgeIdx < edgeCount; edgeIdx++)
            {
                var sourceMask = copied.EdgeMasks[edgeIdx].Mask;
                var targetMask = targetEdgeMasks[edgeIdx];
                
                ushort srcBit = (ushort)(1 << originalPhaseIndex);
                ushort dstBit = (ushort)(1 << newPhaseIndex);
                
                // Car turns - m_Left, m_Straight, m_Right, m_UTurn sind Turn structs mit Signal members
                CopySignalBit(ref targetMask.m_Car.m_Left, sourceMask.m_Car.m_Left, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_Car.m_Straight, sourceMask.m_Car.m_Straight, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_Car.m_Right, sourceMask.m_Car.m_Right, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_Car.m_UTurn, sourceMask.m_Car.m_UTurn, srcBit, dstBit);
                
                // PublicCar turns
                CopySignalBit(ref targetMask.m_PublicCar.m_Left, sourceMask.m_PublicCar.m_Left, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_PublicCar.m_Straight, sourceMask.m_PublicCar.m_Straight, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_PublicCar.m_Right, sourceMask.m_PublicCar.m_Right, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_PublicCar.m_UTurn, sourceMask.m_PublicCar.m_UTurn, srcBit, dstBit);
                
                // Track turns
                CopySignalBit(ref targetMask.m_Track.m_Left, sourceMask.m_Track.m_Left, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_Track.m_Straight, sourceMask.m_Track.m_Straight, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_Track.m_Right, sourceMask.m_Track.m_Right, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_Track.m_UTurn, sourceMask.m_Track.m_UTurn, srcBit, dstBit);
                
                // Pedestrian signals (Signal structs direkt)
                CopySignalBit(ref targetMask.m_PedestrianStopLine, sourceMask.m_PedestrianStopLine, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_PedestrianNonStopLine, sourceMask.m_PedestrianNonStopLine, srcBit, dstBit);
                
                targetEdgeMasks[edgeIdx] = targetMask;
            }
            
            // Kopiere SubLane Masks
            int subLaneCount = System.Math.Min(copied.SubLaneMasks.Count, targetSubLaneMasks.Length);
            for (int subLaneIdx = 0; subLaneIdx < subLaneCount; subLaneIdx++)
            {
                var sourceMask = copied.SubLaneMasks[subLaneIdx].Mask;
                var targetMask = targetSubLaneMasks[subLaneIdx];
                
                ushort srcBit = (ushort)(1 << originalPhaseIndex);
                ushort dstBit = (ushort)(1 << newPhaseIndex);
                
                // Car (Turn struct)
                CopySignalBit(ref targetMask.m_Car.m_Left, sourceMask.m_Car.m_Left, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_Car.m_Straight, sourceMask.m_Car.m_Straight, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_Car.m_Right, sourceMask.m_Car.m_Right, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_Car.m_UTurn, sourceMask.m_Car.m_UTurn, srcBit, dstBit);
                
                // Track (Turn struct)
                CopySignalBit(ref targetMask.m_Track.m_Left, sourceMask.m_Track.m_Left, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_Track.m_Straight, sourceMask.m_Track.m_Straight, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_Track.m_Right, sourceMask.m_Track.m_Right, srcBit, dstBit);
                CopySignalBit(ref targetMask.m_Track.m_UTurn, sourceMask.m_Track.m_UTurn, srcBit, dstBit);
                
                // Pedestrian (Signal struct direkt)
                CopySignalBit(ref targetMask.m_Pedestrian, sourceMask.m_Pedestrian, srcBit, dstBit);
                
                targetSubLaneMasks[subLaneIdx] = targetMask;
            }
        }
        
        m_MainPanelBinding.Update();
        UpdateEdgeInfo(m_SelectedEntity);
        UpdateEntity(addUpdated: true);
        
        Mod.LogDebug($"[PastePhases] Pasted {phasesToPaste} phases with masks");
        
        // Schedule delayed redraw so lane overlays update after game processes the changes
        ScheduleDelayedRedraw(0.5f);
        return JsonConvert.SerializeObject(new { success = true, count = phasesToPaste });
    }
    
    // Helper: Kopiert ein Signal-Bit von Source zu Target
    private void CopySignalBit(ref GroupMask.Signal target, GroupMask.Signal source, ushort srcBit, ushort dstBit)
    {
        if ((source.m_GoGroupMask & srcBit) != 0)
            target.m_GoGroupMask |= dstBit;
        if ((source.m_YieldGroupMask & srcBit) != 0)
            target.m_YieldGroupMask |= dstBit;
    }
    
    protected string CallDeletePhases(string jsonString)
    {
        var input = JsonConvert.DeserializeObject<PhaseIndicesInput>(jsonString);
        
        if (m_SelectedEntity.Equals(Entity.Null) || input?.indices == null || input.indices.Length == 0)
        {
            return JsonConvert.SerializeObject(new { success = false, error = "No phases selected" });
        }
        
        if (!EntityManager.TryGetBuffer<CustomPhaseData>(m_SelectedEntity, false, out var phaseBuffer))
        {
            return JsonConvert.SerializeObject(new { success = false, error = "No phase data found" });
        }
        
        // Get mask buffers
        if (!EntityManager.TryGetBuffer<EdgeGroupMask>(m_SelectedEntity, false, out var edgeGroupMaskBuffer))
        {
            edgeGroupMaskBuffer = EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
        }
        if (!EntityManager.TryGetBuffer<SubLaneGroupMask>(m_SelectedEntity, false, out var subLaneGroupMaskBuffer))
        {
            subLaneGroupMaskBuffer = EntityManager.AddBuffer<SubLaneGroupMask>(m_SelectedEntity);
        }
        
        // Sort descending to delete from end first
        var sortedIndices = input.indices.OrderByDescending(i => i).ToArray();
        int deletedCount = 0;
        
        foreach (int index in sortedIndices)
        {
            if (index >= 0 && index < phaseBuffer.Length)
            {
                // Remove phase
                phaseBuffer.RemoveAt(index);
                
                // Shift bits in masks (same logic as CallRemoveCustomPhase)
                for (int i = index; i < 16; i++)
                {
                    CustomPhaseUtils.SwapBit(subLaneGroupMaskBuffer, i, i + 1);
                    CustomPhaseUtils.SwapBit(edgeGroupMaskBuffer, i, i + 1);
                }
                
                deletedCount++;
            }
        }
        
        // Reset editing index if needed
        if (m_ActiveEditingCustomPhaseIndexBinding.value >= phaseBuffer.Length)
        {
            UpdateActiveEditingCustomPhaseIndex(-1);
        }
        
        m_MainPanelBinding.Update();
        UpdateEdgeInfo(m_SelectedEntity);
        UpdateEntity(addUpdated: false);
        
        Mod.LogDebug($"[DeletePhases] Deleted {deletedCount} phases");
        return JsonConvert.SerializeObject(new { success = true, count = deletedCount });
    }
    
    protected string CallClearPhaseClipboard(string jsonString)
    {
        int clearedCount = m_PhaseClipboard.Count;
        m_PhaseClipboard.Clear();
        m_ClipboardCountBinding.Update(0);
        Mod.LogDebug($"[ClearClipboard] Cleared {clearedCount} phases from clipboard");
        return JsonConvert.SerializeObject(new { success = true, clearedCount = clearedCount });
    }

    // ==================== GREEN WAVE SYNC METHODS ====================

    /// <summary>
    /// Updates sync settings for the selected intersection.
    /// Input: { syncGroupId: uint, useSyncedCycle: bool, cycleOffsetSeconds: int, useSequentialPhases: bool }
    /// </summary>
    protected string CallUpdateSyncSettings(string jsonString)
    {
        if (m_SelectedEntity == Entity.Null)
        {
            Mod.LogDebug($"[UpdateSyncSettings] ERROR: No intersection selected!");
            return JsonConvert.SerializeObject(new { success = false, error = "No intersection selected" });
        }

        try
        {
            var input = JsonConvert.DeserializeAnonymousType(jsonString, new { 
                syncGroupId = 0u, 
                useSyncedCycle = false, 
                cycleOffsetSeconds = 0,
                syncPhaseIndex = 0,
                useSequentialPhases = false
            });

            Mod.LogDebug($"[UpdateSyncSettings] Entity={m_SelectedEntity.Index}, Input: GroupId={input.syncGroupId}, UseSynced={input.useSyncedCycle}, Sequential={input.useSequentialPhases}");

            if (!EntityManager.TryGetComponent<CustomTrafficLights>(m_SelectedEntity, out var ctl))
            {
                Mod.LogDebug($"[UpdateSyncSettings] ERROR: No CustomTrafficLights component on entity {m_SelectedEntity.Index}!");
                return JsonConvert.SerializeObject(new { success = false, error = "No CustomTrafficLights component" });
            }

            Mod.LogDebug($"[UpdateSyncSettings] BEFORE: Entity {m_SelectedEntity.Index} has SyncGroupId={ctl.m_SyncGroupId}");

            // Store old group ID for updating BaseCycle when intersection moves
            uint oldGroupId = ctl.m_SyncGroupId;

            // Clamp offset to valid range
            ushort offset = (ushort)System.Math.Max(0, System.Math.Min(999, input.cycleOffsetSeconds));
            // Clamp phase index to valid range (0-255)
            byte phaseIndex = (byte)System.Math.Max(0, System.Math.Min(255, input.syncPhaseIndex));

            ctl.SetSyncGroupId(input.syncGroupId);
            ctl.SetUseSyncedCycle(input.useSyncedCycle);
            ctl.SetCycleOffsetSeconds(offset);
            ctl.SetSyncPhaseIndex(phaseIndex);
            
            // IMPORTANT: When Green Wave is active, FORCE sequential mode!
            // Without sequential phases, Green Wave sync cannot work correctly.
            if (input.useSyncedCycle && input.syncGroupId > 0)
            {
                ctl.SetUseSequentialPhases(true);  // Force sequential for Green Wave
            }
            else
            {
                ctl.SetUseSequentialPhases(input.useSequentialPhases);
            }
            
            // IMPORTANT: Copy group definition to intersection for savegame persistence
            if (input.syncGroupId > 0)
            {
                // V199.3: Use try-finally to ALWAYS dispose NativeArray
                Unity.Collections.NativeArray<SyncGroup> groups = default;
                try
                {
                    groups = m_SyncGroupSystem.GetAllGroups(Unity.Collections.Allocator.Temp);
                    for (int i = 0; i < groups.Length; i++)
                    {
                        if (groups[i].m_GroupId == input.syncGroupId)
                        {
                            ctl.SetSyncGroupName(groups[i].GetName());
                            ctl.SetSyncGroupBaseCycle(groups[i].m_BaseCycleDuration);
                            ctl.SetSyncGroupAlwaysActive(groups[i].m_AlwaysActive);
                            ctl.SetSyncGroupTimeWindows(
                                groups[i].m_TimeWindow1Start, groups[i].m_TimeWindow1End,
                                groups[i].m_TimeWindow2Start, groups[i].m_TimeWindow2End,
                                groups[i].m_TimeWindow3Start, groups[i].m_TimeWindow3End
                            );
                            Mod.LogDebug($"[UpdateSyncSettings] Copied group '{groups[i].GetName()}' definition to intersection");
                            break;
                        }
                    }
                }
                finally
                {
                    if (groups.IsCreated)
                        groups.Dispose();
                }
            }
            else
            {
                // Clear group data when removing from group
                ctl.SetSyncGroupName("");
                ctl.SetSyncGroupBaseCycle(60);
                ctl.SetSyncGroupAlwaysActive(true);
                ctl.SetSyncGroupTimeWindows(255, 255, 255, 255, 255, 255);
            }

            EntityManager.SetComponentData(m_SelectedEntity, ctl);
            
            // CRITICAL FIX: Also update the class member to keep it in sync!
            // Without this, UpdateEntity() would overwrite with old data when switching intersections.
            m_CustomTrafficLights = ctl;
            
            // Update the group's BaseCycle duration when intersections change
            if (input.syncGroupId > 0 && input.useSyncedCycle)
            {
                m_SyncGroupSystem.UpdateGroupBaseCycle(input.syncGroupId);
            }
            // Also update old group if intersection was moved
            if (oldGroupId > 0 && oldGroupId != input.syncGroupId)
            {
                m_SyncGroupSystem.UpdateGroupBaseCycle(oldGroupId);
            }
            
            // Verify the change was applied
            var ctlAfter = EntityManager.GetComponentData<CustomTrafficLights>(m_SelectedEntity);
            Mod.LogDebug($"[UpdateSyncSettings] AFTER: Entity {m_SelectedEntity.Index} has SyncGroupId={ctlAfter.m_SyncGroupId}");
            Mod.LogDebug($"[UpdateSyncSettings] m_CustomTrafficLights also updated to SyncGroupId={m_CustomTrafficLights.m_SyncGroupId}");
            
            m_MainPanelBinding.Update();

            Mod.LogDebug($"[UpdateSyncSettings] SUCCESS: GroupId={input.syncGroupId}, UseSynced={input.useSyncedCycle}, Offset={offset}, PhaseIdx={phaseIndex}");
            return JsonConvert.SerializeObject(new { success = true });
        }
        catch (System.Exception ex)
        {
            Mod.LogDebug($"[UpdateSyncSettings] EXCEPTION: {ex.Message}");
            return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new sync group.
    /// Input: { groupName: string, baseCycleDuration: int }
    /// </summary>
    protected string CallCreateSyncGroup(string jsonString)
    {
        try
        {
            var input = JsonConvert.DeserializeAnonymousType(jsonString, new { 
                groupName = "New Group", 
                baseCycleDuration = 60 
            });

            ushort cycleDuration = (ushort)System.Math.Max(10, System.Math.Min(600, input.baseCycleDuration));
            uint newGroupId = m_SyncGroupSystem.CreateGroup(input.groupName, cycleDuration);

            m_MainPanelBinding.Update();
            return JsonConvert.SerializeObject(new { success = true, groupId = newGroupId });
        }
        catch (System.Exception ex)
        {
            return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a sync group.
    /// Input: { groupId: uint }
    /// </summary>
    protected string CallDeleteSyncGroup(string jsonString)
    {
        try
        {
            var input = JsonConvert.DeserializeAnonymousType(jsonString, new { groupId = 0u });

            bool deleted = m_SyncGroupSystem.DeleteGroup(input.groupId);

            if (deleted)
            {
                m_MainPanelBinding.Update();
                return JsonConvert.SerializeObject(new { success = true });
            }
            else
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Group not found" });
            }
        }
        catch (System.Exception ex)
        {
            return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Renames a sync group.
    /// Input: { groupId: uint, newName: string }
    /// </summary>
    protected string CallRenameSyncGroup(string jsonString)
    {
        try
        {
            var input = JsonConvert.DeserializeAnonymousType(jsonString, new { 
                groupId = 0u, 
                newName = "" 
            });

            bool renamed = m_SyncGroupSystem.RenameGroup(input.groupId, input.newName);

            if (renamed)
            {
                m_MainPanelBinding.Update();
                return JsonConvert.SerializeObject(new { success = true });
            }
            else
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Group not found" });
            }
        }
        catch (System.Exception ex)
        {
            return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Sets time windows for a sync group.
    /// Input: { groupId: uint, alwaysActive: bool, 
    ///          timeWindow1Start: int, timeWindow1End: int,
    ///          timeWindow2Start: int, timeWindow2End: int,
    ///          timeWindow3Start: int, timeWindow3End: int }
    /// Use 255 for disabled windows.
    /// </summary>
    protected string CallSetGroupTimeWindows(string jsonString)
    {
        try
        {
            var input = JsonConvert.DeserializeAnonymousType(jsonString, new { 
                groupId = 0u, 
                alwaysActive = true,
                timeWindow1Start = 255,
                timeWindow1End = 255,
                timeWindow2Start = 255,
                timeWindow2End = 255,
                timeWindow3Start = 255,
                timeWindow3End = 255
            });

            bool success = m_SyncGroupSystem.SetGroupTimeWindows(
                input.groupId,
                input.alwaysActive,
                (byte)input.timeWindow1Start, (byte)input.timeWindow1End,
                (byte)input.timeWindow2Start, (byte)input.timeWindow2End,
                (byte)input.timeWindow3Start, (byte)input.timeWindow3End
            );

            if (success)
            {
                m_MainPanelBinding.Update();
                return JsonConvert.SerializeObject(new { success = true });
            }
            else
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Group not found" });
            }
        }
        catch (System.Exception ex)
        {
            Mod.LogError($"[CallSetGroupTimeWindows] Error: {ex.Message}");
            return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
        }
    }

    // === GREEN WAVE PHASE 2: INTERSECTION ORDERING ===

    /// <summary>
    /// Moves an intersection up in the group order (earlier in green wave).
    /// Input: { groupId: uint, entityIndex: int }
    /// </summary>
    protected string CallMoveIntersectionUp(string jsonString)
    {
        try
        {
            var input = JsonConvert.DeserializeAnonymousType(jsonString, new { 
                groupId = 0u, 
                entityIndex = 0 
            });

            // Get intersections in group to find entity
            var intersections = m_SyncGroupSystem.GetIntersectionsInGroup(input.groupId);
            var target = intersections.FirstOrDefault(i => i.Entity.Index == input.entityIndex);
            
            if (target.Entity == Entity.Null)
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Intersection not found" });
            }

            bool moved = m_SyncGroupSystem.MoveIntersectionUp(input.groupId, target.Entity);

            if (moved)
            {
                m_MainPanelBinding.Update();
                return JsonConvert.SerializeObject(new { success = true });
            }
            else
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Cannot move up (already first)" });
            }
        }
        catch (System.Exception ex)
        {
            Mod.LogError($"[CallMoveIntersectionUp] Error: {ex.Message}");
            return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Moves an intersection down in the group order (later in green wave).
    /// Input: { groupId: uint, entityIndex: int }
    /// </summary>
    protected string CallMoveIntersectionDown(string jsonString)
    {
        try
        {
            var input = JsonConvert.DeserializeAnonymousType(jsonString, new { 
                groupId = 0u, 
                entityIndex = 0 
            });

            // Get intersections in group to find entity
            var intersections = m_SyncGroupSystem.GetIntersectionsInGroup(input.groupId);
            var target = intersections.FirstOrDefault(i => i.Entity.Index == input.entityIndex);
            
            if (target.Entity == Entity.Null)
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Intersection not found" });
            }

            bool moved = m_SyncGroupSystem.MoveIntersectionDown(input.groupId, target.Entity);

            if (moved)
            {
                m_MainPanelBinding.Update();
                return JsonConvert.SerializeObject(new { success = true });
            }
            else
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Cannot move down (already last)" });
            }
        }
        catch (System.Exception ex)
        {
            Mod.LogError($"[CallMoveIntersectionDown] Error: {ex.Message}");
            return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Removes an intersection from its sync group.
    /// Input: { entityIndex: int }
    /// </summary>
    protected string CallRemoveIntersectionFromGroup(string jsonString)
    {
        try
        {
            var input = JsonConvert.DeserializeAnonymousType(jsonString, new { 
                entityIndex = 0 
            });

            // Create entity from index
            var entity = new Entity { Index = input.entityIndex, Version = 1 };
            
            // Try to find the actual entity
            // V231: Use cached query to prevent memory leak!
            var query = GetOrCreateTrafficLightsQuery();
            Unity.Collections.NativeArray<Entity> entities = default;
            Entity foundEntity = Entity.Null;
            
            try
            {
                entities = query.ToEntityArray(Allocator.Temp);
                
                for (int i = 0; i < entities.Length; i++)
                {
                    if (entities[i].Index == input.entityIndex)
                    {
                        foundEntity = entities[i];
                        break;
                    }
                }
            }
            finally
            {
                if (entities.IsCreated)
                    entities.Dispose();
            }
            
            if (foundEntity == Entity.Null)
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Intersection not found" });
            }

            // Get the group ID before removing (to update BaseCycle after)
            uint oldGroupId = 0;
            if (EntityManager.TryGetComponent<CustomTrafficLights>(foundEntity, out var ctl))
            {
                oldGroupId = ctl.m_SyncGroupId;
            }

            bool removed = m_SyncGroupSystem.RemoveIntersectionFromGroup(foundEntity);

            if (removed)
            {
                // Update the old group's BaseCycle
                if (oldGroupId > 0)
                {
                    m_SyncGroupSystem.UpdateGroupBaseCycle(oldGroupId);
                }
                m_MainPanelBinding.Update();
                return JsonConvert.SerializeObject(new { success = true });
            }
            else
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Failed to remove intersection" });
            }
        }
        catch (System.Exception ex)
        {
            Mod.LogError($"[CallRemoveIntersectionFromGroup] Error: {ex.Message}");
            return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Sets the offset for an intersection in real-time seconds.
    /// Input: { entityIndex: int, offsetRealSeconds: float }
    /// </summary>
    protected string CallSetIntersectionOffset(string jsonString)
    {
        try
        {
            var input = JsonConvert.DeserializeAnonymousType(jsonString, new { 
                entityIndex = 0,
                offsetRealSeconds = 0f
            });

            // Find the actual entity
            // V231: Use cached query to prevent memory leak!
            var query = GetOrCreateTrafficLightsQuery();
            Unity.Collections.NativeArray<Entity> entities = default;
            Entity foundEntity = Entity.Null;
            
            try
            {
                entities = query.ToEntityArray(Allocator.Temp);
                
                for (int i = 0; i < entities.Length; i++)
                {
                    if (entities[i].Index == input.entityIndex)
                    {
                        foundEntity = entities[i];
                        break;
                    }
                }
            }
            finally
            {
                if (entities.IsCreated)
                    entities.Dispose();
            }
            
            if (foundEntity == Entity.Null)
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Intersection not found" });
            }

            m_SyncGroupSystem.SetIntersectionOffsetRealSeconds(foundEntity, input.offsetRealSeconds);
            m_MainPanelBinding.Update();
            
            return JsonConvert.SerializeObject(new { success = true });
        }
        catch (System.Exception ex)
        {
            Mod.LogError($"[CallSetIntersectionOffset] Error: {ex.Message}");
            return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
        }
    }
}












