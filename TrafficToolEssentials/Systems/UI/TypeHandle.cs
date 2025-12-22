using System.Runtime.CompilerServices;
using C2VM.TrafficToolEssentials.Components;
using Game.Net;
using Game.Objects;  // V179: For LaneObject
using Game.Vehicles; // V181: For Controller (trailer detection)
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficToolEssentials.Systems.UI;

public struct TypeHandle
{
    [ReadOnly]
    public BufferLookup<SubLane> m_SubLane;

    [ReadOnly]
    public BufferLookup<ConnectedEdge> m_ConnectedEdge;

    [ReadOnly]
    public BufferLookup<EdgeGroupMask> m_EdgeGroupMask;

    [ReadOnly]
    public BufferLookup<SubLaneGroupMask> m_SubLaneGroupMask;

    [ReadOnly]
    public BufferLookup<LaneOverlap> m_LaneOverlap;
    
    // V179: For counting waiting vehicles on incoming lanes
    [ReadOnly]
    public BufferLookup<LaneObject> m_LaneObject;
    
    // V181: For filtering out trailers (they have a Controller component pointing to the truck)
    [ReadOnly]
    public ComponentLookup<Controller> m_Controller;

    [ReadOnly]
    public ComponentLookup<Edge> m_Edge;

    [ReadOnly]
    public ComponentLookup<EdgeGeometry> m_EdgeGeometry;

    [ReadOnly]
    public ComponentLookup<Lane> m_Lane;

    [ReadOnly]
    public ComponentLookup<PedestrianLane> m_PedestrianLane;

    [ReadOnly]
    public ComponentLookup<MasterLane> m_MasterLane;

    [ReadOnly]
    public ComponentLookup<TrackLane> m_TrackLane;

    [ReadOnly]
    public ComponentLookup<CarLane> m_CarLane;

    [ReadOnly]
    public ComponentLookup<SecondaryLane> m_SecondaryLane;

    [ReadOnly]
    public ComponentLookup<Curve> m_Curve;

    [ReadOnly]
    public ComponentLookup<TrainTrack> m_TrainTrack;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignHandles(ref SystemState state)
    {
        m_SubLane = state.GetBufferLookup<SubLane>(true);
        m_ConnectedEdge = state.GetBufferLookup<ConnectedEdge>(true);
        m_EdgeGroupMask = state.GetBufferLookup<EdgeGroupMask>(true);
        m_SubLaneGroupMask = state.GetBufferLookup<SubLaneGroupMask>(true);
        m_LaneOverlap = state.GetBufferLookup<LaneOverlap>(true);
        m_LaneObject = state.GetBufferLookup<LaneObject>(true);  // V179
        m_Controller = state.GetComponentLookup<Controller>(true);  // V181
        m_Edge = state.GetComponentLookup<Edge>(true);
        m_EdgeGeometry = state.GetComponentLookup<EdgeGeometry>(true);
        m_Lane = state.GetComponentLookup<Lane>(true);
        m_PedestrianLane = state.GetComponentLookup<PedestrianLane>(true);
        m_MasterLane = state.GetComponentLookup<MasterLane>(true);
        m_TrackLane = state.GetComponentLookup<TrackLane>(true);
        m_CarLane = state.GetComponentLookup<CarLane>(true);
        m_SecondaryLane = state.GetComponentLookup<SecondaryLane>(true);
        m_Curve = state.GetComponentLookup<Curve>(true);
        m_TrainTrack = state.GetComponentLookup<TrainTrack>(true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(SystemBase system)
    {
        m_SubLane.Update(system);
        m_ConnectedEdge.Update(system);
        m_EdgeGroupMask.Update(system);
        m_SubLaneGroupMask.Update(system);
        m_LaneOverlap.Update(system);
        m_LaneObject.Update(system);  // V179
        m_Controller.Update(system);  // V181
        m_Edge.Update(system);
        m_EdgeGeometry.Update(system);
        m_Lane.Update(system);
        m_PedestrianLane.Update(system);
        m_MasterLane.Update(system);
        m_TrackLane.Update(system);
        m_CarLane.Update(system);
        m_SecondaryLane.Update(system);
        m_Curve.Update(system);
        m_TrainTrack.Update(system);
    }
}