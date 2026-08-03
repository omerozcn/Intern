import { fileURLToPath, URL } from 'node:url'

import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [vue()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      proxy: {
        '/api': {
          target: env.VITE_API_PROXY_TARGET || 'http://localhost:5005',
          changeOrigin: true,
          secure: false,
        },
      },
    },
    build: {
      rollupOptions: {
        output: {
          manualChunks(id) {
            if (id.includes('node_modules/chart.js') || id.includes('node_modules/vue-chartjs')) {
              return 'charts'
            }
            if (
              id.includes('node_modules/vue/') ||
              id.includes('node_modules/vue-router/') ||
              id.includes('node_modules/pinia/') ||
              id.includes('node_modules/vue-i18n/')
            ) {
              return 'vue'
            }
          },
        },
      },
    },
    test: {
      environment: 'jsdom',
      globals: true,
      clearMocks: true,
      restoreMocks: true,
      setupFiles: ['./src/tests/setup.js'],
      // e2e/ belongs to Playwright; Vitest cannot load @playwright/test.
      include: ['src/**/*.{test,spec}.{js,mjs}'],
      exclude: ['**/node_modules/**', '**/dist/**', 'e2e/**'],
    },
  }
})
