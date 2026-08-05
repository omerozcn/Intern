<template>
  <div :class="['data-toolbar', { 'data-toolbar--sticky': sticky }]">
    <div v-if="$slots.search" class="data-toolbar__search">
      <slot name="search" />
    </div>

    <div v-if="$slots.default" class="data-toolbar__filters">
      <slot />
    </div>

    <p v-if="$slots.meta" class="data-toolbar__meta">
      <slot name="meta" />
    </p>

    <div v-if="$slots.actions" class="data-toolbar__actions">
      <slot name="actions" />
    </div>
  </div>
</template>

<script setup>
/*
  Replaces five near-identical .toolbar / .list-toolbar / .create-bar blocks that
  had drifted to five different breakpoints. One layout, one place to change it.
*/
defineProps({
  sticky: { type: Boolean, default: false },
})
</script>

<style scoped>
.data-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1.25rem;
  padding: 0.9rem;
  border: 1px solid var(--tv-border);
  border-radius: var(--tv-radius-md);
  background: var(--tv-surface-card);
  box-shadow: var(--tv-shadow-sm);
}

.data-toolbar--sticky {
  position: sticky;
  /* Clears the topbar, which is sticky at the same edge. */
  top: calc(var(--tv-topbar-height) + 8px);
  z-index: 10;
}

.data-toolbar__search {
  min-width: min(100%, 260px);
  flex: 1 1 260px;
}

.data-toolbar__filters {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
}

.data-toolbar__meta {
  margin: 0;
  color: var(--tv-text-muted);
  font-size: 0.875rem;
  font-variant-numeric: tabular-nums;
}

.data-toolbar__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  /* Pushed to the trailing edge so actions never interleave with filters. */
  margin-left: auto;
}

@media (max-width: 767.98px) {
  .data-toolbar {
    align-items: stretch;
    flex-direction: column;
  }

  .data-toolbar__actions {
    margin-left: 0;
  }

  .data-toolbar__actions > * {
    width: 100%;
  }
}
</style>
