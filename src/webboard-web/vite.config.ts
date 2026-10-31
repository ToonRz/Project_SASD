/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Dev proxy for /api (ADR-presentation-technology D2) — no CORS needed.
// Default is the http profile in src/Webboard.Api/Properties/launchSettings.json.
const apiUrl = process.env.WEBBOARD_API_URL ?? 'http://localhost:5001'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: { '/api': { target: apiUrl, changeOrigin: true } },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    globals: false,
  },
})
