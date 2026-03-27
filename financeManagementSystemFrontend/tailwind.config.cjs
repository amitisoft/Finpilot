/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      boxShadow: {
        glass: '0 8px 32px 0 rgba(31,38,135,0.07)',
        glow: '0 18px 40px rgba(37,99,235,0.18)'
      },
      borderRadius: {
        '4xl': '2rem'
      },
      backgroundImage: {
        'hero-gradient': 'linear-gradient(135deg, #0f172a 0%, #1d4ed8 58%, #2563eb 100%)',
        'mesh-gradient': 'radial-gradient(circle at top left, rgba(29,78,216,0.22), transparent 35%), radial-gradient(circle at top right, rgba(37,99,235,0.18), transparent 30%), radial-gradient(circle at bottom, rgba(15,23,42,0.14), transparent 35%)'
      },
      keyframes: {
        pulseSoft: {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '.7' }
        },
        fadeInUp: {
          '0%': { opacity: '0', transform: 'translateY(12px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' }
        }
      },
      animation: {
        'pulse-soft': 'pulseSoft 2.4s ease-in-out infinite',
        'fade-in-up': 'fadeInUp .45s ease-out'
      }
    }
  },
  plugins: []
};
