/**
 * SUB-MENU CONTAINER TEMPLATE
 * 
 * Standardisiertes Layout für alle Sub-Menüs
 * - Konsistentes Padding
 * - Section-Struktur
 * - Header mit Icon
 * - Button-Bereich
 */

import styled from 'styled-components';
import subMenuTokens from '@/styles/submenu-tokens';

// ===== MAIN CONTAINER =====

export const SubMenuContainer = styled.div`
  display: flex;
  flex-direction: column;
  gap: ${subMenuTokens.spacing.sectionGap};
  padding: ${subMenuTokens.spacing.containerPadding};
  background: ${subMenuTokens.colors.backgroundPrimary};
  border-radius: 8rem;
  min-width: 320rem;
  max-width: 600rem;
`;

// ===== HEADER =====

export const SubMenuHeader = styled.div`
  display: flex;
  align-items: center;
  gap: 12rem;
  padding-bottom: ${subMenuTokens.spacing.itemPadding};
  border-bottom: 1rem solid ${subMenuTokens.colors.borderSecondary};
`;

export const SubMenuHeaderIcon = styled.img`
  width: 24rem;
  height: 24rem;
  flex-shrink: 0;
`;

export const SubMenuHeaderTitle = styled.h3`
  font-size: ${subMenuTokens.typography.fontXL};
  color: ${subMenuTokens.colors.textPrimary};
  font-weight: ${subMenuTokens.typography.weightBold};
  margin: 0;
`;

// ===== SECTIONS =====

export const SubMenuSection = styled.section`
  display: flex;
  flex-direction: column;
  gap: ${subMenuTokens.spacing.itemGap};
`;

export const SubMenuSectionTitle = styled.h4`
  font-size: ${subMenuTokens.typography.fontSM};
  color: ${subMenuTokens.colors.textSecondary};
  font-weight: ${subMenuTokens.typography.weightMedium};
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin: 0 0 8rem 0;
`;

export const SubMenuDivider = styled.div`
  height: 1rem;
  background: ${subMenuTokens.colors.borderSecondary};
  margin: ${subMenuTokens.spacing.itemGap} 0;
`;

// ===== CONTENT GROUPS =====

export const SubMenuGroup = styled.div`
  display: flex;
  flex-direction: column;
  gap: ${subMenuTokens.spacing.itemGap};
  padding: ${subMenuTokens.spacing.itemPadding};
  background: ${subMenuTokens.colors.backgroundSecondary};
  border-radius: ${subMenuTokens.borders.radiusMD};
  border: 1rem solid ${subMenuTokens.colors.borderSecondary};
`;

export const SubMenuRow = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16rem;
  padding: 8rem 0;
`;

export const SubMenuLabel = styled.label`
  font-size: ${subMenuTokens.typography.fontMD};
  color: ${subMenuTokens.colors.textPrimary};
  font-weight: ${subMenuTokens.typography.weightMedium};
`;

export const SubMenuDescription = styled.p`
  font-size: ${subMenuTokens.typography.fontSM};
  color: ${subMenuTokens.colors.textSecondary};
  line-height: ${subMenuTokens.typography.lineHeightLoose};
  margin: 0;
`;

// ===== BUTTON AREA =====

export const SubMenuButtonArea = styled.div`
  display: flex;
  gap: ${subMenuTokens.spacing.buttonGap};
  padding-top: ${subMenuTokens.spacing.itemPadding};
  border-top: 1rem solid ${subMenuTokens.colors.borderSecondary};
  margin-top: auto;
  
  /* Responsive: Stack on small screens */
  @media (max-width: 500px) {
    flex-direction: column;
  }
`;

export const SubMenuButton = styled.button<{ $variant?: 'primary' | 'secondary' | 'danger' }>`
  flex: 1;
  padding: ${subMenuTokens.spacing.buttonPadding};
  border-radius: ${subMenuTokens.borders.radiusMD};
  font-size: ${subMenuTokens.typography.fontMD};
  font-weight: ${subMenuTokens.typography.weightSemiBold};
  cursor: pointer;
  transition: all ${subMenuTokens.transitions.fast};
  border: none;
  
  /* Variants */
  ${props => {
    switch (props.$variant) {
      case 'primary':
        return `
          background: ${subMenuTokens.colors.accentPrimary};
          color: rgba(255, 255, 255, 0.95);
          
          &:hover {
            background: rgba(120, 200, 255, 1);
            transform: translateY(-1rem);
            box-shadow: ${subMenuTokens.shadows.medium};
          }
          
          &:active {
            transform: translateY(0);
          }
        `;
      case 'danger':
        return `
          background: ${subMenuTokens.colors.accentWarning};
          color: rgba(255, 255, 255, 0.95);
          
          &:hover {
            background: rgba(255, 200, 100, 1);
            transform: translateY(-1rem);
            box-shadow: ${subMenuTokens.shadows.medium};
          }
          
          &:active {
            transform: translateY(0);
          }
        `;
      case 'secondary':
      default:
        return `
          background: ${subMenuTokens.colors.backgroundTertiary};
          color: ${subMenuTokens.colors.textPrimary};
          border: 1rem solid ${subMenuTokens.colors.borderPrimary};
          
          &:hover {
            background: ${subMenuTokens.colors.backgroundSecondary};
            border-color: ${subMenuTokens.colors.borderFocus};
          }
          
          &:active {
            background: ${subMenuTokens.colors.backgroundPrimary};
          }
        `;
    }
  }}
  
  /* V232: Use attribute selector instead of :disabled for COHTML */
  &[disabled] {
    opacity: 0.5;
    cursor: not-allowed;
    pointer-events: none;
    
    &:hover {
      transform: none;
      box-shadow: none;
    }
  }
  
  &:focus {
    outline: none;
  }
  
  &:focus-visible {
    box-shadow: 0 0 0 3rem ${subMenuTokens.colors.borderFocus};
  }
`;

// ===== UTILITY COMPONENTS =====

export const SubMenuGrid = styled.div<{ $columns?: number }>`
  display: flex;
  flex-wrap: wrap;
  gap: ${subMenuTokens.spacing.itemGap};
  
  > * {
    flex: 1 1 ${props => props.$columns === 3 ? '30%' : props.$columns === 4 ? '22%' : '45%'};
    min-width: 120rem;
  }
`;

export const SubMenuSpacer = styled.div<{ $size?: 'small' | 'medium' | 'large' }>`
  height: ${props => {
    switch (props.$size) {
      case 'small': return '8rem';
      case 'large': return '32rem';
      case 'medium':
      default: return '16rem';
    }
  }};
`;
