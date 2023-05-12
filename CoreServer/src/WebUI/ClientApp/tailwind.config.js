/** @type {import('tailwindcss').Config} */
module.exports = {
  includeLanguages: {},
  files: {
    exclude: [
      "**/.git/**",
      "**/node_modules/**",
      "**/.hg/**",
      "**/.svn/**"
    ]
  },
  emmetCompletions: false,
  classAttributes: ["class", "className", "ngClass"],
  colorDecorators: false,
  showPixelEquivalents: true,
  rootFontSize: 16,
  hovers: true,
  suggestions: true,
  codeActions: true,
  validate: true,
  lint: {
    invalidScreen: "error",
    invalidVariant: "error",
    invalidTailwindDirective: "error",
    invalidApply: "error",
    invalidConfigPath: "error",
    cssConflict: "warning",
    recommendedVariantOrder: "warning"
  },
  experimental: {
    configFile: null,
    classRegex: []
  },
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        primary: "#444791",
        secondary: "#7579eb",
        "primary-bg": "#2f2f49",
        text: "#535353",
        "background-primary": "#1f1f1f",
        "background-secondary": "#141414",
        "background-tertiary": "#0a0a0a",
        "background-highlight": "#2c2c2c",
        border: "#0f0f0f",
        warning: "#f5bb2f",
        error: "#cb4d55",
        success: "#99c25b",
        info: "#4d9eeb",
      },
      backgroundImage: {
        'gradient-radial': 'radial-gradient(var(--tw-gradient-stops))',
      }
    },
  },
  plugins: [
    function ({addBase, theme}) {
    function extractColorVars(colorObj, colorGroup = '') {
      return Object.keys(colorObj).reduce((vars, colorKey) => {
        const value = colorObj[colorKey];

        const newVars =
          typeof value === 'string'
            ? {[`--color${colorGroup}-${colorKey}`]: value}
            : extractColorVars(value, `-${colorKey}`);

        return {...vars, ...newVars};
      }, {});
    }

    addBase({
      ':root': extractColorVars(theme('colors')),
    });
  },
  ],
}

