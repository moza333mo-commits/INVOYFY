// File: tailwind.config.js
/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: "class",
  content: [
    "./wwwroot/**/*.html",
    "./wwwroot/**/*.js",
    "./Pages/**/*.cshtml"
  ],
  theme: {
    extend: {
      colors: {
        invoify: {
          dark: "#064E3B",
          darker: "#032E22",
          primary: "#047857",
          accent: "#10B981",
          soft: "#ECFDF5",
          surface: "#F8FAFC"
        },
        emeraldBrand: {
          50: "#ECFDF5",
          100: "#D1FAE5",
          200: "#A7F3D0",
          400: "#34D399",
          500: "#10B981",
          600: "#059669",
          700: "#047857",
          800: "#065F46",
          900: "#064E3B",
          950: "#032E22"
        }
      },
      fontFamily: {
        sans: ["'Cairo'", "'Segoe UI'", "Tahoma", "sans-serif"],
        cairo: ["'Cairo'", "'Segoe UI'", "Tahoma", "sans-serif"]
      },
      screens: {
        xs: "360px",
        sm: "640px",
        md: "768px",
        lg: "1024px",
        xl: "1366px",
        "2xl": "1920px"
      }
    }
  },
  plugins: []
};