<template>
  <p v-if="!tickets.length" class="ticket-queue__empty">{{ emptyMessage }}</p>

  <ul v-else class="ticket-queue">
    <li v-for="ticket in normalized" :key="ticket.id" class="ticket-queue__item">
      <span class="ticket-queue__id tv-tabular">#{{ ticket.id }}</span>

      <span class="ticket-queue__copy">
        <strong>{{ ticket.productName || t('tickets.newProductRequest') }}</strong>
        <small>{{ ticket.firmName || ticket.createdBy || '—' }}</small>
      </span>

      <span class="ticket-queue__meta">
        <span v-if="showAge && ticket.age !== null" class="ticket-queue__age">
          {{ t('dashboard.waitingDays', { count: ticket.age }) }}
        </span>
        <time v-else :datetime="ticket.created || undefined" :title="ticket.absolute">
          {{ ticket.relative }}
        </time>
        <StatusBadge :status="ticket.status" />
      </span>
    </li>
  </ul>
</template>

<script setup>
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import StatusBadge from './StatusBadge.vue'
import { daysSince, parseApiDate, relativeTime } from '@/utils/datetime'

const props = defineProps({
  tickets: { type: Array, default: () => [] },
  emptyMessage: { type: String, required: true },
  // Ageing view: how long it has waited matters more than when it arrived.
  showAge: { type: Boolean, default: false },
})

const { t, locale } = useI18n()

const absoluteFormat = computed(
  () =>
    new Intl.DateTimeFormat(locale.value === 'tr' ? 'tr-TR' : 'en-US', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }),
)

const normalized = computed(() =>
  props.tickets.map((ticket) => {
    const created = parseApiDate(ticket.created)
    return {
      ...ticket,
      relative: relativeTime(ticket.created, locale.value),
      // The exact timestamp stays available on hover; the visible label is
      // relative because "3 days ago" is what a queue is actually read for.
      absolute: created ? absoluteFormat.value.format(created) : '',
      age: daysSince(ticket.created),
    }
  }),
)
</script>

<style scoped>
.ticket-queue {
  display: grid;
  margin: 0;
  padding: 0;
  gap: 0.35rem;
  list-style: none;
}

.ticket-queue__item {
  display: flex;
  min-height: 56px;
  align-items: center;
  gap: 0.75rem;
  padding: 0.5rem 0.6rem;
  border-radius: 12px;
  transition: background-color var(--tv-duration-fast) ease;
}

.ticket-queue__item:hover {
  background: var(--tv-surface-sunken);
}

.ticket-queue__id {
  color: var(--tv-text-subtle);
  font-size: 0.8rem;
  font-weight: 700;
}

.ticket-queue__copy {
  display: grid;
  min-width: 0;
  flex: 1;
}

.ticket-queue__copy strong {
  overflow: hidden;
  color: var(--tv-text-strong);
  font-size: 0.95rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ticket-queue__copy small {
  overflow: hidden;
  color: var(--tv-text-muted);
  font-size: 0.8rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ticket-queue__meta {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  gap: 0.6rem;
  color: var(--tv-text-subtle);
  font-size: 0.78rem;
}

.ticket-queue__age {
  color: var(--tv-warning-on-soft);
  font-weight: 700;
}

.ticket-queue__empty {
  margin: 0;
  padding: 1.5rem 0;
  color: var(--tv-text-muted);
  text-align: center;
}

@media (max-width: 479px) {
  .ticket-queue__item {
    align-items: flex-start;
    flex-wrap: wrap;
  }

  .ticket-queue__meta {
    width: 100%;
    justify-content: space-between;
  }
}
</style>
