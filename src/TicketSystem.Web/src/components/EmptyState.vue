<template>
  <div :class="['state-panel', { 'state-panel--compact': compact }]">
    <span class="state-panel__icon" aria-hidden="true">
      <i :class="['bi', icon]"></i>
    </span>
    <h2>{{ title || t('common.emptyTitle') }}</h2>
    <p>{{ message || t('common.emptyMessage') }}</p>
    <div v-if="$slots.actions || $slots.default" class="mt-2">
      <slot name="actions"><slot /></slot>
    </div>
  </div>
</template>

<script setup>
import { useI18n } from 'vue-i18n'

defineProps({
  title: { type: String, default: '' },
  message: { type: String, default: '' },
  icon: { type: String, default: 'bi-inbox' },
  compact: { type: Boolean, default: false },
})
const { t } = useI18n()
</script>
