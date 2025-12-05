/**
 * MODERN COLLAPSIBLE SECTION
 * 
 * Features:
 * - Smooth expand/collapse animation
 * - Clear visual state
 * - Übersichtlich und nicht technisch
 * - Pfeil-Indikator für State
 */

import { useState, useRef, useEffect } from 'react';
import styled from 'styled-components';
import subMenuTokens from '@/styles/submenu-tokens';

// ===== STYLED COMPONENTS =====

const Container = styled.div`
  display: flex;
  flex-direction: column;
  border: 1rem solid ${subMenuTokens.colors.borderSecondary};
  border-radius: ${subMenuTokens.borders.radiusMD};
  background: ${subMenuTokens.colors.backgroundSecondary};
  overflow: hidden;
`;

const Header = styled.button<{ $isOpen: boolean }>`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: ${subMenuTokens.spacing.itemPadding};
  background: ${props => 
    props.$isOpen 
      ? subMenuTokens.colors.backgroundTertiary 
      : 'transparent'
  };
  border: none;
  cursor: pointer;
  transition: all ${subMenuTokens.transitions.normal};
  
  &:hover {
    background: ${subMenuTokens.colors.backgroundTertiary};
  }
  
  &:focus {
    outline: none;
  }
  
  &:focus-visible {
    box-shadow: inset 0 0 0 2rem ${subMenuTokens.colors.borderFocus};
  }
`;

const HeaderLeft = styled.div`
  display: flex;
  align-items: center;
  gap: 12rem;
`;

const HeaderTitle = styled.span`
  font-size: ${subMenuTokens.typography.fontLG};
  color: ${subMenuTokens.colors.textPrimary};
  font-weight: ${subMenuTokens.typography.weightSemiBold};
  text-align: left;
`;

const HeaderBadge = styled.span`
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem 8rem;
  background: ${subMenuTokens.colors.backgroundPrimary};
  color: ${subMenuTokens.colors.textSecondary};
  font-size: ${subMenuTokens.typography.fontSM};
  border-radius: 10rem;
  font-weight: ${subMenuTokens.typography.weightMedium};
`;

const Arrow = styled.svg<{ $isOpen: boolean }>`
  width: 16rem;
  height: 16rem;
  flex-shrink: 0;
  color: ${subMenuTokens.colors.textSecondary};
  transition: transform ${subMenuTokens.transitions.normal};
  transform: rotate(${props => props.$isOpen ? '180deg' : '0deg'});
`;

const ContentWrapper = styled.div<{ $isOpen: boolean; $height: number }>`
  max-height: ${props => props.$isOpen ? `${props.$height}px` : '0px'};
  overflow: hidden;
  transition: max-height ${subMenuTokens.transitions.normal};
`;

const Content = styled.div`
  padding: ${subMenuTokens.spacing.itemPadding};
  border-top: 1rem solid ${subMenuTokens.colors.borderSecondary};
`;

// ===== COMPONENT =====

interface CollapsibleSectionProps {
  title: string;
  badge?: string;
  defaultOpen?: boolean;
  children: React.ReactNode;
}

export default function CollapsibleSection({ 
  title, 
  badge, 
  defaultOpen = false, 
  children 
}: CollapsibleSectionProps) {
  const [isOpen, setIsOpen] = useState(defaultOpen);
  const [contentHeight, setContentHeight] = useState(0);
  const contentRef = useRef<HTMLDivElement>(null);
  
  // Measure content height when it changes
  useEffect(() => {
    if (contentRef.current) {
      setContentHeight(contentRef.current.scrollHeight);
    }
  }, [children]);
  
  // Update height on window resize
  useEffect(() => {
    const handleResize = () => {
      if (contentRef.current) {
        setContentHeight(contentRef.current.scrollHeight);
      }
    };
    
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);
  
  return (
    <Container>
      <Header 
        $isOpen={isOpen} 
        onClick={() => setIsOpen(!isOpen)}
        type="button"
      >
        <HeaderLeft>
          <HeaderTitle>{title}</HeaderTitle>
          {badge && <HeaderBadge>{badge}</HeaderBadge>}
        </HeaderLeft>
        <Arrow $isOpen={isOpen} viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M4 6L8 10L12 6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
        </Arrow>
      </Header>
      <ContentWrapper $isOpen={isOpen} $height={contentHeight}>
        <Content ref={contentRef}>
          {children}
        </Content>
      </ContentWrapper>
    </Container>
  );
}
