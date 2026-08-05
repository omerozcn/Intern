<template>
  <span :class="['role-badge', `role-badge--${normalized}`]">
    <i :class="['bi', normalized === 'admin' ? 'bi-shield-lock' : 'bi-person']" aria-hidden="true"></i>
    {{ t(`roles.${normalized}`) }}
  </span>
</template>

<script setup>
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

const props = defineProps({
  role: { type: String, default: 'User' },
})

const { t } = useI18n()
// The API only ever sends 'Admin' or 'User'; anything else is treated as the
// lower privilege rather than rendered as an unknown label.
const normalized = computed(() =>
  String(props.role).toLowerCase() === 'admin' ? 'admin' : 'user',
)
</script>

<style scoped>
.role-badge {
  display: inline-flex;
  min-height: 28px;
  align-items: center;
  gap: 0.4rem;
  padding: 0.25rem 0.7rem;
  border-radius: 999px;
  font-size: 0.78rem;
  font-weight: 700;
  white-space: nowrap;
}

.role-badge--admin {
  background: var(--tv-accent-soft);
  color: var(--tv-accent-on-soft);
}

.role-badge--user {
  background: var(--tv-surface-muted);
  color: var(--tv-text-muted);
}
</style>
