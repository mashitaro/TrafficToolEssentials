import { MouseEventHandler, useContext } from 'react';
import styled from 'styled-components';

import { LocaleContext } from '@/context';
import { getString } from '@/localisations';

const ButtonComponent = styled.div`
  padding: 8rem 12rem;
  border-radius: 6rem;
  color: rgba(255, 255, 255, 0.95);
  background: rgba(100, 180, 255, 0.15);
  border: 1rem solid rgba(100, 180, 255, 0.3);
  flex: 1;
  text-align: center;
  font-weight: 500;
`;

const ButtonDisabled = styled.div`
  padding: 8rem 12rem;
  border-radius: 6rem;
  color: rgba(255, 255, 255, 0.4);
  background: rgba(60, 70, 80, 0.6);
  border: 1rem solid rgba(255, 255, 255, 0.1);
  flex: 1;
  text-align: center;
  font-weight: 500;
`;

export default function Button(props: {label: string, disabled?: boolean, onClick?: MouseEventHandler<HTMLDivElement>}) {
  const locale = useContext(LocaleContext);
  const Comp = props.disabled ? ButtonDisabled : ButtonComponent;
  return (
    <Comp onClick={props.disabled ? undefined : props.onClick}>{getString(locale, props.label)}</Comp>
  );
}
