<template>
  <section>
    <PageHeader :title="t('tickets.myTitle')" :description="t('tickets.myDescription')">
      <template #actions>
        <RouterLink class="btn btn-primary" to="/ticket">
          <i class="bi bi-plus-lg" aria-hidden="true"></i>
          {{ t('tickets.new') }}
        </RouterLink>
      </template>
    </PageHeader>

    <div class="filter-bar" role="group" :aria-label="t('tickets.filterLabel')">
      <button
        v-for="filter in filters"
        :key="filter.value"
        type="button"
        class="filter-chip"
        :class="{ active: activeFilter === filter.value }"
        :aria-pressed="activeFilter === filter.value"
        @click="activeFilter = filter.value"
      >
        {{ filter.label }}
        <span>{{ filter.count }}</span>
      </button>
    </div>

    <LoadingState v-if="loading" :message="t('common.loading')" />
    <ErrorState v-else-if="error" :message="error" @retry="load" />
    <EmptyState
      v-else-if="!tickets.length"
      icon="bi-inbox"
      :title="t('tickets.emptyTitle')"
      :message="activeFilter === 'all' ? t('tickets.emptyDescription') : t('tickets.emptyFilter')"
    >
      <template #actions>
        <RouterLink class="btn btn-primary" to="/ticket">{{ t('tickets.createFirst') }}</RouterLink>
      </template>
    </EmptyState>

    <div v-else class="ticket-grid">
      <article v-for="ticket in tickets" :key="ticket.id" class="surface-card ticket-card">
        <div class="ticket-card__top">
          <div class="ticket-product">
            <span class="product-icon"><i :class="ticket.newProduct ? 'bi bi-stars' : 'bi bi-box-seam'" aria-hidden="true"></i></span>
            <div>
              <span class="ticket-id">#{{ ticket.id }}</span>
              <h2>{{ ticket.productName || t('tickets.newProductRequest') }}</h2>
            </div>
          </div>
          <StatusBadge :status="ticket.status" />
        </div>

        <div v-if="editingId === ticket.id" class="edit-panel">
          <div class="label-row">
            <label :for="`description-${ticket.id}`" class="form-label">{{ t('fields.description') }}</label>
            <span>{{ editText.length }}/280</span>
          </div>
          <textarea
            :id="`description-${ticket.id}`"
            v-model="editText"
            class="form-control"
            :class="{ 'is-invalid': editError }"
            rows="5"
            maxlength="280"
            :disabled="savingId === ticket.id"
          ></textarea>
          <div v-if="editError" class="invalid-feedback">{{ editError }}</div>
          <div class="edit-actions">
            <button type="button" class="btn btn-outline-secondary" :disabled="savingId === ticket.id" @click="cancelEdit">{{ t('common.cancel') }}</button>
            <button type="button" class="btn btn-primary" :disabled="savingId === ticket.id" @click="saveEdit(ticket)">
              <span v-if="savingId === ticket.id" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
              {{ t('common.save') }}
            </button>
          </div>
        </div>
        <template v-else>
          <p class="ticket-description">{{ ticket.description }}</p>
          <div v-if="ticket.answer" class="answer-box">
            <span>{{ t('tickets.answer') }}</span>
            <p>{{ ticket.answer }}</p>
          </div>
        </template>

        <div class="ticket-card__footer">
          <dl>
            <div>
              <dt>{{ t('tickets.created') }}</dt>
              <dd>{{ formatDate(ticket.created) }}</dd>
            </div>
            <div v-if="ticket.updated">
              <dt>{{ t('tickets.updated') }}</dt>
              <dd>{{ formatDate(ticket.updated) }}</dd>
            </div>
          </dl>
          <div v-if="ticket.status === TICKET_STATUS.PENDING && editingId !== ticket.id" class="ticket-actions">
            <button type="button" class="btn btn-sm btn-outline-secondary" :aria-label="t('tickets.editAria', { id: ticket.id })" @click="startEdit(ticket)">
              <i class="bi bi-pencil" aria-hidden="true"></i>
              {{ t('common.edit') }}
            </button>
            <button type="button" class="btn btn-sm btn-outline-danger" :aria-label="t('tickets.deleteAria', { id: ticket.id })" @click="askDelete(ticket)">
              <i class="bi bi-trash" aria-hidden="true"></i>
              {{ t('common.delete') }}
            </button>
          </div>
        </div>
      </article>
    </div>


    <PaginationBar
      :page="page"
      :total-pages="totalPages"
      :total-count="totalCount"
      :has-previous="hasPrevious"
      :has-next="hasNext"
      :busy="loading"
      @change="goToPage"
    />

    <ConfirmDialog
      v-model:open="deleteDialogOpen"
      :title="t('tickets.deleteTitle')"
      :message="t('tickets.deleteMessage')"
      :confirm-label="t('common.delete')"
      variant="danger"
      :busy="deleting"
      @confirm="deleteTicket"
    />
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import PageHeader from "@/components/PageHeader.vue";
import StatusBadge from "@/components/StatusBadge.vue";
import LoadingState from "@/components/LoadingState.vue";
import EmptyState from "@/components/EmptyState.vue";
import ErrorState from "@/components/ErrorState.vue";
import ConfirmDialog from "@/components/ConfirmDialog.vue";
import PaginationBar from "@/components/PaginationBar.vue";
import { usePagedList } from "@/composables/usePagedList";
import { api } from "@/services/api";
import { useToastStore } from "@/stores/toast";
import { normalizeTicket, TICKET_STATUS } from "@/utils/tickets";

const { t, locale } = useI18n();
const toast = useToastStore();

const activeFilter = ref("all");
const editingId = ref(null);
const editText = ref("");
const editError = ref("");
const savingId = ref(null);
const selectedTicket = ref(null);
const deleteDialogOpen = ref(false);
const deleting = ref(false);
const statusCounts = reactive({ all: 0, pending: 0, inProgress: 0, completed: 0 });

// Status filtering happens server side so it covers every page, not just the visible one.
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
  loading,
  error,
  load: loadPage,
  goToPage,
} = usePagedList("/api/tickets/mine", {
  params: listParams,
  map: (rows) => rows.map(normalizeTicket),
});

const filters = computed(() => [
  { value: "all", label: t("common.all"), count: statusCounts.all },
  { value: TICKET_STATUS.PENDING, label: t("status.pending"), count: statusCounts.pending },
  { value: TICKET_STATUS.IN_PROGRESS, label: t("status.inProgress"), count: statusCounts.inProgress },
  { value: TICKET_STATUS.COMPLETED, label: t("status.completed"), count: statusCounts.completed },
]);

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
    // The list still renders; keeping the previous counts is acceptable.
  }
}

function formatDate(value) {
  if (!value) return t("common.notAvailable");
  return new Intl.DateTimeFormat(locale.value === "tr" ? "tr-TR" : "en-US", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

async function load() {
  await Promise.all([loadPage(), loadStatusCounts()]);
}

function startEdit(ticket) {
  editingId.value = ticket.id;
  editText.value = ticket.description;
  editError.value = "";
}

function cancelEdit() {
  editingId.value = null;
  editText.value = "";
  editError.value = "";
}

async function saveEdit(ticket) {
  const length = editText.value.trim().length;
  editError.value = length < 30 || length > 280 ? t("validation.descriptionLength") : "";
  if (editError.value || savingId.value) return;
  savingId.value = ticket.id;
  try {
    await api.put(`/api/tickets/${ticket.id}/description`, { description: editText.value.trim() });
    cancelEdit();
    await load();
    toast.success(t("ticket.updated"));
  } catch (requestError) {
    toast.error(requestError.message || t("errors.updateTicket"));
  } finally {
    savingId.value = null;
  }
}

function askDelete(ticket) {
  selectedTicket.value = ticket;
  deleteDialogOpen.value = true;
}

async function deleteTicket() {
  if (!selectedTicket.value || deleting.value) return;
  deleting.value = true;
  try {
    await api.delete(`/api/tickets/${selectedTicket.value.id}`);
    deleteDialogOpen.value = false;
    selectedTicket.value = null;
    await load();
    toast.success(t("ticket.deleted"));
  } catch (requestError) {
    toast.error(requestError.message || t("errors.deleteTicket"));
  } finally {
    deleting.value = false;
  }
}

onMounted(load);
</script>

<style scoped>
.filter-bar { display: flex; flex-wrap: wrap; gap: .5rem; margin-bottom: 1.25rem; }
.filter-chip { display: inline-flex; align-items: center; gap: .5rem; min-height: 44px; padding: .55rem .9rem; border: 1px solid var(--color-border); border-radius: 999px; color: var(--color-text-muted); background: white; font-weight: 700; }
.filter-chip span { display: grid; place-items: center; min-width: 24px; height: 24px; padding: 0 .35rem; border-radius: 999px; background: var(--color-page); font-size: .75rem; }
.filter-chip.active { border-color: var(--color-primary); color: var(--color-primary); background: var(--color-primary-soft); }
.ticket-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1rem; }
.ticket-card { display: flex; flex-direction: column; min-width: 0; padding: 1.25rem; }
.ticket-card__top, .ticket-card__footer { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; }
.ticket-product { display: flex; align-items: center; min-width: 0; gap: .75rem; }
.product-icon { display: grid; place-items: center; width: 42px; height: 42px; flex: 0 0 42px; border-radius: 12px; color: var(--color-primary); background: var(--color-primary-soft); }
.ticket-id { color: var(--color-text-muted); font-size: .75rem; }
.ticket-product h2 { margin: .1rem 0 0; overflow-wrap: anywhere; font-size: 1rem; }
.ticket-description { flex: 1; margin: 1.25rem 0; color: var(--color-text); white-space: pre-wrap; overflow-wrap: anywhere; }
.answer-box { margin: 0 0 1.25rem; padding: 1rem; border-left: 3px solid var(--color-primary); border-radius: 0 12px 12px 0; background: var(--color-primary-soft); }
.answer-box span { color: var(--color-primary); font-size: .75rem; font-weight: 800; text-transform: uppercase; }
.answer-box p { margin: .35rem 0 0; white-space: pre-wrap; overflow-wrap: anywhere; }
.ticket-card__footer { align-items: flex-end; margin-top: auto; padding-top: 1rem; border-top: 1px solid var(--color-border); }
.ticket-card__footer dl { display: flex; flex-wrap: wrap; gap: 1rem; margin: 0; }
.ticket-card__footer dt { color: var(--color-text-muted); font-size: .7rem; font-weight: 600; }
.ticket-card__footer dd { margin: .15rem 0 0; font-size: .8rem; }
.ticket-actions { display: flex; gap: .5rem; }
.edit-panel { margin: 1.25rem 0; }
.label-row, .edit-actions { display: flex; justify-content: space-between; gap: .75rem; }
.label-row span { color: var(--color-text-muted); font-size: .8rem; }
.edit-actions { justify-content: flex-end; margin-top: .75rem; }
@media (max-width: 899px) { .ticket-grid { grid-template-columns: 1fr; } }
@media (max-width: 575px) { .ticket-card__top, .ticket-card__footer { flex-direction: column; align-items: stretch; } .ticket-actions .btn { flex: 1; } }
</style>
