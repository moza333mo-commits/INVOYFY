// File: tailwind.config.js
/** @type {import('tailwindcss').Config} */
module.exports = {
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
        }
      },
      fontFamily: {
        sans: ["'Cairo'", "'Segoe UI'", "Tahoma", "sans-serif"],
        cairo: ["'Cairo'", "sans-serif"]
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