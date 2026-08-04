<template>
  <!--
    A placeholder that mirrors the shape of the list being loaded. It reads as
    faster than a spinner because the layout settles once instead of twice, and
    it keeps the page height stable while data arrives.

    aria-busy plus a single polite status message: announcing one row per
    placeholder would be noise.
  -->
  <div class="skeleton-list" :class="`skeleton-list--${variant}`" aria-busy="true" role="status">
    <span class="visually-hidden">{{ message || t('common.loading') }}</span>

    <div v-for="row in rows" :key="row" class="skeleton-list__item surface-card" aria-hidden="true">
      <div v-if="variant === 'card'" class="tv-skeleton skeleton-list__avatar"></div>

      <div class="skeleton-list__lines">
        <div class="tv-skeleton skeleton-list__line skeleton-list__line--title"></div>
        <div class="tv-skeleton skeleton-list__line"></div>
      </div>

      <div class="tv-skeleton skeleton-list__action"></div>
    </div>
  </div>
</template>

<script setup>
import { useI18n } from 'vue-i18n'

defineProps({
  rows: { type: Number, default: 4 },
  /** `card` adds a leading avatar/icon block; `row` is the plainer table shape. */
  variant: { type: String, default: 'card' },
  message: { type: String, default: '' },
})

const { t } = useI18n()
</script>

<style scoped>
.skeleton-list { display: grid; gap: 1rem; }
.skeleton-list--row { gap: .5rem; }

.skeleton-list__item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.25rem;
}

.skeleton-list--row .skeleton-list__item { padding: .85rem 1rem; }

.skeleton-list__avatar { width: 44px; height: 44px; flex: 0 0 44px; border-radius: 13px; }
.skeleton-list__lines { display: grid; flex: 1; gap: .5rem; min-width: 0; }
.skeleton-list__line { height: 10px; }
.skeleton-list__line--title { width: 40%; height: 14px; }
.skeleton-list__line:not(.skeleton-list__line--title) { width: 65%; }
.skeleton-list__action { width: 84px; height: 32px; flex: 0 0 84px; border-radius: 10px; }

@media (max-width: 575px) {
  .skeleton-list__action { display: none; }
}
</style>
