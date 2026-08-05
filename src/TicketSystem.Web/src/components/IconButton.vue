<template>
  <component
    :is="as === 'router-link' ? RouterLink : 'button'"
    :class="[
      'icon-button',
      variant !== 'neutral' && `icon-button--${variant}`,
      size === 'sm' && 'icon-button--sm',
    ]"
    :type="as === 'button' ? 'button' : null"
    :to="as === 'router-link' ? to : null"
    :disabled="as === 'button' ? disabled || busy : null"
    :aria-label="label"
    :title="label"
  >
    <span v-if="busy" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
    <i v-else :class="['bi', icon]" aria-hidden="true"></i>
  </component>
</template>

<script setup>
import { RouterLink } from 'vue-router'

/*
  One implementation of the icon button. Before this there were four, at 44px,
  44px, 40px and 38px — the last of which quietly broke the 44px touch target
  the rest of the app is careful about.

  `md` (44px) is the default and the only size allowed to stand alone. `sm`
  (40px) exists for rows that are already at least 44px tall, where the control
  is not itself the touch target.
*/
defineProps({
  icon: { type: String, required: true },
  // Required: an icon-only control has no accessible name without it.
  label: { type: String, required: true },
  variant: { type: String, default: 'neutral' },
  size: { type: String, default: 'md' },
  busy: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false },
  as: { type: String, default: 'button' },
  to: { type: [String, Object], default: null },
})
</script>
