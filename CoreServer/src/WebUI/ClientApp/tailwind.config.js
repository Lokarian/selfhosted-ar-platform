/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        "primary": "#444791",
        "secondary": "#7579eb",
        "primary-bg": "#2f2f49",
        "text": "#535353",
        "background-primary": "#1f1f1f",
        "background-secondary": "#141414",
        "background-tertiary": "#0a0a0a",
        "background-highlight": "#2c2c2c",
        "border": "#0f0f0f",
        "warning": "#f5bb2f",
        "error": "#cb4d55",
        "success": "#99c25b",
      }
    },
  },
  plugins: [],
}

