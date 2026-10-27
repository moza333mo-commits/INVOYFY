/* File: wwwroot/css/style.css */
html,
body {
  font-family: 'Cairo', 'Segoe UI', Tahoma, sans-serif;
  overflow-x: hidden;
}

.sidebar-gradient {
  background: linear-gradient(180deg, #064E3B 0%, #032E22 100%);
}

.hero-pattern {
  background-image: radial-gradient(rgba(16, 185, 129, 0.14) 1px, transparent 1px);
  background-size: 24px 24px;
}

/* Hide scrollbars on horizontal pill/tab strips while preserving touch scroll */
.no-scrollbar::-webkit-scrollbar {
  display: none;
}

.no-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}

/* Focus outline for keyboard accessibility */
:focus-visible {
  outline: 2px solid #10B981;
  outline-offset: 2px;
}

/* Smooth modal transitions */
.modal-backdrop {
  transition: opacity 0.2s ease-in-out;
}