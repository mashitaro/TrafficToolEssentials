/**
 * MODERN SLIDER COMPONENT
 * 
 * Features:
 * - Large, clear Thumb
 * - Clearr Track mit filled Portion
 * - Value Display
 * - Einheit-Support
 * - Less technical, more clear
 */

import styled from 'styled-components';
import subMenuTokens from '@/styles/submenu-tokens';

// ===== STYLED COMPONENTS =====

const Container = styled.div`
  display: flex;
  flex-direction: column;
  gap: ${subMenuTokens.spacing.sliderLabelGap};
  width: 100%;
`;

const TopRow = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4rem;
`;

const Label = styled.label`
  font-size: ${subMenuTokens.typography.fontMD};
  color: ${subMenuTokens.colors.textPrimary};
  font-weight: ${subMenuTokens.typography.weightMedium};
  display: flex;
  align-items: center;
  gap: 6rem;
`;

const InfoIcon = styled.span`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 16rem;
  height: 16rem;
  border-radius: 50%;
  background: ${subMenuTokens.colors.backgroundTertiary};
  color: ${subMenuTokens.colors.textSecondary};
  font-size: 11rem;
  cursor: help;
  
  &:hover {
    background: ${subMenuTokens.colors.backgroundSecondary};
    color: ${subMenuTokens.colors.accentPrimary};
  }
`;

const ValueDisplay = styled.div`
  display: flex;
  align-items: baseline;
  gap: 4rem;
  font-size: ${subMenuTokens.typography.fontLG};
  color: ${subMenuTokens.colors.accentPrimary};
  font-weight: ${subMenuTokens.typography.weightSemiBold};
  font-variant-numeric: tabular-nums;
  min-width: 80rem;
  justify-content: flex-end;
`;

const Unit = styled.span`
  font-size: ${subMenuTokens.typography.fontSM};
  color: ${subMenuTokens.colors.textSecondary};
  font-weight: ${subMenuTokens.typography.weightRegular};
`;

const SliderContainer = styled.div`
  position: relative;
  height: ${subMenuTokens.spacing.sliderHeight};
  display: flex;
  align-items: center;
`;

const SliderTrack = styled.div`
  position: absolute;
  width: 100%;
  height: 6rem;
  background: ${subMenuTokens.colors.sliderTrack};
  border-radius: 3rem;
  overflow: hidden;
`;

const SliderTrackFilled = styled.div<{ $fillPercent: number }>`
  position: absolute;
  left: 0;
  top: 0;
  height: 100%;
  width: ${props => props.$fillPercent}%;
  background: ${subMenuTokens.colors.sliderTrackFilled};
  transition: width ${subMenuTokens.transitions.fast};
  border-radius: 3rem;
`;

const SliderInput = styled.input`
  position: relative;
  width: 100%;
  height: 100%;
  -webkit-appearance: none;
  appearance: none;
  background: transparent;
  cursor: pointer;
  z-index: 2;
  
  /* Thumb Styling - Chrome/Safari */
  &::-webkit-slider-thumb {
    -webkit-appearance: none;
    appearance: none;
    width: ${subMenuTokens.spacing.sliderThumbSize};
    height: ${subMenuTokens.spacing.sliderThumbSize};
    border-radius: 50%;
    background: ${subMenuTokens.colors.sliderThumb};
    cursor: pointer;
    box-shadow: 0 2rem 8rem rgba(0, 0, 0, 0.3);
    transition: all ${subMenuTokens.transitions.fast};
    
    &:hover {
      background: ${subMenuTokens.colors.sliderThumbHover};
      transform: scale(1.1);
      box-shadow: 0 4rem 12rem rgba(100, 180, 255, 0.4);
    }
    
    &:active {
      transform: scale(1.15);
      box-shadow: 0 6rem 16rem rgba(100, 180, 255, 0.5);
    }
  }
  
  /* Thumb Styling - Firefox */
  &::-moz-range-thumb {
    width: ${subMenuTokens.spacing.sliderThumbSize};
    height: ${subMenuTokens.spacing.sliderThumbSize};
    border-radius: 50%;
    background: ${subMenuTokens.colors.sliderThumb};
    cursor: pointer;
    border: none;
    box-shadow: 0 2rem 8rem rgba(0, 0, 0, 0.3);
    transition: all ${subMenuTokens.transitions.fast};
    
    &:hover {
      background: ${subMenuTokens.colors.sliderThumbHover};
      transform: scale(1.1);
      box-shadow: 0 4rem 12rem rgba(100, 180, 255, 0.4);
    }
    
    &:active {
      transform: scale(1.15);
      box-shadow: 0 6rem 16rem rgba(100, 180, 255, 0.5);
    }
  }
  
  /* Focus State */
  &:focus {
    outline: none;
  }
  
  &:focus-visible::-webkit-slider-thumb {
    box-shadow: 0 0 0 3rem ${subMenuTokens.colors.borderFocus},
                0 4rem 12rem rgba(100, 180, 255, 0.4);
  }
  
  &:focus-visible::-moz-range-thumb {
    box-shadow: 0 0 0 3rem ${subMenuTokens.colors.borderFocus},
                0 4rem 12rem rgba(100, 180, 255, 0.4);
  }
`;

const Ticks = styled.div`
  position: absolute;
  width: 100%;
  height: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  pointer-events: none;
  padding: 0 10rem;
`;

const Tick = styled.div`
  width: 2rem;
  height: 8rem;
  background: ${subMenuTokens.colors.borderSecondary};
  border-radius: 1rem;
`;

// ===== COMPONENT =====

interface SliderProps {
  label: string;
  value: number;
  min: number;
  max: number;
  step?: number;
  unit?: string;
  infoTooltip?: string;
  showTicks?: boolean;
  tickCount?: number;
  onChange: (value: number) => void;
  formatValue?: (value: number) => string;
}

export default function ModernSlider({
  label,
  value,
  min,
  max,
  step = 1,
  unit,
  infoTooltip,
  showTicks = false,
  tickCount = 5,
  onChange,
  formatValue
}: SliderProps) {
  const fillPercent = ((value - min) / (max - min || 1)) * 100;
  
  const displayValue = formatValue ? formatValue(value) : value.toString();
  
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange(Number(e.target.value));
  };
  
  return (
    <Container>
      <TopRow>
        <Label>
          {label}
          {infoTooltip && <InfoIcon title={infoTooltip}>?</InfoIcon>}
        </Label>
        <ValueDisplay>
          {displayValue}
          {unit && <Unit>{unit}</Unit>}
        </ValueDisplay>
      </TopRow>
      
      <SliderContainer>
        <SliderTrack>
          <SliderTrackFilled $fillPercent={fillPercent} />
        </SliderTrack>
        
        {showTicks && (
          <Ticks>
            {Array.from({ length: tickCount }).map((_, i) => (
              <Tick key={i} />
            ))}
          </Ticks>
        )}
        
        <SliderInput
          type="range"
          min={min}
          max={max}
          step={step}
          value={value}
          onChange={handleChange}
        />
      </SliderContainer>
    </Container>
  );
}
