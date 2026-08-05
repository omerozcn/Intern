import { onScopeDispose, ref, watch } from 'vue'

const DURATION_MS = 900

/** Matches the ease-out curve the rest of the motion layer uses. */
function easeOut(t) {
  return 1 - Math.pow(1 - t, 3)
}

function prefersReducedMotion() {
  return globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false
}

/**
 * Animates a number towards its target so a dashboard figure reads as having been
 * counted rather than pasted in.
 *
 * @param {import('vue').Ref<number>|(() => number)} source the live value
 * @returns {import('vue').Ref<number>} the displayed value, always a whole number
 */
export function useCountUp(source) {
  const displayed = ref(0)
  let frame = null
  let startedAt = 0
  let from = 0
  let to = 0

  function stop() {
    if (frame !== null) {
      globalThis.cancelAnimationFrame(frame)
      frame = null
    }
  }

  function step(now) {
    const elapsed = now - startedAt
    const progress = Math.min(1, elapsed / DURATION_MS)
    displayed.value = Math.round(from + (to - from) * easeOut(progress))

    if (progress < 1) {
      frame = globalThis.requestAnimationFrame(step)
      return
    }

    // Land exactly on the target rather than on a rounded approximation of it.
    displayed.value = to
    frame = null
  }

  watch(
    source,
    (next) => {
      const target = Number(next) || 0
      stop()

      // Counting up is decoration, not information: honour the motion preference
      // and the first paint of a zero value, both of which should be instant.
      if (prefersReducedMotion() || target === displayed.value) {
        displayed.value = target
        return
      }

      from = displayed.value
      to = target
      startedAt = globalThis.performance.now()
      frame = globalThis.requestAnimationFrame(step)
    },
    { immediate: true },
  )

  onScopeDispose(stop)

  return displayed
}
