/**
 * SIMPLE MODERN RANGE WRAPPER
 * 
 * Minimalistischer Wrapper für MainPanelRange
 * - KEINE weißen Kästen!
 * - Passt perfekt ins bestehende UI
 * - Große Slider Thumbs
 * - Main Menu Farben
 */

import { ChangeEvent, KeyboardEvent, useContext, useEffect, useMemo, useState } from 'react';
import styled from 'styled-components';

import { LocaleContext } from '@/context';
import { engineCall } from '@/engine';
import { getString } from '@/localisations';

import Input from '@/components/common/input';
import subMenuTokens from '@/styles/submenu-tokens';

import Check from '@/components/common/icons/check';
import Edit from '@/components/common/icons/edit';
import ResetSettings from '@/components/common/icons/reset-settings';

// ===== STYLED COMPONENTS =====

const Container = styled.div`
  padding: 8rem 8rem;
  margin: 4rem 0;
  /* KEIN Background! KEIN Border! */
`;

const TopRow = styled.div`
  display: flex;
  align-items: center;
  gap: 8rem;
  margin-bottom: 8rem;
`;

const LabelGroup = styled.div`
  flex: 1;
  display: flex;
  align-items: center;
  gap: 6rem;
  color: var(--textColorDim);
  font-size: 13rem;
`;

const ValueDisplay = styled.div`
  color: var(--textColor);
  font-size: 14rem;
  font-weight: 500;
  min-width: 60rem;
  text-align: right;
`;

const IconButton = styled.button`
  background: transparent;
  border: none;
  padding: 4rem;
  cursor: pointer;
  border-radius: 4rem;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--textColorDim);
  
  &:hover {
    background: rgba(255, 255, 255, 0.1);
    color: var(--textColor);
  }
`;

const IconStyle = {
  width: "16rem",
  height: "16rem"
};

// ===== SLIDER (Simplified!) =====

const SliderContainer = styled.div`
  position: relative;
  height: 36rem;
  display: flex;
  align-items: center;
  width: 100%;
  /* KEIN Background! KEIN Border! */
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
  transition: width 150ms ease-in-out;
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
  
  /* Thumb - Chrome/Safari */
  &::-webkit-slider-thumb {
    -webkit-appearance: none;
    appearance: none;
    width: 20rem;
    height: 20rem;
    border-radius: 50%;
    background: ${subMenuTokens.colors.sliderThumb};
    cursor: pointer;
    box-shadow: 0 2rem 8rem rgba(0, 0, 0, 0.3);
    transition: all 150ms ease-in-out;
    
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
  
  /* Thumb - Firefox */
  &::-moz-range-thumb {
    width: 20rem;
    height: 20rem;
    border-radius: 50%;
    background: ${subMenuTokens.colors.sliderThumb};
    cursor: pointer;
    border: none;
    box-shadow: 0 2rem 8rem rgba(0, 0, 0, 0.3);
    transition: all 150ms ease-in-out;
    
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
  
  /* Focus */
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

const TextFieldInput = styled(Input)`
  min-width: 80rem;
  width: 100rem;
`;

// ===== COMPONENT =====

interface ModernRangeWrapperProps {
  data: MainPanelItemRange;
  tooltip?: React.ReactNode;
}

export default function ModernRangeWrapper({ data, tooltip }: ModernRangeWrapperProps) {
  const locale = useContext(LocaleContext);
  const [value, setValue] = useState(data.value);
  const [textFieldActive, setTextFieldActive] = useState(false);
  const [textFieldValue, setTextFieldValue] = useState("");
  
  const textFieldRegExp = useMemo(() => {
    return data.textFieldRegExp ? new RegExp(data.textFieldRegExp) : null;
  }, [data.textFieldRegExp]);
  
  useEffect(() => {
    setValue(data.value);
  }, [data.value]);
  
  const handleSliderChange = (newValue: number) => {
    setValue(newValue);
    if ("engineEventName" in data) {
      engineCall(data.engineEventName, JSON.stringify({key: data.key, value: newValue}));
    }
  };
  
  const enableTextField = () => {
    setTextFieldValue(value.toString());
    setTextFieldActive(true);
  };
  
  const submitTextField = () => {
    setTextFieldActive(false);
    if (textFieldValue.length > 0) {
      const newValue = parseFloat(textFieldValue);
      if (!isNaN(newValue)) {
        setValue(newValue);
        if ("engineEventName" in data) {
          engineCall(data.engineEventName, JSON.stringify({key: data.key, value: newValue}));
        }
      }
    }
  };
  
  const textFieldChangeHandler = (event: ChangeEvent<HTMLInputElement>) => {
    if (textFieldRegExp !== null) {
      if (event.target.value.match(textFieldRegExp)) {
        setTextFieldValue(event.target.value);
      }
    } else {
      setTextFieldValue(event.target.value);
    }
  };
  
  const textFieldKeyDownHandler = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Enter") {
      submitTextField();
    }
  };
  
  const resetHandler = () => {
    setTextFieldActive(false);
    setValue(data.defaultValue);
    if ("engineEventName" in data) {
      engineCall(data.engineEventName, JSON.stringify({key: data.key, value: data.defaultValue}));
    }
  };
  
  const formatValue = (val: number) => {
    const rounded = Math.round(val * 100) / 100;
    const prefix = getString(locale, data.valuePrefix);
    const suffix = getString(locale, data.valueSuffix);
    return prefix ? `${prefix}${rounded}${suffix}` : `${rounded}${suffix}`;
  };
  
  const fillPercent = ((value - data.min) / (data.max - data.min)) * 100;
  
  return (
    <Container>
      {/* Top Row: Label, Value, Icons */}
      <TopRow>
        <LabelGroup>
          {getString(locale, data.label)}
          {tooltip && <span style={{color: "var(--textColorDim)", cursor: "help"}}>ⓘ</span>}
        </LabelGroup>
        
        {!textFieldActive && (
          <ValueDisplay>{formatValue(value)}</ValueDisplay>
        )}
        
        {textFieldActive && (
          <TextFieldInput
            type="number"
            onChange={textFieldChangeHandler}
            onKeyDown={textFieldKeyDownHandler}
            value={textFieldValue}
            autoFocus
          />
        )}
        
        {data.enableTextField && (
          <>
            {textFieldActive ? (
              <IconButton onClick={submitTextField} title="Confirm">
                <Check style={IconStyle} />
              </IconButton>
            ) : (
              <IconButton onClick={enableTextField} title="Edit">
                <Edit style={IconStyle} />
              </IconButton>
            )}
          </>
        )}
        
        <IconButton onClick={resetHandler} title="Reset to default">
          <ResetSettings style={IconStyle} />
        </IconButton>
      </TopRow>
      
      {/* Slider (SIMPLE!) */}
      <SliderContainer>
        <SliderTrack>
          <SliderTrackFilled $fillPercent={fillPercent} />
        </SliderTrack>
        
        <SliderInput
          type="range"
          min={data.min}
          max={data.max}
          step={data.step}
          value={value}
          onChange={(e) => handleSliderChange(Number(e.target.value))}
        />
      </SliderContainer>
    </Container>
  );
}
