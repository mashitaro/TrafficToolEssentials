/**
 * MODERN CHECKBOX & RADIO COMPONENTS
 * 
 * Features:
 * - Klare, große Hit-Boxen
 * - Sanfte Animationen
 * - Deutlich sichtbare States
 * - Übersichtliches Design
 */

import styled from 'styled-components';
import subMenuTokens from '@/styles/submenu-tokens';

// ===== CHECKBOX =====

const CheckboxContainer = styled.label`
  display: flex;
  align-items: center;
  gap: 12rem;
  cursor: pointer;
  padding: 8rem 0;
  user-select: none;
  
  &:hover {
    background: ${subMenuTokens.colors.hoverOverlay};
    margin: 0 -12rem;
    padding: 8rem 12rem;
    border-radius: ${subMenuTokens.borders.radiusSM};
  }
`;

const CheckboxInput = styled.input.attrs({ type: 'checkbox' })`
  position: absolute;
  opacity: 0;
  cursor: pointer;
`;

const CheckboxCustom = styled.div<{ $checked: boolean }>`
  position: relative;
  width: 20rem;
  height: 20rem;
  border: 2rem solid ${props => 
    props.$checked 
      ? subMenuTokens.colors.checkboxChecked 
      : subMenuTokens.colors.checkboxBorder
  };
  border-radius: ${subMenuTokens.borders.radiusSM};
  background: ${props => 
    props.$checked 
      ? subMenuTokens.colors.checkboxChecked 
      : 'transparent'
  };
  transition: all ${subMenuTokens.transitions.fast};
  flex-shrink: 0;
  
  /* Checkmark */
  &::after {
    content: '';
    position: absolute;
    display: ${props => props.$checked ? 'block' : 'none'};
    left: 6rem;
    top: 2rem;
    width: 5rem;
    height: 10rem;
    border: solid white;
    border-width: 0 2rem 2rem 0;
    transform: rotate(45deg);
  }
  
  ${CheckboxInput}:focus-visible + & {
    box-shadow: 0 0 0 3rem ${subMenuTokens.colors.borderFocus};
  }
  
  /* V232: Use attribute selector instead of :disabled for COHTML */
  ${CheckboxInput}[disabled] + & {
    opacity: 0.4;
    cursor: not-allowed;
  }
`;

const CheckboxLabel = styled.span`
  font-size: ${subMenuTokens.typography.fontMD};
  color: ${subMenuTokens.colors.textPrimary};
  line-height: ${subMenuTokens.typography.lineHeightNormal};
`;

interface CheckboxProps {
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  disabled?: boolean;
}

export function ModernCheckbox({ label, checked, onChange, disabled }: CheckboxProps) {
  return (
    <CheckboxContainer>
      <CheckboxInput
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        disabled={disabled}
      />
      <CheckboxCustom $checked={checked} />
      <CheckboxLabel>{label}</CheckboxLabel>
    </CheckboxContainer>
  );
}

// ===== RADIO BUTTON =====

const RadioContainer = styled.label`
  display: flex;
  align-items: center;
  gap: 12rem;
  cursor: pointer;
  padding: 10rem 0;
  user-select: none;
  
  &:hover {
    background: ${subMenuTokens.colors.hoverOverlay};
    margin: 0 -12rem;
    padding: 10rem 12rem;
    border-radius: ${subMenuTokens.borders.radiusSM};
  }
`;

const RadioInput = styled.input.attrs({ type: 'radio' })`
  position: absolute;
  opacity: 0;
  cursor: pointer;
`;

const RadioCustom = styled.div<{ $selected: boolean }>`
  position: relative;
  width: 20rem;
  height: 20rem;
  border: 2rem solid ${props => 
    props.$selected 
      ? subMenuTokens.colors.radioSelected 
      : subMenuTokens.colors.radioBorder
  };
  border-radius: 50%;
  background: transparent;
  transition: all ${subMenuTokens.transitions.fast};
  flex-shrink: 0;
  
  /* Inner Dot */
  &::after {
    content: '';
    position: absolute;
    display: ${props => props.$selected ? 'block' : 'none'};
    left: 50%;
    top: 50%;
    transform: translate(-50%, -50%);
    width: 10rem;
    height: 10rem;
    border-radius: 50%;
    background: ${subMenuTokens.colors.radioSelected};
  }
  
  ${RadioInput}:focus-visible + & {
    box-shadow: 0 0 0 3rem ${subMenuTokens.colors.borderFocus};
  }
  
  /* V232: Use attribute selector instead of :disabled for COHTML */
  ${RadioInput}[disabled] + & {
    opacity: 0.4;
    cursor: not-allowed;
  }
`;

const RadioLabel = styled.span`
  font-size: ${subMenuTokens.typography.fontMD};
  color: ${subMenuTokens.colors.textPrimary};
  line-height: ${subMenuTokens.typography.lineHeightNormal};
`;

interface RadioProps {
  label: string;
  value: string;
  selectedValue: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  name: string;
}

export function ModernRadio({ label, value, selectedValue, onChange, disabled, name }: RadioProps) {
  const isSelected = value === selectedValue;
  
  return (
    <RadioContainer>
      <RadioInput
        name={name}
        value={value}
        checked={isSelected}
        onChange={() => onChange(value)}
        disabled={disabled}
      />
      <RadioCustom $selected={isSelected} />
      <RadioLabel>{label}</RadioLabel>
    </RadioContainer>
  );
}

// ===== RADIO GROUP =====

const RadioGroupContainer = styled.div`
  display: flex;
  flex-direction: column;
  gap: 4rem;
`;

const RadioGroupLabel = styled.div`
  font-size: ${subMenuTokens.typography.fontSM};
  color: ${subMenuTokens.colors.textSecondary};
  font-weight: ${subMenuTokens.typography.weightMedium};
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 8rem;
`;

interface RadioGroupProps {
  label?: string;
  name: string;
  options: Array<{ value: string; label: string }>;
  selectedValue: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}

export function ModernRadioGroup({ 
  label, 
  name, 
  options, 
  selectedValue, 
  onChange, 
  disabled 
}: RadioGroupProps) {
  return (
    <div>
      {label && <RadioGroupLabel>{label}</RadioGroupLabel>}
      <RadioGroupContainer>
        {options.map(option => (
          <ModernRadio
            key={option.value}
            name={name}
            label={option.label}
            value={option.value}
            selectedValue={selectedValue}
            onChange={onChange}
            disabled={disabled}
          />
        ))}
      </RadioGroupContainer>
    </div>
  );
}
