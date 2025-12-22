import { CSSProperties, useContext, useEffect, useState, useCallback } from "react";
import styled from "styled-components";

import { bindValue, call, useValue } from "cs2/api";

import { MainPanelState } from "@/constants";
import { LocaleContext } from "@/context";
import { getString } from "@/localisations";

import Button from "@/components/common/button";
import Scrollable from "@/components/common/scrollable";
import Tooltip from "@/components/common/tooltip";
import TooltipContainer from "@/components/common/tooltip-container";
import Divider from "@/components/main-panel/items/divider";
import Row from "@/components/main-panel/items/row";

import Item from "./item";
// ManualControlPanel removed - now only used internally by Green Wave sync
import SubPanel from "./sub-panel";
import SyncSection from "./SyncSection";

// === STATIC STYLES ONLY ===
const Container = styled.div`
  width: 32em;
  display: flex;
  flex-direction: row;
  flex-grow: 1;
  flex-shrink: 1;
  flex-basis: auto;
`;

const LeftPanelContainer = styled.div`
  width: 15em;
  max-width: 15em;
  background-color: var(--panelColorNormal);
  backdrop-filter: var(--panelBlur);
  color: var(--textColor);
  flex: 1;
  position: relative;
  padding: 0.25em;
`;

const RightPanelContainer = styled.div`
  width: 17em;
  max-width: 17em;
  background-color: var(--sectionBackgroundColor);
  backdrop-filter: var(--panelBlur);
  flex: 1;
  position: relative;
  padding: 0.25em;
`;

const SelectionHeader = styled.div`
  display: flex;
  flex-direction: column;
  gap: 0.4em;
  padding: 0.4em;
  margin-bottom: 0.25em;
  background: rgba(0, 0, 0, 0.15);
  border-radius: 0.25em;
`;

const IconButtonRow = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: center;
  gap: 0.8em;
`;

const SelectionBottomRow = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin-top: 0.15em;
`;

const IconButton = styled.div<{$disabled?: boolean}>`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2.5em;
  height: 2.5em;
  border-radius: 0.3em;
  cursor: ${props => props.$disabled ? "not-allowed" : "pointer"};
  opacity: ${props => props.$disabled ? 0.4 : 1};
  background: ${props => props.$disabled ? "rgba(255, 255, 255, 0.05)" : "rgba(255, 255, 255, 0.12)"};
  border: 1px solid rgba(255, 255, 255, 0.2);
  
  &:hover {
    background: ${props => props.$disabled ? "rgba(255, 255, 255, 0.05)" : "rgba(255, 255, 255, 0.25)"};
  }
`;

const ItemContainerStyle: CSSProperties = {
  display: "flex",
  flexDirection: "column",
  flex: 1,
};

const checkboxStyle: CSSProperties = {
  width: "1.15em",
  height: "1.15em",
  border: "2px solid rgba(255, 255, 255, 0.5)",
  borderRadius: "0.2em",
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  cursor: "pointer",
  background: "transparent",
};

const checkboxCheckedStyle: CSSProperties = {
  ...checkboxStyle,
  background: "rgba(80, 160, 220, 0.4)",
  borderColor: "rgba(80, 170, 220, 0.8)",
};

const labelStyle: CSSProperties = {
  color: "var(--textColor)",
  fontSize: "0.9em",
  marginLeft: "0.5em",
};

const countBadgeStyle: CSSProperties = {
  background: "rgba(80, 160, 220, 0.4)",
  color: "var(--textColor)",
  padding: "0.15em 0.5em",
  borderRadius: "0.2em",
  fontSize: "0.8em",
  marginLeft: "0.5em",
};

// V240: Delete Confirmation Dialog
const DialogOverlay = styled.div`
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
`;

const DialogBox = styled.div`
  background: var(--panelColorNormal);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 0.5em;
  padding: 1.2em;
  min-width: 18em;
  max-width: 22em;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
`;

const DialogTitle = styled.h3`
  color: var(--textColor);
  font-size: 1.1em;
  font-weight: 600;
  margin: 0 0 0.8em 0;
  display: flex;
  align-items: center;
  gap: 0.5em;
`;

const DialogMessage = styled.p`
  color: rgba(255, 255, 255, 0.8);
  font-size: 0.9em;
  margin: 0 0 1.2em 0;
  line-height: 1.4;
`;

const DialogButtons = styled.div`
  display: flex;
  justify-content: flex-end;
  gap: 0.6em;
`;

const DialogButton = styled.button<{$danger?: boolean}>`
  padding: 0.5em 1em;
  border-radius: 0.3em;
  font-size: 0.9em;
  cursor: pointer;
  border: 1px solid rgba(255, 255, 255, 0.2);
  transition: background 0.15s ease;
  
  background: ${props => props.$danger 
    ? 'rgba(220, 80, 80, 0.8)' 
    : 'rgba(255, 255, 255, 0.1)'};
  color: var(--textColor);
  
  &:hover {
    background: ${props => props.$danger 
      ? 'rgba(220, 80, 80, 1)' 
      : 'rgba(255, 255, 255, 0.2)'};
  }
`;

const WarningIcon = () => (
  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#dc5050" strokeWidth="2">
    <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
    <line x1="12" y1="9" x2="12" y2="13"/>
    <line x1="12" y1="17" x2="12.01" y2="17"/>
  </svg>
);

// SVG Icons
const CopyIcon = () => (
  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2">
    <rect x="9" y="9" width="13" height="13" rx="2" ry="2"/>
    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>
  </svg>
);

const PasteIcon = () => (
  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2">
    <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/>
    <rect x="8" y="2" width="8" height="4" rx="1" ry="1"/>
  </svg>
);

const DeleteIcon = () => (
  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2">
    <polyline points="3 6 5 6 21 6"/>
    <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
  </svg>
);

const CheckIcon = () => (
  <svg viewBox="0 0 24 24" fill="rgba(80, 170, 220, 1)" style={{ width: "0.85em", height: "0.85em" }}>
    <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41L9 16.17z"/>
  </svg>
);

const MinusIcon = () => (
  <svg viewBox="0 0 24 24" fill="rgba(80, 170, 220, 1)" style={{ width: "0.85em", height: "0.85em" }}>
    <path d="M19 13H5v-2h14v2z"/>
  </svg>
);

// === Button Components ===
const AddButton = () => {
  const data: MainPanelItemButton = {
    itemType: "button",
    type: "button",
    key: "add",
    value: "add",
    label: "Add",
    engineEventName: "C2VM.TLE.CallAddCustomPhase"
  };
  return <Row data={data}><Button {...data} /></Row>;
};

const BackButton = () => {
  const data: MainPanelItemButton = {
    itemType: "button",
    type: "button",
    key: "state",
    value: `${MainPanelState.Main}`,
    label: "Back",
    engineEventName: "C2VM.TLE.CallSetMainPanelState"
  };
  return <Row data={data}><Button {...data} /></Row>;
};

// === MAIN COMPONENT ===
export default function MainPanel(props: {
  items: MainPanelItem[];
  onOpenGreenWave?: () => void;
  isGreenWavePanelOpen?: boolean;
}) {
  const locale = useContext(LocaleContext);
  
  const [selectedIndices, setSelectedIndices] = useState<number[]>([]);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);  // V240: Delete confirmation dialog
  
  // Clipboard count from Backend Binding (persists across intersection changes!)
  const clipboardCount = useValue(bindValue<number>("C2VM.TLE", "GetClipboardCount", 0)) ?? 0;
  
  // Extract data from props
  let activeIndex = -1;
  let activeViewingIndex = -1;
  let activeItem: MainPanelItemCustomPhase | null = null;
  let currentItem: MainPanelItemCustomPhase | null = null;
  let currentSignalGroup = 0;
  let manualSignalGroup = 0;
  let length = 0;
  
  if (props.items.length > 0 && props.items[0].itemType == "customPhase") {
    activeIndex = props.items[0].activeIndex;
    activeViewingIndex = props.items[0].activeViewingIndex;
    currentSignalGroup = props.items[0].currentSignalGroup;
    manualSignalGroup = props.items[0].manualSignalGroup;
    length = props.items[0].length;
  }
  
  if (activeIndex >= 0 && props.items[activeIndex]?.itemType == "customPhase") {
    activeItem = props.items[activeIndex] as MainPanelItemCustomPhase;
  }
  
  if (manualSignalGroup > 0 && props.items[manualSignalGroup - 1]?.itemType == "customPhase") {
    currentItem = props.items[manualSignalGroup - 1] as MainPanelItemCustomPhase;
  } else if (activeViewingIndex >= 0 && props.items[activeViewingIndex]?.itemType == "customPhase") {
    currentItem = props.items[activeViewingIndex] as MainPanelItemCustomPhase;
  } else if (currentSignalGroup > 0 && props.items[currentSignalGroup - 1]?.itemType == "customPhase") {
    currentItem = props.items[currentSignalGroup - 1] as MainPanelItemCustomPhase;
  }
  
  // Reset selection when length changes
  useEffect(() => {
    setSelectedIndices([]);
  }, [length]);
  
  // Calculate selection state
  const phaseCount = props.items.filter(i => i.itemType === "customPhase").length;
  const allSelected = phaseCount > 0 && selectedIndices.length === phaseCount;
  const someSelected = selectedIndices.length > 0 && selectedIndices.length < phaseCount;
  const noneSelected = selectedIndices.length === 0;
  
  // Handlers
  const handleSelectAll = useCallback(() => {
    if (allSelected) {
      setSelectedIndices([]);
    } else {
      const indices: number[] = [];
      props.items.forEach(item => {
        if (item.itemType === "customPhase") indices.push(item.index);
      });
      setSelectedIndices(indices);
    }
  }, [allSelected, props.items]);
  
  const handleToggleSelect = useCallback((index: number) => {
    setSelectedIndices(prev => 
      prev.includes(index) ? prev.filter(i => i !== index) : [...prev, index]
    );
  }, []);
  
  const handleCopy = useCallback(() => {
    if (selectedIndices.length === 0) return;
    call("C2VM.TLE", "CallCopyPhases", JSON.stringify({ indices: selectedIndices }));
  }, [selectedIndices]);
  
  const handlePaste = useCallback(() => {
    if (clipboardCount <= 0) return;
    call("C2VM.TLE", "CallPastePhases", JSON.stringify({}));
  }, [clipboardCount]);
  
  // V240: Show confirmation dialog instead of deleting immediately
  const handleDelete = useCallback(() => {
    if (selectedIndices.length === 0) return;
    setShowDeleteConfirm(true);
  }, [selectedIndices]);
  
  // V240: Actually delete after confirmation
  const confirmDelete = useCallback(() => {
    call("C2VM.TLE", "CallDeletePhases", JSON.stringify({ indices: [...selectedIndices].sort((a, b) => b - a) }));
    setSelectedIndices([]);
    setShowDeleteConfirm(false);
  }, [selectedIndices]);
  
  // V240: Cancel delete
  const cancelDelete = useCallback(() => {
    setShowDeleteConfirm(false);
  }, []);
  
  const handleClearClipboard = useCallback(() => {
    call("C2VM.TLE", "CallClearPhaseClipboard", JSON.stringify({}));
  }, []);
  
  // handleManualControl removed - ManualControl is now only used internally by Green Wave sync
  
  // Localized strings
  const selectText = getString(locale, allSelected ? "DeselectAll" : "SelectAll") || "Alle auswählen";
  const copyTip = getString(locale, "CopyPhasesTooltip") || "Ausgewählte Phasen kopieren";
  const pasteTip = getString(locale, "PastePhasesTooltip") || "Phasen einfügen";
  const deleteTip = getString(locale, "DeleteSelectedPhasesTooltip") || "Ausgewählte Phasen löschen";
  const clearText = getString(locale, "ClearClipboard") || "Zwischenspeicher leeren";
  
  // V240: Delete confirmation strings
  const deleteConfirmTitle = getString(locale, "DeletePhaseConfirmTitle") || "Delete Phase(s)?";
  const deleteConfirmMessage = getString(locale, "DeletePhaseConfirmMessage") || "This action cannot be undone. The selected phase(s) will be permanently removed.";
  const deleteConfirmButton = getString(locale, "DeletePhaseConfirmButton") || "Delete";
  const cancelButton = getString(locale, "CancelButton") || "Cancel";
  
  return (
    <Container>
      <LeftPanelContainer>
        {/* Always show the normal editor - ManualControl is now only used internally by Green Wave */}
        <>
          <SelectionHeader>
            <IconButtonRow>
              {/* Copy Button with Tooltip */}
              <Tooltip position="bottom" tooltip={<TooltipContainer>{copyTip}</TooltipContainer>}>
                <IconButton $disabled={noneSelected} onClick={noneSelected ? undefined : handleCopy}>
                  <CopyIcon />
                </IconButton>
              </Tooltip>
              
              {/* Paste Button with Tooltip */}
              <Tooltip position="bottom" tooltip={<TooltipContainer>{pasteTip}</TooltipContainer>}>
                <IconButton $disabled={clipboardCount <= 0} onClick={clipboardCount <= 0 ? undefined : handlePaste}>
                  <PasteIcon />
                </IconButton>
              </Tooltip>
              
              {/* Delete Button with Tooltip */}
              <Tooltip position="bottom" tooltip={<TooltipContainer>{deleteTip}</TooltipContainer>}>
                <IconButton $disabled={noneSelected} onClick={noneSelected ? undefined : handleDelete}>
                  <DeleteIcon />
                </IconButton>
              </Tooltip>
            </IconButtonRow>
            
            <SelectionBottomRow>
              <div 
                style={allSelected || someSelected ? checkboxCheckedStyle : checkboxStyle}
                onClick={handleSelectAll}
              >
                {allSelected && <CheckIcon />}
                {someSelected && !allSelected && <MinusIcon />}
              </div>
              <span style={labelStyle}>{selectText}</span>
              {selectedIndices.length > 0 && (
                <span style={countBadgeStyle}>{selectedIndices.length}</span>
              )}
            </SelectionBottomRow>
          </SelectionHeader>
          
          <Scrollable style={{flex: 1}} contentStyle={ItemContainerStyle}>
            {props.items.map((item, idx) => 
              item.itemType == "customPhase" && (
                <Item 
                  key={idx}
                  data={item}
                  isSelected={selectedIndices.includes(item.index)}
                  onToggleSelect={() => handleToggleSelect(item.index)}
                  selectionMode={selectedIndices.length > 0}
                />
              )
            )}
          </Scrollable>
          
          {length > 0 && <Divider />}
          {length < 16 && <AddButton />}
          {/* ManualControl button hidden - now only used internally by Green Wave sync */}
          {clipboardCount > 0 && (
            <Row hoverEffect={true}>
              <Button label={clearText} onClick={handleClearClipboard} />
            </Row>
          )}
          <BackButton />
        </>
      </LeftPanelContainer>
      <RightPanelContainer>
        <Scrollable style={{flex: 1}} contentStyle={{flex: 1}} trackStyle={{marginLeft: "0.25em"}}>
          {activeItem && <SubPanel data={activeItem} />}
          {!activeItem && currentItem && <SubPanel data={currentItem} statisticsOnly={true} />}
          <Divider />
          <SyncSection 
            onOpenPanel={props.onOpenGreenWave || (() => {})} 
            isPanelOpen={props.isGreenWavePanelOpen || false} 
          />
        </Scrollable>
      </RightPanelContainer>
      
      {/* V240: Delete Confirmation Dialog */}
      {showDeleteConfirm && (
        <DialogOverlay onClick={cancelDelete}>
          <DialogBox onClick={(e) => e.stopPropagation()}>
            <DialogTitle>
              <WarningIcon />
              {deleteConfirmTitle}
            </DialogTitle>
            <DialogMessage>
              {selectedIndices.length === 1 
                ? deleteConfirmMessage
                : deleteConfirmMessage.replace("phase(s)", `${selectedIndices.length} phases`)}
            </DialogMessage>
            <DialogButtons>
              <DialogButton onClick={cancelDelete}>
                {cancelButton}
              </DialogButton>
              <DialogButton $danger onClick={confirmDelete}>
                {deleteConfirmButton}
              </DialogButton>
            </DialogButtons>
          </DialogBox>
        </DialogOverlay>
      )}
    </Container>
  );
}
