<template>
  <span :class="['status-badge', `status-badge--${normalizedStatus}`]">
    <span class="status-badge__dot" aria-hidden="true"></span>
    {{ label }}
  </span>
</template>

<script setup>
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

const props = defineProps({
  status: { type: [String, Number], required: true },
})

const { t } = useI18n()
const normalizedStatus = computed(() => normalizeStatus(props.status))
const label = computed(() => t(`status.${normalizedStatus.value}`))

function normalizeStatus(value) {
  const normalized = String(value ?? '').replace(/[\s_-]/g, '').toLowerCase()
  if (normalized === '1' || normalized === 'pending') return 'pending'
  if (normalized === '2' || normalized === 'inprogress' || normalized === 'processing') {
    return 'inProgress'
  }
  if (normalized === '3' || normalized === 'completed' || normalized === 'complete') {
    return 'completed'
  }
  return 'pending'
}
</script>
