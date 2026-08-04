import { defineConfig, devices } from '@playwright/test'

const BASE_URL = process.env.E2E_BASE_URL || 'http://localhost:5173'
const API_URL = process.env.E2E_API_URL || 'http://localhost:5005'

// When the servers are already running (local development, or started by CI), reuse them
// instead of spawning another pair.
const reuseExisting = !process.env.CI

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : [['list']],

  use: {
    baseURL: BASE_URL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],

  webServer: [
    {
      command: 'dotnet run --project ../../src/TicketSystem.Api --no-launch-profile --urls http://localhost:5005',
      url: `${API_URL}/swagger/index.html`,
      reuseExistingServer: reuseExisting,
      timeout: 180_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        // Every test signs in, which would otherwise trip the 5-per-minute login limit.
        RateLimiting__LoginPermitLimit: '100000',
        RateLimiting__PasswordPermitLimit: '100000',
        RateLimiting__GlobalPermitLimit: '100000',
      },
    },
    {
      command: 'npm run dev',
      url: BASE_URL,
      reuseExistingServer: reuseExisting,
      timeout: 120_000,
    },
  ],
})
