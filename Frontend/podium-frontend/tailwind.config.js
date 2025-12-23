/** @type {import('tailwindcss').Config} */
const colors = require('tailwindcss/colors')

module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        // PRIMARY BRAND COLORS - Change these to update entire theme
        primary: {
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6',  // Main brand color
          600: '#2563eb',  // Hover/Active states
          700: '#1d4ed8',  // Pressed states
          800: '#1e40af',
          900: '#1e3a8a',
          950: '#172554',
        },
        
        // SECONDARY ACCENT COLOR (Gold/Amber for highlights)
        secondary: {
          50: '#fffbeb',
          100: '#fef3c7',
          200: '#fde68a',
          300: '#fcd34d',
          400: '#fbbf24',
          500: '#f59e0b',  // Main secondary color
          600: '#d97706',  // Hover states
          700: '#b45309',
          800: '#92400e',
          900: '#78350f',
          950: '#451a03',
        },

        // SUCCESS COLOR (Green)
        success: {
          50: '#f0fdf4',
          100: '#dcfce7',
          200: '#bbf7d0',
          300: '#86efac',
          400: '#4ade80',
          500: '#22c55e',  // Main success color
          600: '#16a34a',
          700: '#15803d',
          800: '#166534',
          900: '#14532d',
        },

        // WARNING COLOR (Orange/Yellow)
        warning: {
          50: '#fff7ed',
          100: '#ffedd5',
          200: '#fed7aa',
          300: '#fdba74',
          400: '#fb923c',
          500: '#f97316',  // Main warning color
          600: '#ea580c',
          700: '#c2410c',
          800: '#9a3412',
          900: '#7c2d12',
        },

        // ERROR/DANGER COLOR (Red)
        danger: {
          50: '#fef2f2',
          100: '#fee2e2',
          200: '#fecaca',
          300: '#fca5a5',
          400: '#f87171',
          500: '#ef4444',  // Main error color
          600: '#dc2626',
          700: '#b91c1c',
          800: '#991b1b',
          900: '#7f1d1d',
        },

        // INFO COLOR (Cyan/Blue)
        info: {
          50: '#ecfeff',
          100: '#cffafe',
          200: '#a5f3fc',
          300: '#67e8f9',
          400: '#22d3ee',
          500: '#06b6d4',  // Main info color
          600: '#0891b2',
          700: '#0e7490',
          800: '#155e75',
          900: '#164e63',
        },

        // NEUTRAL COLORS (Slate for better contrast)
        neutral: colors.slate,
        
        // SEMANTIC TOKENS - Use these in your HTML for easy theme switching
        // Background colors
        background: {
          DEFAULT: '#f8fafc',  // slate-50
          dark: '#f1f5f9',     // slate-100
          darker: '#e2e8f0',   // slate-200
        },
        
        // Surface colors (cards, panels, etc.)
        surface: {
          DEFAULT: '#ffffff',
          hover: '#f8fafc',    // slate-50
          active: '#f1f5f9',   // slate-100
        },

        // Border colors
        border: {
          DEFAULT: '#e2e8f0',  // slate-200
          light: '#f1f5f9',    // slate-100
          dark: '#cbd5e1',     // slate-300
        },

        // Text colors
        text: {
          primary: '#0f172a',    // slate-900
          secondary: '#475569',  // slate-600
          tertiary: '#94a3b8',   // slate-400
          disabled: '#cbd5e1',   // slate-300
          inverse: '#ffffff',
        },
      },
      
      // Typography
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'Helvetica Neue', 'Arial', 'sans-serif'],
      },
      
      // Spacing scale (if you need custom spacing)
      spacing: {
        '18': '4.5rem',
        '88': '22rem',
        '100': '25rem',
        '128': '32rem',
      },

      // Box shadows
      boxShadow: {
        'card': '0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06)',
        'card-hover': '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)',
        'input': '0 0 0 3px rgba(59, 130, 246, 0.1)',
      },

      // Border radius
      borderRadius: {
        'card': '0.75rem',
        'button': '0.5rem',
      },

      // Animation
      animation: {
        'fade-in': 'fadeIn 0.3s ease-in-out',
        'slide-in': 'slideIn 0.3s ease-out',
        'slide-out': 'slideOut 0.3s ease-out',
      },
      
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideIn: {
          '0%': { transform: 'translateX(100%)', opacity: '0' },
          '100%': { transform: 'translateX(0)', opacity: '1' },
        },
        slideOut: {
          '0%': { transform: 'translateX(0)', opacity: '1' },
          '100%': { transform: 'translateX(100%)', opacity: '0' },
        },
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
  ],
}