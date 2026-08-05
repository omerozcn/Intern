/* Theme mode, in the same shape as i18n/index.js: a module-level singleton with
   an allowlist, a turkuvaz.* storage key, and an exported setter.

   The inline script in index.html has already written the correct theme
   attribute before this module loads. Everything here keeps the reactive state
   aligned with that and reacts to later changes; it never causes a first flip. */

import { ref, watch } from 'vue'

const THEME_KEY = 'turkuvaz.theme'
const SUPPORTED_MODES = ['light', 'dark', 'system']
const DARK_QUERY = '(prefers-color-scheme: dark)'
const THEME_COLOR = { light: '#0f766e', dark: '#0b1220' }

/* jsdom implements neither matchMedia nor requestAnimationFrame reliably, and
   this module is imported transitively by component tests. Every platform call
   below goes through a guard for that reason. */
function darkMediaQuery() {
  return globalThis.matchMedia?.(DARK_QUERY) ?? null
}

function readStoredMode() {
  try {
    const stored = globalThis.localStorage?.getItem(THEME_KEY)
    return SUPPORTED_MODES.includes(stored) ? stored : 'system'
  } catch {
    // Storage can throw outright in private-browsing modes.
    return 'system'
  }
}

function resolve(mode) {
  if (mode !== 'system') return mode
  return darkMediaQuery()?.matches ? 'dark' : 'light'
}

export const themeMode = ref(readStoredMode())
export const resolvedTheme = ref(resolve(themeMode.value))

/* Canvas cannot read CSS variables. Anything drawn with Chart.js re-reads its
   colours when this counter changes — see utils/chartTheme.js. */
export const tokenEpoch = ref(0)

export const themeModes = Object.freeze([
  { value: 'light', icon: 'bi-sun', labelKey: 'theme.light' },
  { value: 'dark', icon: 'bi-moon-stars', labelKey: 'theme.dark' },
  { value: 'system', icon: 'bi-circle-half', labelKey: 'theme.system' },
])

function applyTheme(theme) {
  const root = document.documentElement
  root.dataset.theme = theme
  /* Bootstrap 5.3 ships a complete dark theme behind this attribute, including
     the inverted select caret. Writing it hands us every native control. */
  root.dataset.bsTheme = theme
  document
    .querySelector('meta[name="theme-color"]')
    ?.setAttribute('content', THEME_COLOR[theme] ?? THEME_COLOR.light)
}

let mediaListener = null

function stopWatchingSystem() {
  if (mediaListener) darkMediaQuery()?.removeEventListener('change', mediaListener)
  mediaListener = null
}

/* Attached only while the mode is 'system', so an explicit choice genuinely
   stops following the OS rather than quietly still listening. */
function startWatchingSystem() {
  const query = darkMediaQuery()
  if (!query || mediaListener) return
  mediaListener = () => {
    resolvedTheme.value = resolve('system')
  }
  query.addEventListener('change', mediaListener)
}

function prefersReducedMotion() {
  return globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false
}

/* Written synchronously rather than left to the watcher below: the view
   transition has to see the finished DOM inside its callback, and a Vue watcher
   flushes on the microtask queue, which is too late. */
function commit(mode, theme) {
  themeMode.value = mode
  resolvedTheme.value = theme
  applyTheme(theme)
}

/* A circular wipe out of the control that was pressed.

   This is the one place the View Transitions API is used. It is deliberately not
   used for route changes: it fights <Transition mode="out-in"> and introduces
   timing non-determinism that makes Playwright flaky. Here it is self-contained
   and degrades to an instant switch wherever the API is missing. */
async function commitWithTransition(mode, theme, origin) {
  const canTransition =
    typeof document !== 'undefined' &&
    typeof document.startViewTransition === 'function' &&
    origin &&
    !prefersReducedMotion()

  if (!canTransition) {
    commit(mode, theme)
    return
  }

  const transition = document.startViewTransition(() => commit(mode, theme))

  try {
    await transition.ready
    const { x, y } = origin
    // Reach the furthest corner, or the wipe stops short of part of the screen.
    const radius = Math.hypot(Math.max(x, innerWidth - x), Math.max(y, innerHeight - y))

    document.documentElement.animate(
      {
        clipPath: [`circle(0px at ${x}px ${y}px)`, `circle(${radius}px at ${x}px ${y}px)`],
      },
      {
        duration: 480,
        easing: 'cubic-bezier(0.2, 0, 0, 1)',
        pseudoElement: '::view-transition-new(root)',
      },
    )
  } catch {
    // A skipped or interrupted transition already left the theme applied.
  }
}

export function setTheme(mode, origin = null) {
  const next = SUPPORTED_MODES.includes(mode) ? mode : 'system'

  try {
    globalThis.localStorage?.setItem(THEME_KEY, next)
  } catch {
    // Not being able to remember the choice is not a reason to refuse it.
  }

  if (next === 'system') startWatchingSystem()
  else stopWatchingSystem()

  commitWithTransition(next, resolve(next), origin)
}

export function cycleTheme(origin = null) {
  const order = SUPPORTED_MODES
  setTheme(order[(order.indexOf(themeMode.value) + 1) % order.length], origin)
}

watch(resolvedTheme, (theme) => {
  applyTheme(theme)
  /* getComputedStyle has to run after the attribute flip has been applied, or
     the chart re-reads exactly the colours it already had. */
  globalThis.requestAnimationFrame?.(() => {
    tokenEpoch.value += 1
  })
})

if (typeof document !== 'undefined') {
  setTheme(themeMode.value)
  applyTheme(resolvedTheme.value)
}
