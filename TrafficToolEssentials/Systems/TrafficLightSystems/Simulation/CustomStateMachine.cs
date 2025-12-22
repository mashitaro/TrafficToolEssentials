using C2VM.TrafficToolEssentials.Components;
using Game.Net;
using Unity.Entities;
using Unity.Mathematics;

namespace C2VM.TrafficToolEssentials.Systems.TrafficLightSystems.Simulation
{
    public struct CustomStateMachine
    {
        public static bool UpdateTrafficLightState(ref TrafficLights trafficLights, ref CustomTrafficLights customTrafficLights, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            if (trafficLights.m_State == TrafficLightState.None || trafficLights.m_State == TrafficLightState.Extending || trafficLights.m_State == TrafficLightState.Extended)
            {
                trafficLights.m_State = TrafficLightState.Beginning;
                trafficLights.m_CurrentSignalGroup = 0;
                trafficLights.m_NextSignalGroup = GetNextSignalGroup(trafficLights.m_CurrentSignalGroup, customPhaseDataBuffer, customTrafficLights, out _);
                trafficLights.m_Timer = 0;
                customTrafficLights.m_Timer = 0;
                return true;
            }
            else if (trafficLights.m_State == TrafficLightState.Beginning)
            {
                if (trafficLights.m_NextSignalGroup <= 0)
                {
                    trafficLights.m_State = TrafficLightState.None; // roll a new group
                    return true;
                }
                trafficLights.m_State = TrafficLightState.Ongoing;
                trafficLights.m_CurrentSignalGroup = trafficLights.m_NextSignalGroup;
                trafficLights.m_NextSignalGroup = 0;
                trafficLights.m_Timer = 0;
                customTrafficLights.m_Timer = 0;
                for (int i = 0; i < customPhaseDataBuffer.Length; i++)
                {
                    CustomPhaseData phase = customPhaseDataBuffer[i];
                    if (trafficLights.m_CurrentSignalGroup == i + 1)
                    {
                        phase.m_TurnsSinceLastRun = 0;
                        phase.m_LowFlowTimer = 0;
                        phase.m_LowPriorityTimer = 0;
                    }
                    else
                    {
                        phase.m_TurnsSinceLastRun++;
                    }
                    phase.m_Options &= ~CustomPhaseData.Options.EndPhasePrematurely;
                    customPhaseDataBuffer[i] = phase;
                }
                return true;
            }
            else if (trafficLights.m_State == TrafficLightState.Ongoing)
            {
                int currentSignalIndex = trafficLights.m_CurrentSignalGroup - 1;
                if (currentSignalIndex < 0 || currentSignalIndex >= customPhaseDataBuffer.Length)
                {
                    trafficLights.m_State = TrafficLightState.None; // roll a new group
                    return true;
                }
                customTrafficLights.m_Timer++;
                CustomPhaseData phase = customPhaseDataBuffer[currentSignalIndex];
                
                // V113: Bei Green Wave den TargetDurationMultiplier neutralisieren
                // User-Einstellung wird ignoriert damit Sync-Timing vorhersagbar bleibt
                float effectiveTargetDurationMultiplier = customTrafficLights.m_UseSyncedCycle ? 1.0f : phase.m_TargetDurationMultiplier;
                float targetDuration = 10f * (phase.AverageCarFlow() + (float)(phase.m_TrackLaneOccupied * 0.5)) * effectiveTargetDurationMultiplier;
                bool preferChange = false;
                phase.m_TargetDuration = targetDuration;
                if (customTrafficLights.m_Timer <= phase.m_MinimumDuration)
                {
                    phase.m_LowFlowTimer = 0;
                    phase.m_LowPriorityTimer = 0;
                }
                else if (phase.m_Priority > 0 && phase.m_Priority >= MaxPriority(customPhaseDataBuffer))
                {
                    if (customTrafficLights.m_Timer >= phase.m_MaximumDuration)
                    {
                        preferChange = true;
                    }
                    else if (customTrafficLights.m_Timer <= targetDuration)
                    {
                        phase.m_LowFlowTimer = 0;
                    }
                    else if (phase.m_LowFlowTimer < 3)
                    {
                        phase.m_LowFlowTimer++;
                    }
                    else
                    {
                        preferChange = true;
                    }
                    phase.m_LowPriorityTimer = 0;
                }
                else if (phase.m_Priority < MaxPriority(customPhaseDataBuffer))
                {
                    if (phase.m_LowPriorityTimer >= 1)
                    {
                        preferChange = true;
                    }
                    phase.m_LowPriorityTimer++;
                }
                else
                {
                    preferChange = true;
                }
                if ((phase.m_Options & CustomPhaseData.Options.EndPhasePrematurely) != 0)
                {
                    preferChange = true;
                }
                if (customTrafficLights.m_ManualSignalGroup > 0 && customTrafficLights.m_ManualSignalGroup != trafficLights.m_CurrentSignalGroup)
                {
                    preferChange = true;
                }
                customPhaseDataBuffer[currentSignalIndex] = phase;
                byte nextGroup = GetNextSignalGroup(trafficLights.m_CurrentSignalGroup, customPhaseDataBuffer, customTrafficLights, out var linked);
                if (preferChange && nextGroup != trafficLights.m_CurrentSignalGroup)
                {
                    trafficLights.m_State = TrafficLightState.Ending;
                    trafficLights.m_NextSignalGroup = nextGroup;
                    if (linked)
                    {
                        for (int i = trafficLights.m_CurrentSignalGroup; i < trafficLights.m_NextSignalGroup - 1; i++)
                        {
                            CustomPhaseData nextPhase = customPhaseDataBuffer[i];
                            if (nextPhase.m_Priority <= 0)
                            {
                                nextPhase.m_TurnsSinceLastRun = 0;
                                customPhaseDataBuffer[i] = nextPhase;
                            }
                        }
                    }
                    return true;
                }
                return false;
            }
            else if (trafficLights.m_State == TrafficLightState.Ending)
            {
                trafficLights.m_State = TrafficLightState.Changing;
                return true;
            }
            else if (trafficLights.m_State == TrafficLightState.Changing)
            {
                trafficLights.m_State = TrafficLightState.Beginning;
                return true;
            }
            return false;
        }

        public static void CalculateFlow(PatchedTrafficLightSystem.UpdateTrafficLightsJob job, int unfilteredChunkIndex, DynamicBuffer<SubLane> subLaneBuffer, TrafficLights trafficLights, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            float4 timeFactors = job.m_ExtraData.m_TimeFactors * 0.125f;
            for (int i = 0; i < customPhaseDataBuffer.Length; i++)
            {
                CustomPhaseData customPhaseData = customPhaseDataBuffer[i];
                customPhaseData.m_CarFlow.z = customPhaseData.m_CarFlow.y;
                customPhaseData.m_CarFlow.y = customPhaseData.m_CarFlow.x;
                customPhaseData.m_CarFlow.x = 0f;
                customPhaseDataBuffer[i] = customPhaseData;
            }
            foreach (var subLane in subLaneBuffer)
            {
                Entity subLaneEntity = subLane.m_SubLane;
                float4 newDistance = 0f;
                float4 newDuration = 0f;
                float4 oldDistance = 0f;
                float4 oldDuration = 0f;
                float4 diffDistance = 0f;
                float4 diffDuration = 0f;
                uint newFrame = job.m_ExtraData.m_Frame;
                uint oldFrame = 0;
                uint diffFrame = 0;

                if (!job.m_LaneSignalData.TryGetComponent(subLaneEntity, out var laneSignal))
                {
                    continue;
                }
                if (!job.m_ExtraTypeHandle.m_LaneFlow.TryGetComponent(subLaneEntity, out var laneFlow))
                {
                    continue;
                }
                if ((laneSignal.m_GroupMask & (1 << trafficLights.m_CurrentSignalGroup - 1)) == 0)
                {
                    continue;
                }

                newDistance = math.lerp(laneFlow.m_Distance, laneFlow.m_Next.y, timeFactors);
                newDuration = math.lerp(laneFlow.m_Duration, laneFlow.m_Next.x, timeFactors);

                LaneFlowHistory laneFlowHistory = new LaneFlowHistory();
                if (job.m_ExtraTypeHandle.m_LaneFlowHistory.TryGetComponent(subLaneEntity, out laneFlowHistory))
                {
                    oldDistance = laneFlowHistory.m_Distance;
                    oldDuration = laneFlowHistory.m_Duration;
                    oldFrame = laneFlowHistory.m_Frame;
                }
                else
                {
                    job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLaneEntity, laneFlowHistory);
                }

                diffDistance = newDistance - oldDistance;
                diffDuration = newDuration - oldDuration;
                diffFrame = newFrame - oldFrame;

                laneFlowHistory.m_Distance = newDistance;
                laneFlowHistory.m_Duration = newDuration;
                laneFlowHistory.m_Frame = newFrame;

                job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLaneEntity, laneFlowHistory);

                int group = trafficLights.m_CurrentSignalGroup - 1;
                if (group < customPhaseDataBuffer.Length && diffFrame > 0)
                {
                    CustomPhaseData customPhaseData = customPhaseDataBuffer[group];
                    float totalDiff = math.abs(Max(diffDistance)) + math.abs(Max(diffDuration));
                    customPhaseData.m_CarFlow.x += totalDiff * (64f / (float)diffFrame); // 64 frames per traffic light tick
                    customPhaseDataBuffer[group] = customPhaseData;
                }
            }
        }

        public static void CalculatePriority(PatchedTrafficLightSystem.UpdateTrafficLightsJob job, DynamicBuffer<SubLane> subLaneBuffer, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            for (int i = 0; i < customPhaseDataBuffer.Length; i++)
            {
                CustomPhaseData customPhaseData = customPhaseDataBuffer[i];
                customPhaseData.m_CarLaneOccupied = 0;
                customPhaseData.m_PublicCarLaneOccupied = 0;
                customPhaseData.m_TrackLaneOccupied = 0;
                customPhaseData.m_PedestrianLaneOccupied = 0;
                customPhaseData.m_BicycleLaneOccupied = 0; // V126: Bicycle Support
                customPhaseData.m_Priority = 0;
                customPhaseDataBuffer[i] = customPhaseData;
            }
            foreach (var subLane in subLaneBuffer)
            {
                Entity subLaneEntity = subLane.m_SubLane;

                if (!job.m_LaneSignalData.TryGetComponent(subLaneEntity, out var laneSignal))
                {
                    continue;
                }

                Entity lanePetitioner = laneSignal.m_Petitioner;
                int lanePriority = laneSignal.m_Priority;

                laneSignal.m_Petitioner = Entity.Null;
                laneSignal.m_Priority = laneSignal.m_Default;
                job.m_LaneSignalData[subLaneEntity] = laneSignal;

                if (job.m_ExtraTypeHandle.m_MasterLane.HasComponent(subLaneEntity))
                {
                    continue;
                }
                if (lanePetitioner == Entity.Null)
                {
                    continue;
                }

                for (int i = 0; i < customPhaseDataBuffer.Length; i++)
                {
                    if ((laneSignal.m_GroupMask & (1 << i)) == 0)
                    {
                        continue;
                    }

                    CustomPhaseData customPhaseData = customPhaseDataBuffer[i];

                    if (job.m_ExtraTypeHandle.m_CarLane.HasComponent(subLaneEntity))
                    {
                        customPhaseData.m_CarLaneOccupied++;
                        if (job.m_ExtraTypeHandle.m_ExtraLaneSignal.TryGetComponent(subLaneEntity, out var extraLaneSignal))
                        {
                            if (extraLaneSignal.m_SourceSubLane != Entity.Null && job.m_ExtraTypeHandle.m_CarLane.TryGetComponent(extraLaneSignal.m_SourceSubLane, out var sourceCarLane))
                            {
                                if ((sourceCarLane.m_Flags & CarLaneFlags.PublicOnly) != 0)
                                {
                                    customPhaseData.m_PublicCarLaneOccupied++;
                                    if ((customPhaseData.m_Options & CustomPhaseData.Options.PrioritisePublicCar) != 0)
                                    {
                                        lanePriority = math.max(lanePriority, 104); // 104 is the priority for trams
                                    }
                                    else
                                    {
                                        lanePriority = math.min(lanePriority, 100); // 100 is the default priority
                                    }
                                }
                            }
                        }
                    }
                    if (job.m_ExtraTypeHandle.m_TrackLane.HasComponent(subLaneEntity))
                    {
                        customPhaseData.m_TrackLaneOccupied++;
                        if ((customPhaseData.m_Options & CustomPhaseData.Options.PrioritiseTrack) == 0)
                        {
                            // Do not lower priority for trains, as they do not stop for signals
                            // 110 is the priority for trains
                            if (lanePriority < 110)
                            {
                                lanePriority = math.min(lanePriority, 100); // 100 is the default priority
                            }
                        }
                    }
                    if (job.m_ExtraTypeHandle.m_PedestrianLane.TryGetComponent(subLaneEntity, out var pedestrianLane))
                    {
                        if ((pedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) != 0)
                        {
                            customPhaseData.m_PedestrianLaneOccupied++;
                            if ((customPhaseData.m_Options & CustomPhaseData.Options.PrioritisePedestrian) != 0)
                            {
                                lanePriority = math.max(lanePriority, 104); // 104 is the priority for trams
                            }
                        }
                    }
                    // V126: Bicycle Lane Support - SecondaryLane = Bicycle Lane
                    if (job.m_ExtraTypeHandle.m_SecondaryLane.HasComponent(subLaneEntity))
                    {
                        customPhaseData.m_BicycleLaneOccupied++;
                        if ((customPhaseData.m_Options & CustomPhaseData.Options.PrioritiseBicycle) != 0)
                        {
                            lanePriority = math.max(lanePriority, 104); // 104 is the priority for trams/buses
                        }
                    }

                    customPhaseData.m_Priority = math.max(customPhaseData.m_Priority, lanePriority);

                    customPhaseDataBuffer[i] = customPhaseData;
                }
            }
        }

        public static byte GetNextSignalGroup(byte currentGroup, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer, CustomTrafficLights customTrafficLights, out bool linked)
        {
            linked = false;
            
            // Manual signal group override - highest priority
            if (customTrafficLights.m_ManualSignalGroup > 0 && customTrafficLights.m_ManualSignalGroup - 1 < customPhaseDataBuffer.Length)
            {
                return customTrafficLights.m_ManualSignalGroup;
            }
            
            // ========================================
            // SEQUENTIAL MODE (for Green Wave sync)
            // ========================================
            // When enabled, phases progress in strict order: 1→2→3→4→1
            // This is REQUIRED for Green Wave synchronization to work correctly!
            // Also respects AvoidSignalGroup to prevent entering sync phase too early.
            if (customTrafficLights.m_UseSequentialPhases)
            {
                int phaseCount = customPhaseDataBuffer.Length;
                if (phaseCount == 0) return 1;
                
                // Simple sequential: next = current + 1 (wrap around)
                byte nextSequential = (byte)((currentGroup % phaseCount) + 1);
                
                // Check if we should avoid this phase (Green Wave holding pattern)
                if (customTrafficLights.m_AvoidSignalGroup > 0 && nextSequential == customTrafficLights.m_AvoidSignalGroup)
                {
                    // Stay at current phase - don't advance to avoided phase
                    return currentGroup;
                }
                
                return nextSequential;
            }
            
            // ========================================
            // LEGACY MODE (priority-based selection)
            // ========================================
            // Original behavior: select next phase based on priority and waiting traffic
            byte nextGroup = 0;
            int maxPriority = -1;
            float maxWaiting = -1;
            
            // Track second-best option in case we need to avoid the best one
            byte secondBestGroup = 0;
            int secondMaxPriority = -1;
            float secondMaxWaiting = -1;
            
            for (int i = 0; i < customPhaseDataBuffer.Length; i++)
            {
                CustomPhaseData phase = customPhaseDataBuffer[i];
                
                // V113: Bei Green Wave die Multiplikatoren neutralisieren
                // User-Einstellungen werden ignoriert damit Sync-Timing vorhersagbar bleibt
                float effectiveLaneOccupiedMultiplier = customTrafficLights.m_UseSyncedCycle ? 1.0f : phase.m_LaneOccupiedMultiplier;
                float effectiveIntervalExponent = customTrafficLights.m_UseSyncedCycle ? 2.0f : phase.m_IntervalExponent;
                
                float weightedWaiting = ((float)phase.TotalLaneOccupied()) * effectiveLaneOccupiedMultiplier * math.pow((float)phase.m_TurnsSinceLastRun / (float)customPhaseDataBuffer.Length, effectiveIntervalExponent);
                if (phase.m_Priority > maxPriority)
                {
                    // Current best becomes second best
                    secondBestGroup = nextGroup;
                    secondMaxPriority = maxPriority;
                    secondMaxWaiting = maxWaiting;
                    // New best
                    nextGroup = (byte)(i + 1);
                    maxPriority = phase.m_Priority;
                    maxWaiting = weightedWaiting;
                }
                else if (phase.m_Priority == maxPriority && weightedWaiting > maxWaiting)
                {
                    // Current best becomes second best
                    secondBestGroup = nextGroup;
                    secondMaxPriority = maxPriority;
                    secondMaxWaiting = maxWaiting;
                    // New best
                    nextGroup = (byte)(i + 1);
                    maxWaiting = weightedWaiting;
                }
                else if (phase.m_Priority > secondMaxPriority || 
                         (phase.m_Priority == secondMaxPriority && weightedWaiting > secondMaxWaiting))
                {
                    // Update second best
                    secondBestGroup = (byte)(i + 1);
                    secondMaxPriority = phase.m_Priority;
                    secondMaxWaiting = weightedWaiting;
                }
                phase.m_WeightedWaiting = weightedWaiting;
                customPhaseDataBuffer[i] = phase;
            }

            int linkedPriority = -1;
            byte linkedNextGroup = 0;
            for (int i = currentGroup - 1; i >= 0 && i < customPhaseDataBuffer.Length - 1; i++)
            {
                CustomPhaseData phase = customPhaseDataBuffer[i];
                if ((phase.m_Options & CustomPhaseData.Options.LinkedWithNextPhase) == 0)
                {
                    break;
                }

                CustomPhaseData nextPhase = customPhaseDataBuffer[i + 1];
                if (linkedNextGroup == 0 && nextPhase.m_Priority > 0)
                {
                    linkedNextGroup = (byte)(i + 2);
                }
                linkedPriority = math.max(linkedPriority, nextPhase.m_Priority);
            }
            if (linkedNextGroup > 0 && linkedPriority >= maxPriority)
            {
                linked = true;
                // Check avoid logic for linked group too
                if (customTrafficLights.m_AvoidSignalGroup > 0 && linkedNextGroup == customTrafficLights.m_AvoidSignalGroup)
                {
                    // Stay at current group instead of going to avoided linked group
                    return currentGroup;
                }
                return linkedNextGroup;
            }

            for (int i = nextGroup - 2; i >= 0; i--)
            {
                CustomPhaseData phase = customPhaseDataBuffer[i];
                if ((phase.m_Options & CustomPhaseData.Options.LinkedWithNextPhase) == 0)
                {
                    break;
                }
                if (phase.m_Priority > 0)
                {
                    nextGroup = (byte)(i + 1);
                }
            }
            
            // === AVOID SIGNAL GROUP LOGIC ===
            // If the normal logic wants to go to the avoided group, use second best instead
            // If no second best, stay at current group
            if (customTrafficLights.m_AvoidSignalGroup > 0 && nextGroup == customTrafficLights.m_AvoidSignalGroup)
            {
                if (secondBestGroup > 0 && secondBestGroup != customTrafficLights.m_AvoidSignalGroup)
                {
                    return secondBestGroup;
                }
                // No valid alternative - stay at current group
                return currentGroup;
            }
            
            return nextGroup;
        }

        private static int MaxPriority(DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            int max = int.MinValue;
            foreach (var phase in customPhaseDataBuffer)
            {
                max = math.max(max, phase.m_Priority);
            }
            return max;
        }

        private static float Max(float4 f)
        {
            return math.max(f.w, math.max(f.x, math.max(f.y, f.z)));
        }
        
        /// <summary>
        /// V182: REAL THROUGHPUT MEASUREMENT
        /// 
        /// Instead of counting waiting vehicles, we count vehicles PASSING THROUGH the intersection.
        /// The subLaneBuffer contains Node-SubLanes = short connecting pieces INSIDE the intersection.
        /// Vehicles on these lanes are actively crossing, not waiting!
        /// 
        /// Method: Integral measurement
        /// - Every frame: Accumulate count of vehicles on Node-SubLanes
        /// - Every 30 game minutes: Calculate average, convert to VPH, store sample
        /// 
        /// Formula: avgVehiclesInNode × THROUGHPUT_MULTIPLIER = VPH
        /// If average 5 vehicles are crossing and each takes ~5 seconds, that's 3600 veh/hour.
        /// </summary>
        public static void UpdateFlowHistory(
            PatchedTrafficLightSystem.UpdateTrafficLightsJob job, 
            int unfilteredChunkIndex,
            Entity nodeEntity,
            DynamicBuffer<SubLane> subLaneBuffer)
        {
            // V182: Count vehicles INSIDE the intersection (on Node-SubLanes)
            // These are vehicles actively crossing, not waiting
            int vehiclesInNode = 0;
            
            foreach (var subLane in subLaneBuffer)
            {
                Entity subLaneEntity = subLane.m_SubLane;
                
                // Skip non-car lanes (pedestrians, tracks, etc.)
                if (!job.m_ExtraTypeHandle.m_CarLane.HasComponent(subLaneEntity))
                    continue;
                
                // Skip master lanes (they're just containers)
                if (job.m_ExtraTypeHandle.m_MasterLane.HasComponent(subLaneEntity))
                    continue;
                
                // Count vehicles on this node sublane (= crossing through intersection)
                if (job.m_LaneObjects.TryGetBuffer(subLaneEntity, out var laneObjects))
                {
                    vehiclesInNode += laneObjects.Length;
                }
            }
            
            // V182: Get or create PersistentTrafficHistory component
            PersistentTrafficHistory history;
            bool hasHistory = job.m_ExtraTypeHandle.m_PersistentTrafficHistory.TryGetComponent(nodeEntity, out history);
            
            if (!hasHistory)
            {
                // Create new history component
                history = new PersistentTrafficHistory();
                history.m_LastSampleTime = job.m_ExtraData.m_NormalizedTime;
                history.m_ShortSampleTime = job.m_ExtraData.m_NormalizedTime;
                history.m_VehiclesInNodeEMA = vehiclesInNode;
                history.m_AccumulatedCount = vehiclesInNode;
                history.m_FrameCount = 1;
                history.m_CurrentVPH = vehiclesInNode * PersistentTrafficHistory.THROUGHPUT_MULTIPLIER;
                
                job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, nodeEntity, history);
                return;
            }
            
            // V182: Update EMA for smooth live display (alpha = 0.1 for stability)
            const float EMA_ALPHA = 0.1f;
            history.m_VehiclesInNodeEMA = EMA_ALPHA * vehiclesInNode + (1f - EMA_ALPHA) * history.m_VehiclesInNodeEMA;
            
            // V182: Live VPH from EMA
            history.m_CurrentVPH = history.m_VehiclesInNodeEMA * PersistentTrafficHistory.THROUGHPUT_MULTIPLIER;
            
            // Sanity bounds
            if (history.m_CurrentVPH < 0) history.m_CurrentVPH = 0;
            if (history.m_CurrentVPH > 10000) history.m_CurrentVPH = 10000;
            
            // V182: Accumulate for integral measurement
            history.m_AccumulatedCount += vehiclesInNode;
            history.m_FrameCount++;
            
            // V182: Add history sample every 30 game minutes
            if (history.ShouldSample(job.m_ExtraData.m_NormalizedTime))
            {
                // Calculate average vehicles in node over the sample period
                float avgVehiclesInNode = history.m_FrameCount > 0 
                    ? history.m_AccumulatedCount / history.m_FrameCount 
                    : 0;
                
                // Convert to VPH and store
                float sampleVPH = avgVehiclesInNode * PersistentTrafficHistory.THROUGHPUT_MULTIPLIER;
                
                // Sanity bounds for sample
                if (sampleVPH < 0) sampleVPH = 0;
                if (sampleVPH > 10000) sampleVPH = 10000;
                
                history.AddSample(sampleVPH, job.m_ExtraData.m_NormalizedTime, job.m_ExtraData.m_Frame);
                
                // Reset accumulators for next period
                history.m_AccumulatedCount = 0;
                history.m_FrameCount = 0;
            }
            
            // Always update
            job.m_ExtraTypeHandle.m_PersistentTrafficHistory[nodeEntity] = history;
        }
    }
}