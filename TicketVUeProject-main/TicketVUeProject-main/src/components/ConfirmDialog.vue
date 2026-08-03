<template>
  <Teleport to="body">
    <Transition name="dialog-fade">
      <div v-if="open" class="dialog-layer" @mousedown.self="cancel">
        <section
          ref="dialog"
          class="confirm-dialog"
          role="alertdialog"
          aria-modal="true"
          :aria-labelledby="titleId"
          :aria-describedby="messageId"
          @keydown="trapFocus"
        >
          <div class="confirm-dialog__icon" aria-hidden="true">
            <i class="bi bi-exclamation-triangle"></i>
          </div>
          <div class="confirm-dialog__content">
            <h2 :id="titleId">{{ title }}</h2>
            <p :id="messageId">{{ message }}</p>
          </div>
          <div class="confirm-dialog__actions">
            <button ref="cancelButton" class="btn btn-outline-secondary" type="button" :disabled="busy" @click="cancel">
              {{ cancelLabel || t('common.cancel') }}
            </button>
            <button
              :class="['btn', confirmButtonClass]"
              type="button"
              :disabled="busy"
              @click="confirm"
            >
              <span v-if="busy" class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
              {{ confirmLabel || t('common.confirm') }}
            </button>
          </div>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'

const props = defineProps({
  open: { type: Boolean, required: true },
  title: { type: String, required: true },
  message: { type: String, required: true },
  confirmLabel: { type: String, default: '' },
  cancelLabel: { type: String, default: '' },
  variant: { type: String, default: 'danger' },
  busy: { type: Boolean, default: false },
})
const emit = defineEmits(['update:open', 'confirm', 'cancel'])
const { t } = useI18n()

const dialog = ref(null)
const cancelButton = ref(null)
const titleId = `confirm-title-${useId()}`
const messageId = `confirm-message-${useId()}`
const confirmButtonClass = computed(() =>
  props.variant === 'primary' ? 'btn-primary' : 'btn-danger',
)
let returnFocusTo = null

watch(
  () => props.open,
  async (open) => {
    document.body.classList.toggle('dialog-is-open', open)
    if (open) {
      returnFocusTo = document.activeElement
      await nextTick()
      cancelButton.value?.focus()
    } else {
      returnFocusTo?.focus?.()
      returnFocusTo = null
    }
  },
  { immediate: true },
)

function cancel() {
  if (props.busy) return
  emit('cancel')
  emit('update:open', false)
}

function confirm() {
  if (props.busy) return
  emit('confirm')
}

function trapFocus(event) {
  if (event.key === 'Escape') {
    event.preventDefault()
    cancel()
    return
  }
  if (event.key !== 'Tab') return

  const elements = dialog.value?.querySelectorAll(
    'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
  )
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

onBeforeUnmount(() => document.body.classList.remove('dialog-is-open'))
</script>
