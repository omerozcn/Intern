import { expect, test } from '@playwright/test'

import { ADMIN, USER, signIn } from './accounts.js'

const DESCRIPTION = 'Playwright ucdan uca testi icin olusturulmus yeterince uzun bir aciklama.'
const API_BASE = process.env.E2E_API_URL || 'http://localhost:5005'

test.describe('ticket flow', () => {
  test('a user creates a ticket and sees it in their list', async ({ page, request }) => {
    await signIn(page, USER)

    await page.goto('/tickets/new')
    await page.getByRole('radio', { name: 'Yeni ürün talebi' }).check()
    await page.getByLabel('Açıklama').fill(DESCRIPTION)

    // Navigating before the POST settles would abort it, so wait for the response.
    const [response] = await Promise.all([
      page.waitForResponse(
        (res) => res.url().includes('/api/tickets') && res.request().method() === 'POST',
      ),
      page.getByRole('button', { name: 'Talebi gönder' }).click(),
    ])
    expect(response.status()).toBe(201)
    const created = await response.json()

    await page.goto('/tickets')
    await expect(page.getByText(DESCRIPTION)).toBeVisible()

    // Leave the seeded data as it was found.
    const token = await page.evaluate(
      () => JSON.parse(sessionStorage.getItem('turkuvaz.auth.session')).accessToken,
    )
    await request.delete(`${API_BASE}/api/tickets/${created.id}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
  })

  test('the description length rule is enforced', async ({ page }) => {
    await signIn(page, USER)

    await page.goto('/tickets/new')
    await page.getByRole('radio', { name: 'Yeni ürün talebi' }).check()
    await page.getByLabel('Açıklama').fill('cok kisa')
    await page.getByRole('button', { name: 'Talebi gönder' }).click()

    // The form must not navigate away while the description is invalid.
    await expect(page).toHaveURL(/\/ticket/)
  })

  test('an admin sees the ticket and its status tabs', async ({ page }) => {
    await signIn(page, ADMIN)

    await page.goto('/admin/tickets')

    await expect(page.getByRole('heading', { name: 'Talep yönetimi' })).toBeVisible()
    await expect(page.getByRole('button', { name: /Bekliyor/ })).toBeVisible()
    await expect(page.getByRole('button', { name: /Tamamlandı/ })).toBeVisible()
  })
})
