/**
 * Accounts created by DevelopmentDataSeeder when the API runs in Development.
 * Overridable from the environment so a non-default seed can be tested without
 * editing this file.
 *
 * These twelve Turkish strings are queried by name across the suite. A redesign
 * may change any other tr.json value, but changing one of these breaks tests:
 *
 *   Giriş yap · E-posta adresi · Parola (matched with exact: true, so it cannot
 *   become "Parolanız") · Genel Bakış (must stay the <h1>) · Firmalar ·
 *   Hizmetler · Talep yönetimi · Bekliyor · Tamamlandı · Yeni ürün talebi
 *   (the radio's accessible name, derived from its wrapping <label>) ·
 *   Açıklama · Talebi gönder
 *
 * Playwright matches accessible names as a case-insensitive substring, so a
 * second *visible* element containing one of these is also a failure. Anything
 * that can offer navigation or actions — the command palette especially — must
 * render with v-if so it leaves the accessibility tree when closed.
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
