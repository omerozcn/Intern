<template>
  <div class="section-heading">
    <span v-if="icon" class="section-heading__icon" aria-hidden="true">
      <i :class="['bi', icon]"></i>
    </span>

    <div class="section-heading__copy">
      <component :is="level">{{ title }}</component>
      <p v-if="description">{{ description }}</p>
    </div>

    <div v-if="$slots.actions" class="section-heading__actions">
      <slot name="actions" />
    </div>
  </div>
</template>

<script setup>
/*
  The icon + heading + supporting-line unit that Profile, SendFeedback,
  CreateTicket and Dashboard each had their own copy of. `level` is a prop
  because the correct heading rank depends on where the section sits, and
  hardcoding h2 would break the outline on pages that nest.
*/
defineProps({
  title: { type: String, required: true },
  description: { type: String, default: '' },
  icon: { type: String, default: '' },
  level: { type: String, default: 'h2' },
})
</script>

<style scoped>
.section-heading {
  display: flex;
  align-items: flex-start;
  gap: 0.85rem;
  margin-bottom: 1.25rem;
}

.section-heading__icon {
  display: grid;
  width: 40px;
  height: 40px;
  flex: 0 0 40px;
  place-items: center;
  border-radius: 12px;
  background: var(--tv-accent-soft);
  color: var(--tv-accent-on-soft);
  font-size: 1.1rem;
}

.section-heading__copy {
  min-width: 0;
  flex: 1;
}

.section-heading__copy :is(h2, h3, h4) {
  margin: 0;
  color: var(--tv-text-strong);
  font-size: 1.125rem;
  font-weight: 750;
}

.section-heading__copy p {
  margin: 0.25rem 0 0;
  color: var(--tv-text-muted);
  font-size: 0.9rem;
}

.section-heading__actions {
  display: flex;
  flex: 0 0 auto;
  gap: 0.5rem;
}
</style>
