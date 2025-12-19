/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        // Mapped from your original styles.scss
        primary: {
          DEFAULT: '#667eea', 
          dark: '#5a6fd1', // slightly darker for hover states
          light: '#8fa1f0'
        },
        accent: {
          DEFAULT: '#764ba2',
          dark: '#633d8a'
        },
        warn: '#f44336',
        success: '#4caf50',
        info: '#2196f3',
        background: '#f5f5f5',
        surface: '#ffffff',
        text: {
          primary: '#333333',
          secondary: '#666666'
        },
        border: '#e0e0e0'
      },
      fontFamily: {
        sans: ['Roboto', 'Helvetica Neue', 'sans-serif'],
      }
    },
  },
  plugins: [],
}