using Unity.Mathematics;

namespace C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Simulation
{
    public struct ExtraData
    {
        public uint m_Frame;

        public float4 m_TimeFactors;
        
        /// <summary>V141: Normalized game time (0.0-1.0 representing full day) for history sampling</summary>
        public float m_NormalizedTime;

        public ExtraData(PatchedTrafficLightSystem system)
        {
            float normalizedTime = system.m_TimeSystem.normalizedTime;
            float num = normalizedTime * 4f;
            float4 x = new float4(math.max(num - 3f, 1f - num), 1f - math.abs(num - new float3(1f, 2f, 3f)));
            x = math.saturate(x);
            m_TimeFactors = x;
            m_Frame = system.m_SimulationSystem.frameIndex;
            m_NormalizedTime = normalizedTime; // V141: Store for history sampling
        }
    }
}