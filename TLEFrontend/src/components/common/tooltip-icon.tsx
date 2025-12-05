import styled from "styled-components";

import Info from "@/components/common/icons/info";
import { CSSProperties } from "react";

const IconContainer = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
`;

const IconStyle = {
  color: "rgba(100, 180, 255, 1)",
  width: "1.3em",
  height: "1.3em",
  fontSize: "1.3em"
};

export default function TooltipIcon(props: {style?: CSSProperties, iconStyle?: CSSProperties}) {
  return (
    <IconContainer style={props.style}>
      <Info style={{...IconStyle, ...props.iconStyle}} />
    </IconContainer>
  );
}
