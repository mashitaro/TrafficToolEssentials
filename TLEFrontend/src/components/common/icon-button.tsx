import { CSSProperties, ReactNode, useContext, useState } from "react";
import styled from "styled-components";

import { LocaleContext } from "@/context";
import { getString } from "@/localisations";

const IconButtonContainer = styled.div<{ disabled?: boolean }>`
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.25em;
  border-radius: 0.25em;
  cursor: ${props => props.disabled ? "not-allowed" : "pointer"};
  opacity: ${props => props.disabled ? 0.35 : 1};
  transition: all 0.15s ease;
  position: relative;
  
  &:hover {
    background: ${props => props.disabled ? "transparent" : "rgba(255, 255, 255, 0.1)"};
    filter: ${props => props.disabled ? "none" : "brightness(1.2)"};
  }
  
  &:active {
    transform: ${props => props.disabled ? "none" : "scale(0.95)"};
  }
`;

const TooltipContainer = styled.div`
  position: absolute;
  bottom: 100%;
  left: 50%;
  transform: translateX(-50%);
  margin-bottom: 0.5em;
  padding: 0.5em 0.75em;
  background: rgba(0, 0, 0, 0.9);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 0.25em;
  white-space: nowrap;
  z-index: 1000;
  pointer-events: none;
  
  /* Arrow */
  &::after {
    content: "";
    position: absolute;
    top: 100%;
    left: 50%;
    transform: translateX(-50%);
    border: 0.4em solid transparent;
    border-top-color: rgba(0, 0, 0, 0.9);
  }
`;

const TooltipText = styled.span`
  color: var(--textColor);
  font-size: 0.85em;
`;

interface IconButtonProps {
  icon: ReactNode;
  tooltip?: string;
  tooltipKey?: string;
  disabled?: boolean;
  onClick?: () => void;
  style?: CSSProperties;
  iconStyle?: CSSProperties;
}

export default function IconButton(props: IconButtonProps) {
  const locale = useContext(LocaleContext);
  const [showTooltip, setShowTooltip] = useState(false);
  
  const tooltipText = props.tooltipKey 
    ? getString(locale, props.tooltipKey) 
    : props.tooltip;
  
  const handleClick = () => {
    if (!props.disabled && props.onClick) {
      props.onClick();
    }
  };
  
  return (
    <IconButtonContainer
      disabled={props.disabled}
      onClick={handleClick}
      onMouseEnter={() => setShowTooltip(true)}
      onMouseLeave={() => setShowTooltip(false)}
      style={props.style}
    >
      {showTooltip && tooltipText && (
        <TooltipContainer>
          <TooltipText>{tooltipText}</TooltipText>
        </TooltipContainer>
      )}
      <div style={{ 
        display: "flex", 
        color: "var(--textColor)",
        width: "1.2em",
        height: "1.2em",
        ...props.iconStyle 
      }}>
        {props.icon}
      </div>
    </IconButtonContainer>
  );
}
