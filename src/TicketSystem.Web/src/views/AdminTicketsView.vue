<template>
  <section>
    <PageHeader :title="t('adminTickets.title')" :description="t('adminTickets.description')" />

    <DataToolbar>
      <template #search>
        <SearchField
          id="ticket-search"
          v-model="search"
          :placeholder="t('adminTickets.searchPlaceholder')"
          :loading="loading"
        />
      </template>

      <FilterChips
        v-model="activeFilter"
        :options="filters"
        :aria-label="t('tickets.filterLabel')"
        size="sm"
      />
    </DataToolbar>

    <SkeletonList v-if="loading" :rows="4" />
    <ErrorState v-else-if="error" :message="error" @retry="load" />
    <EmptyState
      v-else-if="!tickets.length"
      icon="bi-inbox"
      :title="t('adminTickets.emptyTitle')"
      :message="t('adminTickets.emptyMessage')"
    />

    <template v-else>
      <div class="surface-card table-card desktop-table">
        <table class="table align-middle mb-0">
          <thead>
            <tr>
              <th scope="col">{{ t('adminTickets.ticket') }}</th>
              <th scope="col">{{ t('fields.firm') }}</th>
              <th scope="col">{{ t('fields.product') }}</th>
              <th scope="col">{{ t('tickets.created') }}</th>
              <th scope="col">{{ t('fields.status') }}</th>
              <th scope="col" class="text-end">{{ t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody class="tv-stagger">
            <!-- The status stripe makes the queue scannable without reading any
                 cell; the badge in the status column carries the same fact in
                 text for anyone who cannot see the colour. -->
            <tr v-for="ticket in tickets" :key="ticket.id" :class="`row--${ticket.status}`">
              <td>
                <span class="ticket-id tv-tabular">#{{ ticket.id }} · {{ ticket.createdBy }}</span>
                <p class="description-cell">{{ ticket.description }}</p>
              </td>
              <td>{{ ticket.firmName || t('common.notAvailable') }}</td>
              <td>{{ ticket.productName || t('tickets.newProductRequest') }}</td>
              <td class="tv-tabular">
                <time :datetime="ticket.created || undefined" :title="formatDate(ticket.created)">
                  {{ relativeTime(ticket.created, locale) || t('common.notAvailable') }}
                </time>
              </td>
              <td><StatusBadge :status="ticket.status" /></td>
              <td class="text-end">
                <button
                  type="button"
                  class="btn btn-sm btn-outline-primary"
                  :aria-label="t('adminTickets.manageAria', { id: ticket.id })"
                  @click="openManage(ticket)"
                >
                  <i class="bi bi-sliders" aria-hidden="true"></i>
                  {{ t('adminTickets.manage') }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="mobile-cards">
        <AppCard
          v-for="ticket in tickets"
          :key="ticket.id"
          :class="`edge--${ticket.status}`"
          class="mobile-ticket"
        >
          <template #header>
            <span class="ticket-id tv-tabular">#{{ ticket.id }}</span>
            <StatusBadge :status="ticket.status" />
          </template>

          <h2>{{ ticket.productName || t('tickets.newProductRequest') }}</h2>
          <p class="mobile-ticket__body">{{ ticket.description }}</p>

          <dl>
            <div>
              <dt>{{ t('fields.firm') }}</dt>
              <dd>{{ ticket.firmName || t('common.notAvailable') }}</dd>
            </div>
            <div>
              <dt>{{ t('adminTickets.requester') }}</dt>
              <dd>{{ ticket.createdBy }}</dd>
            </div>
            <div>
              <dt>{{ t('tickets.created') }}</dt>
              <dd>{{ formatDate(ticket.created) }}</dd>
            </div>
          </dl>

          <template #footer>
            <button type="button" class="btn btn-outline-primary w-100" @click="openManage(ticket)">
              <i class="bi bi-sliders" aria-hidden="true"></i> {{ t('adminTickets.manage') }}
            </button>
          </template>
        </AppCard>
      </div>
    </template>

    <PaginationBar
      :page="page"
      :total-pages="totalPages"
      :total-count="totalCount"
      :has-previous="hasPrevious"
      :has-next="hasNext"
      :busy="loading"
      @change="goToPage"
    />

    <AppModal
      v-model:open="manageOpen"
      :title="t('adminTickets.dialogTitle')"
      size="lg"
      :busy="saving"
      initial-focus="[data-status-chips] button"
      @close="resetDialog"
    >
      <form v-if="selectedTicket" id="manage-ticket-form" @submit.prevent="saveTicket">
        <div class="dialog-context">
          <span class="ticket-id tv-tabular">#{{ selectedTicket.id }}</span>
          <strong>{{ selectedTicket.productName || t('tickets.newProductRequest') }}</strong>
          <p>{{ selectedTicket.description }}</p>
        </div>

        <div class="form-field">
          <span class="form-label d-block">{{ t('fields.status') }}</span>
          <div data-status-chips>
            <FilterChips
              v-model="manageForm.status"
              :options="statusOptions"
              :aria-label="t('fields.status')"
              size="sm"
            />
          </div>
        </div>

        <div class="form-field">
          <div class="label-row">
            <label for="admin-answer" class="form-label">{{ t('adminTickets.answer') }}</label>
            <span :class="['counter tv-tabular', counterTone]">{{ manageForm.answer.length }}/1000</span>
          </div>
          <textarea
            id="admin-answer"
            v-model="manageForm.answer"
            class="form-control"
            :class="{ 'is-invalid': answerError }"
            rows="6"
            maxlength="1000"
            :disabled="saving"
            :aria-describedby="answerError ? 'admin-answer-error' : 'admin-answer-hint'"
          ></textarea>
          <div v-if="answerError" id="admin-answer-error" class="invalid-feedback d-block">
            {{ answerError }}
          </div>
          <div v-else id="admin-answer-hint" class="form-text">{{ t('adminTickets.answerHint') }}</div>
        </div>
      </form>

      <template #footer>
        <button
          type="button"
          class="btn btn-outline-secondary"
          :disabled="saving"
          @click="manageOpen = false"
        >
          {{ t('common.cancel') }}
        </button>
        <button
          type="submit"
          form="manage-ticket-form"
          class="btn btn-primary"
          :disabled="saving || blocksCompletion"
        >
          <span v-if="saving" class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
          {{ t('common.saveChanges') }}
        </button>
      </template>
    </AppModal>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from "vue";
import { useI18n } from "vue-i18n";

import AppCard from "@/components/AppCard.vue";
import AppModal from "@/components/AppModal.vue";
import DataToolbar from "@/components/DataToolbar.vue";
import EmptyState from "@/components/EmptyState.vue";
import ErrorState from "@/components/ErrorState.vue";
import FilterChips from "@/components/FilterChips.vue";
import PageHeader from "@/components/PageHeader.vue";
import PaginationBar from "@/components/PaginationBar.vue";
import SearchField from "@/components/SearchField.vue";
import SkeletonList from "@/components/SkeletonList.vue";
import StatusBadge from "@/components/StatusBadge.vue";
import { usePagedList } from "@/composables/usePagedList";
import { api } from "@/services/api";
import { useToastStore } from "@/stores/toast";
import { parseApiDate, relativeTime } from "@/utils/datetime";
import { normalizeTicket, TICKET_STATUS } from "@/utils/tickets";

const { t, locale } = useI18n();
const toast = useToastStore();

const activeFilter = ref("all");
const selectedTicket = ref(null);
const manageOpen = ref(false);
const manageForm = reactive({ status: TICKET_STATUS.PENDING, answer: "" });
const answerError = ref("");
const saving = ref(false);
const statusCounts = reactive({ all: 0, pending: 0, inProgress: 0, completed: 0 });

// Status filtering happens server side; filtering only the visible page would hide
// matching tickets that live on other pages.
const listParams = computed(() => ({
  status: activeFilter.value === "all" ? "" : activeFilter.value,
}));

const {
  items: tickets,
  page,
  totalPages,
  totalCount,
  hasPrevious,
  hasNext,
  search,
  loading,
  error,
  load: loadPage,
  goToPage,
} = usePagedList("/api/tickets", {
  params: listParams,
  map: (rows) => rows.map(normalizeTicket),
});

const filters = computed(() => [
  { value: "all", label: t("common.all"), count: statusCounts.all },
  { value: TICKET_STATUS.PENDING, label: t("status.pending"), count: statusCounts.pending },
  { value: TICKET_STATUS.IN_PROGRESS, label: t("status.inProgress"), count: statusCounts.inProgress },
  { value: TICKET_STATUS.COMPLETED, label: t("status.completed"), count: statusCounts.completed },
]);

const statusOptions = computed(() => [
  { value: TICKET_STATUS.PENDING, label: t("status.pending"), icon: "bi-clock" },
  { value: TICKET_STATUS.IN_PROGRESS, label: t("status.inProgress"), icon: "bi-arrow-repeat" },
  { value: TICKET_STATUS.COMPLETED, label: t("status.completed"), icon: "bi-check2-circle" },
]);

/* The API rejects completing a ticket with a blank answer. Surfacing that as a
   disabled button and an inline message means the rule is visible while it can
   still be acted on, instead of arriving as a 400 after submit. */
const blocksCompletion = computed(
  () => manageForm.status === TICKET_STATUS.COMPLETED && !manageForm.answer.trim(),
);

const counterTone = computed(() => {
  if (manageForm.answer.length >= 1000) return "counter--danger";
  if (manageForm.answer.length >= 900) return "counter--warning";
  return "";
});

watch(
  () => [manageForm.status, manageForm.answer],
  () => {
    if (!blocksCompletion.value) answerError.value = "";
  },
);

function formatDate(value) {
  const date = parseApiDate(value);
  return date
    ? new Intl.DateTimeFormat(locale.value === "tr" ? "tr-TR" : "en-US", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(date)
    : t("common.notAvailable");
}

/** Tab counts cover every page, so they come from the dedicated summary endpoint. */
async function loadStatusCounts() {
  try {
    const rows = await api.get("/api/tickets/status-counts");
    const byStatus = Object.fromEntries((rows ?? []).map((row) => [row.status, Number(row.count) || 0]));
    statusCounts.pending = byStatus[TICKET_STATUS.PENDING] ?? 0;
    statusCounts.inProgress = byStatus[TICKET_STATUS.IN_PROGRESS] ?? 0;
    statusCounts.completed = byStatus[TICKET_STATUS.COMPLETED] ?? 0;
    statusCounts.all = statusCounts.pending + statusCounts.inProgress + statusCounts.completed;
  } catch {
    // The list itself still renders; leaving the counts at their last value is enough.
  }
}

async function load() {
  await Promise.all([loadPage(), loadStatusCounts()]);
}

function openManage(ticket) {
  selectedTicket.value = ticket;
  manageForm.status = ticket.status;
  manageForm.answer = ticket.answer ?? "";
  answerError.value = "";
  manageOpen.value = true;
}

function resetDialog() {
  selectedTicket.value = null;
  manageForm.status = TICKET_STATUS.PENDING;
  manageForm.answer = "";
  answerError.value = "";
}

async function saveTicket() {
  if (blocksCompletion.value) {
    answerError.value = t("validation.answerRequired");
    return;
  }
  if (saving.value || !selectedTicket.value) return;

  saving.value = true;
  try {
    await api.put(`/api/tickets/${selectedTicket.value.id}`, {
      status: manageForm.status,
      answer: manageForm.answer.trim() || null,
    });
    manageOpen.value = false;
    await load();
    toast.success(t("adminTickets.updated"));
  } catch (requestError) {
    toast.error(requestError.message || t("errors.updateTicket"));
  } finally {
    saving.value = false;
  }
}

onMounted(load);
</script>

<style scoped>
/* `clip`, not `hidden`. Both round off the table corners, but `hidden` creates a
   scroll container, which confines the sticky header to this card instead of the
   page — it ended up parked 72px down, on top of the first row. `clip` does not
   create one, so the header sticks against the viewport as intended. */
.table-card { overflow: clip; }

.table thead th {
  position: sticky;
  top: var(--tv-topbar-height);
  z-index: 2;
  padding: 1rem;
}

.table td { padding: 1rem; }

.table tbody tr {
  transition: background-color var(--tv-duration-fast) ease;
}

.table tbody tr:hover { background: var(--tv-surface-sunken); }

.table tbody tr td:first-child { box-shadow: inset 3px 0 0 var(--tv-border); }
.row--pending td:first-child { box-shadow: inset 3px 0 0 var(--tv-chart-pending); }
.row--inProgress td:first-child { box-shadow: inset 3px 0 0 var(--tv-chart-inProgress); }
.row--completed td:first-child { box-shadow: inset 3px 0 0 var(--tv-chart-completed); }

.ticket-id { color: var(--tv-text-subtle); font-size: .75rem; font-weight: 700; }

/* Two lines rather than a single ellipsised one: the first line of a request is
   almost never enough to tell two apart. */
.description-cell {
  display: -webkit-box;
  max-width: 430px;
  margin: .3rem 0 0;
  overflow: hidden;
  color: var(--tv-text);
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
  line-clamp: 2;
}

.mobile-cards { display: none; gap: 1rem; }
.mobile-ticket h2 { margin: 0; color: var(--tv-text-strong); font-size: 1rem; }
.mobile-ticket__body { margin: 0; color: var(--tv-text-muted); overflow-wrap: anywhere; }
.mobile-ticket dl { display: grid; margin: 0; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .75rem; }
.mobile-ticket dt { color: var(--tv-text-subtle); font-size: .7rem; }
.mobile-ticket dd { margin: .15rem 0 0; font-size: .85rem; overflow-wrap: anywhere; }

.edge--pending { border-left: 3px solid var(--tv-chart-pending); }
.edge--inProgress { border-left: 3px solid var(--tv-chart-inProgress); }
.edge--completed { border-left: 3px solid var(--tv-chart-completed); }

.dialog-context {
  margin-bottom: 1.25rem;
  padding: 1rem;
  border-radius: 12px;
  background: var(--tv-surface-sunken);
}

.dialog-context strong { display: block; margin-top: .2rem; color: var(--tv-text-strong); }
.dialog-context p { margin: .35rem 0 0; color: var(--tv-text-muted); white-space: pre-wrap; overflow-wrap: anywhere; }

.form-field { margin-bottom: 1.25rem; }
.label-row { display: flex; justify-content: space-between; gap: 1rem; }
.counter { color: var(--tv-text-subtle); font-size: .8rem; }
.counter--warning { color: var(--tv-warning-on-soft); font-weight: 700; }
.counter--danger { color: var(--tv-danger-on-soft); font-weight: 700; }

@media (max-width: 767px) {
  .desktop-table { display: none; }
  .mobile-cards { display: grid; }
}

@media (max-width: 480px) {
  .mobile-ticket dl { grid-template-columns: 1fr; }
}
</style>
