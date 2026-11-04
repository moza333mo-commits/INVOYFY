/* File: wwwroot/css/input.css */
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  *,
  *::before,
  *::after {
    box-sizing: border-box;
  }

  html {
    scroll-behavior: smooth;
    -webkit-text-size-adjust: 100%;
    direction: rtl;
  }

  html,
  body {
    font-family: 'Cairo', 'Segoe UI', Tahoma, sans-serif;
    overflow-x: hidden;
    max-width: 100vw;
    background-color: #F8FAFC;
    color: #0F172A;
  }
}