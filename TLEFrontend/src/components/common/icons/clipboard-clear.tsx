import { CSSProperties } from "react";

interface IconProps {
  style?: CSSProperties;
  onClick?: () => void;
}

export default function ClipboardClear(props: IconProps) {
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
      <path d="M19,3H14.82C14.4,1.84 13.3,1 12,1C10.7,1 9.6,1.84 9.18,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M12,3A1,1 0 0,1 13,4A1,1 0 0,1 12,5A1,1 0 0,1 11,4A1,1 0 0,1 12,3M15.54,15.54L13.41,13.41L15.54,11.29L14.12,9.88L12,12L9.88,9.88L8.46,11.29L10.59,13.41L8.46,15.54L9.88,16.95L12,14.83L14.12,16.95L15.54,15.54Z" />
    </svg>
  );
}
