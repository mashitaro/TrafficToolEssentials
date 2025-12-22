/**
 * Function Selection Component - V233 Simplified
 * 
 * Redesigned to match the intersection menu style:
 * - Uses CS2 native CSS variables
 * - Compact, clean layout
 * - Simple button design
 * - No separate header (uses standard Header component)
 */

import { useContext } from 'react';
import styled from 'styled-components';
import engine from 'cohtml/cohtml';

import { LocaleContext } from '@/context';
import { getString } from '@/localisations';
import { MainPanelState } from '@/constants';

// ===== STYLED COMPONENTS =====

// Main container - matches intersection menu width
const Container = styled.div`
  width: 18em;
  background-color: var(--panelColorNormal);
  backdrop-filter: var(--panelBlur);
  color: var(--textColor);
  padding: 12rem;
  display: flex;
  flex-direction: column;
  gap: 12rem;
`;

// Section title
const SectionTitle = styled.div`
  color: var(--textColorDim);
  font-size: 11rem;
  text-transform: uppercase;
  letter-spacing: 0.5rem;
  padding-bottom: 8rem;
  border-bottom: 1rem solid rgba(255, 255, 255, 0.1);
  margin-bottom: 4rem;
`;

// Button container
const ButtonList = styled.div`
  display: flex;
  flex-direction: column;
  gap: 8rem;
`;

// Function button - primary style (active)
const FunctionButton = styled.button`
  display: flex;
  align-items: center;
  gap: 10rem;
  padding: 10rem 14rem;
  border-radius: 6rem;
  border: 1rem solid rgba(255, 255, 255, 0.15);
  background: rgba(40, 50, 60, 0.8);
  color: var(--textColor);
  font-size: 12rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease;
  text-align: left;
  
  &:hover {
    background: rgba(50, 65, 80, 0.9);
    border-color: rgba(255, 255, 255, 0.25);
    transform: translateX(2rem);
  }
  
  &:active {
    background: rgba(35, 45, 55, 0.9);
    transform: translateX(0);
  }
`;

// Function button - accent style (Werder Green for main action)
const FunctionButtonPrimary = styled(FunctionButton)`
  background: linear-gradient(135deg, rgba(30, 144, 83, 0.4) 0%, rgba(20, 120, 70, 0.35) 100%);
  border-color: rgba(30, 180, 100, 0.5);
  
  &:hover {
    background: linear-gradient(135deg, rgba(30, 144, 83, 0.55) 0%, rgba(20, 120, 70, 0.5) 100%);
    border-color: rgba(60, 200, 120, 0.7);
    box-shadow: 0 2rem 8rem rgba(30, 180, 100, 0.3);
  }
  
  &:active {
    background: linear-gradient(135deg, rgba(20, 120, 70, 0.5) 0%, rgba(15, 100, 60, 0.45) 100%);
  }
`;

// Function button - disabled style
const FunctionButtonDisabled = styled(FunctionButton)`
  opacity: 0.5;
  cursor: not-allowed;
  pointer-events: none;
  
  &:hover {
    background: rgba(40, 50, 60, 0.8);
    border-color: rgba(255, 255, 255, 0.15);
    transform: none;
  }
`;

// Icon container
const IconWrapper = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20rem;
  height: 20rem;
  flex-shrink: 0;
  opacity: 0.9;
`;

// Button text
const ButtonText = styled.span`
  flex: 1;
`;

// Badge for "Coming Soon"
const Badge = styled.span`
  font-size: 9rem;
  padding: 3rem 6rem;
  border-radius: 4rem;
  background: rgba(255, 255, 255, 0.1);
  color: var(--textColorDim);
  text-transform: uppercase;
  letter-spacing: 0.3rem;
`;

// Description text
const Description = styled.div`
  color: var(--textColorDim);
  font-size: 10rem;
  line-height: 1.4;
  padding: 8rem 0;
`;

// ===== ICONS (inline SVG) =====

const EditIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" width="100%" height="100%">
    <path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/>
  </svg>
);

const MonitorIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" width="100%" height="100%">
    <path d="M21 3H3c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h7v2H8v2h8v-2h-2v-2h7c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 14H3V5h18v12z"/>
  </svg>
);

// ===== COMPONENT =====

export default function FunctionSelection() {
  const locale = useContext(LocaleContext);
  
  const handleEditClick = () => {
    engine.call('C2VM.TLE.CallSetMainPanelState', JSON.stringify({ value: `${MainPanelState.Empty}` }));
  };
  
  const handleMonitorClick = () => {
    // Currently disabled
    // engine.call('C2VM.TLE.CallSetMainPanelState', JSON.stringify({ value: `${MainPanelState.IntersectionMonitor}` }));
  };
  
  return (
    <Container>
      <SectionTitle>{getString(locale, 'SelectFunction')}</SectionTitle>
      
      <Description>
        {getString(locale, 'SelectFunctionDescription')}
      </Description>
      
      <ButtonList>
        {/* Button 1: Edit Intersection Phases - Primary Action */}
        <FunctionButtonPrimary onClick={handleEditClick}>
          <IconWrapper>
            <EditIcon />
          </IconWrapper>
          <ButtonText>{getString(locale, 'EditIntersectionPhases')}</ButtonText>
        </FunctionButtonPrimary>
        
        {/* Button 2: Intersection Monitor - Coming Soon */}
        <FunctionButtonDisabled onClick={handleMonitorClick}>
          <IconWrapper>
            <MonitorIcon />
          </IconWrapper>
          <ButtonText>{getString(locale, 'IntersectionMonitor')}</ButtonText>
          <Badge>{getString(locale, 'DashboardComingSoon')}</Badge>
        </FunctionButtonDisabled>
      </ButtonList>
    </Container>
  );
}
