import { useContext, useCallback, useEffect, useMemo, useState } from "react";
import styled from "styled-components";

import { bindValue, call, trigger, useValue } from "cs2/api";

import { LocaleContext } from "@/context";
import { getString } from "@/localisations";

import Checkbox from "@/components/common/checkbox";
import Scrollable from "@/components/common/scrollable";
import Tooltip from "@/components/common/tooltip";
import TooltipIcon from "@/components/common/tooltip-icon";
import Range from "@/components/common/range";

// === WERDER BREMEN COLORS ===
const WERDER_GREEN = "rgba(30, 144, 83, 1)";
const WERDER_GREEN_LIGHT = "rgba(40, 170, 100, 1)";
const WERDER_GREEN_DARK = "rgba(20, 120, 70, 1)";
const WERDER_GLOW = "0 0 15rem rgba(30, 144, 83, 0.6)";

// === TYPES ===
interface SyncSettings {
  syncGroupId: number;
  useSyncedCycle: boolean;
  cycleOffsetSeconds: number;
  syncPhaseIndex: number;
  totalCycleDuration: number;
  phaseCount: number;
  selectedEntityIndex?: number;  // Node ID of selected intersection
}

interface IntersectionInfo {
  index: number;
  entity: number;  // Entity index for backend calls
  offset: number;
  syncPhase: number;
  isSynced: boolean;
  cycleDuration: number;
  phaseCount: number;
  progress: number; // 0-100%
  currentPhase: number;       // Which phase is currently active (0-indexed)
  isSyncPhaseActive: boolean; // Is the green wave phase active right now?
  isReference?: boolean;      // Is this the reference intersection?
  position?: number;          // Position in group ordering
  // Extended stats (V15)
  edgeCount?: number;         // Number of connected roads
  totalLanes?: number;        // Total car lanes
  leftLanes?: number;         // Left-turning lanes
  straightLanes?: number;     // Straight lanes
  rightLanes?: number;        // Right-turning lanes
  pedestrianLanes?: number;   // Pedestrian crossings
}

interface SyncGroup {
  groupId: number;
  groupName: string;
  baseCycleDuration: number;
  intersectionCount: number;
  groupTimer?: number;
  intersections?: IntersectionInfo[];
  // Time window settings
  alwaysActive?: boolean;
  timeWindow1Start?: number;
  timeWindow1End?: number;
  timeWindow2Start?: number;
  timeWindow2End?: number;
  timeWindow3Start?: number;
  timeWindow3End?: number;
}

interface MainPanelData {
  syncSettings: SyncSettings | null;
  syncGroups: SyncGroup[];
}

interface GreenWavePanelProps {
  onClose: () => void;
}

// =============================================================================
// STYLED COMPONENTS - MAIN LAYOUT
// =============================================================================

const PanelContainer = styled.div`
  width: 520rem;
  min-width: 480rem;
  max-width: 600rem;
  background-color: var(--panelColorNormal);
  backdrop-filter: var(--panelBlur);
  color: var(--textColor);
  display: flex;
  flex-direction: column;
  position: relative;
  border-left: 4rem solid ${WERDER_GREEN};
`;

const PanelHeader = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: linear-gradient(135deg, ${WERDER_GREEN} 0%, ${WERDER_GREEN_DARK} 100%);
  padding: 14rem 20rem;
  box-shadow: ${WERDER_GLOW};
`;

const PanelTitle = styled.div`
  font-size: 1.2em;
  font-weight: bold;
  color: white;
  text-shadow: 0 2rem 4rem rgba(0, 0, 0, 0.3);
  display: flex;
  align-items: center;
  gap: 10rem;
`;

const TitleIcon = styled.span`
  font-size: 1.1em;
`;

const CloseButton = styled.div`
  width: 32rem;
  height: 32rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 6rem;
  cursor: pointer;
  color: white;
  font-size: 1.2em;
  font-weight: bold;
  transition: all 0.2s;
  &:hover { 
    background: rgba(255, 255, 255, 0.3);
    transform: scale(1.05);
  }
`;

// === TOAST ===
const ToastContainer = styled.div<{$visible: boolean; $type: 'success' | 'info' | 'warning'}>`
  position: absolute;
  top: 70rem;
  left: 50%;
  transform: translateX(-50%);
  z-index: 1000;
  background: ${props => 
    props.$type === 'success' ? 'rgba(30, 144, 83, 0.95)' :
    props.$type === 'warning' ? 'rgba(255, 165, 0, 0.95)' :
    'rgba(60, 60, 80, 0.95)'
  };
  color: white;
  padding: 12rem 20rem;
  border-radius: 8rem;
  font-size: 0.95em;
  font-weight: 500;
  box-shadow: 0 4rem 16rem rgba(0, 0, 0, 0.3);
  opacity: ${props => props.$visible ? 1 : 0};
  transition: opacity 0.3s ease;
  display: flex;
  align-items: center;
  gap: 10rem;
  pointer-events: ${props => props.$visible ? 'auto' : 'none'};
`;

const ToastIcon = styled.span`
  font-size: 1.3em;
`;

// === 3 TABS ===
const TabContainer = styled.div`
  display: flex;
  background: rgba(0, 0, 0, 0.25);
  border-bottom: 2rem solid rgba(255, 255, 255, 0.1);
`;

const Tab = styled.div<{$active: boolean}>`
  flex: 1;
  padding: 12rem 16rem;
  text-align: center;
  cursor: pointer;
  font-size: 0.95em;
  font-weight: ${props => props.$active ? 'bold' : 'normal'};
  color: ${props => props.$active ? 'white' : 'var(--textColorDim)'};
  background: ${props => props.$active ? WERDER_GREEN : 'transparent'};
  transition: all 0.2s;
  border-bottom: ${props => props.$active ? `3rem solid ${WERDER_GREEN_LIGHT}` : '3rem solid transparent'};
  
  &:hover {
    background: ${props => props.$active ? WERDER_GREEN : 'rgba(255, 255, 255, 0.08)'};
    color: ${props => props.$active ? 'white' : 'var(--textColor)'};
  }
`;

const TabIcon = styled.span`
  margin-right: 6rem;
`;

const ContentArea = styled.div`
  padding: 16rem 20rem 48rem 20rem;
  display: flex;
  flex-direction: column;
  width: 100%;  /* V244: Fill full width of scrollable container */
  box-sizing: border-box;  /* V244: Include padding in width calculation */
  & > * + * {
    margin-top: 14rem;
  }
`;

// === SYNC STATUS ===
const SyncStatusBox = styled.div<{$syncing: boolean}>`
  background: ${props => props.$syncing ? 
    'linear-gradient(135deg, rgba(30, 144, 83, 0.25) 0%, rgba(30, 144, 83, 0.1) 100%)' : 
    'rgba(0, 0, 0, 0.2)'
  };
  border: 2rem solid ${props => props.$syncing ? WERDER_GREEN : 'rgba(255, 255, 255, 0.1)'};
  border-radius: 10rem;
  padding: 16rem;
  transition: all 0.3s ease;
`;

const SyncStatusHeader = styled.div`
  display: flex;
  align-items: center;
  gap: 10rem;
  margin-bottom: 12rem;
`;

const SyncStatusIcon = styled.div<{$active: boolean}>`
  width: 14rem;
  height: 14rem;
  border-radius: 50%;
  background: ${props => props.$active ? WERDER_GREEN : 'rgba(255, 255, 255, 0.3)'};
  box-shadow: ${props => props.$active ? `0 0 8rem ${WERDER_GREEN}` : 'none'};
  transition: all 0.3s ease;
`;

const SyncStatusText = styled.div<{$active: boolean}>`
  font-size: 1em;
  font-weight: bold;
  color: ${props => props.$active ? WERDER_GREEN_LIGHT : 'var(--textColorDim)'};
`;

const SyncInfoGrid = styled.div`
  display: flex;
  flex-wrap: wrap;
  margin: -4rem;
`;

const SyncInfoItem = styled.div`
  background: rgba(0, 0, 0, 0.25);
  padding: 10rem 14rem;
  border-radius: 6rem;
  text-align: center;
  flex-grow: 1;
  flex-shrink: 1;
  flex-basis: 20%;
  min-width: 80rem;
  margin: 4rem;
`;

const SyncInfoLabel = styled.div`
  font-size: 0.75em;
  color: var(--textColorDim);
  margin-bottom: 4rem;
`;

const SyncInfoValue = styled.div`
  font-size: 1.15em;
  font-weight: bold;
  color: ${WERDER_GREEN_LIGHT};
`;

// === SECTION ===
const Section = styled.div`
  display: flex;
  flex-direction: column;
  & > * + * {
    margin-top: 10rem;
  }
`;

const SectionTitle = styled.div`
  font-size: 0.85em;
  font-weight: bold;
  color: ${WERDER_GREEN};
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding-bottom: 6rem;
  border-bottom: 1rem solid rgba(30, 144, 83, 0.3);
  display: flex;
  align-items: center;
  gap: 4rem;
`;

const SectionDivider = styled.div`
  height: 1rem;
  background: rgba(255, 255, 255, 0.08);
  margin: 8rem 0;
`;

// === INFO ROW - Icons LINKS ===
const InfoRow = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8rem 0;
  border-bottom: 1rem solid rgba(255, 255, 255, 0.05);
  &:last-child { border-bottom: none; }
`;

const InfoLabelWithIcon = styled.div`
  display: flex;
  align-items: center;
  gap: 8rem;
  flex: 1;
`;

const InfoIconWrapper = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24rem;
  flex-shrink: 0;
`;

const InfoLabel = styled.span`
  font-size: 0.9em;
  color: var(--textColorDim);
`;

const InfoValue = styled.div`
  font-size: 1em;
  font-weight: 600;
  color: var(--textColor);
  text-align: right;
`;

const InfoValueHighlight = styled(InfoValue)`
  color: ${WERDER_GREEN_LIGHT};
  font-size: 1.1em;
`;

const InfoCard = styled.div`
  background: rgba(0, 0, 0, 0.2);
  border-radius: 8rem;
  padding: 12rem 16rem;
`;

// === DROPDOWN (V13) ===
const DropdownContainer = styled.div`
  position: relative;
  margin-bottom: 12rem;
`;

const DropdownTrigger = styled.div<{$isOpen?: boolean; $hasSelection?: boolean}>`
  background: ${p => p.$hasSelection 
    ? `linear-gradient(135deg, ${WERDER_GREEN} 0%, ${WERDER_GREEN_DARK} 100%)`
    : 'rgba(0, 0, 0, 0.3)'};
  border: 2rem solid ${p => p.$isOpen ? WERDER_GREEN_LIGHT : (p.$hasSelection ? WERDER_GREEN : 'rgba(255,255,255,0.1)')};
  border-radius: 8rem;
  padding: 12rem 16rem;
  cursor: pointer;
  display: flex;
  justify-content: space-between;
  align-items: center;
  transition: all 0.2s;
  
  &:hover {
    border-color: ${WERDER_GREEN_LIGHT};
    background: ${p => p.$hasSelection 
      ? `linear-gradient(135deg, ${WERDER_GREEN_LIGHT} 0%, ${WERDER_GREEN} 100%)`
      : 'rgba(255,255,255,0.05)'};
  }
`;

const DropdownTriggerText = styled.span<{$hasSelection?: boolean}>`
  font-weight: ${p => p.$hasSelection ? 'bold' : '500'};
  color: ${p => p.$hasSelection ? 'white' : 'var(--textColorDim)'};
`;

const DropdownTriggerInfo = styled.div`
  display: flex;
  align-items: center;
  gap: 10rem;
`;

const DropdownTriggerBadge = styled.span`
  background: rgba(255,255,255,0.2);
  padding: 3rem 8rem;
  border-radius: 10rem;
  font-size: 0.8em;
  color: white;
`;

const DropdownArrow = styled.span<{$isOpen?: boolean}>`
  color: ${p => p.$isOpen ? WERDER_GREEN_LIGHT : 'var(--textColorDim)'};
  transition: transform 0.2s;
  transform: rotate(${p => p.$isOpen ? '180deg' : '0deg'});
  font-size: 0.8em;
`;

const DropdownOverlay = styled.div`
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 99;
`;

const DropdownContent = styled.div`
  position: absolute;
  top: 100%;
  margin-top: 4rem;
  left: 0;
  right: 0;
  background: rgba(30, 35, 45, 0.98);
  border: 2rem solid ${WERDER_GREEN};
  border-radius: 8rem;
  z-index: 100;
  box-shadow: 0 8rem 24rem rgba(0,0,0,0.4);
  overflow: hidden;
`;

const DropdownSearch = styled.input`
  width: 100%;
  padding: 12rem 14rem;
  border: none;
  border-bottom: 1rem solid rgba(255,255,255,0.1);
  background: rgba(0,0,0,0.3);
  color: var(--textColor);
  font-size: 0.95em;
  outline: none;
  
  /* V232: COHTML doesn't support ::placeholder - removed */
  /* Placeholder will use default browser styling */
  
  &:focus {
    background: rgba(0,0,0,0.4);
  }
`;

const DropdownList = styled.div`
  max-height: 220rem;
  overflow-y: auto;
`;

const DropdownItem = styled.div<{$selected?: boolean; $isNoGroup?: boolean}>`
  padding: 10rem 14rem;
  cursor: pointer;
  display: flex;
  justify-content: space-between;
  align-items: center;
  transition: all 0.15s;
  background: ${p => p.$selected ? `rgba(30, 144, 83, 0.3)` : 'transparent'};
  border-left: 3rem solid ${p => p.$selected ? WERDER_GREEN : 'transparent'};
  
  ${p => p.$isNoGroup && `
    color: var(--textColorDim);
    font-style: italic;
  `}
  
  &:hover {
    background: ${p => p.$selected ? `rgba(30, 144, 83, 0.4)` : 'rgba(255,255,255,0.08)'};
  }
`;

const DropdownItemName = styled.span<{$selected?: boolean}>`
  font-weight: ${p => p.$selected ? '600' : '400'};
  color: ${p => p.$selected ? WERDER_GREEN_LIGHT : 'var(--textColor)'};
`;

const DropdownItemInfo = styled.div`
  display: flex;
  align-items: center;
  gap: 8rem;
  font-size: 0.85em;
  color: var(--textColorDim);
`;

const DropdownItemBadge = styled.span`
  background: rgba(30, 144, 83, 0.25);
  padding: 2rem 8rem;
  border-radius: 8rem;
  color: ${WERDER_GREEN_LIGHT};
`;

// === BUTTONS ===
const Button = styled.div<{$variant?: 'primary' | 'secondary' | 'danger'}>`
  padding: 12rem 20rem;
  border-radius: 8rem;
  cursor: pointer;
  font-weight: 600;
  text-align: center;
  transition: all 0.2s;
  font-size: 0.95em;
  
  ${props => {
    switch (props.$variant) {
      case 'danger':
        return `background: rgba(220, 53, 69, 0.8); color: white; &:hover { background: rgba(220, 53, 69, 1); }`;
      case 'secondary':
        return `background: rgba(255, 255, 255, 0.12); color: var(--textColor); &:hover { background: rgba(255, 255, 255, 0.2); }`;
      default:
        return `background: linear-gradient(135deg, ${WERDER_GREEN} 0%, ${WERDER_GREEN_DARK} 100%); color: white; box-shadow: ${WERDER_GLOW}; &:hover { background: linear-gradient(135deg, ${WERDER_GREEN_LIGHT} 0%, ${WERDER_GREEN} 100%); transform: translateY(-1rem); }`;
    }
  }}
`;

const ButtonRow = styled.div`
  display: flex;
  gap: 10rem;
`;

const SmallButton = styled(Button)`
  padding: 8rem 14rem;
  font-size: 0.9em;
  flex: 1;
`;

// === SETTINGS ROW - Icon links! ===
const SettingRow = styled.div`
  display: flex;
  align-items: center;
  gap: 12rem;
  padding: 10rem 14rem;
  background: rgba(0, 0, 0, 0.2);
  border-radius: 8rem;
`;

const SettingIconWrapper = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28rem;
  flex-shrink: 0;
`;

const SettingLabel = styled.div`
  flex: 1;
  font-size: 0.9em;
  color: var(--textColor);
`;

// V281: SettingControl, PhaseNavButton, PhaseDisplay entfernt (Sync-Phase Selector entfernt)

// === SLIDER ===
const SliderContainer = styled.div`
  background: rgba(0, 0, 0, 0.2);
  border-radius: 8rem;
  padding: 14rem 16rem;
`;

const SliderHeader = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10rem;
`;

const SliderLabelWithIcon = styled.div`
  display: flex;
  align-items: center;
  gap: 8rem;
`;

const SliderValue = styled.div`
  font-size: 1.1em;
  font-weight: bold;
  color: ${WERDER_GREEN_LIGHT};
`;

// === CALCULATOR ===
const CalculatorBox = styled.div`
  background: rgba(0, 0, 0, 0.2);
  border-radius: 10rem;
  padding: 16rem;
  border: 1rem solid rgba(255, 255, 255, 0.08);
`;

const CalculatorRow = styled.div`
  display: flex;
  align-items: center;
  gap: 12rem;
  margin-bottom: 12rem;
`;

const CalculatorIconWrapper = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28rem;
  flex-shrink: 0;
`;

const CalculatorLabel = styled.div`
  flex: 1;
  font-size: 0.9em;
  color: var(--textColorDim);
`;

const CalculatorInput = styled.input`
  width: 100rem;
  padding: 8rem 12rem;
  background: rgba(0, 0, 0, 0.3);
  border: 2rem solid rgba(255, 255, 255, 0.1);
  border-radius: 6rem;
  color: var(--textColor);
  font-size: 1em;
  text-align: right;
  &:focus { outline: none; border-color: ${WERDER_GREEN}; }
`;

const CalculatorUnit = styled.div`
  width: 50rem;
  font-size: 0.9em;
  color: var(--textColorDim);
`;

const CalculatorResult = styled.div`
  display: flex;
  align-items: center;
  gap: 12rem;
  padding-top: 12rem;
  border-top: 1rem solid rgba(255, 255, 255, 0.1);
  margin-top: 4rem;
`;

const CalculatorResultLabel = styled.div`
  flex: 1;
  font-size: 0.9em;
  color: var(--textColorDim);
`;

const CalculatorResultValue = styled.div`
  font-size: 1.2em;
  font-weight: bold;
  color: ${WERDER_GREEN_LIGHT};
  min-width: 60rem;
  text-align: right;
`;

const ApplyButton = styled(Button)`
  padding: 8rem 16rem;
  font-size: 0.9em;
`;

// === RENAME ===
const RenameContainer = styled.div`
  display: flex;
  gap: 8rem;
  align-items: center;
`;

const RenameInput = styled.input`
  flex: 1;
  padding: 10rem 14rem;
  background: rgba(0, 0, 0, 0.3);
  border: 2rem solid ${WERDER_GREEN};
  border-radius: 6rem;
  color: var(--textColor);
  font-size: 0.95em;
  &:focus { outline: none; border-color: ${WERDER_GREEN_LIGHT}; }
`;

// === DASHBOARD ===
const LiveIndicator = styled.div`
  width: 10rem;
  height: 10rem;
  background: ${WERDER_GREEN};
  border-radius: 50%;
  box-shadow: 0 0 8rem ${WERDER_GREEN};
  animation: pulse 1.5s ease-in-out infinite;
  @keyframes pulse {
    0%, 100% { opacity: 1; transform: scale(1); }
    50% { opacity: 0.6; transform: scale(0.9); }
  }
`;

const SaveFeedback = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8rem;
  padding: 12rem 16rem;
  margin-top: 16rem;
  background: rgba(30, 144, 83, 0.15);
  border: 1px solid rgba(30, 144, 83, 0.3);
  border-radius: 8rem;
  color: ${WERDER_GREEN_LIGHT};
  font-size: 0.9em;
`;

const SaveIcon = styled.span`
  color: ${WERDER_GREEN};
  font-weight: bold;
`;

const LiveSummary = styled.div`
  display: flex;
  justify-content: space-around;
  align-items: center;
  padding: 16rem;
  margin-top: 16rem;
  background: rgba(30, 144, 83, 0.15);
  border-radius: 8rem;
  border: 1px solid rgba(30, 144, 83, 0.25);
`;

const LiveSummaryItem = styled.div`
  text-align: center;
  padding: 0 12rem;
`;

const LiveSummaryValue = styled.div`
  font-size: 1.4em;
  font-weight: bold;
  color: ${WERDER_GREEN_LIGHT};
`;

const LiveSummaryLabel = styled.div`
  font-size: 0.8em;
  color: var(--textColorDim);
  margin-top: 4rem;
`;

const LiveSummaryDivider = styled.div`
  width: 1px;
  height: 40rem;
  background: rgba(255,255,255,0.15);
`;

// === DASHBOARD V14 - Komplett neu ===
const DashboardGroupHeader = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16rem;
  background: linear-gradient(135deg, ${WERDER_GREEN} 0%, ${WERDER_GREEN_DARK} 100%);
  border-radius: 10rem;
  margin-bottom: 16rem;
`;

const DashboardGroupName = styled.div`
  font-size: 1.15em;
  font-weight: bold;
  color: white;
  display: flex;
  align-items: center;
  gap: 8rem;
`;

const DashboardGroupMeta = styled.div`
  display: flex;
  align-items: center;
  gap: 6rem;
`;

const DashboardMetaBadge = styled.div<{$highlight?: boolean}>`
  background: ${p => p.$highlight ? 'rgba(255,255,255,0.25)' : 'rgba(255,255,255,0.15)'};
  padding: 4rem 10rem;
  border-radius: 4rem;
  font-size: 0.8em;
  color: white;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 4rem;
`;

const MetaIcon = styled.span`
  font-size: 1em;
`;

const DashboardIntersectionCard = styled.div<{$isActive?: boolean}>`
  background: ${p => p.$isActive 
    ? 'linear-gradient(135deg, rgba(30, 144, 83, 0.2) 0%, rgba(30, 144, 83, 0.08) 100%)'
    : 'rgba(0, 0, 0, 0.2)'};
  border: 1rem solid ${p => p.$isActive ? 'rgba(30, 144, 83, 0.4)' : 'rgba(255,255,255,0.06)'};
  border-radius: 8rem;
  padding: 12rem;
  margin-bottom: 10rem;
`;

const DashboardCardTop = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10rem;
`;

const DashboardCardTitle = styled.div`
  display: flex;
  align-items: flex-start;
  flex: 1;
  min-width: 0;
  overflow: hidden;
`;

const DashboardCardIndex = styled.div`
  background: ${WERDER_GREEN};
  color: white;
  min-width: 28rem;
  width: 28rem;
  height: 28rem;
  border-radius: 6rem;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 0.85em;
  flex-shrink: 0;
  margin-right: 10rem;
`;

// V239: DashboardMoveButtons, DashboardMoveButton, DashboardRemoveButton entfernt
// (Priorisierung via Offset, Entfernen via Dropdown)

const DashboardReferenceTag = styled.div`
  background: ${WERDER_GREEN};
  color: white;
  font-size: 0.6em;
  padding: 2rem 6rem;
  border-radius: 3rem;
  margin-left: 8rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
`;

const DashboardCardInfo = styled.div`
  display: flex;
  flex-direction: column;
  min-width: 0;
`;

const DashboardCardName = styled.div`
  font-size: 0.95em;
  font-weight: 600;
  color: var(--textColor);
  white-space: nowrap;
  display: flex;
  align-items: center;
`;

const DashboardCardSubtitle = styled.div`
  font-size: 0.75em;
  color: var(--textColorDim);
  margin-top: 2rem;
  white-space: nowrap;
`;

const DashboardCardBadges = styled.div`
  display: flex;
  gap: 6rem;
`;

const DashboardStatusBadge = styled.div<{$active?: boolean; $type?: 'sync' | 'phase'}>`
  padding: 3rem 8rem;
  border-radius: 4rem;
  font-size: 0.7em;
  font-weight: bold;
  ${p => p.$type === 'sync' ? `
    background: ${p.$active ? WERDER_GREEN : 'rgba(100,100,100,0.4)'};
    color: ${p.$active ? 'white' : 'rgba(255,255,255,0.5)'};
  ` : `
    background: ${p.$active ? 'rgba(30, 144, 83, 0.3)' : 'rgba(255,165,0,0.2)'};
    color: ${p.$active ? WERDER_GREEN_LIGHT : 'rgba(255,165,0,0.9)'};
  `}
`;

const DashboardProgressSection = styled.div`
  margin-bottom: 10rem;
`;

const DashboardProgressHeader = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6rem;
`;

const DashboardProgressLabel = styled.span`
  font-size: 0.9em;
  color: var(--textColor);
  font-weight: 500;
`;

const DashboardProgressValue = styled.span<{$isGreen?: boolean}>`
  font-size: 0.9em;
  font-weight: 600;
  color: ${p => p.$isGreen ? WERDER_GREEN_LIGHT : 'var(--textColor)'};
`;

const DashboardProgressBar = styled.div`
  height: 10rem;
  background: rgba(0, 0, 0, 0.3);
  border-radius: 5rem;
  overflow: hidden;
  position: relative;
`;

// Main progress fill - goes from 0-50% (minimum phase)
const DashboardProgressFill = styled.div<{$progress: number; $isGreen?: boolean}>`
  height: 100%;
  width: ${p => Math.min(50, Math.max(0, p.$progress))}%;
  background: ${p => p.$isGreen 
    ? `linear-gradient(90deg, ${WERDER_GREEN} 0%, ${WERDER_GREEN_LIGHT} 100%)`
    : 'linear-gradient(90deg, rgba(255,165,0,0.7) 0%, rgba(255,165,0,0.9) 100%)'};
  border-radius: 5rem 0 0 5rem;
  transition: width 0.3s ease;
`;

// Blinking overflow section - appears after 50% (past minimum duration)
const DashboardProgressOverflow = styled.div<{$isGreen?: boolean; $visible: boolean}>`
  position: absolute;
  left: 50%;
  right: 0;
  top: 0;
  bottom: 0;
  /* V232: COHTML can't parse hex-alpha appended to color vars, use explicit rgba */
  background: ${p => p.$isGreen 
    ? 'linear-gradient(90deg, rgba(40, 170, 100, 0.5) 0%, rgba(40, 170, 100, 0.25) 100%)'
    : 'linear-gradient(90deg, rgba(255,200,100,0.6) 0%, rgba(255,200,100,0.3) 100%)'};
  border-radius: 0 5rem 5rem 0;
  opacity: ${p => p.$visible ? 1 : 0};
  animation: ${p => p.$visible ? 'pulseOverflow 1s ease-in-out infinite' : 'none'};
`;

// Marker at 50% showing min duration boundary
const DashboardProgressMinMarker = styled.div`
  position: absolute;
  left: 50%;
  top: 0;
  bottom: 0;
  width: 2rem;
  background: rgba(255, 255, 255, 0.5);
  transform: translateX(-50%);
  pointer-events: none;
  z-index: 2;
`;

// Labels under the progress bar
const DashboardProgressLabels = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 4rem;
  font-size: 0.55em;
  color: var(--textColorDim);
  opacity: 0.7;
`;

const DashboardProgressLabelLeft = styled.span`
  flex: 1;
  text-align: left;
`;

const DashboardProgressLabelCenter = styled.span`
  flex: 1;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2rem;
`;

const DashboardProgressLabelRight = styled.span`
  flex: 1;
  text-align: right;
`;

const DashboardProgressInfoIcon = styled.span`
  /* V232: COHTML doesn't support inline-flex, use flex */
  display: flex;
  align-items: center;
  justify-content: center;
  width: 12rem;
  height: 12rem;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.15);
  font-size: 0.8em;
  cursor: help;
`;

const DashboardProgressBarContainer = styled.div`
  position: relative;
  
  @keyframes pulseOverflow {
    0%, 100% { opacity: 0.4; }
    50% { opacity: 0.9; }
  }
`;

const DashboardStatsRow = styled.div`
  display: flex;
  justify-content: center;
  width: 100%;
  margin-top: 8rem;
`;

const DashboardCompactStat = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 8rem;
  background: rgba(0, 0, 0, 0.2);
  border-radius: 5rem;
  margin: 0 4rem;
`;

const DashboardCompactLabel = styled.div`
  font-size: 0.6em;
  color: var(--textColorDim);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  white-space: nowrap;
  margin-bottom: 2rem;
`;

const DashboardCompactValue = styled.div`
  font-size: 0.9em;
  font-weight: bold;
  color: ${WERDER_GREEN_LIGHT};
  white-space: nowrap;
`;

const DashboardSmallStatValue = styled.div`
  font-size: 0.85em;
  font-weight: bold;
  color: var(--textColor);
  white-space: nowrap;
`;

const DashboardOffsetConnector = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 8rem 0;
  color: ${WERDER_GREEN_LIGHT};
  font-size: 0.9em;
`;

const DashboardOffsetLine = styled.div`
  flex: 1;
  height: 2rem;
  background: linear-gradient(90deg, transparent 0%, ${WERDER_GREEN} 50%, transparent 100%);
  margin: 0 12rem;
`;

const DashboardOffsetValue = styled.span`
  background: rgba(30, 144, 83, 0.2);
  padding: 4rem 12rem;
  border-radius: 12rem;
  font-weight: 600;
`;

// === EMPTY & HINTS ===
const EmptyState = styled.div`
  text-align: center;
  padding: 40rem 20rem;
  color: var(--textColorDim);
`;

const EmptyStateIcon = styled.div`
  font-size: 3.5em;
  margin-bottom: 16rem;
  opacity: 0.5;
`;

const EmptyStateText = styled.div`
  font-size: 1em;
  margin-bottom: 20rem;
`;

const HintBox = styled.div`
  background: rgba(30, 144, 83, 0.12);
  border: 1rem solid rgba(30, 144, 83, 0.25);
  border-radius: 8rem;
  padding: 12rem 16rem;
  font-size: 0.9em;
  color: var(--textColorDim);
  line-height: 1.5;
  display: flex;
  align-items: flex-start;
  gap: 10rem;
`;

const HintIcon = styled.span`
  color: ${WERDER_GREEN};
  font-weight: bold;
  min-width: 20rem;
`;

const TooltipText = styled.div`
  max-width: 280rem;
  line-height: 1.4;
`;

// =============================================================================
// =============================================================================
// BINDINGS
// =============================================================================

const mainPanel$ = bindValue<string>("C2VM.TLE", "GetMainPanel", "{}");

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function GreenWavePanel({ onClose }: GreenWavePanelProps) {
  const locale = useContext(LocaleContext);
  const [activeTab, setActiveTab] = useState<'intersection' | 'groups' | 'dashboard'>('intersection');
  const [isRenaming, setIsRenaming] = useState(false);
  const [renameValue, setRenameValue] = useState("");
  const [renamingGroupId, setRenamingGroupId] = useState<number | null>(null);
  const [calcDistance, setCalcDistance] = useState("500");
  const [calcSpeed, setCalcSpeed] = useState("50");
  
  // Dropdown state
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const [groupSearchQuery, setGroupSearchQuery] = useState("");
  
  // Local slider value for immediate feedback (syncs with backend on release)
  const [localOffsetValue, setLocalOffsetValue] = useState<number | null>(null);
  
  const [toast, setToast] = useState<{message: string; type: 'success' | 'info' | 'warning'; visible: boolean}>({
    message: '', type: 'info', visible: false
  });
  
  // Notify backend when dashboard tab is active for live updates
  useEffect(() => {
    const isDashboardActive = activeTab === 'dashboard';
    trigger("C2VM.TLE", "SetDashboardActive", isDashboardActive);
    return () => {
      // Cleanup: disable live updates when component unmounts
      trigger("C2VM.TLE", "SetDashboardActive", false);
    };
  }, [activeTab]);
  
  const mainPanelJson = useValue(mainPanel$);
  const mainPanelData = useMemo<MainPanelData | null>(() => {
    try {
      const data = JSON.parse(mainPanelJson || "{}");
      return { syncSettings: data.syncSettings || null, syncGroups: data.syncGroups || [] };
    } catch { return null; }
  }, [mainPanelJson]);
  
  const syncSettings = mainPanelData?.syncSettings ?? null;
  const syncGroups = mainPanelData?.syncGroups ?? [];
  
  const currentGroup = useMemo(() => {
    if (!syncSettings || syncSettings.syncGroupId === 0) return null;
    return syncGroups.find(g => g.groupId === syncSettings.syncGroupId) ?? null;
  }, [syncSettings, syncGroups]);
  
  const maxOffset = useMemo(() => {
    if (!syncSettings) return 300;
    if (currentGroup) return Math.max(currentGroup.baseCycleDuration * 3, 300);
    return Math.max(syncSettings.totalCycleDuration * 3, 300);
  }, [currentGroup, syncSettings]);
  
  const calculatedOffset = useMemo(() => {
    const dist = parseFloat(calcDistance) || 0;
    const speed = parseFloat(calcSpeed) || 50;
    if (speed <= 0) return 0;
    return Math.round(dist / (speed / 3.6));
  }, [calcDistance, calcSpeed]);
  
  // Game time to real time conversion (Cities: Skylines 2 runs at ~60x speed)
  // const GAME_TIME_FACTOR = 60;
  // const toRealSeconds = (gameSeconds: number) => Math.round(gameSeconds / GAME_TIME_FACTOR * 10) / 10;
  
  const isSyncing = syncSettings !== null && syncSettings.syncGroupId > 0 && syncSettings.useSyncedCycle;
  
  // Filtered groups for dropdown search
  const filteredGroups = useMemo(() => {
    if (!groupSearchQuery.trim()) return syncGroups;
    const query = groupSearchQuery.toLowerCase();
    return syncGroups.filter(g => g.groupName.toLowerCase().includes(query));
  }, [syncGroups, groupSearchQuery]);
  
  const showToast = useCallback((message: string, type: 'success' | 'info' | 'warning' = 'success') => {
    setToast({ message, type, visible: true });
    setTimeout(() => setToast(prev => ({ ...prev, visible: false })), 2500);
  }, []);
  
  // === HANDLERS ===
  const handleSelectGroup = useCallback((groupId: number) => {
    if (!syncSettings) return;
    // Auto-enable sync when adding to a group, disable when removing
    const shouldSync = groupId > 0;
    call("C2VM.TLE", "CallUpdateSyncSettings", JSON.stringify({
      syncGroupId: groupId, useSyncedCycle: shouldSync,
      cycleOffsetSeconds: syncSettings.cycleOffsetSeconds, syncPhaseIndex: syncSettings.syncPhaseIndex
    }));
    const groupName = syncGroups.find(g => g.groupId === groupId)?.groupName || (getString(locale, "GWP_Group") || "Group");
    showToast(groupId === 0 ? (getString(locale, "GWP_RemovedFromGroup") || "Removed from group") : `${getString(locale, "GWP_AddedToGroup") || "Added to"} "${groupName}"`, 'success');
    setIsDropdownOpen(false);
    setGroupSearchQuery("");
  }, [syncSettings, syncGroups, showToast]);
  
  const handleToggleSynced = useCallback(() => {
    if (!syncSettings) return;
    const newState = !syncSettings.useSyncedCycle;
    call("C2VM.TLE", "CallUpdateSyncSettings", JSON.stringify({
      syncGroupId: syncSettings.syncGroupId, useSyncedCycle: newState,
      cycleOffsetSeconds: syncSettings.cycleOffsetSeconds, syncPhaseIndex: syncSettings.syncPhaseIndex
    }));
    showToast(newState ? (getString(locale, "GWP_SyncEnabled") || "Sync enabled!") : (getString(locale, "GWP_SyncDisabled") || "Sync disabled"), newState ? 'success' : 'info');
  }, [syncSettings, showToast]);
  
  const handleOffsetChange = useCallback((newOffset: number) => {
    // Update local state immediately for responsive UI
    setLocalOffsetValue(Math.round(newOffset));
  }, []);
  
  const handleOffsetChangeComplete = useCallback((newOffset: number) => {
    if (!syncSettings) return;
    // Save to backend when slider is released
    call("C2VM.TLE", "CallUpdateSyncSettings", JSON.stringify({
      syncGroupId: syncSettings.syncGroupId, useSyncedCycle: syncSettings.useSyncedCycle,
      cycleOffsetSeconds: Math.round(newOffset), syncPhaseIndex: syncSettings.syncPhaseIndex
    }));
    // Clear local override after save
    setLocalOffsetValue(null);
  }, [syncSettings]);
  
  // V281: handlePhaseIndexChange entfernt (Sync-Phase Selector entfernt)
  
  const handleApplyCalculatedOffset = useCallback(() => {
    handleOffsetChange(calculatedOffset);
    showToast(`${getString(locale, "GWP_OffsetSet") || "Offset set to"} ${calculatedOffset}s`, 'success');
  }, [handleOffsetChange, calculatedOffset, showToast]);
  
  const handleCreateGroup = useCallback(() => {
    call("C2VM.TLE", "CallCreateSyncGroup", JSON.stringify({ groupName: getString(locale, "NewGroupName") || "New Group" }));
    showToast(getString(locale, "GWP_NewGroupCreated") || "New group created!", 'success');
  }, [showToast]);
  
  const handleStartRename = useCallback((groupId?: number, groupName?: string) => {
    if (groupId !== undefined && groupName !== undefined) {
      setRenamingGroupId(groupId);
      setRenameValue(groupName);
    } else if (currentGroup) {
      setRenamingGroupId(currentGroup.groupId);
      setRenameValue(currentGroup.groupName);
    }
    setIsRenaming(true);
  }, [currentGroup]);
  
  const handleConfirmRename = useCallback(() => {
    if (!renameValue.trim() || renamingGroupId === null) return;
    call("C2VM.TLE", "CallRenameSyncGroup", JSON.stringify({ groupId: renamingGroupId, newName: renameValue.trim() }));
    setIsRenaming(false);
    setRenamingGroupId(null);
    showToast(`${getString(locale, "GWP_GroupRenamed") || "Group renamed to"} "${renameValue.trim()}"`, 'success');
  }, [renameValue, renamingGroupId, showToast]);
  
  const handleDeleteGroup = useCallback((groupId?: number) => {
    const targetId = groupId || currentGroup?.groupId;
    if (!targetId) return;
    call("C2VM.TLE", "CallDeleteSyncGroup", JSON.stringify({ groupId: targetId }));
    showToast(getString(locale, "GWP_GroupDeleted") || "Group deleted", 'warning');
  }, [currentGroup, showToast]);
  
  // V239: handleMoveIntersectionUp, handleMoveIntersectionDown, handleRemoveFromGroup entfernt
  // (Priorisierung via Offset, Entfernen via Dropdown "Keine Gruppe")
  
  if (!syncSettings) {
    return (
      <PanelContainer>
        <PanelHeader>
          <PanelTitle><TitleIcon>||</TitleIcon>Gruene Welle</PanelTitle>
          <CloseButton onClick={onClose}>X</CloseButton>
        </PanelHeader>
        <ContentArea>
          <EmptyState>
            <EmptyStateIcon>[!]</EmptyStateIcon>
            <EmptyStateText>{getString(locale, "GWP_SelectIntersection") || "Please select an intersection."}</EmptyStateText>
          </EmptyState>
        </ContentArea>
      </PanelContainer>
    );
  }
  
  return (
    <PanelContainer>
      {toast.message && (
        <ToastContainer $visible={toast.visible} $type={toast.type}>
          <ToastIcon>{toast.type === 'success' ? '✓' : toast.type === 'warning' ? '⚠' : 'ℹ'}</ToastIcon>
          {toast.message}
        </ToastContainer>
      )}
      
      <PanelHeader>
        <PanelTitle><TitleIcon>||</TitleIcon>{getString(locale, "GreenWave") || "Gruene Welle"}</PanelTitle>
        <CloseButton onClick={onClose}>X</CloseButton>
      </PanelHeader>
      
      <TabContainer>
        <Tab $active={activeTab === 'intersection'} onClick={() => setActiveTab('intersection')}>
          <TabIcon>⊕</TabIcon>{getString(locale, "GWP_TabIntersection") || "Intersection"}
        </Tab>
        <Tab $active={activeTab === 'groups'} onClick={() => setActiveTab('groups')}>
          <TabIcon>≡</TabIcon>{getString(locale, "GWP_TabGroups") || "Groups"}
        </Tab>
        <Tab $active={activeTab === 'dashboard'} onClick={() => setActiveTab('dashboard')}>
          <TabIcon>◉</TabIcon>{getString(locale, "GWP_TabDashboard") || "Dashboard"}
        </Tab>
      </TabContainer>
      
      <Scrollable style={{ height: "auto", maxHeight: "75vh", width: "100%" }} contentStyle={{ flex: 1, width: "100%" }}>
        <ContentArea>
          
          {/* === TAB 1: KREUZUNG === */}
          {activeTab === 'intersection' && (
            <>
              {/* Node ID Info Box */}
              {syncSettings && syncSettings.selectedEntityIndex !== undefined && (
                <div style={{display: 'flex', justifyContent: 'center', marginBottom: '12rem'}}>
                  <SyncInfoItem style={{minWidth: '140rem', cursor: 'default'}}>
                    <SyncInfoLabel>{getString(locale, "GWP_NodeId") || "Node ID (Intersection)"}</SyncInfoLabel>
                    <SyncInfoValue style={{fontSize: '1.3em'}}>{syncSettings.selectedEntityIndex}</SyncInfoValue>
                  </SyncInfoItem>
                </div>
              )}
              
              <SyncStatusBox $syncing={isSyncing}>
                <SyncStatusHeader>
                  <SyncStatusIcon $active={isSyncing} />
                  <SyncStatusText $active={isSyncing}>
                    {isSyncing ? (getString(locale, "GWP_SyncActive") || "Synchronization active") : (getString(locale, "GWP_NotSynced") || "Not synchronized")}
                  </SyncStatusText>
                </SyncStatusHeader>
                {isSyncing && currentGroup && (
                  <SyncInfoGrid>
                    <SyncInfoItem><SyncInfoLabel>{getString(locale, "GWP_Group") || "Group"}</SyncInfoLabel><SyncInfoValue>{currentGroup.groupName}</SyncInfoValue></SyncInfoItem>
                    <SyncInfoItem><SyncInfoLabel>{getString(locale, "GWP_Offset") || "Offset"}</SyncInfoLabel><SyncInfoValue>{syncSettings.cycleOffsetSeconds}s</SyncInfoValue></SyncInfoItem>
                    <SyncInfoItem><SyncInfoLabel>{getString(locale, "SyncPhase") || "Sync Phase"}</SyncInfoLabel><SyncInfoValue>#{syncSettings.syncPhaseIndex + 1}</SyncInfoValue></SyncInfoItem>
                  </SyncInfoGrid>
                )}
              </SyncStatusBox>
              
              <SectionDivider />
              
              <Section>
                <SectionTitle>{getString(locale, "GWP_AssignSyncGroup") || "Assign Sync Group"}</SectionTitle>
                <DropdownContainer>
                  <DropdownTrigger 
                    $isOpen={isDropdownOpen} 
                    $hasSelection={syncSettings.syncGroupId > 0}
                    onClick={() => setIsDropdownOpen(!isDropdownOpen)}
                  >
                    <DropdownTriggerText $hasSelection={syncSettings.syncGroupId > 0}>
                      {currentGroup ? currentGroup.groupName : (getString(locale, "GWP_NoGroupSelected") || "No group selected")}
                    </DropdownTriggerText>
                    <DropdownTriggerInfo>
                      {currentGroup && (
                        <DropdownTriggerBadge>{currentGroup.intersectionCount} {getString(locale, "Intersections") || "Intersections"}</DropdownTriggerBadge>
                      )}
                      <DropdownArrow $isOpen={isDropdownOpen}>▼</DropdownArrow>
                    </DropdownTriggerInfo>
                  </DropdownTrigger>
                  
                  {isDropdownOpen && (
                    <>
                      <DropdownOverlay onClick={() => setIsDropdownOpen(false)} />
                      <DropdownContent>
                        <DropdownSearch 
                          placeholder={getString(locale, "GWP_SearchGroups") || "Search groups..."} 
                          value={groupSearchQuery}
                          onChange={(e) => setGroupSearchQuery(e.target.value)}
                          autoFocus
                        />
                        <DropdownList>
                          <DropdownItem 
                            $selected={syncSettings.syncGroupId === 0}
                            $isNoGroup
                            onClick={() => handleSelectGroup(0)}
                          >
                            <DropdownItemName $selected={syncSettings.syncGroupId === 0}>{getString(locale, "GWP_NoGroup") || "No Group"}</DropdownItemName>
                          </DropdownItem>
                          {filteredGroups.map(group => (
                            <DropdownItem 
                              key={group.groupId}
                              $selected={group.groupId === syncSettings.syncGroupId}
                              onClick={() => handleSelectGroup(group.groupId)}
                            >
                              <DropdownItemName $selected={group.groupId === syncSettings.syncGroupId}>
                                {group.groupName}
                              </DropdownItemName>
                              <DropdownItemInfo>
                                <DropdownItemBadge>{group.intersectionCount}</DropdownItemBadge>
                                <span>{group.baseCycleDuration}s</span>
                              </DropdownItemInfo>
                            </DropdownItem>
                          ))}
                          {filteredGroups.length === 0 && groupSearchQuery && (
                            <DropdownItem $isNoGroup>
                              <DropdownItemName>{getString(locale, "GWP_NoMatches") || "No matches for"} "{groupSearchQuery}"</DropdownItemName>
                            </DropdownItem>
                          )}
                        </DropdownList>
                      </DropdownContent>
                    </>
                  )}
                </DropdownContainer>
                
                <ButtonRow>
                  <Button onClick={handleCreateGroup} style={{flex: 1}}>{getString(locale, "GWP_NewGroup") || "+ New Group"}</Button>
                  {currentGroup && !isRenaming && (
                    <SmallButton $variant="secondary" onClick={() => handleStartRename()}>{getString(locale, "GWP_Rename") || "Rename"}</SmallButton>
                  )}
                </ButtonRow>
                
                {isRenaming && currentGroup && renamingGroupId === currentGroup.groupId && (
                  <RenameContainer style={{marginTop: '10rem'}}>
                    <RenameInput value={renameValue} onChange={(e) => setRenameValue(e.target.value)} onKeyDown={(e) => e.key === 'Enter' && handleConfirmRename()} autoFocus />
                    <SmallButton onClick={handleConfirmRename} style={{flex: 'none', padding: '8rem 12rem'}}>OK</SmallButton>
                    <SmallButton $variant="secondary" onClick={() => { setIsRenaming(false); setRenamingGroupId(null); }} style={{flex: 'none', padding: '8rem 12rem'}}>X</SmallButton>
                  </RenameContainer>
                )}
              </Section>
              
              {syncSettings.syncGroupId > 0 && (
                <>
                  <SectionDivider />
                  <Section>
                    <SectionTitle>{getString(locale, "SyncSettings") || "Sync Settings"}</SectionTitle>
                    <SettingRow onClick={handleToggleSynced} style={{cursor: 'pointer'}}>
                      <SettingIconWrapper><Tooltip position="right" tooltip={<TooltipText>{getString(locale, "GWP_TT_EnableSync") || "Enables synchronization of this intersection with the group."}</TooltipText>}><TooltipIcon /></Tooltip></SettingIconWrapper>
                      <SettingLabel>{getString(locale, "GWP_EnableSync") || "Enable Sync"}</SettingLabel>
                      <Checkbox isChecked={syncSettings.useSyncedCycle} />
                    </SettingRow>
                    {/* V281: Sync-Phase Selector entfernt - immer Phase 1 */}
                    <SliderContainer>
                      <SliderHeader>
                        <SliderLabelWithIcon>
                          <Tooltip position="right" tooltip={<TooltipText>{getString(locale, "GWP_TT_CycleOffset") || "Time offset in seconds from the group base cycle."}</TooltipText>}><TooltipIcon /></Tooltip>
                          <span>{getString(locale, "GWP_CycleOffset") || "Cycle Offset"}</span>
                        </SliderLabelWithIcon>
                        <SliderValue>{localOffsetValue !== null ? localOffsetValue : syncSettings.cycleOffsetSeconds}s</SliderValue>
                      </SliderHeader>
                      <Range 
                        data={{ min: 0, max: Math.min(maxOffset, 60), step: 1, value: localOffsetValue !== null ? localOffsetValue : syncSettings.cycleOffsetSeconds }} 
                        onUpdate={handleOffsetChange}
                        onChange={handleOffsetChangeComplete}
                      />
                    </SliderContainer>
                  </Section>
                  <SectionDivider />
                  <Section>
                    <SectionTitle>{getString(locale, "OffsetCalculator") || "Offset Calculator"}</SectionTitle>
                    <CalculatorBox>
                      <CalculatorRow>
                        <CalculatorIconWrapper><Tooltip position="right" tooltip={<TooltipText>{getString(locale, "GWP_TT_Distance") || "Distance to the previous intersection in meters."}</TooltipText>}><TooltipIcon /></Tooltip></CalculatorIconWrapper>
                        <CalculatorLabel>{getString(locale, "Distance") || "Distance"}</CalculatorLabel>
                        <CalculatorInput type="number" value={calcDistance} onChange={(e) => setCalcDistance(e.target.value)} />
                        <CalculatorUnit>m</CalculatorUnit>
                      </CalculatorRow>
                      <CalculatorRow>
                        <CalculatorIconWrapper><Tooltip position="right" tooltip={<TooltipText>{getString(locale, "GWP_TT_Speed") || "Traffic speed on the road."}</TooltipText>}><TooltipIcon /></Tooltip></CalculatorIconWrapper>
                        <CalculatorLabel>{getString(locale, "Speed") || "Speed"}</CalculatorLabel>
                        <CalculatorInput type="number" value={calcSpeed} onChange={(e) => setCalcSpeed(e.target.value)} />
                        <CalculatorUnit>km/h</CalculatorUnit>
                      </CalculatorRow>
                      <CalculatorResult>
                        <CalculatorResultLabel>{getString(locale, "GWP_Recommended") || "Recommended:"}</CalculatorResultLabel>
                        <CalculatorResultValue>{calculatedOffset}s</CalculatorResultValue>
                        <ApplyButton onClick={handleApplyCalculatedOffset}>{getString(locale, "Apply") || "Apply"}</ApplyButton>
                      </CalculatorResult>
                    </CalculatorBox>
                  </Section>
                </>
              )}
              
              <SectionDivider />
              <Section>
                <SectionTitle>{getString(locale, "Information") || "Information"}</SectionTitle>
                <InfoCard>
                  <InfoRow>
                    <InfoLabelWithIcon><InfoIconWrapper><Tooltip position="right" tooltip={<TooltipText>{getString(locale, "GWP_TT_OwnCycle") || "Total duration of a traffic light cycle for this intersection in game time."}</TooltipText>}><TooltipIcon /></Tooltip></InfoIconWrapper><InfoLabel>{getString(locale, "OwnCycle") || "Own Cycle"}</InfoLabel></InfoLabelWithIcon>
                    <InfoValue>{syncSettings.totalCycleDuration}s</InfoValue>
                  </InfoRow>
                  {currentGroup && (
                    <InfoRow>
                      <InfoLabelWithIcon><InfoIconWrapper><Tooltip position="right" tooltip={<TooltipText>{getString(locale, "GWP_TT_Intersections") || "Number of intersections in this group."}</TooltipText>}><TooltipIcon /></Tooltip></InfoIconWrapper><InfoLabel>{getString(locale, "Intersections") || "Intersections"}</InfoLabel></InfoLabelWithIcon>
                      <InfoValueHighlight>{currentGroup.intersectionCount}</InfoValueHighlight>
                    </InfoRow>
                  )}
                </InfoCard>
                {/* V281: Phase hint removed - always sync to Phase 1 */}
                <SaveFeedback>
                  <SaveIcon>✓</SaveIcon>
                  {getString(locale, "GWP_AutoSave") || "Changes are saved automatically"}
                </SaveFeedback>
              </Section>
            </>
          )}
          
          {/* === TAB 2: GRUPPEN === */}
          {activeTab === 'groups' && (
            <>
              <Section>
                <SectionTitle>{getString(locale, "GWP_ManageGroups") || "Manage Sync Groups"}</SectionTitle>
                {syncGroups.length === 0 ? (
                  <EmptyState>
                    <EmptyStateIcon>📋</EmptyStateIcon>
                    <EmptyStateText>{getString(locale, "NoGroupsYet") || "No groups created yet"}</EmptyStateText>
                    <Button onClick={handleCreateGroup}>{getString(locale, "GWP_NewGroup") || "+ New Group"}</Button>
                  </EmptyState>
                ) : (
                  <div style={{display: 'flex', flexDirection: 'column', gap: '8rem'}}>
                    {syncGroups.map(group => (
                      <div 
                        key={group.groupId}
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'space-between',
                          padding: '10rem 12rem',
                          background: currentGroup?.groupId === group.groupId ? 'rgba(30, 144, 83, 0.15)' : 'rgba(0, 0, 0, 0.2)',
                          borderRadius: '6rem',
                          border: currentGroup?.groupId === group.groupId ? '1rem solid rgba(30, 144, 83, 0.4)' : '1rem solid rgba(255, 255, 255, 0.05)'
                        }}
                      >
                        <div style={{display: 'flex', alignItems: 'center', gap: '10rem', flex: 1}}>
                          <div style={{
                            width: '8rem', 
                            height: '8rem', 
                            borderRadius: '50%', 
                            background: WERDER_GREEN_LIGHT
                          }} />
                          {isRenaming && renamingGroupId === group.groupId ? (
                            <div style={{display: 'flex', alignItems: 'center', gap: '6rem'}}>
                              <input 
                                value={renameValue} 
                                onChange={(e) => setRenameValue(e.target.value)} 
                                onKeyDown={(e) => e.key === 'Enter' && handleConfirmRename()} 
                                autoFocus 
                                style={{
                                  padding: '4rem 8rem', 
                                  fontSize: '0.85em', 
                                  width: '120rem',
                                  background: 'rgba(0,0,0,0.3)',
                                  border: '1rem solid rgba(255,255,255,0.2)',
                                  borderRadius: '4rem',
                                  color: 'var(--textColor)'
                                }} 
                              />
                              <span onClick={handleConfirmRename} style={{cursor: 'pointer', padding: '2rem 6rem', background: WERDER_GREEN, borderRadius: '4rem', fontSize: '0.75em'}}>OK</span>
                              <span onClick={() => { setIsRenaming(false); setRenamingGroupId(null); }} style={{cursor: 'pointer', padding: '2rem 6rem', background: 'rgba(255,100,100,0.3)', borderRadius: '4rem', fontSize: '0.75em'}}>X</span>
                            </div>
                          ) : (
                            <span style={{fontWeight: 'bold', fontSize: '0.9em'}}>{group.groupName}</span>
                          )}
                        </div>
                        
                        <div style={{display: 'flex', alignItems: 'center', gap: '8rem'}}>
                          <span style={{fontSize: '0.75em', color: 'var(--textColorDim)', padding: '3rem 8rem', background: 'rgba(255,255,255,0.05)', borderRadius: '4rem'}}>
                            {group.intersectionCount} Krz.
                          </span>
                          
                          {!isRenaming && (
                            <div style={{display: 'flex', gap: '4rem'}}>
                              <SmallButton 
                                $variant="secondary"
                                onClick={() => handleStartRename(group.groupId, group.groupName)} 
                                style={{padding: '4rem 8rem', fontSize: '0.7em', minWidth: 'auto'}}
                              >
                                Edit
                              </SmallButton>
                              {group.intersectionCount === 0 && (
                                <SmallButton 
                                  $variant="danger"
                                  onClick={() => handleDeleteGroup(group.groupId)} 
                                  style={{padding: '4rem 8rem', fontSize: '0.7em', minWidth: 'auto'}}
                                >
                                  Del
                                </SmallButton>
                              )}
                            </div>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
                {syncGroups.length > 0 && (
                  <Button onClick={handleCreateGroup} style={{marginTop: '12rem', width: '100%'}}>
                    {getString(locale, "GWP_CreateNewGroup") || "+ Create New Group"}
                  </Button>
                )}
              </Section>
              <SectionDivider />
              <HintBox>
                <HintIcon>*</HintIcon>
                {getString(locale, "GWP_GroupsHint") || "Groups without intersections can be deleted. Add intersections in the 'Intersection' tab."}
              </HintBox>
            </>
          )}
          
          {/* === TAB 3: DASHBOARD === */}
          {activeTab === 'dashboard' && (
            <>
              {syncGroups.length === 0 ? (
                <EmptyState>
                  <EmptyStateIcon>≡</EmptyStateIcon>
                  <EmptyStateText>{getString(locale, "NoGroupsYet") || "No groups created yet"}</EmptyStateText>
                  <Button onClick={handleCreateGroup}>{getString(locale, "GWP_NewGroup") || "+ New Group"}</Button>
                </EmptyState>
              ) : !currentGroup ? (
                <EmptyState>
                  <EmptyStateIcon>→</EmptyStateIcon>
                  <EmptyStateText>{getString(locale, "GWP_SelectGroupHint") || "Select a group in the 'Intersection' tab to see details."}</EmptyStateText>
                </EmptyState>
              ) : currentGroup.intersectionCount === 0 ? (
                <Section>
                  <DashboardGroupHeader>
                    <DashboardGroupName>
                      <LiveIndicator />
                      {currentGroup.groupName}
                    </DashboardGroupName>
                    <DashboardMetaBadge>{getString(locale, "GWP_NoIntersections") || "No intersections"}</DashboardMetaBadge>
                  </DashboardGroupHeader>
                  <HintBox>
                    <HintIcon>i</HintIcon>
                    {getString(locale, "GWP_AddIntersectionsHint") || "This group has no intersections yet. Go to the 'Intersection' tab and add intersections!"}
                  </HintBox>
                </Section>
              ) : (
                <>
                  {/* Gruppen-Header */}
                  <DashboardGroupHeader>
                    <DashboardGroupName>
                      <LiveIndicator />
                      {currentGroup.groupName}
                    </DashboardGroupName>
                    <DashboardGroupMeta>
                      <DashboardMetaBadge $highlight>
                        <MetaIcon>⊕</MetaIcon>{currentGroup.intersectionCount}
                      </DashboardMetaBadge>
                      {/* V281: baseCycleDuration Badge entfernt */}
                    </DashboardGroupMeta>
                  </DashboardGroupHeader>
                  
                  {/* Kreuzungs-Karten */}
                  {currentGroup.intersections && currentGroup.intersections.map((intersection, idx) => (
                    <div key={idx}>
                      <DashboardIntersectionCard $isActive={intersection.isSyncPhaseActive}>
                        <DashboardCardTop>
                          {/* V239: Move Buttons entfernt - Priorisierung erfolgt über Offset */}
                          
                          <DashboardCardTitle>
                            <DashboardCardIndex>{intersection.index}</DashboardCardIndex>
                            <DashboardCardInfo>
                              <DashboardCardName>
                                {`${getString(locale, "GWP_DB_Intersection") || "Intersection"} #${intersection.index}`}
                                {idx === 0 && <DashboardReferenceTag>{getString(locale, "GWP_DB_Reference") || "REFERENCE"}</DashboardReferenceTag>}
                              </DashboardCardName>
                              <DashboardCardSubtitle>{`${intersection.phaseCount} ${getString(locale, "GWP_DB_Phases") || "Phases"} • ${intersection.cycleDuration}s`}</DashboardCardSubtitle>
                            </DashboardCardInfo>
                          </DashboardCardTitle>
                          <DashboardCardBadges>
                            <DashboardStatusBadge $active={intersection.isSyncPhaseActive} $type="phase">
                              {intersection.isSyncPhaseActive ? `● ${getString(locale, "GWP_DB_Green") || "GREEN"}` : `Phase ${intersection.currentPhase + 1}`}
                            </DashboardStatusBadge>
                            <DashboardStatusBadge $active={intersection.isSynced} $type="sync">
                              {intersection.isSynced ? (getString(locale, "GWP_DB_SyncOn") || "SYNC") : (getString(locale, "GWP_DB_SyncOff") || "OFF")}
                            </DashboardStatusBadge>
                          </DashboardCardBadges>
                        </DashboardCardTop>
                        
                        <DashboardProgressSection>
                          <DashboardProgressHeader>
                            <DashboardProgressLabel>
                              <Tooltip position="right" tooltip={<TooltipText>{getString(locale, "GWP_DB_PhaseProgressTooltip") || "Left: Minimum duration (phase must run at least this long). Blinking: Phase is ready to switch and waiting for optimal moment."}</TooltipText>}>
                                <span style={{cursor: 'help'}}>{`Phase ${intersection.currentPhase + 1} / ${intersection.phaseCount}`}</span>
                              </Tooltip>
                            </DashboardProgressLabel>
                            <DashboardProgressValue $isGreen={intersection.isSyncPhaseActive}>
                              {intersection.isSyncPhaseActive 
                                ? (intersection.progress >= 50 ? (getString(locale, "GWP_DB_SyncWaiting") || "Sync active - waiting") : (getString(locale, "GWP_DB_GreenWaveActive") || "Green Wave active!"))
                                : (intersection.progress >= 50 ? (getString(locale, "GWP_DB_ReadyToSwitch") || "Ready to switch") : `${Math.round(intersection.progress * 2)}%`)}
                            </DashboardProgressValue>
                          </DashboardProgressHeader>
                          <DashboardProgressBarContainer>
                            <DashboardProgressBar>
                              <DashboardProgressFill 
                                $progress={intersection.progress} 
                                $isGreen={intersection.isSyncPhaseActive}
                              />
                              <DashboardProgressOverflow 
                                $isGreen={intersection.isSyncPhaseActive}
                                $visible={intersection.progress >= 50}
                              />
                              <DashboardProgressMinMarker />
                            </DashboardProgressBar>
                            <DashboardProgressLabels>
                              <DashboardProgressLabelLeft>0%</DashboardProgressLabelLeft>
                              <DashboardProgressLabelCenter>
                                <span>50%</span>
                                <Tooltip position="bottom" tooltip={<TooltipText>{getString(locale, "GWP_DB_MinDurationTooltip") || "At 50% the minimum duration is reached. The phase can switch anytime from here. The blinking area shows that it's waiting for an optimal switch moment."}</TooltipText>}>
                                  <DashboardProgressInfoIcon>i</DashboardProgressInfoIcon>
                                </Tooltip>
                              </DashboardProgressLabelCenter>
                              <DashboardProgressLabelRight>Max</DashboardProgressLabelRight>
                            </DashboardProgressLabels>
                          </DashboardProgressBarContainer>
                        </DashboardProgressSection>
                        
                        {/* Sync Stats Row */}
                        <DashboardStatsRow>
                          <DashboardCompactStat>
                            <DashboardCompactLabel>{getString(locale, "GWP_DB_Offset") || "OFFSET"}</DashboardCompactLabel>
                            <DashboardCompactValue>{intersection.offset}s</DashboardCompactValue>
                          </DashboardCompactStat>
                          <DashboardCompactStat>
                            <DashboardCompactLabel>SYNC</DashboardCompactLabel>
                            <DashboardCompactValue>#{intersection.syncPhase + 1}</DashboardCompactValue>
                          </DashboardCompactStat>
                          <DashboardCompactStat>
                            <DashboardCompactLabel>{getString(locale, "GWP_DB_Cycle") || "CYCLE"}</DashboardCompactLabel>
                            <DashboardCompactValue>{intersection.cycleDuration}s</DashboardCompactValue>
                          </DashboardCompactStat>
                        </DashboardStatsRow>
                        
                        {/* Extended Stats Row */}
                        {(intersection.edgeCount !== undefined && intersection.edgeCount > 0) && (
                          <DashboardStatsRow>
                            <DashboardCompactStat>
                              <DashboardCompactLabel>{getString(locale, "GWP_DB_Roads") || "ROADS"}</DashboardCompactLabel>
                              <DashboardSmallStatValue>{intersection.edgeCount}</DashboardSmallStatValue>
                            </DashboardCompactStat>
                            <DashboardCompactStat>
                              <DashboardCompactLabel>{getString(locale, "GWP_DB_Lanes") || "LANES"}</DashboardCompactLabel>
                              <DashboardSmallStatValue>{intersection.totalLanes || 0}</DashboardSmallStatValue>
                            </DashboardCompactStat>
                            <DashboardCompactStat>
                              <DashboardCompactLabel>&lt; | &gt;</DashboardCompactLabel>
                              <DashboardSmallStatValue>{`${intersection.leftLanes || 0}/${intersection.straightLanes || 0}/${intersection.rightLanes || 0}`}</DashboardSmallStatValue>
                            </DashboardCompactStat>
                            <DashboardCompactStat>
                              <DashboardCompactLabel>{getString(locale, "GWP_DB_Pedestrians") || "PED."}</DashboardCompactLabel>
                              <DashboardSmallStatValue>{intersection.pedestrianLanes || 0}</DashboardSmallStatValue>
                            </DashboardCompactStat>
                          </DashboardStatsRow>
                        )}
                        
                        {/* V239: Remove Button entfernt - Entfernen erfolgt über Dropdown "Keine Gruppe" */}
                      </DashboardIntersectionCard>
                      
                      {/* Offset-Verbindung zur nächsten Kreuzung */}
                      {idx < (currentGroup.intersections?.length || 0) - 1 && (
                        <DashboardOffsetConnector>
                          <DashboardOffsetLine />
                          <DashboardOffsetValue>
                            +{(currentGroup.intersections?.[idx + 1]?.offset || 0) - intersection.offset}s {getString(locale, "GWP_DB_OffsetDelta") || "Offset"}
                          </DashboardOffsetValue>
                          <DashboardOffsetLine />
                        </DashboardOffsetConnector>
                      )}
                    </div>
                  ))}
                  
                  {/* Zusammenfassung */}
                  <LiveSummary>
                    <LiveSummaryItem>
                      <LiveSummaryValue>{currentGroup.baseCycleDuration}s</LiveSummaryValue>
                      <LiveSummaryLabel>{getString(locale, "GWP_DB_BaseCycle") || "Base Cycle"}</LiveSummaryLabel>
                    </LiveSummaryItem>
                    <LiveSummaryDivider />
                    <LiveSummaryItem>
                      <LiveSummaryValue>
                        {currentGroup.intersections?.filter(i => i.isSyncPhaseActive).length || 0}/{currentGroup.intersections?.length || 0}
                      </LiveSummaryValue>
                      <LiveSummaryLabel>{getString(locale, "GWP_DB_GreenWaveLabel") || "Green Wave"}</LiveSummaryLabel>
                    </LiveSummaryItem>
                    <LiveSummaryDivider />
                    <LiveSummaryItem>
                      <LiveSummaryValue>
                        {currentGroup.intersections?.filter(i => i.isSynced).length || 0}/{currentGroup.intersections?.length || 0}
                      </LiveSummaryValue>
                      <LiveSummaryLabel>{getString(locale, "GWP_DB_SyncActiveLabel") || "Sync Active"}</LiveSummaryLabel>
                    </LiveSummaryItem>
                  </LiveSummary>
                  
                  <HintBox style={{marginTop: '16rem', backgroundColor: 'rgba(78, 184, 110, 0.1)'}}>
                    <HintIcon>i</HintIcon>
                    {getString(locale, "GWP_DB_OffsetHint") || "The intersection with the lowest offset is the reference. Adjust offsets in the intersection settings."}
                  </HintBox>
                </>
              )}
            </>
          )}
          
        </ContentArea>
      </Scrollable>
    </PanelContainer>
  );
}
