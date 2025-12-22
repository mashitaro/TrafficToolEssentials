using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficToolEssentials.Components
{
    /// <summary>
    /// V151: Stores historical traffic flow data for chart visualization.
    /// 48 data points, sampled every 30 game minutes (24 game hours of history).
    /// Plus real-time VPH calculation based on 10 game-minute rolling average.
    /// 
    /// V182: Changed to REAL THROUGHPUT measurement instead of waiting vehicles.
    /// Now counts vehicles actually passing through the intersection (Node-SubLanes).
    /// Uses integral method: accumulate count over sample period, then calculate VPH.
    /// 
    /// RENAMED from TrafficFlowHistory to force clean schema (no migration issues).
    /// NOW PERSISTENT: Saves to savegame with stable schema versioning.
    /// </summary>
    public struct PersistentTrafficHistory : IComponentData, IQueryTypeParameter, ISerializable
    {
        // Schema version for future migrations
        // V3 = Full V148b structure with live VPH tracking
        // V4 = V182 integral throughput measurement
        private const ushort CURRENT_SCHEMA = 4;
        
        // Ring buffer: 48 data points (24h at 30min intervals)
        // Each value is vehicles per hour at that sample time
        public float m_H00, m_H01, m_H02, m_H03, m_H04, m_H05, m_H06, m_H07;
        public float m_H08, m_H09, m_H10, m_H11, m_H12, m_H13, m_H14, m_H15;
        public float m_H16, m_H17, m_H18, m_H19, m_H20, m_H21, m_H22, m_H23;
        public float m_H24, m_H25, m_H26, m_H27, m_H28, m_H29, m_H30, m_H31;
        public float m_H32, m_H33, m_H34, m_H35, m_H36, m_H37, m_H38, m_H39;
        public float m_H40, m_H41, m_H42, m_H43, m_H44, m_H45, m_H46, m_H47;
        
        /// <summary>Current write position in ring buffer (0-47)</summary>
        public byte m_WriteIndex;
        
        /// <summary>Number of valid samples (0-48)</summary>
        public byte m_Count;
        
        /// <summary>Game time (normalizedTime 0-1) when last 30-min sample was taken</summary>
        public float m_LastSampleTime;
        
        /// <summary>Frame when last sample was taken</summary>
        public uint m_LastSampleFrame;
        
        /// <summary>
        /// V182: Accumulated vehicle count for integral measurement.
        /// Sum of (vehicles on node) over all frames in current sample period.
        /// </summary>
        public float m_AccumulatedCount;
        
        // === V148b: Live VPH tracking (10 game-minute rolling average) ===
        
        /// <summary>V148b: Current real-time vehicles per hour (updated every frame via EMA)</summary>
        public float m_CurrentVPH;
        
        /// <summary>
        /// V182: EMA of vehicles currently IN the intersection (on Node-SubLanes).
        /// Used for smooth live VPH display. Updated with alpha=0.1.
        /// </summary>
        public float m_VehiclesInNodeEMA;
        
        /// <summary>V148b: Game time when short sample period started</summary>
        public float m_ShortSampleTime;
        
        /// <summary>
        /// V182: Frame count for integral measurement.
        /// Number of frames accumulated in current sample period.
        /// </summary>
        public int m_FrameCount;
        
        public const int HISTORY_SIZE = 48;
        public const float SAMPLE_INTERVAL = 0.0208333f; // 30 game minutes = 0.5h / 24h
        public const float SHORT_SAMPLE_INTERVAL = 0.00694f; // ~10 game minutes = 10/1440 of a day
        
        /// <summary>
        /// V182: Throughput multiplier - converts average vehicles-in-node to VPH.
        /// Based on average intersection crossing time of ~5 seconds.
        /// If 5 vehicles are in the intersection and each takes 5s, that's 3600 veh/hour.
        /// Formula: 3600 / avgCrossingTimeSeconds = multiplier
        /// </summary>
        public const float THROUGHPUT_MULTIPLIER = 720f; // 3600 / 5 seconds
        
        /// <summary>Add a new sample to the ring buffer</summary>
        public void AddSample(float vehiclesPerHour, float normalizedTime, uint frame)
        {
            SetValueAtIndex(m_WriteIndex, vehiclesPerHour);
            m_WriteIndex = (byte)((m_WriteIndex + 1) % HISTORY_SIZE);
            if (m_Count < HISTORY_SIZE) m_Count++;
            m_LastSampleTime = normalizedTime;
            m_LastSampleFrame = frame;
        }
        
        /// <summary>Check if enough time has passed for a new 30-min history sample</summary>
        public bool ShouldSample(float currentNormalizedTime)
        {
            if (m_LastSampleTime <= 0) return true; // First sample
            
            float timeDelta = currentNormalizedTime - m_LastSampleTime;
            if (timeDelta < 0) timeDelta += 1f; // Handle day wrap-around
            
            return timeDelta >= SAMPLE_INTERVAL;
        }
        
        /// <summary>V148b: Check if enough time has passed for a short sample (live VPH update)</summary>
        public bool ShouldUpdateLiveVPH(float currentNormalizedTime)
        {
            if (m_ShortSampleTime <= 0) return true; // First sample
            
            float timeDelta = currentNormalizedTime - m_ShortSampleTime;
            if (timeDelta < 0) timeDelta += 1f; // Handle day wrap-around
            
            return timeDelta >= SHORT_SAMPLE_INTERVAL;
        }
        
        // V182: UpdateLiveVPH removed - logic moved to CustomStateMachine.UpdateFlowHistory()
        // The new integral measurement doesn't need this helper method.
        
        /// <summary>Get value at specific index</summary>
        public float GetValueAtIndex(int index)
        {
            return index switch
            {
                0 => m_H00, 1 => m_H01, 2 => m_H02, 3 => m_H03,
                4 => m_H04, 5 => m_H05, 6 => m_H06, 7 => m_H07,
                8 => m_H08, 9 => m_H09, 10 => m_H10, 11 => m_H11,
                12 => m_H12, 13 => m_H13, 14 => m_H14, 15 => m_H15,
                16 => m_H16, 17 => m_H17, 18 => m_H18, 19 => m_H19,
                20 => m_H20, 21 => m_H21, 22 => m_H22, 23 => m_H23,
                24 => m_H24, 25 => m_H25, 26 => m_H26, 27 => m_H27,
                28 => m_H28, 29 => m_H29, 30 => m_H30, 31 => m_H31,
                32 => m_H32, 33 => m_H33, 34 => m_H34, 35 => m_H35,
                36 => m_H36, 37 => m_H37, 38 => m_H38, 39 => m_H39,
                40 => m_H40, 41 => m_H41, 42 => m_H42, 43 => m_H43,
                44 => m_H44, 45 => m_H45, 46 => m_H46, 47 => m_H47,
                _ => 0f
            };
        }
        
        /// <summary>Set value at specific index</summary>
        public void SetValueAtIndex(int index, float value)
        {
            switch (index)
            {
                case 0: m_H00 = value; break; case 1: m_H01 = value; break;
                case 2: m_H02 = value; break; case 3: m_H03 = value; break;
                case 4: m_H04 = value; break; case 5: m_H05 = value; break;
                case 6: m_H06 = value; break; case 7: m_H07 = value; break;
                case 8: m_H08 = value; break; case 9: m_H09 = value; break;
                case 10: m_H10 = value; break; case 11: m_H11 = value; break;
                case 12: m_H12 = value; break; case 13: m_H13 = value; break;
                case 14: m_H14 = value; break; case 15: m_H15 = value; break;
                case 16: m_H16 = value; break; case 17: m_H17 = value; break;
                case 18: m_H18 = value; break; case 19: m_H19 = value; break;
                case 20: m_H20 = value; break; case 21: m_H21 = value; break;
                case 22: m_H22 = value; break; case 23: m_H23 = value; break;
                case 24: m_H24 = value; break; case 25: m_H25 = value; break;
                case 26: m_H26 = value; break; case 27: m_H27 = value; break;
                case 28: m_H28 = value; break; case 29: m_H29 = value; break;
                case 30: m_H30 = value; break; case 31: m_H31 = value; break;
                case 32: m_H32 = value; break; case 33: m_H33 = value; break;
                case 34: m_H34 = value; break; case 35: m_H35 = value; break;
                case 36: m_H36 = value; break; case 37: m_H37 = value; break;
                case 38: m_H38 = value; break; case 39: m_H39 = value; break;
                case 40: m_H40 = value; break; case 41: m_H41 = value; break;
                case 42: m_H42 = value; break; case 43: m_H43 = value; break;
                case 44: m_H44 = value; break; case 45: m_H45 = value; break;
                case 46: m_H46 = value; break; case 47: m_H47 = value; break;
            }
        }
        
        /// <summary>Get all values in chronological order (oldest first) for UI display</summary>
        public float[] GetOrderedHistory()
        {
            if (m_Count == 0) return System.Array.Empty<float>();
            
            var result = new float[m_Count];
            int readIndex = (m_WriteIndex - m_Count + HISTORY_SIZE) % HISTORY_SIZE;
            
            for (int i = 0; i < m_Count; i++)
            {
                result[i] = GetValueAtIndex((readIndex + i) % HISTORY_SIZE);
            }
            
            return result;
        }
        
        /// <summary>V150: Serialize to savegame</summary>
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_SCHEMA);
            
            // Core tracking
            writer.Write(m_WriteIndex);
            writer.Write(m_Count);
            writer.Write(m_LastSampleTime);
            writer.Write(m_LastSampleFrame);
            writer.Write(m_AccumulatedCount);  // V182: was m_LastTotalDistance
            
            // V148b/V182: Live VPH tracking
            writer.Write(m_CurrentVPH);
            writer.Write(m_VehiclesInNodeEMA);  // V182: was m_ShortSampleDistance
            writer.Write(m_ShortSampleTime);
            writer.Write(m_FrameCount);  // V182: new field
            
            // All 48 history values
            writer.Write(m_H00); writer.Write(m_H01); writer.Write(m_H02); writer.Write(m_H03);
            writer.Write(m_H04); writer.Write(m_H05); writer.Write(m_H06); writer.Write(m_H07);
            writer.Write(m_H08); writer.Write(m_H09); writer.Write(m_H10); writer.Write(m_H11);
            writer.Write(m_H12); writer.Write(m_H13); writer.Write(m_H14); writer.Write(m_H15);
            writer.Write(m_H16); writer.Write(m_H17); writer.Write(m_H18); writer.Write(m_H19);
            writer.Write(m_H20); writer.Write(m_H21); writer.Write(m_H22); writer.Write(m_H23);
            writer.Write(m_H24); writer.Write(m_H25); writer.Write(m_H26); writer.Write(m_H27);
            writer.Write(m_H28); writer.Write(m_H29); writer.Write(m_H30); writer.Write(m_H31);
            writer.Write(m_H32); writer.Write(m_H33); writer.Write(m_H34); writer.Write(m_H35);
            writer.Write(m_H36); writer.Write(m_H37); writer.Write(m_H38); writer.Write(m_H39);
            writer.Write(m_H40); writer.Write(m_H41); writer.Write(m_H42); writer.Write(m_H43);
            writer.Write(m_H44); writer.Write(m_H45); writer.Write(m_H46); writer.Write(m_H47);
        }
        
        /// <summary>V150: Deserialize from savegame with schema migration</summary>
        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out ushort schema);
            
            if (schema != CURRENT_SCHEMA)
            {
                // Unknown or incompatible schema - reset to clean state
                // This handles: old saves (schema 3), corrupted data, future downgrades
                // V182: Schema 3 -> 4 migration: just reset, new measurement system anyway
                ResetToCleanState();
                return;
            }
            
            // Schema 4: V182 integral throughput structure
            reader.Read(out m_WriteIndex);
            reader.Read(out m_Count);
            reader.Read(out m_LastSampleTime);
            reader.Read(out m_LastSampleFrame);
            reader.Read(out m_AccumulatedCount);
            
            // V182: Live VPH tracking
            reader.Read(out m_CurrentVPH);
            reader.Read(out m_VehiclesInNodeEMA);
            reader.Read(out m_ShortSampleTime);
            reader.Read(out m_FrameCount);
            
            // All 48 history values
            reader.Read(out m_H00); reader.Read(out m_H01); reader.Read(out m_H02); reader.Read(out m_H03);
            reader.Read(out m_H04); reader.Read(out m_H05); reader.Read(out m_H06); reader.Read(out m_H07);
            reader.Read(out m_H08); reader.Read(out m_H09); reader.Read(out m_H10); reader.Read(out m_H11);
            reader.Read(out m_H12); reader.Read(out m_H13); reader.Read(out m_H14); reader.Read(out m_H15);
            reader.Read(out m_H16); reader.Read(out m_H17); reader.Read(out m_H18); reader.Read(out m_H19);
            reader.Read(out m_H20); reader.Read(out m_H21); reader.Read(out m_H22); reader.Read(out m_H23);
            reader.Read(out m_H24); reader.Read(out m_H25); reader.Read(out m_H26); reader.Read(out m_H27);
            reader.Read(out m_H28); reader.Read(out m_H29); reader.Read(out m_H30); reader.Read(out m_H31);
            reader.Read(out m_H32); reader.Read(out m_H33); reader.Read(out m_H34); reader.Read(out m_H35);
            reader.Read(out m_H36); reader.Read(out m_H37); reader.Read(out m_H38); reader.Read(out m_H39);
            reader.Read(out m_H40); reader.Read(out m_H41); reader.Read(out m_H42); reader.Read(out m_H43);
            reader.Read(out m_H44); reader.Read(out m_H45); reader.Read(out m_H46); reader.Read(out m_H47);
            
            // Sanity check: clamp values to valid ranges
            if (m_WriteIndex >= HISTORY_SIZE) m_WriteIndex = 0;
            if (m_Count > HISTORY_SIZE) m_Count = HISTORY_SIZE;
            if (m_FrameCount < 0) m_FrameCount = 0;
        }
        
        /// <summary>Reset all fields to clean initial state</summary>
        private void ResetToCleanState()
        {
            m_WriteIndex = 0;
            m_Count = 0;
            m_LastSampleTime = 0;
            m_LastSampleFrame = 0;
            m_AccumulatedCount = 0;  // V182
            m_CurrentVPH = 0;
            m_VehiclesInNodeEMA = 0;  // V182
            m_ShortSampleTime = 0;
            m_FrameCount = 0;  // V182
            
            m_H00 = m_H01 = m_H02 = m_H03 = m_H04 = m_H05 = m_H06 = m_H07 = 0;
            m_H08 = m_H09 = m_H10 = m_H11 = m_H12 = m_H13 = m_H14 = m_H15 = 0;
            m_H16 = m_H17 = m_H18 = m_H19 = m_H20 = m_H21 = m_H22 = m_H23 = 0;
            m_H24 = m_H25 = m_H26 = m_H27 = m_H28 = m_H29 = m_H30 = m_H31 = 0;
            m_H32 = m_H33 = m_H34 = m_H35 = m_H36 = m_H37 = m_H38 = m_H39 = 0;
            m_H40 = m_H41 = m_H42 = m_H43 = m_H44 = m_H45 = m_H46 = m_H47 = 0;
        }
    }
}
