/**
 * V255: COHTML-safe Dropdown with improved visibility
 * 
 * REMOVED problematic features:
 * - No transient props ($isOpen, $isSelected)
 * - No transform on hover
 * - No dynamic styled-component props
 * - Simplified event handling
 * 
 * V255: Enhanced colors for better visibility
 */

import { useState, useRef, useEffect, CSSProperties } from 'react';
import styled from 'styled-components';

// V255: All static styles - no dynamic props!
const DropdownContainer = styled.div`
  position: relative;
  width: 100%;
`;

const DropdownButton = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10rem 14rem;
  border-radius: 6rem;
  color: rgba(255, 255, 255, 1);
  cursor: pointer;
  font-weight: 600;
  font-size: 14rem;
`;

const SelectedLabel = styled.span`
  flex: 1;
  text-align: center;
`;

const Arrow = styled.span`
  font-size: 11rem;
  opacity: 0.9;
  margin-left: 8rem;
`;

// V255: Dropdown menu with stronger background
const DropdownMenu = styled.div`
  position: absolute;
  top: 100%;
  margin-top: 4rem;
  left: 0;
  right: 0;
  background: rgba(25, 35, 50, 0.98);
  border: 2rem solid rgba(100, 180, 255, 0.5);
  border-radius: 6rem;
  z-index: 1000;
  overflow: hidden;
`;

const CloseRow = styled.div`
  display: flex;
  justify-content: flex-end;
  padding: 6rem 8rem;
  border-bottom: 1rem solid rgba(255, 255, 255, 0.12);
`;

// V255: More visible close button
const CloseButton = styled.div`
  width: 22rem;
  height: 22rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(220, 70, 70, 0.95);
  border: 1rem solid rgba(255, 100, 100, 0.5);
  border-radius: 4rem;
  cursor: pointer;
  font-family: Arial, sans-serif;
  font-size: 13rem;
  font-weight: bold;
  color: white;
  line-height: 1;
  
  &:hover {
    background: rgba(240, 80, 80, 1);
  }
`;

const OptionsContainer = styled.div`
  padding: 6rem 0;
`;

// V255: Better item visibility
const DropdownItem = styled.div`
  padding: 10rem 14rem;
  cursor: pointer;
  text-align: center;
  font-weight: 500;
  font-size: 14rem;
`;

interface DropdownOption {
  value: string;
  label: string;
}

interface DropdownProps {
  options: DropdownOption[];
  selectedValue: string;
  onSelect: (value: string) => void;
  getLabel: (label: string) => string;
}

// V255: Enhanced style objects for better visibility
const buttonOpenStyle: CSSProperties = {
  background: 'rgba(80, 160, 255, 0.35)',
  border: '2rem solid rgba(100, 200, 255, 0.7)'
};

const buttonClosedStyle: CSSProperties = {
  background: 'rgba(60, 140, 220, 0.25)',
  border: '2rem solid rgba(100, 180, 255, 0.5)'
};

const itemSelectedStyle: CSSProperties = {
  background: 'rgba(80, 160, 255, 0.3)',
  color: 'rgba(140, 220, 255, 1)'
};

const itemNormalStyle: CSSProperties = {
  background: 'transparent',
  color: 'rgba(255, 255, 255, 0.95)'
};

export default function Dropdown({ options, selectedValue, onSelect, getLabel }: DropdownProps) {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  // V250: Simplified click-outside handler
  useEffect(() => {
    if (!isOpen) return;
    
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as Node;
      if (containerRef.current && !containerRef.current.contains(target)) {
        setIsOpen(false);
      }
    };

    // Use click instead of mousedown - more reliable in COHTML
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, [isOpen]);

  const selectedOption = options.find(opt => opt.value === selectedValue);
  const selectedLabel = selectedOption ? getLabel(selectedOption.label) : '';

  const handleSelect = (value: string) => {
    onSelect(value);
    setIsOpen(false);
  };

  const handleButtonClick = () => {
    setIsOpen(!isOpen);
  };

  // V253: COHTML-SAFE - no transform, use different arrow symbols
  return (
    <DropdownContainer ref={containerRef}>
      <DropdownButton 
        style={isOpen ? buttonOpenStyle : buttonClosedStyle}
        onClick={handleButtonClick}
      >
        <SelectedLabel>{selectedLabel}</SelectedLabel>
        <Arrow>{isOpen ? '▲' : '▼'}</Arrow>
      </DropdownButton>
      {isOpen && (
        <DropdownMenu>
          <CloseRow>
            <CloseButton onClick={() => setIsOpen(false)}>X</CloseButton>
          </CloseRow>
          <OptionsContainer>
            {options.map((option) => (
              <DropdownItem
                key={option.value}
                style={option.value === selectedValue ? itemSelectedStyle : itemNormalStyle}
                onClick={() => handleSelect(option.value)}
              >
                {getLabel(option.label)}
              </DropdownItem>
            ))}
          </OptionsContainer>
        </DropdownMenu>
      )}
    </DropdownContainer>
  );
}
