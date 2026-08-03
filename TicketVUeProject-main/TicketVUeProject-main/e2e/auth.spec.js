import { expect, test } from '@playwright/test'

import { ADMIN, USER, signIn } from './accounts.js'

test.describe('authentication and role routing', () => {
  test('an anonymous visitor is sent to the sign-in page', async ({ page }) => {
    await page.goto('/adminticket')

    await expect(page).toHaveURL(/\/sign-in/)
    await expect(page.getByRole('button', { name: 'Giriş yap' })).toBeVisible()
  })

  test('wrong credentials keep the visitor on the sign-in page', async ({ page }) => {
    await page.goto('/sign-in')
    await page.getByLabel('E-posta adresi').fill(ADMIN.email)
    await page.getByLabel('Parola', { exact: true }).fill('yanlis-parola')
    await page.getByRole('button', { name: 'Giriş yap' }).click()

    await expect(page).toHaveURL(/\/sign-in/)
  })

  test('an admin reaches the dashboard and the admin pages', async ({ page }) => {
    await signIn(page, ADMIN)

    await expect(page.getByRole('heading', { name: 'Genel Bakış' })).toBeVisible()

    await page.goto('/firm')
    await expect(page.getByRole('heading', { name: 'Firmalar' })).toBeVisible()

    await page.goto('/product')
    await expect(page.getByRole('heading', { name: 'Hizmetler' })).toBeVisible()
  })

  test('a user is redirected away from admin-only pages', async ({ page }) => {
    await signIn(page, USER)

    await page.goto('/firm')

    await expect(page).toHaveURL((url) => !url.pathname.startsWith('/firm'))
  })

  test('signing out clears the session', async ({ page }) => {
    await signIn(page, ADMIN)

    await page.getByRole('button', { name: 'Çıkış yap' }).click()
    await expect(page).toHaveURL(/\/sign-in/)

    await page.goto('/adminticket')
    await expect(page).toHaveURL(/\/sign-in/)
  })
})
