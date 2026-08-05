/* A `v-reveal` directive: content below the fold fades up as it comes into view.

   One shared IntersectionObserver for the whole app rather than one per element,
   and each element is unobserved the first time it intersects — this is an
   entrance, not a scroll-linked effect, so there is nothing to keep watching.

   Two escapes, and both matter:

   - jsdom implements no IntersectionObserver, and this directive is registered
     globally, so every component test would throw without the guard.
   - Under reduced motion the observer is never constructed at all. A CSS-only
     escape is not enough here: the element starts at opacity 0, so if the
     observer never runs the content is stranded invisible. */

const REVEALED = 'tv-reveal--in'

let observer = null

function prefersReducedMotion() {
  return globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false
}

function getObserver() {
  if (observer) return observer
  observer = new IntersectionObserver(
    (entries) => {
      for (const entry of entries) {
        if (!entry.isIntersecting) continue
        entry.target.classList.add(REVEALED)
        observer.unobserve(entry.target)
      }
    },
    /* No negative margin and a zero threshold, deliberately. Shrinking the root
       delays the reveal, which means an element that is plainly on screen can
       still be sitting at opacity 0 — that is a far worse failure than revealing
       a few pixels early. Anything visible reveals immediately. */
    { rootMargin: '0px', threshold: 0 },
  )
  return observer
}

export const vReveal = {
  mounted(el, binding) {
    if (!globalThis.IntersectionObserver || prefersReducedMotion()) {
      el.classList.add(REVEALED)
      return
    }

    el.classList.add('tv-reveal')
    // v-reveal="120" staggers this element behind its neighbours.
    if (binding.value) el.style.setProperty('--tv-reveal-delay', `${Number(binding.value)}ms`)
    getObserver().observe(el)
  },

  unmounted(el) {
    observer?.unobserve(el)
  },
}
