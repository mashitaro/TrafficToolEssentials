import { useContext } from 'react';
import styled from 'styled-components';

import { LocaleContext } from '@/context';
import { getString } from '@/localisations';

import Tooltip from '@/components/common/tooltip';
import TooltipIcon from '@/components/common/tooltip-icon';

const Container = styled.div`
  display: flex;
  justify-content: space-between;
  flex-grow: 1;
  flex-shrink: 1;
  flex-basis: auto;
  align-items: center;
`;

const TitleRow = styled.div`
  display: flex;
  align-items: center;
  flex-grow: 1;
  flex-shrink: 1;
  flex-basis: auto;
`;

const TitleText = styled.span`
  color: var(--textColorDim);
`;

const SecondaryText = styled.div`
  color: var(--textColorDim);
  margin-left: 6rem;
`;

export default function TitleDim(props: MainPanelItemTitle & {tooltip?: React.ReactNode}) {
  const locale = useContext(LocaleContext);
  return (
    <Container>
      <TitleRow>
        {props.tooltip && (
          <Tooltip position="right-start" tooltip={props.tooltip}>
            <TooltipIcon style={{marginRight: "0.4em"}} />
          </Tooltip>
        )}
        <TitleText>{getString(locale, props.title)}</TitleText>
      </TitleRow>
      {props.secondaryText && <SecondaryText>{getString(locale, props.secondaryText)}</SecondaryText>}
    </Container>
  );
}
