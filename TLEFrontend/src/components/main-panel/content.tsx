import { useContext, useEffect, useState } from 'react';
// V254: COHTML-SAFE - removed createPortal, keyframes, animations, useRef
import styled from 'styled-components';

import { LocaleContext } from '@/context';
import { getString } from '@/localisations';
import { engineCall } from '@/engine';

import Title from './items/title';
import Message from './items/message';
import Divider from './items/divider';
import Range from './items/range';
import Row from './items/row';
import Notification from './items/notification';

import Button from '@/components/common/button';
import Checkbox from '@/components/common/checkbox';
import Radio from '@/components/common/radio';
import Dropdown from '@/components/common/dropdown';
import Scrollable from '@/components/common/scrollable';
import Tooltip from '@/components/common/tooltip';

// V258: Beautiful panel background restored - backdrop-filter is safe (Dashboard uses it)
// overflow: visible allows tooltips to escape the container bounds
const Container = styled.div`
  width: 18em;
  background-color: var(--panelColorNormal);
  backdrop-filter: var(--panelBlur);
  color: var(--textColor);
  flex: 1;
  position: relative;
  padding: 0.35em;
  overflow: visible;
  border-radius: 0 0 4rem 4rem;
`;

const Label = styled.span`
  color: var(--textColorDim);
  display: flex;
  flex: 1;
`;

// Green Wave Button - Active state (Eigene Phase selected) - Werder Bremen Green!
// V255: Improved visibility - stronger colors, thicker border
const GreenWaveButton = styled.div`
  padding: 10rem 14rem;
  border-radius: 6rem;
  color: rgba(255, 255, 255, 1);
  background: rgba(30, 144, 83, 0.85);
  border: 2rem solid rgba(60, 200, 120, 0.9);
  width: 100%;
  text-align: center;
  font-weight: 600;
  font-size: 14rem;
  cursor: pointer;
  letter-spacing: 0.5rem;
  
  &:hover {
    background: rgba(40, 170, 95, 0.95);
    border-color: rgba(100, 230, 150, 1);
  }
  
  &:active {
    background: rgba(25, 120, 70, 0.9);
  }
`;

// Green Wave Display - Fake disabled look (just visual, no interaction)
// V255: Clearer disabled state
const GreenWaveDisplay = styled.div`
  padding: 10rem 14rem;
  border-radius: 6rem;
  color: rgba(255, 255, 255, 0.45);
  background: rgba(50, 60, 70, 0.7);
  border: 2rem solid rgba(100, 110, 120, 0.4);
  width: 100%;
  text-align: center;
  font-weight: 500;
  font-size: 14rem;
`;

// V138: Traffic Summary Box - full width, improved styling
// V255: Enhanced visibility - stronger background, clearer border
const TrafficSummaryBox = styled.div`
  display: flex;
  flex-direction: column;
  gap: 8rem;
  padding: 14rem 16rem;
  margin: 8rem 0 12rem 0;
  border-radius: 8rem;
  background: rgba(25, 45, 65, 0.92);
  border: 2rem solid rgba(80, 150, 220, 0.5);
  width: 100%;
  box-sizing: border-box;
`;

const TrafficSummaryHeader = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-bottom: 8rem;
  border-bottom: 1rem solid rgba(255, 255, 255, 0.15);
`;

const TrafficSummaryTitle = styled.span`
  color: rgba(140, 200, 255, 1);
  font-size: 12rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.8rem;
`;

const TrafficSummaryStats = styled.div`
  display: flex;
  flex-direction: column;
  gap: 6rem;
`;

const TrafficSummaryStat = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 11rem;
  padding: 2rem 0;
`;

const StatLabel = styled.span`
  color: rgba(255, 255, 255, 0.55);
  font-weight: 400;
`;

const StatValue = styled.span<{ $highlight?: boolean; $estimate?: boolean }>`
  color: ${props => props.$highlight ? 'rgba(100, 220, 160, 0.95)' : 'rgba(255, 255, 255, 0.85)'};
  font-weight: ${props => props.$highlight ? '600' : '500'};
  font-style: ${props => props.$estimate ? 'italic' : 'normal'};
`;

// V138: Container for value + info icon
const StatValueWithInfo = styled.div`
  display: flex;
  align-items: center;
  gap: 6rem;
`;

// V138: Info icon - color indicates data quality
// V161: Added margin-left for spacing from value
// V253: COHTML-SAFE - removed transition
const InfoIcon = styled.span<{ $hasData: boolean }>`
  font-size: 10rem;
  color: ${props => props.$hasData ? 'rgba(100, 200, 255, 0.7)' : 'rgba(255, 200, 100, 0.7)'};
  cursor: help;
  margin-left: 6rem;
  
  &:hover {
    color: ${props => props.$hasData ? 'rgba(100, 200, 255, 1)' : 'rgba(255, 200, 100, 1)'};
  }
`;

const StatDivider = styled.div`
  height: 1rem;
  background: rgba(255, 255, 255, 0.08);
  margin: 4rem 0;
`;

// V255: Enhanced TrafficFlowIndicator - stronger colors, better contrast
const TrafficFlowIndicator = styled.div<{ $status: string }>`
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8rem;
  padding: 8rem 12rem;
  margin-top: 6rem;
  border-radius: 6rem;
  font-size: 12rem;
  font-weight: 600;
  background: ${props => {
    switch (props.$status) {
      case 'high': return 'rgba(200, 60, 60, 0.35)';
      case 'medium': return 'rgba(200, 160, 40, 0.35)';
      default: return 'rgba(60, 160, 80, 0.35)';
    }
  }};
  border: 2rem solid ${props => {
    switch (props.$status) {
      case 'high': return 'rgba(255, 100, 100, 0.6)';
      case 'medium': return 'rgba(255, 200, 80, 0.6)';
      default: return 'rgba(100, 220, 130, 0.6)';
    }
  }};
  color: ${props => {
    switch (props.$status) {
      case 'high': return 'rgba(255, 140, 140, 1)';
      case 'medium': return 'rgba(255, 230, 120, 1)';
      default: return 'rgba(140, 240, 160, 1)';
    }
  }};
`;

// V255: FlowDot - bigger, more visible, with border for definition
const FlowDot = styled.div<{ $status: string }>`
  width: 10rem;
  height: 10rem;
  border-radius: 50%;
  border: 1rem solid rgba(255, 255, 255, 0.3);
  background: ${props => {
    switch (props.$status) {
      case 'high': return 'rgba(255, 80, 80, 1)';
      case 'medium': return 'rgba(255, 200, 60, 1)';
      case 'disabled': return 'rgba(100, 100, 100, 0.6)';
      default: return 'rgba(80, 220, 120, 1)';
    }
  }};
`;

// V138: Traffic Summary Row with full width
const TrafficSummaryRow = styled.div`
  width: 100%;
  padding: 0;
`;

// V256: Chart container - subtle, elegant
const TrafficHistoryContainer = styled.div<{ $disabled?: boolean }>`
  width: 100%;
  margin-top: 8rem;
  padding: 8rem;
  background: rgba(0, 0, 0, 0.15);
  border: 1rem solid rgba(100, 180, 255, 0.15);
  border-radius: 4rem;
  opacity: ${props => props.$disabled ? 0.4 : 1};
`;

const TrafficHistoryTitle = styled.div`
  font-size: 10rem;
  color: rgba(160, 200, 240, 0.7);
  margin-bottom: 6rem;
  display: flex;
  align-items: center;
  gap: 4rem;
  font-weight: 500;
`;

// V256: ChartWrapper - compact
const ChartWrapper = styled.div`
  display: flex;
  width: 100%;
  height: 50rem;
`;

const YAxisLabels = styled.div`
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding-right: 6rem;
  font-size: 9rem;
  color: rgba(255, 255, 255, 0.5);
  text-align: right;
  min-width: 32rem;
`;

const ChartArea = styled.div`
  flex: 1;
  position: relative;
`;

const XAxisLabels = styled.div`
  display: flex;
  justify-content: space-between;
  padding-left: 38rem;
  padding-top: 4rem;
  font-size: 10rem;
  color: rgba(255, 255, 255, 0.6);
`;


// V255: COHTML-SAFE - Simplified Chart Interface
interface TrafficHistoryChartProps {
  data: number[];
  currentVPH: number;
  isCustomPhase: boolean;
  currentGameTime?: number;
  color?: string;
}

// V256: COHTML-SAFE TrafficHistoryChart - elegant and subtle
// - NO mouse events
// - NO tooltips  
// - NO linearGradient
// - Simple static SVG polyline only
function TrafficHistoryChart({ data, currentVPH, isCustomPhase, color = 'rgba(100, 210, 160, 0.9)' }: TrafficHistoryChartProps) {
  const isDisabled = !isCustomPhase;
  
  // Simple data validation
  const validData = data && data.length > 0 ? data : [];
  const hasData = validData.length > 0 || currentVPH > 0;
  
  // V256: Compact chart dimensions
  const width = 280;
  const height = 50;
  const padding = 4;
  
  // Calculate max value
  const allValues = [...validData, currentVPH].filter(v => v > 0);
  const maxVal = allValues.length > 0 ? Math.max(...allValues, 100) : 100;
  const chartMax = Math.ceil(maxVal * 1.2 / 100) * 100 || 100;
  
  // Generate simple polyline points
  let points = '';
  if (hasData) {
    const displayData = validData.length > 0 ? [...validData, currentVPH] : [currentVPH];
    const effectiveWidth = width - padding * 2;
    const effectiveHeight = height - padding * 2;
    
    points = displayData.map((value, index) => {
      const x = padding + (index / Math.max(displayData.length - 1, 1)) * effectiveWidth;
      const y = padding + effectiveHeight - (value / chartMax) * effectiveHeight;
      return `${x},${y}`;
    }).join(' ');
  }
  
  // Current value Y position
  const currentY = hasData 
    ? padding + (height - padding * 2) - (currentVPH / chartMax) * (height - padding * 2)
    : height / 2;

  return (
    <TrafficHistoryContainer $disabled={isDisabled}>
      <TrafficHistoryTitle>
        24h Trend (Fz/h)
      </TrafficHistoryTitle>
      <ChartWrapper>
        <YAxisLabels>
          <span>{chartMax}</span>
          <span>{Math.round(chartMax / 2)}</span>
          <span>0</span>
        </YAxisLabels>
        <ChartArea>
          <svg width="100%" height={height} viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none">
            {/* V256: Subtle grid lines */}
            <line x1={padding} y1={padding} x2={width - padding} y2={padding} stroke="rgba(255,255,255,0.08)" strokeWidth="0.5" />
            <line x1={padding} y1={height/2} x2={width - padding} y2={height/2} stroke="rgba(255,255,255,0.08)" strokeWidth="0.5" />
            <line x1={padding} y1={height - padding} x2={width - padding} y2={height - padding} stroke="rgba(255,255,255,0.08)" strokeWidth="0.5" />
            
            {/* V256: Elegant polyline - not too thick */}
            {hasData && points && (
              <polyline
                points={points}
                fill="none"
                stroke={color}
                strokeWidth="1.5"
              />
            )}
            
            {/* V256: Clean current value dot */}
            {hasData && (
              <circle
                cx={width - padding}
                cy={currentY}
                r="4"
                fill={color}
              />
            )}
          </svg>
        </ChartArea>
      </ChartWrapper>
      <XAxisLabels>
        <span>-24h</span>
        <span>-12h</span>
        <span>jetzt</span>
      </XAxisLabels>
    </TrafficHistoryContainer>
  );
}

export default function Content(props: {items: MainPanelItem[], onOpenGreenWave?: () => void}) {
  const locale = useContext(LocaleContext);
  
  // V138: Auto-refresh trigger every 2 seconds
  // V162: refreshTick triggers re-render but key stays stable for tooltip persistence
  const [, setRefreshTick] = useState(0);
  
  useEffect(() => {
    const interval = setInterval(() => {
      setRefreshTick(t => t + 1);
      // The state change triggers re-render, which fetches fresh data from bindings
    }, 2000);
    
    return () => clearInterval(interval);
  }, []);
  
  return (
    <Container>
      <Scrollable style={{flex: 1}} contentStyle={{flex: 1}} trackStyle={{marginLeft: "0.25em"}}>
        {props.items.map((item, idx) => {
          if (item.itemType == "title") {
            return <Row key={idx} data={item}><Title {...item} /></Row>;
          }
          if (item.itemType == "message") {
            return <Row key={idx} data={item}><Message {...item} /></Row>;
          }
          if (item.itemType == "divider") {
            return <Divider key={idx} />;
          }
          if (item.itemType == "dropdown") {
            // V167: Phase mode dropdown
            const handleDropdownSelect = (value: string) => {
              engineCall(item.engineEventName, JSON.stringify({
                key: item.key,
                value: value
              }));
            };
            return (
              <div key={idx} style={{ padding: '4rem 8rem' }}>
                <Dropdown
                  options={item.options}
                  selectedValue={item.selectedValue}
                  onSelect={handleDropdownSelect}
                  getLabel={(label: string) => getString(locale, label)}
                />
              </div>
            );
          }
          if (item.itemType == "radio") {
            return (
              <Row key={idx} data={item} hoverEffect={true}>
                <Radio {...item} />
                <Label>{getString(locale, item.label)}</Label>
              </Row>
            );
          }
          if (item.itemType == "checkbox") {
            return (
              <Row key={idx} data={item} hoverEffect={true}>
                <Checkbox {...item} />
                <Label>{getString(locale, item.label)}</Label>
              </Row>
            );
          }
          if (item.itemType == "button") {
            // Special handling for Green Wave button
            if (item.label === "GreenWaveButton") {
              const isDisabled = item.value === "disabled";
              const tooltipText = isDisabled 
                ? getString(locale, "GreenWaveTooltipDisabled")
                : getString(locale, "GreenWaveTooltipActive");
              
              // If disabled: show fake display (no data = no onClick on Row)
              // If active: show real button with onClick
              if (isDisabled) {
                return (
                  <Row key={idx}>
                    <div style={{flex: 1}}>
                      <Tooltip position="bottom" tooltip={tooltipText}>
                        <GreenWaveDisplay>
                          {getString(locale, item.label)}
                        </GreenWaveDisplay>
                      </Tooltip>
                    </div>
                  </Row>
                );
              } else {
                return (
                  <Row key={idx} data={item}>
                    <div style={{flex: 1}}>
                      <Tooltip position="bottom" tooltip={tooltipText}>
                        <GreenWaveButton onClick={() => {
                          if (props.onOpenGreenWave) {
                            props.onOpenGreenWave();
                          }
                        }}>
                          {getString(locale, item.label)}
                        </GreenWaveButton>
                      </Tooltip>
                    </div>
                  </Row>
                );
              }
            }
            return <Row key={idx} data={item}><Button {...item} /></Row>;
          }
          if (item.itemType == "notification") {
            return <Notification key={idx} data={item} />;
          }
          if (item.itemType == "range") {
            return <Range key={idx} data={item} />;
          }
          // V138: Improved Traffic Summary Box with pulsing indicator
          if (item.itemType == "trafficSummary") {
            const flowStatusText = item.flowStatus === 'high' 
              ? getString(locale, "TrafficFlowHigh")
              : item.flowStatus === 'medium'
                ? getString(locale, "TrafficFlowMedium")
                : getString(locale, "TrafficFlowLow");
            
            // V161: Round to nearest 10 to reduce visual "jumping"
            // e.g. 1167 → 1170, 1142 → 1140
            const roundedVPH = Math.round(item.vehiclesPerHour / 10) * 10;
            
            // V141: Format vehicles per hour - show "~" only if no history data yet
            // V185: Also show "~" when Custom Phase is not active (always estimate)
            const vphDisplay = (!item.isCustomPhase || !item.hasFlowData)
              ? `~${roundedVPH.toLocaleString()}`
              : roundedVPH.toLocaleString();
            
            // V141: Info tooltip text - different based on data availability
            // V185: Show CustomPhaseRequired when not Custom Phase
            const vphInfoText = !item.isCustomPhase
              ? getString(locale, "CustomPhaseRequiredTooltip")
              : item.hasFlowData 
                ? getString(locale, "VPH_InfoRealData")
                : getString(locale, "VPH_InfoEstimate");
            
            // V179: Calculate Peak Hour and Max VPH from history data
            let peakHourLabel = "--:--";
            let maxVPH = 0;
            if (item.historyData && item.historyData.length > 0) {
              maxVPH = Math.round(Math.max(...item.historyData));
              
              // Find peak hour index and calculate time
              const peakIndex = item.historyData.indexOf(Math.max(...item.historyData));
              const currentTime = item.currentGameTime || 0;
              const SAMPLE_INTERVAL = 30 / 1440; // 30 minutes normalized
              const samplesFromNow = item.historyData.length - 1 - peakIndex;
              let peakTime = currentTime - (samplesFromNow * SAMPLE_INTERVAL);
              while (peakTime < 0) peakTime += 1;
              while (peakTime >= 1) peakTime -= 1;
              
              const totalMinutes = Math.round(peakTime * 24 * 60);
              const hours = Math.floor(totalMinutes / 60) % 24;
              const minutes = totalMinutes % 60;
              peakHourLabel = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
            }
            
            return (
              <TrafficSummaryRow key={`traffic-${idx}`}>
                <TrafficSummaryBox>
                  <TrafficSummaryHeader>
                    <TrafficSummaryTitle>{getString(locale, "TrafficSummary")}</TrafficSummaryTitle>
                  </TrafficSummaryHeader>
                  <TrafficSummaryStats>
                    <TrafficSummaryStat>
                      <StatLabel>{getString(locale, "VehiclesPerHour")}</StatLabel>
                      <StatValueWithInfo>
                        <StatValue $highlight={true} $estimate={!item.hasFlowData}>
                          {vphDisplay}
                        </StatValue>
                        <Tooltip position="bottom" tooltip={vphInfoText}>
                          <InfoIcon $hasData={item.isCustomPhase && item.hasFlowData}>ⓘ</InfoIcon>
                        </Tooltip>
                      </StatValueWithInfo>
                    </TrafficSummaryStat>
                    <TrafficSummaryStat>
                      <StatLabel>{getString(locale, "Approaches")}</StatLabel>
                      <StatValue>{item.approaches}</StatValue>
                    </TrafficSummaryStat>
                    <TrafficSummaryStat>
                      <Tooltip position="bottom" tooltip={getString(locale, "IncomingLanesTooltip")}>
                        <StatLabel style={{cursor: 'help'}}>{getString(locale, "IncomingLanes")} <span style={{marginLeft: '4rem'}}>ⓘ</span></StatLabel>
                      </Tooltip>
                      <StatValue>{item.incomingLanes}</StatValue>
                    </TrafficSummaryStat>
                    <StatDivider />
                    {item.isCustomPhase ? (
                      <>
                        <TrafficSummaryStat>
                          <Tooltip position="bottom" tooltip={getString(locale, "OccupiedLanesTooltip")}>
                            <StatLabel style={{cursor: 'help'}}>{getString(locale, "OccupiedLanes")} <span style={{marginLeft: '4rem'}}>ⓘ</span></StatLabel>
                          </Tooltip>
                          <StatValue>{item.occupiedLanes} / {item.incomingLanes}</StatValue>
                        </TrafficSummaryStat>
                        <TrafficSummaryStat>
                          <StatLabel>{getString(locale, "OccupiedPedestrianLanes")}</StatLabel>
                          <StatValue>{item.occupiedPedestrianLanes}</StatValue>
                        </TrafficSummaryStat>
                      </>
                    ) : (
                      <TrafficSummaryStat>
                        <Tooltip position="bottom" tooltip={getString(locale, "CustomPhaseRequiredTooltip")}>
                          <StatLabel style={{cursor: 'help', opacity: 0.5}}>{getString(locale, "OccupiedLanes")} <span style={{marginLeft: '4rem'}}>ⓘ</span></StatLabel>
                        </Tooltip>
                        <StatValue style={{opacity: 0.5}}>---</StatValue>
                      </TrafficSummaryStat>
                    )}
                    {/* V179: New stats - Waiting Vehicles, Peak Hour, Max VPH */}
                    {item.isCustomPhase && (
                      <>
                        <StatDivider />
                        <TrafficSummaryStat>
                          <Tooltip position="bottom" tooltip={getString(locale, "WaitingVehiclesTooltip")}>
                            <StatLabel style={{cursor: 'help'}}>{getString(locale, "WaitingVehicles")} <span style={{marginLeft: '4rem'}}>ⓘ</span></StatLabel>
                          </Tooltip>
                          <StatValue $highlight={true}>{item.waitingVehicles || 0}</StatValue>
                        </TrafficSummaryStat>
                        <TrafficSummaryStat>
                          <Tooltip position="bottom" tooltip={getString(locale, "PeakHourTooltip")}>
                            <StatLabel style={{cursor: 'help'}}>{getString(locale, "PeakHour")} <span style={{marginLeft: '4rem'}}>ⓘ</span></StatLabel>
                          </Tooltip>
                          <StatValue>{peakHourLabel}</StatValue>
                        </TrafficSummaryStat>
                        <TrafficSummaryStat>
                          <Tooltip position="bottom" tooltip={getString(locale, "MaxVPHTooltip")}>
                            <StatLabel style={{cursor: 'help'}}>{getString(locale, "MaxVPH")} <span style={{marginLeft: '4rem'}}>ⓘ</span></StatLabel>
                          </Tooltip>
                          <StatValue>{maxVPH > 0 ? maxVPH.toLocaleString() : '--'}</StatValue>
                        </TrafficSummaryStat>
                      </>
                    )}
                  </TrafficSummaryStats>
                  {item.isCustomPhase ? (
                    <TrafficFlowIndicator $status={item.flowStatus}>
                      <FlowDot $status={item.flowStatus} />
                      <span style={{marginLeft: '4rem'}}>{flowStatusText}</span>
                    </TrafficFlowIndicator>
                  ) : (
                    <Tooltip position="bottom" tooltip={getString(locale, "CustomPhaseRequiredTooltip")}>
                      <TrafficFlowIndicator $status="disabled" style={{opacity: 0.5, cursor: 'help'}}>
                        <FlowDot $status="disabled" />
                        <span style={{marginLeft: '4rem'}}>---</span>
                      </TrafficFlowIndicator>
                    </Tooltip>
                  )}
                  {/* V148: Traffic History Chart with live projection */}
                  <TrafficHistoryChart 
                    data={item.historyData || []} 
                    currentVPH={item.currentVPH || item.vehiclesPerHour}
                    isCustomPhase={item.isCustomPhase}
                    currentGameTime={item.currentGameTime || 0}
                  />
                </TrafficSummaryBox>
              </TrafficSummaryRow>
            );
          }
          return <></>;
        })}
      </Scrollable>
    </Container>
  );
}
