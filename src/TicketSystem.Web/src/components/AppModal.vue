<template>
  <Teleport to="body">
    <Transition name="dialog-fade">
      <div v-if="open" class="dialog-layer" @mousedown.self="onBackdrop">
        <section
          ref="panel"
          :class="['app-modal', `app-modal--${size}`]"
          :role="role"
          aria-modal="true"
          :aria-labelledby="titleId"
          :aria-describedby="description ? descriptionId : undefined"
          @keydown="trapFocus"
        >
          <header class="app-modal__header">
            <!-- The ids are exposed because aria-labelledby/-describedby point at
                 them; a replacement header has to be able to keep the wiring. -->
            <slot name="header" :title-id="titleId" :description-id="descriptionId">
              <div class="app-modal__heading">
                <h2 :id="titleId">{{ title }}</h2>
                <p v-if="description" :id="descriptionId">{{ description }}</p>
              </div>
            </slot>
            <IconButton
              icon="bi-x-lg"
              :label="t('common.close')"
              size="sm"
              :disabled="busy"
              @click="close"
            />
          </header>

          <div class="app-modal__body">
            <slot />
          </div>

          <footer v-if="$slots.footer" class="app-modal__footer">
            <slot name="footer" />
          </footer>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { nextTick, onBeforeUnmount, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import { acquireModalLock, releaseModalLock } from '@/utils/modalLock'
import IconButton from './IconButton.vue'

/*
  The one modal. Replaces both the native <dialog> used by AdminTicketsView and
  AccountsView and the bespoke Teleport inside ConfirmDialog.

  Native <dialog> gives a free focus trap and the top layer, but both views hand
  rolled everything around it anyway, and Vue's <Transition> cannot animate a
  top-layer element — which is why those two dialogs appeared with no animation
  at all while ConfirmDialog got the full blur-and-rise.

  The focus trap, focus restore and scroll lock are lifted unchanged from the
  ConfirmDialog implementation that was already reviewed and shipping.
*/
const props = defineProps({
  open: { type: Boolean, required: true },
  title: { type: String, required: true },
  description: { type: String, default: '' },
  size: { type: String, default: 'md' },
  role: { type: String, default: 'dialog' },
  busy: { type: Boolean, default: false },
  closeOnBackdrop: { type: Boolean, default: true },
  closeOnEscape: { type: Boolean, default: true },
  // CSS selector for the control that should take focus on open. Defaults to
  // the first focusable element in the panel.
  initialFocus: { type: String, default: '' },
})
const emit = defineEmits(['update:open', 'close', 'opened'])

const FOCUSABLE =
  'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

const { t } = useI18n()
const panel = ref(null)
const titleId = `modal-title-${useId()}`
const descriptionId = `modal-description-${useId()}`
let returnFocusTo = null
let holdsLock = false

function acquire() {
  if (holdsLock) return
  holdsLock = true
  acquireModalLock()
}

function release() {
  if (!holdsLock) return
  holdsLock = false
  releaseModalLock()
}

watch(
  () => props.open,
  async (open) => {
    if (open) {
      acquire()
      returnFocusTo = document.activeElement
      await nextTick()
      const target = props.initialFocus
        ? panel.value?.querySelector(props.initialFocus)
        : panel.value?.querySelector(FOCUSABLE)
      target?.focus()
      emit('opened')
    } else {
      release()
      returnFocusTo?.focus?.()
      returnFocusTo = null
    }
  },
  { immediate: true },
)

function close() {
  if (props.busy) return
  emit('close')
  emit('update:open', false)
}

/* Separate from close() on purpose: closeOnBackdrop must not disable the close
   button, only the click-outside shortcut. */
function onBackdrop() {
  if (props.closeOnBackdrop) close()
}

function trapFocus(event) {
  if (event.key === 'Escape') {
    if (!props.closeOnEscape) return
    event.preventDefault()
    close()
    return
  }
  if (event.key !== 'Tab') return

  const elements = panel.value?.querySelectorAll(FOCUSABLE)
  if (!elements?.length) return
  const first = elements[0]
  const last = elements[elements.length - 1]

  if (event.shiftKey && document.activeElement === first) {
    event.preventDefault()
    last.focus()
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault()
    first.focus()
  }
}

onBeforeUnmount(release)
</script>

<style scoped>
.app-modal {
  display: flex;
  width: min(100%, 560px);
  max-height: min(88vh, 900px);
  flex-direction: column;
  border: 1px solid var(--tv-border);
  border-radius: var(--tv-radius-lg);
  background: var(--tv-surface-2);
  box-shadow: var(--tv-shadow-lg);
}

.app-modal--sm {
  width: min(100%, 440px);
}

.app-modal--lg {
  width: min(100%, 780px);
}

.app-modal__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.25rem 1.25rem 0.75rem;
}

.app-modal__heading h2 {
  margin: 0;
  color: var(--tv-text-strong);
  font-size: 1.2rem;
  font-weight: 750;
}

.app-modal__heading p {
  margin: 0.35rem 0 0;
  color: var(--tv-text-muted);
  font-size: 0.9rem;
}

/* The body is the only scrolling region, so the heading and the actions stay
   reachable on a short viewport. */
.app-modal__body {
  padding: 0.5rem 1.25rem 1.25rem;
  overflow-y: auto;
}

.app-modal__footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
  padding: 1rem 1.25rem;
  border-top: 1px solid var(--tv-border-subtle);
  border-radius: 0 0 calc(var(--tv-radius-lg) - 1px) calc(var(--tv-radius-lg) - 1px);
  background: var(--tv-surface-sunken);
}

@media (max-width: 575.98px) {
  .app-modal__footer {
    align-items: stretch;
    flex-direction: column-reverse;
  }

  .app-modal__footer > * {
    width: 100%;
  }
}
</style>
