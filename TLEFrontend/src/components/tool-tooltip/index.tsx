/**
 * V257 COHTML-SAFE: Tool Tooltip WITHOUT createPortal
 * 
 * NO transform - uses left/top positioning directly
 * NO filter - COHTML incompatible
 * V257: Very high z-index to appear above everything
 */

import { useContext, useEffect, useMemo, useState } from "react";
import styled from "styled-components";

import { bindValue, useValue } from "cs2/api";

import { LocaleContext } from "@/context";
import { getString } from "@/localisations";

// V257: Container with position:fixed, very high z-index
const Container = styled.div`
  position: fixed;
  z-index: 9999999;
  pointer-events: none;
`;

// V257: TooltipContainer - solid background, high visibility
const TooltipContainer = styled.div`
  font-size: var(--fontSizeXS);
  color: var(--textColorDim);
  background-color: var(--tooltipColor);
  border-radius: 4rem;
  padding: 0.25em 0.5em;
  margin: 0.25em 0 0 0;
  display: flex;
  flex-direction: row;
  align-items: center;
`;

const Image = styled.img`
  width: 1.25em;
  height: 1.25em;
  margin-right: 0.25em;
`;

export default function ToolTooltip() {
  const locale = useContext(LocaleContext);

  const [top, setTop] = useState(0);
  const [left, setLeft] = useState(0);

  const tooltipMessage: ToolTooltipMessage[] = useValue(bindValue("C2VM.TLE", "GetToolTooltipMessage", []));

  useEffect(() => {
    const mouseMoveHandler = (e: MouseEvent) => {
      setTop(e.clientY + 20);
      setLeft(e.clientX + 20);
    };
    document.body.addEventListener("mousemove", mouseMoveHandler);
    return () => document.body.removeEventListener("mousemove", mouseMoveHandler);
  }, []);

  const tooltip = useMemo(() => tooltipMessage.map(item => (
    <TooltipContainer key={item.image + item.message}>
      <Image src={item.image} />
      {getString(locale, item.message)}
    </TooltipContainer>
  )), [locale, tooltipMessage]);

  // V253: Only render when there's content
  if (tooltipMessage.length === 0 || top <= 0) {
    return null;
  }

  // V253: Use left/top directly instead of transform
  return (
    <Container style={{left: left, top: top}}>
      {tooltip}
    </Container>
  );
}
