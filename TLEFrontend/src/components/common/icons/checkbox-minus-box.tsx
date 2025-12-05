import { CSSProperties } from "react";

interface IconProps {
  style?: CSSProperties;
  onClick?: () => void;
}

export default function CheckboxMinusBox(props: IconProps) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      style={{
        fill: "currentColor",
        width: "1em",
        height: "1em",
        ...props.style
      }}
      onClick={props.onClick}
    >
      <path d="M19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M19,19H5V5H19V19M17,11H7V13H17V11Z" />
    </svg>
  );
}
