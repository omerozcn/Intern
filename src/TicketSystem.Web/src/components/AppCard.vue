<template>
  <component
    :is="to ? RouterLink : as"
    :to="to || null"
    :class="[
      'surface-card',
      'app-card',
      `app-card--${padding}`,
      tone !== 'default' && `app-card--${tone}`,
      (interactive || to) && 'surface-card--interactive',
      to && 'app-card--link',
    ]"
  >
    <header v-if="$slots.header" class="app-card__header">
      <slot name="header" />
      <div v-if="$slots.actions" class="app-card__actions">
        <slot name="actions" />
      </div>
    </header>

    <slot />

    <footer v-if="$slots.footer" class="app-card__footer">
      <slot name="footer" />
    </footer>
  </component>
</template>

<script setup>
import { RouterLink } from 'vue-router'

/*
  Passing `to` renders a real RouterLink rather than a div with a click handler,
  so the card is reachable by keyboard and opens in a new tab like any link.
*/
defineProps({
  as: { type: String, default: 'article' },
  to: { type: [String, Object], default: null },
  interactive: { type: Boolean, default: false },
  padding: { type: String, default: 'md' },
  tone: { type: String, default: 'default' },
})
</script>

<style scoped>
.app-card {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 0.75rem;
}

.app-card--sm {
  padding: 1rem;
}

.app-card--md {
  padding: 1.25rem;
}

.app-card--lg {
  padding: clamp(1.25rem, 3vw, 2rem);
}

.app-card--accent {
  border-color: var(--tv-accent-soft-strong);
  background: var(--tv-accent-soft);
}

.app-card--muted {
  background: var(--tv-surface-sunken);
}

.app-card--link {
  color: inherit;
  text-decoration: none;
}

.app-card__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.75rem;
}

.app-card__actions {
  display: flex;
  flex: 0 0 auto;
  gap: 0.35rem;
}

.app-card__footer {
  margin-top: auto;
  padding-top: 0.75rem;
  border-top: 1px solid var(--tv-border-subtle);
}
</style>
