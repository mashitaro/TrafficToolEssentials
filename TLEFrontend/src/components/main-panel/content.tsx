import { useContext } from 'react';
import styled from 'styled-components';

import { LocaleContext } from '@/context';
import { getString } from '@/localisations';

import Title from './items/title';
import Message from './items/message';
import Divider from './items/divider';
import Range from './items/range';
import Row from './items/row';
import Notification from './items/notification';

import Button from '@/components/common/button';
import Checkbox from '@/components/common/checkbox';
import Radio from '@/components/common/radio';
import Scrollable from '@/components/common/scrollable';
import Tooltip from '@/components/common/tooltip';

const Container = styled.div`
  width: 18em;
  background-color: var(--panelColorNormal);
  backdrop-filter: var(--panelBlur);
  color: var(--textColor);
  flex: 1;
  position: relative;
  padding: 0.25em;
  overflow-y: scroll;
`;

const Label = styled.span`
  color: var(--textColorDim);
  display: flex;
  flex: 1;
`;

// Green Wave Button - Active state (Eigene Phase selected) - Werder Bremen Green!
const GreenWaveButton = styled.div`
  padding: 8rem 12rem;
  border-radius: 6rem;
  color: rgba(255, 255, 255, 0.98);
  background: linear-gradient(135deg, rgba(30, 180, 90, 0.5) 0%, rgba(0, 166, 80, 0.45) 100%);
  border: 1rem solid rgba(30, 200, 100, 0.6);
  width: 100%;
  text-align: center;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  
  &:hover {
    background: linear-gradient(135deg, rgba(30, 200, 100, 0.6) 0%, rgba(0, 180, 90, 0.55) 100%);
    border-color: rgba(100, 220, 140, 0.8);
    transform: translateY(-1rem);
    box-shadow: 0 3rem 12rem rgba(30, 200, 100, 0.4);
  }
  
  &:active {
    transform: translateY(0);
    background: linear-gradient(135deg, rgba(0, 166, 80, 0.65) 0%, rgba(0, 140, 70, 0.6) 100%);
  }
`;

// Green Wave Display - Fake disabled look (just visual, no interaction)
const GreenWaveDisplay = styled.div`
  padding: 8rem 12rem;
  border-radius: 6rem;
  color: rgba(255, 255, 255, 0.4);
  background: rgba(60, 70, 80, 0.6);
  border: 1rem solid rgba(255, 255, 255, 0.1);
  width: 100%;
  text-align: center;
  font-weight: 500;
`;

export default function Content(props: {items: MainPanelItem[], onOpenGreenWave?: () => void}) {
  const locale = useContext(LocaleContext);
  return (
    <Container>
      <Scrollable style={{flex: 1}} contentStyle={{flex: 1}} trackStyle={{marginLeft: "0.25em"}}>
        {props.items.map((item) => {
          if (item.itemType == "title") {
            return <Row data={item}><Title {...item} /></Row>;
          }
          if (item.itemType == "message") {
            return <Row data={item}><Message {...item} /></Row>;
          }
          if (item.itemType == "divider") {
            return <Divider />;
          }
          if (item.itemType == "radio") {
            return (
              <Row data={item} hoverEffect={true}>
                <Radio {...item} />
                <Label>{getString(locale, item.label)}</Label>
              </Row>
            );
          }
          if (item.itemType == "checkbox") {
            return (
              <Row data={item} hoverEffect={true}>
                <Checkbox {...item} />
                <Label>{getString(locale, item.label)}</Label>
              </Row>
            );
          }
          if (item.itemType == "button") {
            // Special handling for Green Wave button
            if (item.label === "GreenWaveButton") {
              const isDisabled = item.value === "disabled";
              const tooltipText = isDisabled 
                ? getString(locale, "GreenWaveTooltipDisabled")
                : getString(locale, "GreenWaveTooltipActive");
              
              // If disabled: show fake display (no data = no onClick on Row)
              // If active: show real button with onClick
              if (isDisabled) {
                return (
                  <Row>
                    <div style={{flex: 1}}>
                      <Tooltip position="bottom" tooltip={tooltipText}>
                        <GreenWaveDisplay>
                          {getString(locale, item.label)}
                        </GreenWaveDisplay>
                      </Tooltip>
                    </div>
                  </Row>
                );
              } else {
                return (
                  <Row data={item}>
                    <div style={{flex: 1}}>
                      <Tooltip position="bottom" tooltip={tooltipText}>
                        <GreenWaveButton onClick={() => {
                          if (props.onOpenGreenWave) {
                            props.onOpenGreenWave();
                          }
                        }}>
                          {getString(locale, item.label)}
                        </GreenWaveButton>
                      </Tooltip>
                    </div>
                  </Row>
                );
              }
            }
            return <Row data={item}><Button {...item} /></Row>;
          }
          if (item.itemType == "notification") {
            return <Notification data={item} />;
          }
          if (item.itemType == "range") {
            return <Range data={item} />;
          }
          return <></>;
        })}
      </Scrollable>
    </Container>
  );
}