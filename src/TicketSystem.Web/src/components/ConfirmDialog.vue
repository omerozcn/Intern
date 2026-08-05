<template>
  <AppModal
    :open="open"
    :title="title"
    :description="message"
    size="sm"
    role="alertdialog"
    :busy="busy"
    initial-focus="[data-confirm-cancel]"
    @update:open="$emit('update:open', $event)"
    @close="$emit('cancel')"
  >
    <template #header="{ titleId, descriptionId }">
      <div class="confirm-lead">
        <span :class="['confirm-lead__icon', `confirm-lead__icon--${variant}`]" aria-hidden="true">
          <i :class="['bi', variant === 'primary' ? 'bi-question-circle' : 'bi-exclamation-triangle']"></i>
        </span>
        <div>
          <h2 :id="titleId">{{ title }}</h2>
          <p :id="descriptionId">{{ message }}</p>
        </div>
      </div>
    </template>

    <template #footer>
      <button
        data-confirm-cancel
        class="btn btn-outline-secondary"
        type="button"
        :disabled="busy"
        @click="cancel"
      >
        {{ cancelLabel || t('common.cancel') }}
      </button>
      <button :class="['btn', confirmButtonClass]" type="button" :disabled="busy" @click="confirm">
        <span v-if="busy" class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
        {{ confirmLabel || t('common.confirm') }}
      </button>
    </template>
  </AppModal>
</template>

<script setup>
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import AppModal from './AppModal.vue'

/*
  A thin specialisation of AppModal: role="alertdialog", a leading warning icon,
  and a fixed cancel/confirm pair. The focus trap, focus restore, scroll lock and
  transition all come from AppModal, so there is one implementation of each.

  The public API is unchanged from the standalone version — MyTickets, Services
  and Firms use it as-is.
*/
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
const confirmButtonClass = computed(() =>
  props.variant === 'primary' ? 'btn-primary' : 'btn-danger',
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
</script>

<style scoped>
.confirm-lead {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
}

.confirm-lead__icon {
  display: grid;
  width: 48px;
  height: 48px;
  flex: 0 0 48px;
  place-items: center;
  border-radius: 14px;
  background: var(--tv-danger-soft);
  color: var(--tv-danger-on-soft);
  font-size: 1.2rem;
}

.confirm-lead__icon--primary {
  background: var(--tv-accent-soft);
  color: var(--tv-accent-on-soft);
}

.confirm-lead h2 {
  margin: 0;
  color: var(--tv-text-strong);
  font-size: 1.15rem;
  font-weight: 750;
}

.confirm-lead p {
  margin: 0.35rem 0 0;
  color: var(--tv-text-muted);
}
</style>
