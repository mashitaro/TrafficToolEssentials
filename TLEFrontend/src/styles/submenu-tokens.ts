/**
 * SUB-MENU DESIGN TOKENS - V2 IMPROVED
 * 
 * Design principles:
 * - Colors based on Main Menu (tokens-v2)
 * - More muted/subtle than Main Menu
 * - Consistent with Main Menu aesthetics
 * - Not cramped (enough padding/spacing)
 * - Modern sliders (less technical)
 */

export const subMenuTokens = {
  // ===== COLORS (Based on Main Menu, but muted) =====
  colors: {
    // Backgrounds (slightly lighter than Main Menu for differentiation)
    backgroundPrimary: 'rgba(25, 30, 35, 0.94)',      // Slightly lighter than Main (20, 25, 30)
    backgroundSecondary: 'rgba(35, 40, 45, 0.88)',    // For sections
    backgroundTertiary: 'rgba(45, 50, 55, 0.82)',     // For hover
    backgroundHeader: 'rgba(30, 35, 40, 0.96)',       // Header background
    
    // Borders (subtle, like Main Menu)
    borderPrimary: 'rgba(255, 255, 255, 0.10)',       // Main border (Main: 0.12)
    borderSecondary: 'rgba(255, 255, 255, 0.06)',     // Dividers (Main: 0.08)
    borderFocus: 'rgba(100, 180, 255, 0.4)',          // Focus (muted) (Main: 0.5)
    
    // Text (muted compared to Main Menu)
    textPrimary: 'rgba(255, 255, 255, 0.90)',         // Main text (Main: 0.95)
    textSecondary: 'rgba(255, 255, 255, 0.65)',       // Secondary (Main: 0.7)
    textTertiary: 'rgba(255, 255, 255, 0.45)',        // Labels (Main: 0.5)
    textDisabled: 'rgba(255, 255, 255, 0.25)',        // Disabled (Main: 0.3)
    
    // Accents (SAME color as Main Menu, but with reduced opacity!)
    // Main Menu: rgba(100, 180, 255, 1) - we use 0.85 for more subtle
    accentPrimary: 'rgba(100, 180, 255, 0.85)',       // Soft blue (Main color, less intense)
    accentSecondary: 'rgba(120, 150, 255, 0.75)',     // Violet-blue (muted)
    accentSuccess: 'rgba(80, 200, 120, 0.7)',         // Green (muted)
    accentWarning: 'rgba(255, 180, 80, 0.7)',         // Orange (muted)
    
    // Interactive Elements
    checkboxBorder: 'rgba(255, 255, 255, 0.25)',      // Checkbox Border
    checkboxChecked: 'rgba(100, 180, 255, 0.9)',      // Checked State (Main Menu blue)
    radioBorder: 'rgba(255, 255, 255, 0.25)',         // Radio Border
    radioSelected: 'rgba(100, 180, 255, 0.9)',        // Selected State
    
    // Slider Colors (using Main Menu accent!)
    sliderTrack: 'rgba(255, 255, 255, 0.12)',         // Track Background
    sliderTrackFilled: 'rgba(100, 180, 255, 0.65)',   // Filled Track (Main Menu blue, muted)
    sliderThumb: 'rgba(100, 180, 255, 1)',            // Thumb (FULL Main Menu blue!)
    sliderThumbHover: 'rgba(120, 200, 255, 1)',       // Thumb Hover (slightly brighter)
    
    // Hover/Active (very subtle, like Main Menu)
    hoverOverlay: 'rgba(255, 255, 255, 0.04)',        // Very subtle (Main: 0.05)
    activeOverlay: 'rgba(255, 255, 255, 0.08)',       // Slightly visible (Main: 0.1)
  },
  
  // ===== SPACING (Not cramped!) =====
  spacing: {
    // Container
    containerPadding: '28rem',        // Inner padding
    sectionPadding: '20rem',          // Section padding
    itemPadding: '16rem',             // Item padding
    
    // Gaps
    sectionGap: '24rem',              // Between sections
    itemGap: '16rem',                 // Between items
    labelGap: '12rem',                // Label to input
    
    // Slider-specific
    sliderHeight: '36rem',            // Height of slider area
    sliderThumbSize: '20rem',         // Thumb size (clear!)
    sliderLabelGap: '8rem',           // Label to slider
    
    // Buttons
    buttonPadding: '12rem 24rem',     // Button inner padding
    buttonGap: '12rem',               // Between buttons
  },
  
  // ===== TYPOGRAPHY =====
  typography: {
    // Sizes (not too small!)
    fontXS: '11rem',                  // Very small
    fontSM: '12rem',                  // Small (labels)
    fontMD: '14rem',                  // Normal (text)
    fontLG: '16rem',                  // Large (Headings)
    fontXL: '18rem',                  // Very large (header)
    
    // Weights
    weightRegular: 400,
    weightMedium: 500,
    weightSemiBold: 600,
    weightBold: 700,
    
    // Line Heights
    lineHeightTight: 1.2,
    lineHeightNormal: 1.4,
    lineHeightLoose: 1.6,
  },
  
  // ===== TRANSITIONS (Smooth animations) =====
  transitions: {
    fast: '150ms ease-in-out',
    normal: '250ms ease-in-out',
    slow: '350ms ease-in-out',
  },
  
  // ===== BORDERS & RADIUS =====
  borders: {
    radiusSM: '4rem',
    radiusMD: '6rem',
    radiusLG: '8rem',
    radiusXL: '12rem',
    radiusFull: '999rem',
  },
  
  // ===== SHADOWS =====
  shadows: {
    subtle: '0 2rem 8rem rgba(0, 0, 0, 0.2)',
    medium: '0 4rem 16rem rgba(0, 0, 0, 0.3)',
    strong: '0 8rem 24rem rgba(0, 0, 0, 0.4)',
  },
};

export default subMenuTokens;
