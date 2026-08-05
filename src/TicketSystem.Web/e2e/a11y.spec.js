import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

import { ADMIN, USER, signIn } from './accounts.js'

/*
  Route-level scans plus the three states a route-level scan cannot reach: the
  modal, the drawer and the command palette. Those are exactly where a redesign
  breaks accessibility — they add focus traps, aria-modal and inert, and none of
  it is exercised by loading a page.

  Both themes, because contrast is the failure mode a light-only pass misses and
  the dark palette is new.

  Serial with one shared page per block: every sign-in counts against the
  5-per-minute login limit. Playwright's own webServer raises that (see
  playwright.config.js), but reusing a hand-started API does not, so the suite
  stays frugal either way.
*/

const ADMIN_ROUTES = [
  '/',
  '/admin/tickets',
  '/services',
  '/firms',
  '/accounts',
  '/admin/feedback',
  '/profile',
]

const USER_ROUTES = ['/', '/tickets', '/tickets/new', '/feedback/new', '/profile']

const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']

async function setTheme(page, theme) {
  await page.evaluate((value) => {
    localStorage.setItem('turkuvaz.theme', value)
    document.documentElement.dataset.theme = value
    document.documentElement.dataset.bsTheme = value
  }, theme)
}

async function scan(page) {
  const results = await new AxeBuilder({ page }).withTags(TAGS).analyze()
  return results.violations.map((violation) => ({
    id: violation.id,
    impact: violation.impact,
    nodes: violation.nodes.map((node) => node.target.join(' ')),
  }))
}

test.describe('accessibility — anonymous', () => {
  for (const theme of ['light', 'dark']) {
    test(`sign-in page has no violations (${theme})`, async ({ page }) => {
      await page.goto('/sign-in')
      await setTheme(page, theme)
      await page.reload()
      await expect(page.getByRole('button', { name: 'Giriş yap' })).toBeVisible()
      expect(await scan(page)).toEqual([])
    })
  }
})

test.describe('accessibility — admin', () => {
  test.describe.configure({ mode: 'serial' })

  let context
  let page

  // AxeBuilder rejects a page from browser.newPage(); it needs an explicit
  // context to inject axe-core into.
  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext({ reducedMotion: 'reduce' })
    page = await context.newPage()
    await signIn(page, ADMIN)
  })

  test.afterAll(async () => {
    await context?.close()
  })

  for (const theme of ['light', 'dark']) {
    test(`admin routes have no violations (${theme})`, async () => {
      await setTheme(page, theme)
      for (const route of ADMIN_ROUTES) {
        await page.goto(route)
        await expect(page.locator('main')).toBeVisible()
        expect(await scan(page), `${route} (${theme})`).toEqual([])
      }
    })
  }

  test('the command palette has no violations while open', async () => {
    await page.goto('/')
    await page.keyboard.press('Control+k')
    await expect(page.getByRole('dialog', { name: 'Komut paleti' })).toBeVisible()
    expect(await scan(page)).toEqual([])

    // And it must leave the tree entirely when closed, or its "Çıkış yap" entry
    // makes the topbar sign-out button ambiguous for every other spec.
    await page.keyboard.press('Escape')
    await expect(page.getByRole('dialog', { name: 'Komut paleti' })).toHaveCount(0)
    await expect(page.getByRole('button', { name: 'Çıkış yap' })).toHaveCount(1)
  })

  test('the manage modal has no violations while open', async () => {
    await page.goto('/admin/tickets')
    const manage = page.getByRole('button', { name: /Yönet/ }).first()

    if ((await manage.count()) === 0) test.skip(true, 'no tickets seeded to manage')

    await manage.click()
    await expect(page.getByRole('dialog')).toBeVisible()
    expect(await scan(page)).toEqual([])
  })

  test('the mobile drawer has no violations while open', async () => {
    await page.setViewportSize({ width: 375, height: 812 })
    await page.goto('/')
    await page.getByRole('button', { name: 'Menüyü aç' }).click()
    await expect(page.getByRole('dialog', { name: 'Mobil navigasyon' })).toBeVisible()
    expect(await scan(page)).toEqual([])

    // Escape closes it and focus returns to the trigger that opened it.
    await page.keyboard.press('Escape')
    await expect(page.getByRole('dialog', { name: 'Mobil navigasyon' })).toHaveCount(0)
    await expect(page.getByRole('button', { name: 'Menüyü aç' })).toBeFocused()
    await page.setViewportSize({ width: 1280, height: 720 })
  })
})

test.describe('accessibility — user', () => {
  test.describe.configure({ mode: 'serial' })

  let context
  let page

  // AxeBuilder rejects a page from browser.newPage(); it needs an explicit
  // context to inject axe-core into.
  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext({ reducedMotion: 'reduce' })
    page = await context.newPage()
    await signIn(page, USER)
  })

  test.afterAll(async () => {
    await context?.close()
  })

  for (const theme of ['light', 'dark']) {
    test(`user routes have no violations (${theme})`, async () => {
      await setTheme(page, theme)
      for (const route of USER_ROUTES) {
        await page.goto(route)
        await expect(page.locator('main')).toBeVisible()
        expect(await scan(page), `${route} (${theme})`).toEqual([])
      }
    })
  }
})
