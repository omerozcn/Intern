/**
 * Accounts created by DevelopmentDataSeeder when the API runs in Development.
 * Overridable from the environment so a non-default seed can be tested without
 * editing this file.
 */
export const ADMIN = {
  email: process.env.E2E_ADMIN_EMAIL || 'admin.test@turkuvaz.local',
  password: process.env.E2E_ADMIN_PASSWORD || 'AdminTest!2026',
}

export const USER = {
  email: process.env.E2E_USER_EMAIL || 'user.test@turkuvaz.local',
  password: process.env.E2E_USER_PASSWORD || 'UserTest!2026',
}

export async function signIn(page, account) {
  await page.goto('/sign-in')
  await page.getByLabel('E-posta adresi').fill(account.email)
  await page.getByLabel('Parola', { exact: true }).fill(account.password)
  await page.getByRole('button', { name: 'Giriş yap' }).click()
  await page.waitForURL((url) => !url.pathname.startsWith('/sign-in'))
}
