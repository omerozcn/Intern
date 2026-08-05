/* Sidebar rail state, in the same shape as useTheme.js: a module-level
   singleton with a turkuvaz.* storage key, so every mount agrees and the choice
   survives a reload. Desktop only — below 992px the sidebar is replaced by the
   drawer and this value is ignored. */

import { ref } from 'vue'

const SIDEBAR_KEY = 'turkuvaz.sidebar'
const SUPPORTED = ['expanded', 'rail']

function readStored() {
  try {
    const stored = globalThis.localStorage?.getItem(SIDEBAR_KEY)
    return SUPPORTED.includes(stored) ? stored : 'expanded'
  } catch {
    return 'expanded'
  }
}

export const sidebarMode = ref(readStored())

export function setSidebarMode(mode) {
  sidebarMode.value = SUPPORTED.includes(mode) ? mode : 'expanded'
  try {
    globalThis.localStorage?.setItem(SIDEBAR_KEY, sidebarMode.value)
  } catch {
    // Not remembering the choice is not a reason to refuse it.
  }
}

export function toggleSidebar() {
  setSidebarMode(sidebarMode.value === 'rail' ? 'expanded' : 'rail')
}
