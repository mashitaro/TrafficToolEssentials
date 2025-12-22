/**
 * Enhanced Button Component - Cities Skylines 2 (CORRECTED SIZING!)
 * 
 * WICHTIG: Alle rem-Werte muessen 10-12x groesser sein!
 * 
 * Features:
 * - 3 Varianten: Primary (Cyan), Secondary (Grau), Accent (Orange)
 * - Icon Support
 * - Hover Animationen
 * - Disabled State
 */

import { MouseEventHandler, ReactNode } from 'react';
import styled, { css } from 'styled-components';

// ===== TYPES =====

type ButtonVariant = 'primary' | 'secondary' | 'accent';
type ButtonSize = 'sm' | 'md' | 'lg';

interface ButtonEnhancedProps {
  variant?: ButtonVariant;
  size?: ButtonSize;
  icon?: ReactNode;
  iconPosition?: 'left' | 'right';
  onClick?: MouseEventHandler<HTMLButtonElement>;
  disabled?: boolean;
  children: ReactNode;
  className?: string;
}

// ===== STYLED COMPONENTS =====

interface ButtonStyledProps {
  $variant: ButtonVariant;
  $size: ButtonSize;
  $disabled?: boolean;
  $hasIcon?: boolean;
}

const getVariantStyles = (variant: ButtonVariant) => {
  switch (variant) {
    case 'primary':
      return css`
        background: linear-gradient(135deg, #00D5E4 0%, #00F0FF 100%);
        color: #000000;
        border: 2rem solid #00F0FF;
        box-shadow: 0 4rem 8rem rgba(0, 0, 0, 0.3);
        
        /* V232: COHTML doesn't support :not(:disabled), use separate hover */
        &:hover {
          box-shadow: 0 6rem 12rem rgba(0, 0, 0, 0.4), 0 0 20rem rgba(0, 213, 228, 0.5);
          filter: brightness(1.15);
        }
      `;
    
    case 'secondary':
      return css`
        background: rgba(30, 30, 30, 0.95);
        color: #FFFFFF;
        border: 2rem solid rgba(255, 255, 255, 0.15);
        box-shadow: 0 4rem 8rem rgba(0, 0, 0, 0.3);
        
        /* V232: COHTML doesn't support :not(:disabled), use separate hover */
        &:hover {
          background: rgba(45, 45, 45, 0.95);
          box-shadow: 0 6rem 12rem rgba(0, 0, 0, 0.4);
          border-color: rgba(255, 255, 255, 0.25);
        }
      `;
    
    case 'accent':
      return css`
        background: linear-gradient(135deg, #FF6B35 0%, #FF8C42 100%);
        color: #FFFFFF;
        border: 2rem solid #FF8C42;
        box-shadow: 0 4rem 8rem rgba(0, 0, 0, 0.3);
        
        /* V232: COHTML doesn't support :not(:disabled), use separate hover */
        &:hover {
          box-shadow: 0 6rem 12rem rgba(0, 0, 0, 0.4), 0 0 20rem rgba(255, 107, 53, 0.5);
          filter: brightness(1.15);
        }
      `;
  }
};

const getSizeStyles = (size: ButtonSize) => {
  switch (size) {
    case 'sm':
      return css`
        padding: 8rem 12rem;
        font-size: 11rem;
        min-height: 32rem;
        border-radius: 6rem;
      `;
    case 'md':
      return css`
        padding: 10rem 15rem;
        font-size: 12rem;
        min-height: 40rem;
        border-radius: 8rem;
      `;
    case 'lg':
      return css`
        padding: 12rem 20rem;
        font-size: 13rem;
        min-height: 45rem;
        border-radius: 10rem;
      `;
  }
};

const StyledButton = styled.button<ButtonStyledProps>`
  /* Base Styles */
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8rem;
  
  font-weight: 600;
  font-family: inherit;
  text-align: center;
  cursor: pointer;
  user-select: none;
  
  transition: all 0.2s ease;
  
  /* Variant Styles */
  ${props => getVariantStyles(props.$variant)}
  
  /* Size Styles */
  ${props => getSizeStyles(props.$size)}
  
  /* V232: Disabled State - use attribute selector instead of :disabled */
  &[disabled] {
    opacity: 0.5;
    cursor: not-allowed;
    filter: grayscale(50%);
    pointer-events: none;
  }
  
  /* V232: Active/Pressed State - simplified without :not(:disabled) */
  &:active {
    transform: translateY(2rem);
    box-shadow: 0 2rem 4rem rgba(0, 0, 0, 0.2);
  }
`;

const IconWrapper = styled.span<{ $position: 'left' | 'right' }>`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20rem;
  height: 20rem;
  
  svg {
    width: 100%;
    height: 100%;
  }
`;

const ButtonContent = styled.span`
  display: flex;
  align-items: center;
  gap: 8rem;
`;

// ===== COMPONENT =====

export default function ButtonEnhanced({
  variant = 'primary',
  size = 'md',
  icon,
  iconPosition = 'left',
  onClick,
  disabled = false,
  children,
  className
}: ButtonEnhancedProps) {
  
  const handleClick: MouseEventHandler<HTMLButtonElement> = (event) => {
    if (disabled || !onClick) return;
    onClick(event);
  };

  return (
    <StyledButton
      $variant={variant}
      $size={size}
      $disabled={disabled}
      $hasIcon={!!icon}
      onClick={handleClick}
      disabled={disabled}
      className={className}
    >
      <ButtonContent>
        {icon && iconPosition === 'left' && (
          <IconWrapper $position="left">{icon}</IconWrapper>
        )}
        {children}
        {icon && iconPosition === 'right' && (
          <IconWrapper $position="right">{icon}</IconWrapper>
        )}
      </ButtonContent>
    </StyledButton>
  );
}
