/** @type {import('tailwindcss').Config} */
const colors = require('tailwindcss/colors')

module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        // 1. Define your "Primary" Brand Color (e.g., Royal Blue)
        // This replaces standard 'blue' in your mind.
        primary: {
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6', // Main Brand Color
          600: '#2563eb', // Hover State
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a8a',
        },
        // 2. Define a "Secondary" Accent Color (e.g., Gold/Amber for bands)
        secondary: colors.amber, 
        
        // 3. Define a neutral text/bg color (e.g., Slate is cleaner than Gray)
        gray: colors.slate,
      },
      fontFamily: {
        sans: ['Inter', 'sans-serif'], // Optional: If you want a cleaner font
      }
    },
  },
  plugins: [
    require('@tailwindcss/forms')
  ],
}