/**
 * Monitor Icon - Fuer "Kreuzungsueberwachung"
 * 
 * SVG Icon mit variabler Groesse und Farbe
 * Zeigt ein Dashboard/Analytics Symbol
 */

import { SVGProps } from "react";

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: string | number;
  color?: string;
}

export default function IconMonitor({ size = '1.5rem', color = 'currentColor', className, ...props }: IconProps) {
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
      {/* Monitor/Screen */}
      <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
      <line x1="8" y1="21" x2="16" y2="21" />
      <line x1="12" y1="17" x2="12" y2="21" />
      {/* Chart/Analytics Linien */}
      <polyline points="6,10 9,7 13,11 18,6" />
      <circle cx="6" cy="10" r="1" fill={color} />
      <circle cx="9" cy="7" r="1" fill={color} />
      <circle cx="13" cy="11" r="1" fill={color} />
      <circle cx="18" cy="6" r="1" fill={color} />
    </svg>
  );
}
