/**
 * Design System Tokens - Cities Skylines 2 Style
 * 
 * Dieses Design System orientiert sich am visuellen Stil von Cities Skylines 2:
 * - Modern aber game-like
 * - Cyan/Türkis als Primary Color
 * - Dunkelgrau/Schwarz für Backgrounds
 * - Orange/Gelb für wichtige Actions
 * - 3D-Effekte durch Schatten und Depth
 */

export const tokens = {
  // ===== COLORS =====
  colors: {
    // Primary - Cyan/Türkis (wie im Spiel)
    primary: {
      base: 'var(--accentColorNormal)',      // Cyan normal
      light: 'var(--accentColorLighter)',    // Cyan hell
      glow: 'rgba(0, 255, 255, 0.4)',        // Cyan glow für Hover
    },
    
    // Secondary - Dunkelgrau/Schwarz
    secondary: {
      base: 'var(--toolbarFieldColor)',      // Toolbar/Button BG
      dark: 'var(--panelColorDark)',         // Panel dunkel
      normal: 'var(--panelColorNormal)',     // Panel normal
    },
    
    // Accent - Orange/Gelb für wichtige Actions
    accent: {
      base: '#ff9500',                       // Orange
      light: '#ffb340',                      // Orange hell
      glow: 'rgba(255, 149, 0, 0.4)',        // Orange glow
    },
    
    // Text
    text: {
      primary: 'var(--textColor)',           // Text normal
      dim: 'var(--textColorDim)',            // Text gedimmt
      onPrimary: '#ffffff',                  // Text auf Primary Color
      onAccent: '#ffffff',                   // Text auf Accent Color
    },
    
    // States
    states: {
      hover: 'rgba(255, 255, 255, 0.1)',     // Hover overlay
      pressed: 'rgba(0, 0, 0, 0.2)',         // Pressed overlay
      disabled: 'rgba(255, 255, 255, 0.3)',  // Disabled overlay
    },
  },

  // ===== SPACING =====
  spacing: {
    xs: '0.25rem',   // 4px
    sm: '0.5rem',    // 8px
    md: '1rem',      // 16px
    lg: '1.5rem',    // 24px
    xl: '2rem',      // 32px
    xxl: '3rem',     // 48px
  },

  // ===== SHADOWS =====
  shadows: {
    // Basis Schatten für 3D-Effekt
    sm: '0 1px 2px rgba(0, 0, 0, 0.3)',
    md: '0 2px 4px rgba(0, 0, 0, 0.4)',
    lg: '0 4px 8px rgba(0, 0, 0, 0.5)',
    
    // Hover Schatten - mehr Depth
    hoverSm: '0 2px 4px rgba(0, 0, 0, 0.4)',
    hoverMd: '0 4px 8px rgba(0, 0, 0, 0.5)',
    hoverLg: '0 6px 12px rgba(0, 0, 0, 0.6)',
    
    // Glow Effekte
    glowPrimary: '0 0 8px var(--glow-primary, rgba(0, 255, 255, 0.4))',
    glowAccent: '0 0 8px var(--glow-accent, rgba(255, 149, 0, 0.4))',
  },

  // ===== TRANSITIONS =====
  transitions: {
    fast: '150ms ease-in-out',
    normal: '250ms ease-in-out',
    slow: '400ms ease-in-out',
    
    // Spezifische Transitions
    button: '200ms ease-in-out',
    transform: '200ms cubic-bezier(0.4, 0, 0.2, 1)',
    shadow: '200ms ease-in-out',
    color: '150ms ease-in-out',
  },

  // ===== BORDER RADIUS =====
  radius: {
    sm: '2rem',      // 32px - klein
    md: '3rem',      // 48px - mittel (Standard für Buttons)
    lg: '4rem',      // 64px - groß
  },

  // ===== SIZES =====
  sizes: {
    button: {
      sm: {
        padding: '0.5rem 1rem',      // 8px 16px
        fontSize: '0.875rem',         // 14px
        iconSize: '1rem',             // 16px
        height: '2rem',               // 32px
      },
      md: {
        padding: '0.75rem 1.5rem',   // 12px 24px
        fontSize: '1rem',             // 16px
        iconSize: '1.25rem',          // 20px
        height: '2.5rem',             // 40px
      },
      lg: {
        padding: '1rem 2rem',        // 16px 32px
        fontSize: '1.125rem',         // 18px
        iconSize: '1.5rem',           // 24px
        height: '3rem',               // 48px
      },
    },
  },

  // ===== Z-INDEX =====
  zIndex: {
    base: 1,
    dropdown: 1000,
    overlay: 2000,
    modal: 3000,
    tooltip: 4000,
  },
};

// Type Helpers für TypeScript
export type ButtonSize = 'sm' | 'md' | 'lg';
export type ButtonVariant = 'primary' | 'secondary' | 'accent';
