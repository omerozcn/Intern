/* Chart.js takes plain colour strings and never re-reads CSS, so a theme flip
   leaves a canvas painted in the old palette. Views resolve their colours
   through here and depend on tokenEpoch from composables/useTheme.js so the
   computed re-runs after each flip. */

const FALLBACK = {
  pending: '#f59e0b',
  inProgress: '#0ea5a8',
  completed: '#15803d',
  grid: '#e2e8f0',
  tick: '#475569',
  track: '#f1f5f9',
  surface: '#ffffff',
  text: '#1e293b',
  border: '#e2e8f0',
}

const TOKENS = {
  pending: '--tv-chart-pending',
  inProgress: '--tv-chart-inProgress',
  completed: '--tv-chart-completed',
  grid: '--tv-chart-grid',
  tick: '--tv-chart-tick',
  track: '--tv-chart-track',
  surface: '--tv-surface-1',
  text: '--tv-text',
  border: '--tv-border',
}

export function readChartTokens() {
  // jsdom returns empty strings for custom properties; the fallbacks keep any
  // component that renders a chart in a unit test from drawing with `undefined`.
  if (typeof document === 'undefined') return { ...FALLBACK }

  const styles = getComputedStyle(document.documentElement)
  const resolved = {}
  for (const [key, token] of Object.entries(TOKENS)) {
    resolved[key] = styles.getPropertyValue(token).trim() || FALLBACK[key]
  }
  return resolved
}
