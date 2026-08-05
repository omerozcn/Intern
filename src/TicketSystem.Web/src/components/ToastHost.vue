<template>
  <Teleport to="body">
    <!--
      The live regions are the two containers, which exist from first render. A
      region created together with its content is announced unreliably, so each
      toast is placed into whichever region already matches its urgency.
    -->
    <div
      class="toast-host"
      role="status"
      aria-live="polite"
      :aria-label="t('common.notifications')"
    >
      <TransitionGroup name="toast-list">
        <div v-for="toast in politeToasts" :key="toast.id" :class="toastClass(toast)">
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
          <span
            v-if="toast.duration > 0"
            class="app-toast__timer"
            :style="{ animationDuration: `${toast.duration}ms` }"
            aria-hidden="true"
          ></span>
        </div>
      </TransitionGroup>
    </div>

    <div class="toast-host toast-host--assertive" role="alert" aria-live="assertive">
      <TransitionGroup name="toast-list">
        <div v-for="toast in assertiveToasts" :key="toast.id" :class="toastClass(toast)">
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
          <span
            v-if="toast.duration > 0"
            class="app-toast__timer"
            :style="{ animationDuration: `${toast.duration}ms` }"
            aria-hidden="true"
          ></span>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<script setup>
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import { useToastStore } from '@/stores/toast'

const store = useToastStore()
const { t } = useI18n()

const URGENT_TYPES = ['error', 'warning']

const assertiveToasts = computed(() => store.items.filter((item) => URGENT_TYPES.includes(item.type)))
const politeToasts = computed(() => store.items.filter((item) => !URGENT_TYPES.includes(item.type)))

function toastClass(toast) {
  return ['app-toast', `app-toast--${toast.type}`]
}

function iconFor(type) {
  return {
    success: 'bi-check-circle-fill',
    error: 'bi-exclamation-circle-fill',
    warning: 'bi-exclamation-triangle-fill',
    info: 'bi-info-circle-fill',
  }[type] || 'bi-info-circle-fill'
}
</script>
