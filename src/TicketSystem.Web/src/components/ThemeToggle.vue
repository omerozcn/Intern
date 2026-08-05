<template>
  <!--
    Icon variant: one button that cycles light → dark → system. The accessible
    name states the current mode rather than the next one, because the icon a
    user is looking at is the state, not the action.
  -->
  <button
    v-if="variant === 'icon'"
    type="button"
    class="icon-button theme-toggle"
    :aria-label="t('theme.current', { mode: t(`theme.${themeMode}`) })"
    :title="t('theme.current', { mode: t(`theme.${themeMode}`) })"
    @click="onCycle"
  >
    <i :class="['bi', activeIcon]" aria-hidden="true"></i>
  </button>

  <div v-else class="theme-segmented" role="group" :aria-label="t('theme.label')">
    <button
      v-for="mode in themeModes"
      :key="mode.value"
      type="button"
      class="theme-segmented__option"
      :aria-pressed="mode.value === themeMode"
      @click="onSelect(mode.value, $event)"
    >
      <i :class="['bi', mode.icon]" aria-hidden="true"></i>
      {{ t(mode.labelKey) }}
    </button>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import { cycleTheme, setTheme, themeMode, themeModes } from '@/composables/useTheme'

defineProps({
  variant: { type: String, default: 'icon' },
})

const { t } = useI18n()
const activeIcon = computed(
  () => themeModes.find((mode) => mode.value === themeMode.value)?.icon ?? 'bi-circle-half',
)

/* The new theme wipes out of the control that was pressed, so the origin comes
   from the button's own box rather than the pointer — that way a keyboard press
   animates from the same place a click does. */
function originOf(event) {
  const box = event.currentTarget?.getBoundingClientRect()
  if (!box) return null
  return { x: box.left + box.width / 2, y: box.top + box.height / 2 }
}

function onCycle(event) {
  cycleTheme(originOf(event))
}

function onSelect(mode, event) {
  setTheme(mode, originOf(event))
}
</script>

<style scoped>
.theme-segmented {
  display: grid;
  grid-auto-columns: 1fr;
  grid-auto-flow: column;
  gap: 4px;
  padding: 4px;
  border: 1px solid var(--tv-border);
  border-radius: 999px;
  background: var(--tv-surface-sunken);
}

.theme-segmented__option {
  display: inline-flex;
  min-height: 40px;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  padding: 0.4rem 0.75rem;
  border: 0;
  border-radius: 999px;
  background: transparent;
  color: var(--tv-text-muted);
  font-size: 0.875rem;
  font-weight: 650;
  transition:
    background-color var(--tv-duration-fast) ease,
    color var(--tv-duration-fast) ease,
    transform var(--tv-duration-fast) var(--tv-ease-spring);
}

.theme-segmented__option:hover:not([aria-pressed='true']) {
  color: var(--tv-text-strong);
}

.theme-segmented__option[aria-pressed='true'] {
  background: var(--tv-surface-1);
  color: var(--tv-accent-on-soft);
  box-shadow: var(--tv-shadow-sm);
}

.theme-segmented__option:active {
  transform: scale(0.96);
}

@media (prefers-reduced-motion: reduce) {
  .theme-segmented__option:active {
    transform: none;
  }
}
</style>
