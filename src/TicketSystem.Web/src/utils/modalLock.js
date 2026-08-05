/* Page-level state that a modal owns while it is open: the body scroll lock and
   inert on the app root.

   Reference counted because modals nest — a ConfirmDialog can be raised from
   inside an AppModal — and the naive version has the inner one clear both flags
   on close while the outer one is still open. */

let openCount = 0

const SUPPORTS_INERT =
  typeof HTMLElement !== 'undefined' && 'inert' in HTMLElement.prototype

export function acquireModalLock() {
  openCount += 1
  if (openCount > 1) return
  document.body.classList.add('dialog-is-open')
  if (SUPPORTS_INERT) document.getElementById('app')?.setAttribute('inert', '')
}

export function releaseModalLock() {
  openCount = Math.max(0, openCount - 1)
  if (openCount > 0) return
  document.body.classList.remove('dialog-is-open')
  if (SUPPORTS_INERT) document.getElementById('app')?.removeAttribute('inert')
}
