<template>
  <div :class="['state-panel', { 'state-panel--compact': compact }]" role="status" aria-live="polite">
    <span class="spinner-border text-primary" aria-hidden="true"></span>
    <p>{{ message || t('common.loading') }}</p>
  </div>
</template>

<script setup>
import { useI18n } from 'vue-i18n'

defineProps({
  message: { type: String, default: '' },
  compact: { type: Boolean, default: false },
})
const { t } = useI18n()
</script>
