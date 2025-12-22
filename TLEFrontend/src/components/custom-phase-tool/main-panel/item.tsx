import { useContext, useEffect, useState, useCallback, memo, CSSProperties } from "react";
import styled from "styled-components";

import { call } from "cs2/api";

import { LocaleContext } from "@/context";
import { getString } from "@/localisations";

import Check from "@/components/common/icons/check";
import ChevronDown from "@/components/common/icons/chevron-down";
import ChevronUp from "@/components/common/icons/chevron-up";
import Delete from "@/components/common/icons/delete";
import Tune from "@/components/common/icons/tune";
import Visibility from "@/components/common/icons/visibility";
import VisibilityOff from "@/components/common/icons/visibility-off";

import ItemDivider from "./item-divider";

// === Row with selection highlight ===
const SelectableRow = styled.div<{ $isSelected?: boolean }>`
  display: flex;
  flex-direction: row;
  align-items: center;
  padding: 0.3em 0.25em;
  background: ${props => props.$isSelected 
    ? "rgba(80, 160, 220, 0.15)" 
    : "transparent"};
  border-left: ${props => props.$isSelected 
    ? "2px solid rgba(80, 160, 220, 0.6)" 
    : "2px solid transparent"};
  transition: background 0.1s ease;
  
  &:hover {
    background: ${props => props.$isSelected 
      ? "rgba(80, 160, 220, 0.2)" 
      : "rgba(255, 255, 255, 0.05)"};
  }
`;

// === V246: Inline Delete Confirmation - centered row below phase ===
const DeleteConfirmRow = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5em;
  background: rgba(220, 80, 80, 0.12);
  border: 1px solid rgba(220, 80, 80, 0.3);
  border-radius: 0.3em;
  padding: 0.4em 0.6em;
  margin-top: 0.3em;
`;

const DeleteConfirmText = styled.span`
  font-size: 0.85em;
  color: rgba(255, 255, 255, 0.9);
  white-space: nowrap;
`;

const DeleteConfirmButton = styled.div<{$danger?: boolean}>`
  padding: 0.25em 0.6em;
  border-radius: 0.25em;
  font-size: 0.8em;
  cursor: pointer;
  background: ${props => props.$danger 
    ? 'rgba(220, 80, 80, 0.8)' 
    : 'rgba(255, 255, 255, 0.15)'};
  color: white;
  font-weight: 500;
  
  &:hover {
    background: ${props => props.$danger 
      ? 'rgba(220, 80, 80, 1)' 
      : 'rgba(255, 255, 255, 0.25)'};
  }
`;

// === Checkbox Styles ===
const CheckboxStyle: CSSProperties = {
  width: "1.1em",
  height: "1.1em",
  minWidth: "1.1em",
  border: "2px solid rgba(255, 255, 255, 0.5)",
  borderRadius: "0.15em",
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  marginRight: "0.5em",
  cursor: "pointer",
  background: "transparent",
};

const CheckboxCheckedStyle: CSSProperties = {
  ...CheckboxStyle,
  background: "rgba(80, 160, 220, 0.35)",
  borderColor: "rgba(80, 170, 220, 0.8)",
};

const Label = styled.div<{$dim?: boolean}>`
  color: ${props => props.$dim ? "var(--textColorDim)" : "var(--textColor)"};
  flex-grow: 1;
  flex-shrink: 1;
  flex-basis: auto;
  display: inline;
  filter: ${props => props.$dim ? "brightness(0.8)" : "none"};
`;

const IconBarContainer = styled.div`
  flex-grow: 1;
  flex-shrink: 1;
  flex-basis: auto;
  flex-direction: row;
  display: flex;
  justify-content: flex-end;
`;

const IconContainer = styled.div<{$disabled?: boolean}>`
  display: flex;
  margin-left: 0.35em;
  border-radius: 0.2em;
  cursor: ${props => props.$disabled ? "default" : "pointer"};
  &:hover {
    filter: ${props => props.$disabled ? "none" : "brightness(1.2) contrast(1.2)"};
    background: ${props => props.$disabled ? "transparent" : "rgba(0, 0, 0, 0.1)"};
  }
`;

const IconStyle = {
  color: "var(--textColorDim)",
  width: "1.1em",
  height: "1.1em",
  fontSize: "1.1em"
};

const IconStyleDisabled = {
  opacity: 0.3
};

const ActiveDot = () => <div style={{color: "#34bf42", marginLeft: "0.3em"}}>●</div>;

// === Props interface ===
interface ItemProps {
  data: MainPanelItemCustomPhase;
  isSelected?: boolean;
  onToggleSelect?: () => void;
  selectionMode?: boolean;
}

// Memoized component to prevent unnecessary re-renders
const Item = memo(function Item(props: ItemProps) {
  const locale = useContext(LocaleContext);
  const [isActiveLabel, setIsActiveLabel] = useState(false);
  const [showEditor, setShowEditor] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);  // V243: Delete confirmation
  
  const swap = useCallback((index1: number, index2: number) => {
    if (index1 < 0 || index2 < 0 || index1 >= props.data.length || index2 >= props.data.length) {
      return;
    }
    call("C2VM.TLE", "CallSwapCustomPhase", JSON.stringify({index1, index2}));
  }, [props.data.length]);
  
  // V243: Delete confirmation handlers
  const handleDeleteClick = useCallback(() => {
    setShowDeleteConfirm(true);
  }, []);
  
  const confirmDelete = useCallback(() => {
    call("C2VM.TLE", "CallRemoveCustomPhase", JSON.stringify({index: props.data.index}));
    setShowDeleteConfirm(false);
  }, [props.data.index]);
  
  const cancelDelete = useCallback(() => {
    setShowDeleteConfirm(false);
  }, []);
  
  useEffect(() => {
    if (props.data.activeViewingIndex >= 0) {
      setIsActiveLabel(props.data.activeViewingIndex == props.data.index);
    } else if (props.data.activeIndex >= 0) {
      setIsActiveLabel(props.data.activeIndex == props.data.index);
    } else {
      setIsActiveLabel(props.data.index + 1 == props.data.currentSignalGroup);
    }
    setShowEditor(props.data.activeIndex == props.data.index);
  }, [props.data.activeViewingIndex, props.data.activeIndex, props.data.index, props.data.currentSignalGroup]);
  
  // Handle checkbox click
  const handleCheckboxClick = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    if (props.onToggleSelect) {
      props.onToggleSelect();
    }
  }, [props.onToggleSelect]);
  
  // Get phase label text
  const phaseLabel = getString(locale, "Phase") + " #" + (props.data.index + 1);
  
  return (
    <>
      <SelectableRow $isSelected={props.isSelected}>
        {/* Selection Checkbox */}
        {(props.selectionMode || props.isSelected) && (
          <div 
            style={props.isSelected ? CheckboxCheckedStyle : CheckboxStyle}
            onClick={handleCheckboxClick}
          >
            {props.isSelected && (
              <svg viewBox="0 0 24 24" fill="currentColor" style={{ width: "0.8em", height: "0.8em", color: "rgba(80, 170, 220, 1)" }}>
                <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41L9 16.17z"/>
              </svg>
            )}
          </div>
        )}
        
        <Label $dim={!isActiveLabel}>
          {phaseLabel}
          {props.data.activeIndex < 0 && props.data.index + 1 == props.data.currentSignalGroup && <ActiveDot />}
        </Label>
        
        <IconBarContainer>
          {!showEditor && <>
            {props.data.activeViewingIndex == props.data.index && (
              <IconContainer>
                <VisibilityOff 
                  style={IconStyle} 
                  onClick={() => call("C2VM.TLE", "CallSetActiveCustomPhaseIndex", JSON.stringify({key: "ActiveViewingCustomPhaseIndex", value: -1}))} 
                />
              </IconContainer>
            )}
            {props.data.activeViewingIndex != props.data.index && (
              <IconContainer>
                <Visibility 
                  style={IconStyle} 
                  onClick={() => call("C2VM.TLE", "CallSetActiveCustomPhaseIndex", JSON.stringify({key: "ActiveViewingCustomPhaseIndex", value: props.data.index}))} 
                />
              </IconContainer>
            )}
            <IconContainer>
              <Tune 
                style={IconStyle} 
                onClick={() => call("C2VM.TLE", "CallSetActiveCustomPhaseIndex", JSON.stringify({key: "ActiveEditingCustomPhaseIndex", value: props.data.index}))} 
              />
            </IconContainer>
          </>}
          {showEditor && <>
            <IconContainer>
              <Delete 
                style={IconStyle} 
                onClick={handleDeleteClick}
              />
            </IconContainer>
            <IconContainer>
              <Check 
                style={IconStyle} 
                onClick={() => call("C2VM.TLE", "CallSetActiveCustomPhaseIndex", JSON.stringify({key: "ActiveEditingCustomPhaseIndex", value: -1}))} 
              />
            </IconContainer>
            <IconContainer $disabled={props.data.activeIndex <= 0}>
              <ChevronUp 
                style={{...IconStyle, ...(props.data.activeIndex <= 0 && IconStyleDisabled)}} 
                onClick={() => swap(props.data.activeIndex, props.data.activeIndex - 1)} 
              />
            </IconContainer>
            <IconContainer $disabled={props.data.activeIndex >= (props.data.length - 1)}>
              <ChevronDown 
                style={{...IconStyle, ...(props.data.activeIndex >= (props.data.length - 1) && IconStyleDisabled)}} 
                onClick={() => swap(props.data.activeIndex, props.data.activeIndex + 1)} 
              />
            </IconContainer>
          </>}
        </IconBarContainer>
      </SelectableRow>
      
      {/* V246: Delete confirmation row - shown below phase row */}
      {showDeleteConfirm && (
        <DeleteConfirmRow>
          <DeleteConfirmText>{getString(locale, "DeletePhaseConfirmShort") || "Phase löschen?"}</DeleteConfirmText>
          <DeleteConfirmButton onClick={cancelDelete}>X</DeleteConfirmButton>
          <DeleteConfirmButton $danger onClick={confirmDelete}>OK</DeleteConfirmButton>
        </DeleteConfirmRow>
      )}
      
      {props.data.index + 1 < props.data.length && (
        <ItemDivider index={props.data.index} linked={props.data.linkedWithNextPhase} />
      )}
    </>
  );
});

export default Item;
