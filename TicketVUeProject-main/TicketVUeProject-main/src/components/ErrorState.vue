<template>
  <div :class="['state-panel', 'state-panel--error', { 'state-panel--compact': compact }]" role="alert">
    <span class="state-panel__icon" aria-hidden="true"><i class="bi bi-exclamation-triangle"></i></span>
    <h2>{{ title || t('common.errorTitle') }}</h2>
    <p>{{ message || t('errors.generic') }}</p>
    <button v-if="showRetry" class="btn btn-outline-primary" type="button" @click="$emit('retry')">
      <i class="bi bi-arrow-clockwise me-2" aria-hidden="true"></i>{{ t('common.retry') }}
    </button>
  </div>
</template>

<script setup>
import { useI18n } from 'vue-i18n'

defineProps({
  title: { type: String, default: '' },
  message: { type: String, default: '' },
  showRetry: { type: Boolean, default: true },
  compact: { type: Boolean, default: false },
})
defineEmits(['retry'])
const { t } = useI18n()
</script>
