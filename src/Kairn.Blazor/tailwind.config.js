/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './**/*.razor',
    './**/*.cshtml',
    './**/*.html',
  ],
  theme: {
    extend: {
      colors: {
        stone: {
          50:  '#F5F4F1',
          200: '#D6D3CA',
          500: '#8C8980',
          700: '#4A4843',
          900: '#1F1E1C',
        },
        lichen: {
          50:  '#E8F4ED',
          200: '#A8D9BC',
          500: '#3A9463',
          700: '#1F6040',
          900: '#0D3322',
        },
        slate: {
          50:  '#EAF0F6',
          200: '#AABFD4',
          500: '#4D7A9E',
          700: '#2A4F6E',
          900: '#112840',
        },
        summit: {
          50:  '#FBF2E0',
          200: '#F2CF84',
          500: '#D4920F',
          700: '#8F6008',
          900: '#4A3004',
        },
        signal: {
          50:  '#FAECE8',
          200: '#F0B5A6',
          500: '#C2492A',
          700: '#7E2A14',
          900: '#3E1208',
        },
      },
      fontFamily: {
        sans: ['Inter', 'Segoe UI', 'Arial', 'sans-serif'],
      },
      borderRadius: {
        card:  '8px',
        input: '4px',
      },
    },
  },
  plugins: [],
};
