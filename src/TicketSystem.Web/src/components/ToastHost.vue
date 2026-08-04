<template>
  <Teleport to="body">
    <div class="toast-host" :aria-label="t('common.notifications')">
      <TransitionGroup name="toast-list">
        <div
          v-for="toast in store.items"
          :key="toast.id"
          :class="['app-toast', `app-toast--${toast.type}`]"
          :role="toast.type === 'error' || toast.type === 'warning' ? 'alert' : 'status'"
          :aria-live="toast.type === 'error' || toast.type === 'warning' ? 'assertive' : 'polite'"
        >
          <i :class="['bi', iconFor(toast.type)]" aria-hidden="true"></i>
          <p>{{ toast.message }}</p>
          <button
            class="app-toast__close"
            type="button"
            :aria-label="t('common.dismissNotification')"
            @click="store.dismiss(toast.id)"
          >
            <i class="bi bi-x-lg" aria-hidden="true"></i>
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<script setup>
import { useI18n } from 'vue-i18n'

import { useToastStore } from '@/stores/toast'

const store = useToastStore()
const { t } = useI18n()

function iconFor(type) {
  return {
    success: 'bi-check-circle-fill',
    error: 'bi-exclamation-circle-fill',
    warning: 'bi-exclamation-triangle-fill',
    info: 'bi-info-circle-fill',
  }[type] || 'bi-info-circle-fill'
}
</script>
