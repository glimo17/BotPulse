/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        gray: {
          950: '#111217',
          900: '#181b1f',
          850: '#1e2228',
          800: '#22262b',
          750: '#272c33',
          700: '#2c3235',
          600: '#3d444d',
          500: '#535d67',
          400: '#6e7a86',
          300: '#8e9aaa',
          200: '#b3bfcc',
          100: '#d4dce6',
          50:  '#edf1f5',
        },
        accent: {
          DEFAULT: '#3d71e8',
          hover:   '#5285f0',
          muted:   '#1f3a7a',
        },
        success: {
          DEFAULT: '#73bf69',
          muted:   '#1e4a1b',
        },
        warning: {
          DEFAULT: '#f5a623',
          muted:   '#5a3d0a',
        },
        error: {
          DEFAULT: '#f2495c',
          muted:   '#5c1520',
        },
        running: {
          DEFAULT: '#5794f2',
          muted:   '#1a3a6e',
        },
        idle: '#8ab8ff',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'Fira Code', 'monospace'],
      },
      animation: {
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'spin-slow':  'spin 3s linear infinite',
      },
    },
  },
  plugins: [],
}
