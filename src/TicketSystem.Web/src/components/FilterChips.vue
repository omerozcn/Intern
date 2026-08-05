<template>
  <!--
    Deliberately toggle buttons with aria-pressed, not role="tab". A tab swaps a
    visible panel; these filter a list that stays in place, so the pressed-button
    pattern is the accurate one. It is also what e2e/tickets.spec.js queries —
    getByRole('button', { name: /Bekliyor/ }) — which means the count has to stay
    inside the button so it contributes to the accessible name.
  -->
  <div class="filter-chips" role="group" :aria-label="ariaLabel">
    <button
      v-for="option in options"
      :key="option.value"
      type="button"
      :class="['filter-chip', `filter-chip--${option.value}`, { 'filter-chip--sm': size === 'sm' }]"
      :aria-pressed="option.value === modelValue"
      @click="$emit('update:modelValue', option.value)"
    >
      <i v-if="option.icon" :class="['bi', option.icon]" aria-hidden="true"></i>
      {{ option.label }}
      <span v-if="option.count !== undefined && option.count !== null" class="filter-chip__count">
        {{ option.count }}
      </span>
    </button>
  </div>
</template>

<script setup>
defineProps({
  modelValue: { type: [String, Number], default: '' },
  // [{ value, label, count?, icon? }]
  options: { type: Array, required: true },
  ariaLabel: { type: String, required: true },
  size: { type: String, default: 'md' },
})
defineEmits(['update:modelValue'])
</script>

<style scoped>
.filter-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.filter-chip {
  display: inline-flex;
  min-height: 44px;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.95rem;
  border: 1px solid var(--tv-border-strong);
  border-radius: 999px;
  background: var(--tv-surface-1);
  color: var(--tv-text-muted);
  font-weight: 650;
  white-space: nowrap;
  transition:
    border-color var(--tv-duration-fast) ease,
    background-color var(--tv-duration-fast) ease,
    color var(--tv-duration-fast) ease,
    transform var(--tv-duration-fast) var(--tv-ease-spring);
}

.filter-chip--sm {
  min-height: 40px;
  padding: 0.4rem 0.8rem;
  font-size: 0.9rem;
}

.filter-chip:hover:not([aria-pressed='true']) {
  border-color: var(--tv-accent);
  color: var(--tv-text-strong);
}

.filter-chip[aria-pressed='true'] {
  border-color: var(--tv-accent);
  background: var(--tv-accent-soft);
  color: var(--tv-accent-on-soft);
}

.filter-chip:active {
  transform: scale(0.96);
}

.filter-chip__count {
  display: inline-grid;
  min-width: 1.5rem;
  height: 1.5rem;
  place-items: center;
  padding: 0 0.35rem;
  border-radius: 999px;
  background: var(--tv-surface-muted);
  color: var(--tv-text-muted);
  font-size: 0.78rem;
  font-variant-numeric: tabular-nums;
}

.filter-chip[aria-pressed='true'] .filter-chip__count {
  background: var(--tv-accent);
  color: var(--tv-text-on-accent);
}

@media (prefers-reduced-motion: reduce) {
  .filter-chip:active {
    transform: none;
  }
}
</style>
