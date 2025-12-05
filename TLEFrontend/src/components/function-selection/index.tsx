/**
 * Function Selection Component - Final Version
 * 
 * CHANGES:
 * - Container 10% narrower (450-495rem)
 * - Icon left in header (like original bar)
 * - Separate header bar hidden
 * - Icons left, text right for buttons (retained)
 * - Colors and gap retained
 */

import { useContext } from 'react';
import styled, { keyframes } from 'styled-components';
import engine from 'cohtml/cohtml';

import { LocaleContext } from '@/context';
import { getString } from '@/localisations';
import { MainPanelState } from '@/constants';

import ButtonEnhanced from '@/components/common/button-enhanced';
import IconEdit from '@/components/common/icons/icon-edit';
import IconMonitor from '@/components/common/icons/icon-monitor';

import tokens from '@/styles/tokens-v2';

// ===== ANIMATIONS =====
const fadeIn = keyframes`
  from {
    opacity: 0;
    transform: translateY(10rem);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
`;

const slideIn = keyframes`
  from {
    opacity: 0;
    transform: scale(0.95);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
`;

// ===== STYLED COMPONENTS =====

const OuterContainer = styled.div`
  display: flex;
  justify-content: center;
  align-items: center;
  padding: ${tokens.spacing.xl};
  width: 100%;
`;

const Container = styled.div`
  display: flex;
  flex-direction: column;
  gap: 0;  /* Kein Gap zwischen Header und Content */
  min-width: 450rem;   /* 10% schmaler: 500 * 0.9 = 450 */
  max-width: 495rem;   /* 10% schmaler: 550 * 0.9 = 495 */
  
  background: ${tokens.colors.backgroundPrimary};
  backdrop-filter: ${tokens.blur.medium};
  
  border: ${tokens.borders.borderThin} solid ${tokens.colors.borderPrimary};
  border-radius: ${tokens.borders.radiusLarge};
  box-shadow: ${tokens.shadows.shadowLarge};
  
  animation: ${slideIn} 0.4s ${tokens.transitions.easingOut};
  overflow: hidden;  /* Fuer saubere Header-Integration */
`;

// Integrierter Header mit Icon links
const IntegratedHeader = styled.div`
  display: flex;
  align-items: center;
  padding: 6rem 10rem;  /* Padding wie Original-Header */
  min-height: 36rem;     /* Hoehe wie Original-Header */
  background: rgba(30, 35, 40, 0.95);  /* Etwas dunkler als Container */
  border-bottom: ${tokens.borders.borderThin} solid ${tokens.colors.borderPrimary};
  gap: 8rem;  /* Kleiner Abstand zwischen Icon und Text */
`;

// Icon links (wie im Original)
const HeaderIcon = styled.img`
  width: 24rem;
  height: 24rem;
  flex-shrink: 0;
`;

// Title zentriert (flex: 1)
const HeaderTitle = styled.h2`
  font-size: 14rem;  /* Gleiche Groesse wie Original */
  font-weight: ${tokens.typography.weightBold};
  color: ${tokens.colors.accentPrimary};
  margin: 0;
  text-transform: uppercase;
  letter-spacing: ${tokens.typography.letterSpacingWide};
  text-shadow: ${tokens.shadows.textShadowMedium};
  flex: 1;  /* Nimmt verfuegbaren Platz */
  text-align: center;  /* Text zentriert */
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
`;

// Content-Bereich mit eigenem Padding
const ContentArea = styled.div`
  display: flex;
  flex-direction: column;
  gap: ${tokens.spacing.xl};
  padding: ${tokens.spacing.xxxl};
`;

const Header = styled.div`
  display: flex;
  flex-direction: column;
  gap: ${tokens.spacing.md};
  text-align: center;
`;

const Title = styled.h2`
  font-size: ${tokens.typography.fontXL};
  font-weight: ${tokens.typography.weightBold};
  color: ${tokens.colors.accentPrimary};
  margin: 0;
  text-shadow: ${tokens.shadows.textShadowMedium};
  letter-spacing: ${tokens.typography.letterSpacingWide};
`;

const Description = styled.p`
  font-size: ${tokens.typography.fontMD};
  color: ${tokens.colors.textSecondary};
  margin: 0;
  line-height: ${tokens.typography.lineHeightRelaxed};
  text-shadow: ${tokens.shadows.textShadowSubtle};
`;

// Flex layout - CS2 doesnt support CSS grid with 1fr
const ButtonGrid = styled.div`
  display: flex;
  flex-direction: column;
  gap: 32rem;  /* Deutlich groesserer Gap - war 24rem */
  margin-top: ${tokens.spacing.md};
  
  &.has-many-buttons {
    flex-direction: row;
    flex-wrap: wrap;
    > * {
      flex: 0 0 calc(50% - 16rem);
    }
  }
`;

// Spezieller Button-Wrapper fuer Icons links, Text rechts
const FunctionButton = styled(ButtonEnhanced)`
  width: 100%;
  min-height: ${tokens.spacing.buttonMinHeight};
  font-size: ${tokens.typography.fontMD};
  padding: ${tokens.spacing.buttonPadding};
  
  /* Icons links beibehalten */
  justify-content: flex-start;
  
  /* Animation Delay */
  &:nth-child(1) {
    animation: ${fadeIn} 0.3s 0.1s backwards;
  }
  &:nth-child(2) {
    animation: ${fadeIn} 0.3s 0.2s backwards;
  }
  &:nth-child(3) {
    animation: ${fadeIn} 0.3s 0.3s backwards;
  }
  &:nth-child(4) {
    animation: ${fadeIn} 0.3s 0.4s backwards;
  }
`;

// Custom Button Content fuer Icons links, Text rechts
const ButtonContent = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;  /* Text rechts beibehalten */
  width: 100%;
  gap: ${tokens.spacing.md};
`;

const IconWrapper = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24rem;
  height: 24rem;
  flex-shrink: 0;
  
  svg {
    width: 100%;
    height: 100%;
  }
`;

const TextWrapper = styled.div`
  display: flex;
  align-items: center;
  gap: ${tokens.spacing.sm};
  flex: 1;
  justify-content: flex-end;  /* Text rechtsbuendig beibehalten */
`;

const ComingSoonBadge = styled.span`
  display: inline-block;
  padding: ${tokens.spacing.xs} ${tokens.spacing.sm};
  border-radius: ${tokens.borders.radiusSmall};
  background: ${tokens.colors.backgroundTertiary};
  color: ${tokens.colors.textSecondary};
  font-size: ${tokens.typography.fontXS};
  font-weight: ${tokens.typography.weightSemiBold};
  vertical-align: middle;
  border: ${tokens.borders.borderThin} solid ${tokens.colors.borderSecondary};
`;

// ===== COMPONENT =====

export default function FunctionSelection() {
  const locale = useContext(LocaleContext);
  
  const handleEditClick = () => {
    engine.call('C2VM.TLE.CallSetMainPanelState', JSON.stringify({ value: `${MainPanelState.Empty}` }));
  };
  
  const handleMonitorClick = () => {
    engine.call('C2VM.TLE.CallSetMainPanelState', JSON.stringify({ value: `${MainPanelState.IntersectionMonitor}` }));
  };
  
  return (
    <OuterContainer>
      <Container>
        {/* Integrierter Header mit Icon links */}
        <IntegratedHeader>
          <HeaderIcon src="Media/Game/Icons/TrafficLights.svg" />
          <HeaderTitle>Traffic Tool Essentials</HeaderTitle>
        </IntegratedHeader>
        
        {/* Content Area */}
        <ContentArea>
          <Header>
            <Title>{getString(locale, 'SelectFunction')}</Title>
            <Description>{getString(locale, 'SelectFunctionDescription')}</Description>
          </Header>
          
          <ButtonGrid>
            {/* Button 1: Kreuzungsphasen bearbeiten */}
            <FunctionButton
              variant="primary"
              size="lg"
              onClick={handleEditClick}
            >
              <ButtonContent>
                <IconWrapper>
                  <IconEdit />
                </IconWrapper>
                <TextWrapper>
                  {getString(locale, 'EditIntersectionPhases')}
                </TextWrapper>
              </ButtonContent>
            </FunctionButton>
            
            {/* Button 2: Kreuzungsueberwachung */}
            <FunctionButton
              variant="accent"
              size="lg"
              onClick={handleMonitorClick}
              disabled={true}
            >
              <ButtonContent>
                <IconWrapper>
                  <IconMonitor />
                </IconWrapper>
                <TextWrapper>
                  {getString(locale, 'IntersectionMonitor')}
                  <ComingSoonBadge>
                    {getString(locale, 'DashboardComingSoon')}
                  </ComingSoonBadge>
                </TextWrapper>
              </ButtonContent>
            </FunctionButton>
          </ButtonGrid>
        </ContentArea>
      </Container>
    </OuterContainer>
  );
}
