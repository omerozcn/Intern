/** Accounts created by DevelopmentDataSeeder when the API runs in Development. */
export const ADMIN = {
  email: 'admin.test@turkuvaz.local',
  password: 'AdminTest!2026',
}

export const USER = {
  email: 'user.test@turkuvaz.local',
  password: 'UserTest!2026',
}

export async function signIn(page, account) {
  await page.goto('/sign-in')
  await page.getByLabel('E-posta adresi').fill(account.email)
  await page.getByLabel('Parola', { exact: true }).fill(account.password)
  await page.getByRole('button', { name: 'Giriş yap' }).click()
  await page.waitForURL((url) => !url.pathname.startsWith('/sign-in'))
}
