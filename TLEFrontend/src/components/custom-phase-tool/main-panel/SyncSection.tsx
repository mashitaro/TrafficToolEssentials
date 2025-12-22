import { useContext, useMemo, useCallback } from "react";
import styled from "styled-components";

import { bindValue, useValue, call } from "cs2/api";

import { LocaleContext } from "@/context";
import { getString } from "@/localisations";

import Collapsible from "@/components/common/collapsible";
import Tooltip from "@/components/common/tooltip";

// === WERDER BREMEN COLORS ===
const WERDER_GREEN = "rgba(30, 144, 83, 1)";
const WERDER_GREEN_LIGHT = "rgba(40, 170, 100, 1)";
const WERDER_GREEN_DARK = "rgba(20, 120, 70, 1)";
const WERDER_GREEN_GLOW = "rgba(30, 144, 83, 0.6)";

// === TYPES ===
interface SyncSettings {
  syncGroupId: number;
  useSyncedCycle: boolean;
  cycleOffsetSeconds: number;
  totalCycleDuration: number;
  useSequentialPhases: boolean;
  syncPhaseIndex: number;
}

interface SyncGroup {
  groupId: number;
  groupName: string;
  baseCycleDuration: number;
  intersectionCount: number;
}

interface MainPanelData {
  syncSettings: SyncSettings | null;
  syncGroups: SyncGroup[];
}

interface SyncSectionProps {
  onOpenPanel: () => void;
  isPanelOpen?: boolean;
}

// === STYLED COMPONENTS ===
const InfoRow = styled.div`
  color: var(--textColorDim);
  font-size: 0.85em;
  padding: 4rem 8rem;
`;

const InfoLabel = styled.span`
  color: var(--textColorDim);
`;

const InfoValue = styled.span`
  color: var(--textColor);
  font-weight: bold;
`;

const GroupDisplay = styled.div`
  color: var(--textColor);
  font-size: 1em;
  padding: 8rem;
  text-align: center;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 4rem;
  margin: 4rem 8rem;
`;

const StatusIndicator = styled.span<{$active: boolean}>`
  /* V232: COHTML doesn't support inline-block, use block with float */
  display: block;
  float: left;
  width: 8rem;
  height: 8rem;
  border-radius: 50%;
  background: ${props => props.$active ? WERDER_GREEN : "rgba(255, 255, 255, 0.3)"};
  margin-right: 6rem;
  margin-top: 3rem;
  box-shadow: ${props => props.$active ? `0 0 6rem ${WERDER_GREEN_GLOW}` : "none"};
`;

// Normal Button (Panel geschlossen)
const GreenButton = styled.div`
  background: linear-gradient(180deg, ${WERDER_GREEN} 0%, ${WERDER_GREEN_DARK} 100%);
  color: white;
  font-weight: bold;
  padding: 10rem 16rem;
  margin: 8rem;
  border-radius: 6rem;
  text-align: center;
  cursor: pointer;
  box-shadow: 0 2rem 8rem rgba(30, 144, 83, 0.3);
  transition: all 0.15s ease;
  
  &:hover {
    background: linear-gradient(180deg, ${WERDER_GREEN_LIGHT} 0%, ${WERDER_GREEN} 100%);
    box-shadow: 0 4rem 12rem rgba(30, 144, 83, 0.5);
    transform: translateY(-1rem);
  }
  
  &:active {
    transform: translateY(0);
  }
`;

// Glowing Button (Panel offen) - NO ANIMATION, CS2 doesnt support keyframes!
const GreenButtonActive = styled.div`
  background: linear-gradient(180deg, ${WERDER_GREEN_LIGHT} 0%, ${WERDER_GREEN} 100%);
  color: white;
  font-weight: bold;
  padding: 10rem 16rem;
  margin: 8rem;
  border-radius: 6rem;
  text-align: center;
  cursor: pointer;
  box-shadow: 0 0 12rem ${WERDER_GREEN_GLOW}, inset 0 0 6rem rgba(255, 255, 255, 0.1);
  border: 2rem solid rgba(255, 255, 255, 0.3);
  
  &:hover {
    background: linear-gradient(180deg, rgba(60, 190, 120, 1) 0%, ${WERDER_GREEN_LIGHT} 100%);
  }
`;

// Disabled Button (not in custom phase mode)
const GreenButtonDisabled = styled.div`
  background: linear-gradient(180deg, rgba(80, 80, 80, 1) 0%, rgba(60, 60, 60, 1) 100%);
  color: rgba(255, 255, 255, 0.4);
  font-weight: bold;
  padding: 10rem 16rem;
  margin: 8rem;
  border-radius: 6rem;
  text-align: center;
  cursor: not-allowed;
  box-shadow: none;
`;

const DisabledHint = styled.div`
  color: var(--textColorDim);
  font-size: 0.75em;
  text-align: center;
  padding: 0 8rem 8rem;
  font-style: italic;
`;

// === PHASE MODE SECTION STYLES ===
const PhaseModeContainer = styled.div`
  margin: 8rem;
  padding: 8rem;
  background: rgba(255, 255, 255, 0.03);
  border-radius: 6rem;
  border: 1rem solid rgba(255, 255, 255, 0.08);
`;

const PhaseModeOption = styled.div<{$active: boolean, $disabled?: boolean}>`
  display: flex;
  align-items: center;
  padding: 6rem 10rem;
  margin: 4rem 0;
  border-radius: 4rem;
  cursor: ${props => props.$disabled ? 'not-allowed' : 'pointer'};
  background: ${props => props.$active ? 'rgba(30, 144, 83, 0.2)' : 'rgba(255, 255, 255, 0.05)'};
  border: 1rem solid ${props => props.$active ? 'rgba(30, 144, 83, 0.5)' : 'rgba(255, 255, 255, 0.1)'};
  opacity: ${props => props.$disabled ? 0.5 : 1};
  transition: all 0.15s ease;
  
  &:hover {
    background: ${props => props.$disabled ? 'rgba(255, 255, 255, 0.05)' : (props.$active ? 'rgba(30, 144, 83, 0.25)' : 'rgba(255, 255, 255, 0.1)')};
  }
`;

const PhaseModeRadio = styled.div<{$active: boolean}>`
  width: 14rem;
  height: 14rem;
  border-radius: 50%;
  border: 2rem solid ${props => props.$active ? WERDER_GREEN : 'rgba(255, 255, 255, 0.4)'};
  margin-right: 10rem;
  display: flex;
  align-items: center;
  justify-content: center;
  
  &::after {
    content: '';
    width: 8rem;
    height: 8rem;
    border-radius: 50%;
    background: ${props => props.$active ? WERDER_GREEN : 'transparent'};
  }
`;

const PhaseModeLabel = styled.div`
  color: var(--textColor);
  font-size: 0.85em;
  flex: 1;
`;

const PhaseModeHint = styled.div`
  color: var(--textColorDim);
  font-size: 0.7em;
  margin-top: 2rem;
`;

const TooltipText = styled.div`
  max-width: 250rem;
  line-height: 1.4;
  font-size: 0.85em;
`;

const GreenWaveForcedHint = styled.div`
  color: ${WERDER_GREEN};
  font-size: 0.7em;
  text-align: center;
  padding: 4rem 8rem;
  font-style: italic;
`;

// === MAIN COMPONENT ===
export default function SyncSection({ onOpenPanel, isPanelOpen = false }: SyncSectionProps) {
  const locale = useContext(LocaleContext);
  
  // Get data from backend
  const mainPanelJson = useValue(bindValue<string>("C2VM.TLE", "GetMainPanel", "{}"));
  
  // Parse data
  const { syncSettings, syncGroups } = useMemo<MainPanelData>(() => {
    try {
      const data = JSON.parse(mainPanelJson || "{}");
      return {
        syncSettings: data.syncSettings || null,
        syncGroups: data.syncGroups || []
      };
    } catch {
      return { syncSettings: null, syncGroups: [] };
    }
  }, [mainPanelJson]);
  
  // Find current group
  const currentGroup = useMemo(() => {
    if (!syncSettings || !syncGroups || syncSettings.syncGroupId === 0) return null;
    return syncGroups.find(g => g.groupId === syncSettings.syncGroupId) || null;
  }, [syncGroups, syncSettings]);
  
  // Check if custom phase mode is active (syncSettings is only set in custom phase mode)
  const isCustomPhaseMode = syncSettings !== null;
  
  // Handler for phase mode change
  const handlePhaseModeChange = useCallback((useSequential: boolean) => {
    if (!syncSettings) return;
    
    // Don't allow change if Green Wave is active (forces sequential)
    if (syncSettings.syncGroupId > 0 && syncSettings.useSyncedCycle) return;
    
    call("C2VM.TLE", "CallUpdateSyncSettings", JSON.stringify({
      syncGroupId: syncSettings.syncGroupId,
      useSyncedCycle: syncSettings.useSyncedCycle,
      cycleOffsetSeconds: syncSettings.cycleOffsetSeconds,
      syncPhaseIndex: syncSettings.syncPhaseIndex || 0,
      useSequentialPhases: useSequential
    }));
  }, [syncSettings]);
  
  // Check if Green Wave forces sequential mode
  const isGreenWaveForced = (syncSettings?.syncGroupId ?? 0) > 0 && (syncSettings?.useSyncedCycle ?? false);
  
  if (!isCustomPhaseMode) {
    // Not in custom phase mode - show disabled button with hint
    return (
      <Collapsible 
        title={getString(locale, "GreenWave") || "Gruene Welle"} 
        defaultOpen={true}
      >
        <GreenButtonDisabled>
          {getString(locale, "OpenGreenWavePanel") || "Gruene Welle oeffnen"}
        </GreenButtonDisabled>
        <DisabledHint>
          Waehle zuerst "Eigene Phasen" um die Gruene Welle zu nutzen.
        </DisabledHint>
      </Collapsible>
    );
  }
  
  const groupDisplayName = currentGroup 
    ? `${currentGroup.groupName} (${currentGroup.intersectionCount})`
    : (getString(locale, "NoSyncGroup") || "Keine Gruppe");
  
  const isSyncActive = syncSettings.syncGroupId > 0 && syncSettings.useSyncedCycle;
  
  // Choose button based on panel state
  const ButtonComponent = isPanelOpen ? GreenButtonActive : GreenButton;
  
  return (
    <>
      {/* PHASE MODE SECTION */}
      <Collapsible 
        title={getString(locale, "PhaseMode") || "Phasen-Modus"} 
        defaultOpen={true}
      >
        <PhaseModeContainer>
          {/* Legacy Mode Option */}
          <Tooltip position="right" tooltip={
            <TooltipText>
              {getString(locale, "LegacyModeTooltip") || "Phasen werden nach Prioritaet geschaltet - basierend auf wartenden Fahrzeugen und Fussgaengern. Standard-Spielverhalten."}
            </TooltipText>
          }>
            <PhaseModeOption 
              $active={!syncSettings.useSequentialPhases} 
              $disabled={isGreenWaveForced}
              onClick={() => !isGreenWaveForced && handlePhaseModeChange(false)}
            >
              <PhaseModeRadio $active={!syncSettings.useSequentialPhases} />
              <div>
                <PhaseModeLabel>{getString(locale, "LegacyMode") || "Standard (Prioritaet)"}</PhaseModeLabel>
                <PhaseModeHint>{getString(locale, "LegacyModeHint") || "Naechste Phase nach Verkehrsaufkommen"}</PhaseModeHint>
              </div>
            </PhaseModeOption>
          </Tooltip>
          
          {/* Sequential Mode Option */}
          <Tooltip position="right" tooltip={
            <TooltipText>
              {getString(locale, "SequentialModeTooltip") || "Phasen laufen der Reihe nach: 1 → 2 → 3 → 4 → 1... Feste, vorhersagbare Reihenfolge. Erforderlich fuer Gruene Welle."}
            </TooltipText>
          }>
            <PhaseModeOption 
              $active={syncSettings.useSequentialPhases} 
              $disabled={isGreenWaveForced}
              onClick={() => !isGreenWaveForced && handlePhaseModeChange(true)}
            >
              <PhaseModeRadio $active={syncSettings.useSequentialPhases} />
              <div>
                <PhaseModeLabel>{getString(locale, "SequentialMode") || "Sequentiell (Reihenfolge)"}</PhaseModeLabel>
                <PhaseModeHint>{getString(locale, "SequentialModeHint") || "Phasen 1 → 2 → 3 → 4 → 1..."}</PhaseModeHint>
              </div>
            </PhaseModeOption>
          </Tooltip>
          
          {isGreenWaveForced && (
            <GreenWaveForcedHint>
              {getString(locale, "GreenWaveForcesSequential") || "Gruene Welle erfordert sequentiellen Modus"}
            </GreenWaveForcedHint>
          )}
        </PhaseModeContainer>
      </Collapsible>
      
      {/* GREEN WAVE SECTION */}
      <Collapsible 
        title={getString(locale, "GreenWave") || "Gruene Welle"} 
        defaultOpen={true}
      >
        {/* Group Display */}
        <GroupDisplay>
          <StatusIndicator $active={isSyncActive} />
          {groupDisplayName}
        </GroupDisplay>
        
        {/* Cycle Info */}
        <InfoRow>
          <InfoLabel>{getString(locale, "TotalCycleDuration") || "Gesamte Zyklusdauer"}: </InfoLabel>
          <InfoValue>{syncSettings.totalCycleDuration}s</InfoValue>
        </InfoRow>
        
        {/* Show offset if group selected */}
        {syncSettings.syncGroupId > 0 && (
          <InfoRow>
            <InfoLabel>{getString(locale, "CycleOffset") || "Offset"}: </InfoLabel>
            <InfoValue>{syncSettings.cycleOffsetSeconds}s</InfoValue>
          </InfoRow>
        )}
        
        {/* Green Button - glows when panel is open */}
        <ButtonComponent onClick={onOpenPanel}>
          {isPanelOpen 
            ? (getString(locale, "GreenWavePanelActive") || "Gruene Welle aktiv")
            : (getString(locale, "OpenGreenWavePanel") || "Gruene Welle oeffnen")
          }
        </ButtonComponent>
      </Collapsible>
    </>
  );
}
