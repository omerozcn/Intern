<template>
  <nav v-if="totalPages > 1" class="pagination-bar" :aria-label="t('pagination.label')">
    <button
      type="button"
      class="btn btn-sm btn-outline-secondary"
      :disabled="!hasPrevious || busy"
      :aria-label="t('pagination.previous')"
      @click="$emit('change', page - 1)"
    >
      <i class="bi bi-chevron-left" aria-hidden="true"></i>
      <span class="label-text">{{ t('pagination.previous') }}</span>
    </button>

    <span class="pagination-status" aria-live="polite">
      {{ t('pagination.status', { page, totalPages, totalCount }) }}
    </span>

    <button
      type="button"
      class="btn btn-sm btn-outline-secondary"
      :disabled="!hasNext || busy"
      :aria-label="t('pagination.next')"
      @click="$emit('change', page + 1)"
    >
      <span class="label-text">{{ t('pagination.next') }}</span>
      <i class="bi bi-chevron-right" aria-hidden="true"></i>
    </button>
  </nav>
</template>

<script setup>
import { useI18n } from 'vue-i18n'

defineProps({
  page: { type: Number, required: true },
  totalPages: { type: Number, required: true },
  totalCount: { type: Number, default: 0 },
  hasPrevious: { type: Boolean, default: false },
  hasNext: { type: Boolean, default: false },
  busy: { type: Boolean, default: false },
})

defineEmits(['change'])

const { t } = useI18n()
</script>

<style scoped>
.pagination-bar { display: flex; align-items: center; justify-content: center; gap: 1rem; margin-top: 1.5rem; }
.pagination-status { color: var(--color-text-muted); font-size: .875rem; }
.pagination-bar .btn { display: inline-flex; align-items: center; gap: .35rem; }
@media (max-width: 575px) { .label-text { display: none; } }
</style>
