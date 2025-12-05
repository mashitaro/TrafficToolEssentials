/**
 * Edit Icon - Fuer "Kreuzungsphasen bearbeiten"
 * 
 * SVG Icon mit variabler Groesse und Farbe
 */

import { SVGProps } from "react";

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: string | number;
  color?: string;
}

export default function IconEdit({ size = '1.5rem', color = 'currentColor', className, ...props }: IconProps) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke={color}
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      style={{ display: 'block' }}
      {...props}
    >
      {/* Stift/Pencil */}
      <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
      <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
    </svg>
  );
}
