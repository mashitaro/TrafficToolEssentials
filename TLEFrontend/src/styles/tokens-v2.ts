/**
 * Design Tokens V2 - Modern & Clean
 * 
 * ÄNDERUNGEN von V1:
 * - Weniger bunte Farben (kein grelles Cyan/Orange)
 * - Moderne Grau-Töne
 * - Subtile Akzentfarben
 * - Für Cities Skylines 2 optimiert (große rem-Werte)
 * 
 * FIX: TypeScript-Fehler behoben (transitions self-reference)
 */

// ===== FARBEN =====

export const colors = {
  // Backgrounds (dunkel, modern)
  backgroundPrimary: 'rgba(20, 25, 30, 0.92)',      // Sehr dunkles Grau
  backgroundSecondary: 'rgba(30, 35, 40, 0.85)',    // Dunkles Grau
  backgroundTertiary: 'rgba(40, 45, 50, 0.75)',     // Mitteldunkles Grau
  
  // Borders (subtil)
  borderPrimary: 'rgba(255, 255, 255, 0.12)',       // Sehr subtil
  borderSecondary: 'rgba(255, 255, 255, 0.08)',     // Noch subtiler
  borderFocus: 'rgba(100, 180, 255, 0.5)',          // Blau bei Fokus
  
  // Text (gut lesbar)
  textPrimary: 'rgba(255, 255, 255, 0.95)',         // Sehr hell
  textSecondary: 'rgba(255, 255, 255, 0.7)',        // Hell
  textTertiary: 'rgba(255, 255, 255, 0.5)',         // Gedimmt
  textDisabled: 'rgba(255, 255, 255, 0.3)',         // Deaktiviert
  
  // Akzente (subtil, nicht grell!)
  accentPrimary: 'rgba(100, 180, 255, 1)',          // Sanftes Blau (statt grelles Cyan)
  accentSecondary: 'rgba(120, 150, 255, 1)',        // Violett-Blau
  accentSuccess: 'rgba(80, 200, 120, 1)',           // Grün
  accentWarning: 'rgba(255, 180, 80, 1)',           // Orange (sanfter)
  accentDanger: 'rgba(255, 100, 100, 1)',           // Rot
  
  // Hover/Active States
  hoverOverlay: 'rgba(255, 255, 255, 0.05)',        // Sehr subtil
  activeOverlay: 'rgba(255, 255, 255, 0.1)',        // Subtil
  selectedOverlay: 'rgba(100, 180, 255, 0.15)',     // Blau-Overlay
  
  // Shadows
  shadowSubtle: 'rgba(0, 0, 0, 0.2)',               // Leichter Schatten
  shadowMedium: 'rgba(0, 0, 0, 0.3)',               // Mittlerer Schatten
  shadowStrong: 'rgba(0, 0, 0, 0.5)',               // Starker Schatten
};

// ===== SPACING (CS2-optimiert: 10-12x größer!) =====

export const spacing = {
  // Basis-Einheiten
  xs: '4rem',       // Extra small
  sm: '8rem',       // Small
  md: '12rem',      // Medium
  lg: '16rem',      // Large
  xl: '20rem',      // Extra large
  xxl: '24rem',     // XX Large
  xxxl: '32rem',    // XXX Large
  
  // Container/Panel
  panelPadding: '20rem',
  panelGap: '16rem',
  
  // Buttons
  buttonGap: '12rem',        // Gap zwischen Buttons
  buttonPadding: '12rem 18rem',
  buttonMinHeight: '48rem',
  
  // Form Elements
  inputPadding: '10rem 12rem',
  inputHeight: '40rem',
  
  // Sections
  sectionGap: '20rem',
  
  // Items in Lists
  itemGap: '8rem',
};

// ===== TYPOGRAPHY (CS2-optimiert) =====

export const typography = {
  // Font Sizes (10-12x größer!)
  fontXS: '10rem',      // Extra small
  fontSM: '11rem',      // Small
  fontMD: '13rem',      // Medium (Base)
  fontLG: '15rem',      // Large
  fontXL: '18rem',      // Extra large (Titles)
  fontXXL: '22rem',     // XX Large (Big Titles)
  
  // Font Weights
  weightNormal: 400,
  weightMedium: 500,
  weightSemiBold: 600,
  weightBold: 700,
  
  // Line Heights
  lineHeightTight: 1.2,
  lineHeightNormal: 1.4,
  lineHeightRelaxed: 1.6,
  
  // Letter Spacing
  letterSpacingTight: '-0.01em',
  letterSpacingNormal: '0',
  letterSpacingWide: '0.02em',
};

// ===== BORDERS & RADIUS =====

export const borders = {
  // Border Width
  borderThin: '1rem',
  borderMedium: '2rem',
  borderThick: '3rem',
  
  // Border Radius
  radiusNone: '0',
  radiusSmall: '4rem',
  radiusMedium: '6rem',
  radiusLarge: '8rem',
  radiusXLarge: '12rem',
  radiusFull: '999rem',
};

// ===== SHADOWS =====

export const shadows = {
  // Box Shadows
  shadowNone: 'none',
  shadowSubtle: `0 2rem 4rem ${colors.shadowSubtle}`,
  shadowMedium: `0 4rem 8rem ${colors.shadowMedium}`,
  shadowLarge: `0 6rem 12rem ${colors.shadowMedium}`,
  shadowXLarge: `0 8rem 16rem ${colors.shadowStrong}`,
  
  // Text Shadows (für Lesbarkeit)
  textShadowSubtle: `0 1rem 2rem ${colors.shadowSubtle}`,
  textShadowMedium: `0 2rem 4rem ${colors.shadowMedium}`,
};

// ===== TRANSITIONS =====

// FIX: Definiere Werte separat, um Self-Reference zu vermeiden
const durationFast = '0.15s';
const durationNormal = '0.25s';
const durationSlow = '0.35s';

const easingDefault = 'cubic-bezier(0.4, 0, 0.2, 1)';
const easingIn = 'cubic-bezier(0.4, 0, 1, 1)';
const easingOut = 'cubic-bezier(0, 0, 0.2, 1)';
const easingInOut = 'cubic-bezier(0.4, 0, 0.2, 1)';

export const transitions = {
  // Durations
  durationFast,
  durationNormal,
  durationSlow,
  
  // Easings
  easingDefault,
  easingIn,
  easingOut,
  easingInOut,
  
  // Combined (jetzt ohne Self-Reference!)
  default: `${durationNormal} ${easingDefault}`,
  fast: `${durationFast} ${easingDefault}`,
  slow: `${durationSlow} ${easingDefault}`,
};

// ===== Z-INDEX =====

export const zIndex = {
  base: 0,
  dropdown: 100,
  sticky: 200,
  fixed: 300,
  modal: 400,
  popover: 500,
  tooltip: 600,
};

// ===== BLUR (für backdrop-filter) =====

export const blur = {
  none: '0',
  subtle: 'blur(4px)',
  medium: 'blur(8px)',
  strong: 'blur(12px)',
};

// ===== EXPORT ALL =====

const tokens = {
  colors,
  spacing,
  typography,
  borders,
  shadows,
  transitions,
  zIndex,
  blur,
};

export default tokens;
