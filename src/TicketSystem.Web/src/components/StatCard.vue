<template>
  <component
    :is="to ? RouterLink : 'article'"
    :to="to || null"
    :class="['surface-card', 'stat-card', `stat-card--${tone}`, to && 'stat-card--link']"
  >
    <div class="stat-card__top">
      <span class="stat-card__icon" aria-hidden="true">
        <i :class="['bi', icon]"></i>
      </span>
      <i v-if="to" class="bi bi-arrow-right stat-card__go" aria-hidden="true"></i>
    </div>

    <p class="stat-card__label">{{ label }}</p>

    <strong class="stat-card__value tv-tabular">
      <span v-if="loading" class="stat-card__placeholder tv-skeleton" aria-hidden="true"></span>
      <AnimatedNumber v-else :value="value" />
    </strong>

    <p v-if="hint" class="stat-card__hint">{{ hint }}</p>

    <!--
      The rail restates the share the number already carries, so it is decorative
      and hidden from assistive tech rather than given a redundant meter role.
    -->
    <div v-if="share !== null" class="stat-card__rail" aria-hidden="true">
      <span class="stat-card__rail-fill" :style="{ width: `${railWidth}%` }"></span>
    </div>
  </component>
</template>

<script setup>
import { computed } from 'vue'
import { RouterLink } from 'vue-router'

import AnimatedNumber from './AnimatedNumber.vue'

const props = defineProps({
  label: { type: String, required: true },
  value: { type: Number, default: 0 },
  icon: { type: String, default: 'bi-layers' },
  tone: { type: String, default: 'neutral' },
  hint: { type: String, default: '' },
  // 0–1, or null to omit the proportion rail entirely.
  share: { type: Number, default: null },
  to: { type: [String, Object], default: null },
  loading: { type: Boolean, default: false },
})

const railWidth = computed(() => Math.min(100, Math.max(0, (props.share ?? 0) * 100)))
</script>

<style scoped>
.stat-card {
  display: flex;
  min-height: 148px;
  flex-direction: column;
  gap: 0.35rem;
  padding: 1.25rem;
}

.stat-card--link {
  color: inherit;
  text-decoration: none;
}

.stat-card__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.35rem;
}

.stat-card__icon {
  display: grid;
  width: 44px;
  height: 44px;
  place-items: center;
  border-radius: 14px;
  background: var(--tv-accent-soft);
  color: var(--tv-accent-on-soft);
  font-size: 1.2rem;
}

.stat-card--pending .stat-card__icon {
  background: var(--tv-warning-soft);
  color: var(--tv-warning-on-soft);
}

.stat-card--inProgress .stat-card__icon {
  background: var(--tv-info-soft);
  color: var(--tv-info-on-soft);
}

.stat-card--completed .stat-card__icon {
  background: var(--tv-success-soft);
  color: var(--tv-success-on-soft);
}

.stat-card__go {
  color: var(--tv-text-subtle);
  transition: transform var(--tv-duration) var(--tv-ease);
}

.stat-card--link:hover .stat-card__go {
  color: var(--tv-accent);
  transform: translateX(3px);
}

.stat-card__label {
  margin: 0;
  color: var(--tv-text-muted);
  font-size: 0.875rem;
  font-weight: 600;
}

.stat-card__value {
  color: var(--tv-text-strong);
  font-size: 2rem;
  font-weight: 800;
  letter-spacing: -0.02em;
  line-height: 1.1;
}

.stat-card__placeholder {
  display: inline-block;
  width: 3ch;
  height: 1em;
  vertical-align: -0.1em;
}

.stat-card__hint {
  margin: 0;
  color: var(--tv-text-subtle);
  font-size: 0.8rem;
}

.stat-card__rail {
  overflow: hidden;
  height: 4px;
  margin-top: auto;
  border-radius: 999px;
  background: var(--tv-surface-muted);
}

.stat-card__rail-fill {
  display: block;
  height: 100%;
  border-radius: inherit;
  background: var(--tv-accent);
  transition: width var(--tv-duration-slower) var(--tv-ease);
}

.stat-card--pending .stat-card__rail-fill {
  background: var(--tv-chart-pending);
}

.stat-card--inProgress .stat-card__rail-fill {
  background: var(--tv-chart-inProgress);
}

.stat-card--completed .stat-card__rail-fill {
  background: var(--tv-chart-completed);
}

@media (prefers-reduced-motion: reduce) {
  .stat-card--link:hover .stat-card__go {
    transform: none;
  }
}
</style>
