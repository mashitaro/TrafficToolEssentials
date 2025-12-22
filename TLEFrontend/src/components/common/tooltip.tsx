/**
 * V281 COHTML-SAFE Tooltip
 * 
 * Removed all transform CSS - uses direct positioning instead
 * V257: Increased z-index to ensure tooltips are always on top
 * V281: Added vertical boundary checking to prevent tooltips going off-screen
 */

import { CSSProperties, useCallback, useEffect, useRef, useState } from "react";
import styled from "styled-components";

// V257: Container wraps both children and tooltip
const Container = styled.div`
  display: inline-block;
  position: relative;
`;

// V257: Tooltip styles - very high z-index to appear above everything
const tooltipBaseStyle: CSSProperties = {
  position: 'fixed',
  fontSize: 'var(--fontSizeS)',
  color: 'rgba(255, 255, 255, 0.95)',
  background: 'rgba(20, 30, 45, 0.98)',
  border: '1rem solid rgba(100, 180, 255, 0.4)',
  borderRadius: '6rem',
  padding: '10rem 14rem',
  margin: 0,
  textAlign: 'left',
  zIndex: 9999999,
  maxWidth: '280rem',
  wordWrap: 'break-word',
  pointerEvents: 'none',
};

// V253: Position offsets - NO transform
const bottomOffset = { marginTop: 5 };
const rightOffset = { marginLeft: 5 };

// V253/V281: Minimum padding from screen edges
const EDGE_PADDING = 10;

export default function Tooltip(props: {position: "bottom" | "bottom-start" | "right-start" | "right", tooltip: React.ReactNode, tooltipStyle?: CSSProperties, children?: React.ReactNode}) {
  const containerRef = useRef<HTMLDivElement>(null);
  const tooltipRef = useRef<HTMLDivElement>(null);
  const [show, setShow] = useState(false);
  const [left, setLeft] = useState(0);
  const [top, setTop] = useState(0);

  const showTooltip = useCallback(() => {
    if (containerRef.current && !show) {
      const element = containerRef.current;
      const rect = element.getBoundingClientRect();
      
      // V253: Calculate position directly, no transform needed
      if (props.position == "bottom") {
        // Center below: use rect center, adjust after tooltip renders
        setLeft(rect.left + rect.width / 2);
        setTop(rect.bottom + bottomOffset.marginTop);
      } else if (props.position == "bottom-start") {
        setLeft(rect.left);
        setTop(rect.bottom + bottomOffset.marginTop);
      } else if (props.position == "right-start") {
        setLeft(rect.right + rightOffset.marginLeft);
        setTop(rect.top);
      } else if (props.position == "right") {
        setLeft(rect.right + rightOffset.marginLeft);
        setTop(rect.top + rect.height / 2);
      }
      setShow(true);
    }
  }, [containerRef, show, props.position]);

  const hideTooltip = useCallback(() => {
    setShow(false);
  }, []);

  // V253/V281: Adjust position after render to center tooltip and apply boundaries
  useEffect(() => {
    if (show && tooltipRef.current) {
      const timeoutId = setTimeout(() => {
        if (tooltipRef.current) {
          const tooltipRect = tooltipRef.current.getBoundingClientRect();
          const screenWidth = window.innerWidth;
          const screenHeight = window.innerHeight;
          
          // Center adjustment for "bottom" and "right" positions
          if (props.position === "bottom") {
            setLeft(prev => prev - tooltipRect.width / 2);
          } else if (props.position === "right") {
            setTop(prev => prev - tooltipRect.height / 2);
          }
          
          // V281: Horizontal boundary check
          if (tooltipRect.left < EDGE_PADDING) {
            setLeft(EDGE_PADDING);
          }
          else if (tooltipRect.right > screenWidth - EDGE_PADDING) {
            setLeft(screenWidth - EDGE_PADDING - tooltipRect.width);
          }
          
          // V281: Vertical boundary check - NEW!
          if (tooltipRect.top < EDGE_PADDING) {
            setTop(EDGE_PADDING);
          }
          else if (tooltipRect.bottom > screenHeight - EDGE_PADDING) {
            setTop(screenHeight - EDGE_PADDING - tooltipRect.height);
          }
        }
      }, 10);
      
      return () => clearTimeout(timeoutId);
    }
  }, [show, props.position]);

  // Workaround for mouseleave not firing reliably
  useEffect(() => {
    if (containerRef.current && show) {
      const mouseMoveHandler = (e: MouseEvent) => {
        if (containerRef.current) {
          const rect = containerRef.current.getBoundingClientRect();
          if (e.clientX < rect.left || e.clientX > rect.right || e.clientY < rect.top || e.clientY > rect.bottom) {
            hideTooltip();
          }
        }
      };
      document.body.addEventListener("mousemove", mouseMoveHandler);
      return () => document.body.removeEventListener("mousemove", mouseMoveHandler);
    }
  }, [containerRef, show, hideTooltip]);

  // V253: Tooltip is a CHILD of Container with position:fixed
  return (
    <Container ref={containerRef} onMouseEnter={showTooltip} onMouseLeave={hideTooltip}>
      {props.children}
      {show && (
        <div 
          ref={tooltipRef} 
          style={{
            ...tooltipBaseStyle,
            left,
            top,
            ...props.tooltipStyle
          }}
        >
          {props.tooltip}
        </div>
      )}
    </Container>
  );
}
