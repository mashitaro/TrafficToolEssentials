using C2VM.TrafficToolEssentials.Components;
using C2VM.TrafficToolEssentials.Utils;
using Game.Net;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem;

namespace C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Initialisation;

public struct CustomPhaseProcessor
{
    public static void ProcessLanes(ref InitializeTrafficLightsJob job, int unfilteredChunkIndex, Entity nodeEntity, DynamicBuffer<ConnectedEdge> connectedEdges, DynamicBuffer<SubLane> subLanes, out int groupCount, ref TrafficLights trafficLights, ref CustomTrafficLights customTrafficLights, DynamicBuffer<EdgeGroupMask> edgeGroupMasks, DynamicBuffer<SubLaneGroupMask> subLaneGroupMasks, DynamicBuffer<CustomPhaseData> customPhaseDatas)
    {
        NativeHashMap<Entity, NodeUtils.LaneConnection> laneConnectionMap = NodeUtils.GetLaneConnectionMap(Allocator.Temp, subLanes, connectedEdges, job.m_ExtraTypeHandle.m_SubLane, job.m_ExtraTypeHandle.m_Lane);
        groupCount = customPhaseDatas.Length;

        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out LaneSignal laneSignal))
            {
                continue;
            }
            laneSignal.m_GroupMask = 0;
            job.m_LaneSignalData[subLane] = laneSignal;
        }

        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            bool isPedestrian = job.m_PedestrianLaneData.TryGetComponent(subLane, out var pedestrianLane);
            if (!job.m_LaneSignalData.HasComponent(subLane) && (pedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) == 0)
            {
                continue;
            }
            if ((pedestrianLane.m_Flags & (PedestrianLaneFlags.Crosswalk | PedestrianLaneFlags.Unsafe)) == (PedestrianLaneFlags.Crosswalk | PedestrianLaneFlags.Unsafe))
            {
                continue;
            }
            if (job.m_MasterLaneData.HasComponent(subLane))
            {
                continue;
            }
            var laneConnection = NodeUtils.GetLaneConnectionFromNodeSubLane(subLane, laneConnectionMap, (pedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) != 0);
            var sourceEdge = laneConnection.m_SourceEdge == Entity.Null && isPedestrian ? laneConnection.m_DestEdge : laneConnection.m_SourceEdge;
            var edgePosition = NodeUtils.GetEdgePosition(ref job, nodeEntity, sourceEdge);
            LaneSignal laneSignal = new LaneSignal();
            if (job.m_LaneSignalData.HasComponent(subLane))
            {
                laneSignal = job.m_LaneSignalData[subLane];
            }
            laneSignal.m_GroupMask = ushort.MaxValue;
            laneSignal.m_Default = 0;
            ExtraLaneSignal extraLaneSignal = new ExtraLaneSignal();
            extraLaneSignal.m_SourceSubLane = laneConnection.m_SourceSubLane;
            if (CustomPhaseUtils.TryGet(edgeGroupMasks, sourceEdge, edgePosition, out EdgeGroupMask groupMask) >= 0)
            {
                if ((groupMask.m_Options & EdgeGroupMask.Options.PerLaneSignal) != 0)
                {
                    Entity searchKey = isPedestrian ? subLane : laneConnection.m_SourceSubLane;
                    float3 subLanePosition = NodeUtils.GetSubLanePosition(searchKey, job.m_CurveData);
                    CustomPhaseUtils.TryGet(subLaneGroupMasks, searchKey, subLanePosition, out SubLaneGroupMask subLaneGroupMask);
                    groupMask.m_Car = subLaneGroupMask.m_Car;
                    groupMask.m_PublicCar = subLaneGroupMask.m_Car;
                    groupMask.m_Track = subLaneGroupMask.m_Track;
                    groupMask.m_PedestrianStopLine = subLaneGroupMask.m_Pedestrian;
                    groupMask.m_PedestrianNonStopLine = subLaneGroupMask.m_Pedestrian;
                }
                if (job.m_CarLaneData.TryGetComponent(subLane, out var nodeCarLane))
                {
                    job.m_CarLaneData.TryGetComponent(laneConnection.m_SourceSubLane, out var edgeCarLane);
                    var turn = (edgeCarLane.m_Flags & CarLaneFlags.PublicOnly) != 0 ? groupMask.m_PublicCar : groupMask.m_Car;
                    if ((nodeCarLane.m_Flags & (CarLaneFlags.TurnLeft | CarLaneFlags.GentleTurnLeft)) != 0)
                    {
                        laneSignal.m_GroupMask = turn.m_Left.m_GoGroupMask;
                        extraLaneSignal.m_YieldGroupMask = turn.m_Left.m_YieldGroupMask;
                        extraLaneSignal.m_IgnorePriorityGroupMask = turn.m_Left.m_YieldGroupMask;
                    }
                    else if ((nodeCarLane.m_Flags & (CarLaneFlags.TurnRight | CarLaneFlags.GentleTurnRight)) != 0)
                    {
                        laneSignal.m_GroupMask = turn.m_Right.m_GoGroupMask;
                        extraLaneSignal.m_YieldGroupMask = turn.m_Right.m_YieldGroupMask;
                        extraLaneSignal.m_IgnorePriorityGroupMask = turn.m_Right.m_YieldGroupMask;
                    }
                    else
                    {
                        laneSignal.m_GroupMask = turn.m_Straight.m_GoGroupMask;
                        extraLaneSignal.m_YieldGroupMask = turn.m_Straight.m_YieldGroupMask;
                        extraLaneSignal.m_IgnorePriorityGroupMask = turn.m_Straight.m_YieldGroupMask;
                    }
                    if ((nodeCarLane.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.UTurnRight)) != 0)
                    {
                        laneSignal.m_GroupMask = turn.m_UTurn.m_GoGroupMask;
                        extraLaneSignal.m_YieldGroupMask = turn.m_UTurn.m_YieldGroupMask;
                        extraLaneSignal.m_IgnorePriorityGroupMask = turn.m_UTurn.m_YieldGroupMask;
                    }
                    laneSignal.m_Flags |= LaneSignalFlags.CanExtend;
                }
                if (job.m_ExtraTypeHandle.m_TrackLane.TryGetComponent(subLane, out var trackLane))
                {
                    if ((trackLane.m_Flags & TrackLaneFlags.TurnLeft) != 0)
                    {
                        laneSignal.m_GroupMask = groupMask.m_Track.m_Left.m_GoGroupMask;
                    }
                    else if ((trackLane.m_Flags & TrackLaneFlags.TurnRight) != 0)
                    {
                        laneSignal.m_GroupMask = groupMask.m_Track.m_Right.m_GoGroupMask;
                    }
                    else
                    {
                        laneSignal.m_GroupMask = groupMask.m_Track.m_Straight.m_GoGroupMask;
                    }
                }
                if ((pedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) != 0)
                {
                    if (NodeUtils.IsCrossingStopLine(ref job, subLane, sourceEdge))
                    {
                        laneSignal.m_GroupMask = groupMask.m_PedestrianStopLine.m_GoGroupMask;
                    }
                    else
                    {
                        laneSignal.m_GroupMask = groupMask.m_PedestrianNonStopLine.m_GoGroupMask;
                    }
                }
            }

            Simulation.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
            if (job.m_LaneSignalData.HasComponent(subLane))
            {
                job.m_LaneSignalData[subLane] = laneSignal;
            }
            else
            {
                job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane, laneSignal);
            }
            if (job.m_ExtraTypeHandle.m_ExtraLaneSignal.HasComponent(subLane))
            {
                job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
            }
            else
            {
                job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
            }
        }

        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            if (!job.m_MasterLaneData.TryGetComponent(subLane, out MasterLane masterLane))
            {
                continue;
            }
            if (!job.m_LaneSignalData.TryGetComponent(subLane, out LaneSignal laneSignal))
            {
                continue;
            }

            laneSignal.m_GroupMask = 0;
            for (int j = masterLane.m_MinIndex; j <= masterLane.m_MaxIndex; j++)
            {
                Entity slaveSubLane = subLanes[j].m_SubLane;
                if (!job.m_LaneSignalData.TryGetComponent(slaveSubLane, out LaneSignal slaveLaneSignal))
                {
                    continue;
                }
                laneSignal.m_GroupMask |= slaveLaneSignal.m_GroupMask;
            }

            ExtraLaneSignal extraLaneSignal = new();
            Simulation.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
            job.m_LaneSignalData[subLane] = laneSignal;
        }

        // =====================================================================
        // V132/V133: BICYCLE LANE (SecondaryLane) SEPARATE SIGNAL CONTROL
        // =====================================================================
        // SecondaryLanes (bicycle lanes) DON'T have Node-Lanes! They only exist
        // on Edges. So we can't use the laneConnectionMap approach.
        // Instead, we iterate over all connected edges and find SecondaryLanes
        // directly on those edges.
        // =====================================================================
        foreach (var connectedEdge in connectedEdges)
        {
            Entity edgeEntity = connectedEdge.m_Edge;
            var edgePosition = NodeUtils.GetEdgePosition(ref job, nodeEntity, edgeEntity);
            
            // Get the EdgeGroupMask for this edge
            if (CustomPhaseUtils.TryGet(edgeGroupMasks, edgeEntity, edgePosition, out EdgeGroupMask groupMask) < 0)
            {
                continue;
            }
            
            // V133: Calculate the bicycle GroupMask with fallback
            ushort bicycleGoMask = groupMask.m_Bicycle.m_GoGroupMask;
            if (bicycleGoMask == 0)
            {
                // FALLBACK: Bicycle not configured in UI, use car straight phase
                bicycleGoMask = groupMask.m_Car.m_Straight.m_GoGroupMask;
            }
            
            // Now find all SecondaryLanes on this edge that are in the node's subLanes
            for (int i = 0; i < subLanes.Length; i++)
            {
                Entity subLane = subLanes[i].m_SubLane;
                
                // Only process SecondaryLanes (bicycle lanes)
                if (!job.m_ExtraTypeHandle.m_SecondaryLane.HasComponent(subLane))
                {
                    continue;
                }
                
                // Skip CarLanes (some lanes might have both components)
                if (job.m_CarLaneData.HasComponent(subLane))
                {
                    continue;
                }
                
                // Must have a LaneSignal component
                if (!job.m_LaneSignalData.TryGetComponent(subLane, out LaneSignal laneSignal))
                {
                    continue;
                }
                
                // Check if this subLane belongs to this edge by checking the Lane component
                if (job.m_ExtraTypeHandle.m_Lane.TryGetComponent(subLane, out Lane lane))
                {
                    // The lane's start or end node should match our node
                    bool belongsToEdge = false;
                    
                    // Check if this lane connects to/from this edge
                    // We need to check if the lane's geometry is near the edge position
                    if (job.m_CurveData.TryGetComponent(subLane, out Curve curve))
                    {
                        float3 laneStart = curve.m_Bezier.a;
                        float3 laneEnd = curve.m_Bezier.d;
                        
                        // Check if lane endpoints are near the edge position
                        float distStart = math.distance(laneStart, edgePosition);
                        float distEnd = math.distance(laneEnd, edgePosition);
                        
                        // If either endpoint is close to the edge position, this lane belongs to this edge
                        if (distStart < 50f || distEnd < 50f)
                        {
                            belongsToEdge = true;
                        }
                    }
                    
                    if (!belongsToEdge)
                    {
                        continue;
                    }
                }
                
                // Set the bicycle GroupMask
                laneSignal.m_GroupMask = bicycleGoMask;
                laneSignal.m_Default = 0;
                laneSignal.m_Flags |= LaneSignalFlags.CanExtend;
                
                ExtraLaneSignal extraLaneSignal = new ExtraLaneSignal();
                extraLaneSignal.m_YieldGroupMask = 0;
                extraLaneSignal.m_IgnorePriorityGroupMask = 0;
                
                Simulation.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
                job.m_LaneSignalData[subLane] = laneSignal;
                
                if (job.m_ExtraTypeHandle.m_ExtraLaneSignal.HasComponent(subLane))
                {
                    job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
                }
                else
                {
                    job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
                }
            }
        }
        // =====================================================================
        // END V132/V133: BICYCLE LANE SEPARATE SIGNAL CONTROL
        // =====================================================================

        // Set up pedestrian crossings at tracks
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity subLane = subLanes[i].m_SubLane;
            bool isPedestrian = job.m_PedestrianLaneData.TryGetComponent(subLane, out var pedestrianLane);
            if (!isPedestrian)
            {
                continue;
            }
            if ((pedestrianLane.m_Flags & (PedestrianLaneFlags.Crosswalk | PedestrianLaneFlags.Unsafe)) == (PedestrianLaneFlags.Crosswalk | PedestrianLaneFlags.Unsafe))
            {
                continue;
            }
            if (job.m_MasterLaneData.HasComponent(subLane))
            {
                continue;
            }
            var laneConnection = NodeUtils.GetLaneConnectionFromNodeSubLane(subLane, laneConnectionMap, true);
            var sourceEdge = laneConnection.m_SourceEdge == Entity.Null ? laneConnection.m_DestEdge : laneConnection.m_SourceEdge;
            var edgePosition = NodeUtils.GetEdgePosition(ref job, nodeEntity, sourceEdge);
            if (CustomPhaseUtils.TryGet(edgeGroupMasks, sourceEdge, edgePosition, out EdgeGroupMask groupMask) >= 0)
            {
                if ((groupMask.m_Options & EdgeGroupMask.Options.PerLaneSignal) != 0)
                {
                    continue;
                }
            }
            LaneSignal laneSignal = new LaneSignal();
            if (job.m_LaneSignalData.HasComponent(subLane))
            {
                laneSignal = job.m_LaneSignalData[subLane];
            }
            ExtraLaneSignal extraLaneSignal = new ExtraLaneSignal();
            laneSignal.m_GroupMask = ushort.MaxValue;
            laneSignal.m_Default = 0;
            if (job.m_Overlaps.HasBuffer(subLane))
            {
                bool hasCarLane = false;
                foreach (var overlap in job.m_Overlaps[subLane])
                {
                    if (job.m_CarLaneData.HasComponent(overlap.m_Other))
                    {
                        hasCarLane = true;
                        break;
                    }
                    if (!job.m_ExtraTypeHandle.m_TrackLane.HasComponent(overlap.m_Other))
                    {
                        continue;
                    }
                    if (job.m_LaneSignalData.TryGetComponent(overlap.m_Other, out var overlapSignal))
                    {
                        laneSignal.m_GroupMask &= (ushort)~overlapSignal.m_GroupMask;
                    }
                }
                if (hasCarLane)
                {
                    continue;
                }
            }

            Simulation.PatchedTrafficLightSystem.UpdateLaneSignal(trafficLights, ref laneSignal, ref extraLaneSignal);
            if (!job.m_LaneSignalData.HasComponent(subLane))
            {
                job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane, laneSignal);
            }
            if (!job.m_ExtraTypeHandle.m_ExtraLaneSignal.HasComponent(subLane))
            {
                job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
            }
            job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLane, laneSignal);
            job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLane, extraLaneSignal);
        }
        
        // V200: Dispose laneConnectionMap - Allocator.Temp, no longer needed
        laneConnectionMap.Dispose();
    }
}
