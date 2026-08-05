<template>
  <section>
    <PageHeader :title="t('tickets.myTitle')" :description="t('tickets.myDescription')">
      <template #actions>
        <RouterLink class="btn btn-primary" to="/tickets/new">
          <i class="bi bi-plus-lg" aria-hidden="true"></i>
          {{ t('tickets.new') }}
        </RouterLink>
      </template>
    </PageHeader>

    <FilterChips
      v-model="activeFilter"
      class="mb-4"
      :options="filters"
      :aria-label="t('tickets.filterLabel')"
    />

    <SkeletonList v-if="loading" :rows="4" />
    <ErrorState v-else-if="error" :message="error" @retry="load" />
    <EmptyState
      v-else-if="!tickets.length"
      icon="bi-inbox"
      :title="t('tickets.emptyTitle')"
      :message="activeFilter === 'all' ? t('tickets.emptyDescription') : t('tickets.emptyFilter')"
    >
      <template #actions>
        <RouterLink class="btn btn-primary" to="/tickets/new">{{ t('tickets.createFirst') }}</RouterLink>
      </template>
    </EmptyState>

    <!-- TransitionGroup rather than the CSS-only stagger: this grid reorders when
         a filter changes, and FLIP makes the surviving cards glide to their new
         position instead of the whole list cutting. -->
    <TransitionGroup v-else name="tv-list" tag="div" class="ticket-grid">
      <article
        v-for="ticket in tickets"
        :key="ticket.id"
        :class="['surface-card', 'surface-card--interactive', 'ticket-card', `ticket-card--${ticket.status}`]"
      >
        <div class="ticket-card__top">
          <div class="ticket-product">
            <span class="product-icon">
              <i :class="ticket.newProduct ? 'bi bi-stars' : 'bi bi-box-seam'" aria-hidden="true"></i>
            </span>
            <div>
              <span class="ticket-id tv-tabular">#{{ ticket.id }}</span>
              <h2>{{ ticket.productName || t('tickets.newProductRequest') }}</h2>
            </div>
          </div>
          <StatusBadge :status="ticket.status" />
        </div>

        <div v-if="editingId === ticket.id" class="edit-panel">
          <div class="label-row">
            <label :for="`description-${ticket.id}`" class="form-label">{{ t('fields.description') }}</label>
            <span :class="['counter tv-tabular', editCounterTone]">{{ editText.length }}/280</span>
          </div>
          <textarea
            :id="`description-${ticket.id}`"
            ref="editArea"
            v-model="editText"
            class="form-control"
            :class="{ 'is-invalid': editError }"
            rows="5"
            maxlength="280"
            :disabled="savingId === ticket.id"
            @input="autoGrow"
          ></textarea>
          <div v-if="editError" class="invalid-feedback d-block">{{ editError }}</div>
          <div class="edit-actions">
            <button
              type="button"
              class="btn btn-outline-secondary"
              :disabled="savingId === ticket.id"
              @click="cancelEdit"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              type="button"
              class="btn btn-primary"
              :disabled="savingId === ticket.id"
              @click="saveEdit(ticket)"
            >
              <span
                v-if="savingId === ticket.id"
                class="spinner-border spinner-border-sm me-2"
                aria-hidden="true"
              ></span>
              {{ t('common.save') }}
            </button>
          </div>
        </div>

        <template v-else>
          <p class="ticket-description">{{ ticket.description }}</p>

          <!-- The single admin response. Given its own panel rather than a bare
               left border because it is the thing the requester opened the page
               to read. -->
          <div v-if="ticket.answer" class="answer-box">
            <span class="answer-box__label">
              <i class="bi bi-chat-left-quote" aria-hidden="true"></i>
              {{ t('tickets.answer') }}
            </span>
            <p>{{ ticket.answer }}</p>
          </div>
        </template>

        <div class="ticket-card__footer">
          <dl>
            <div>
              <dt>{{ t('tickets.created') }}</dt>
              <dd>
                <time :datetime="ticket.created || undefined" :title="formatDate(ticket.created)">
                  {{ relativeTime(ticket.created, locale) || t('common.notAvailable') }}
                </time>
              </dd>
            </div>
            <div v-if="ticket.updated">
              <dt>{{ t('tickets.updated') }}</dt>
              <dd>
                <time :datetime="ticket.updated" :title="formatDate(ticket.updated)">
                  {{ relativeTime(ticket.updated, locale) }}
                </time>
              </dd>
            </div>
          </dl>

          <div
            v-if="ticket.status === TICKET_STATUS.PENDING && editingId !== ticket.id"
            class="ticket-actions"
          >
            <button
              type="button"
              class="btn btn-sm btn-outline-secondary"
              :aria-label="t('tickets.editAria', { id: ticket.id })"
              @click="startEdit(ticket)"
            >
              <i class="bi bi-pencil" aria-hidden="true"></i>
              {{ t('common.edit') }}
            </button>
            <button
              type="button"
              class="btn btn-sm btn-outline-danger"
              :aria-label="t('tickets.deleteAria', { id: ticket.id })"
              @click="askDelete(ticket)"
            >
              <i class="bi bi-trash" aria-hidden="true"></i>
              {{ t('common.delete') }}
            </button>
          </div>
        </div>
      </article>
    </TransitionGroup>

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
import { computed, nextTick, onMounted, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";

import ConfirmDialog from "@/components/ConfirmDialog.vue";
import EmptyState from "@/components/EmptyState.vue";
import ErrorState from "@/components/ErrorState.vue";
import FilterChips from "@/components/FilterChips.vue";
import PageHeader from "@/components/PageHeader.vue";
import PaginationBar from "@/components/PaginationBar.vue";
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
const editingId = ref(null);
const editText = ref("");
const editError = ref("");
const editArea = ref(null);
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

// The API rejects anything outside 30–280, so the counter warns before the edge.
const editCounterTone = computed(() => {
  const length = editText.value.trim().length;
  if (length > 280 || (length > 0 && length < 30)) return "counter--danger";
  if (editText.value.length >= 250) return "counter--warning";
  return "";
});

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
  const date = parseApiDate(value);
  if (!date) return t("common.notAvailable");
  return new Intl.DateTimeFormat(locale.value === "tr" ? "tr-TR" : "en-US", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

async function load() {
  await Promise.all([loadPage(), loadStatusCounts()]);
}

/* The textarea grows with its content so the card does not sprout an inner
   scrollbar half way through a sentence. */
function autoGrow(event) {
  const field = event.target;
  field.style.height = "auto";
  field.style.height = `${field.scrollHeight}px`;
}

async function startEdit(ticket) {
  editingId.value = ticket.id;
  editText.value = ticket.description;
  editError.value = "";
  await nextTick();
  const field = Array.isArray(editArea.value) ? editArea.value[0] : editArea.value;
  field?.focus();
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
.ticket-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
}

.ticket-card {
  display: flex;
  min-width: 0;
  flex-direction: column;
  padding: 1.25rem;
  border-left: 3px solid var(--tv-border);
}

.ticket-card--pending { border-left-color: var(--tv-chart-pending); }
.ticket-card--inProgress { border-left-color: var(--tv-chart-inProgress); }
.ticket-card--completed { border-left-color: var(--tv-chart-completed); }

.ticket-card__top,
.ticket-card__footer {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}

.ticket-product { display: flex; min-width: 0; align-items: center; gap: .75rem; }

.product-icon {
  display: grid;
  width: 42px;
  height: 42px;
  flex: 0 0 42px;
  place-items: center;
  border-radius: 12px;
  background: var(--tv-accent-soft);
  color: var(--tv-accent-on-soft);
}

.ticket-id { color: var(--tv-text-subtle); font-size: .75rem; font-weight: 700; }
.ticket-product h2 { margin: .1rem 0 0; color: var(--tv-text-strong); font-size: 1rem; overflow-wrap: anywhere; }
.ticket-description { flex: 1; margin: 1.25rem 0; color: var(--tv-text); white-space: pre-wrap; overflow-wrap: anywhere; }

.answer-box {
  margin: 0 0 1.25rem;
  padding: 1rem;
  border: 1px solid var(--tv-accent-soft-strong);
  border-radius: 12px;
  background: var(--tv-accent-soft);
}

.answer-box__label {
  display: inline-flex;
  align-items: center;
  gap: .35rem;
  color: var(--tv-accent-on-soft);
  font-size: .75rem;
  font-weight: 800;
  text-transform: uppercase;
}

.answer-box p { margin: .35rem 0 0; color: var(--tv-text); white-space: pre-wrap; overflow-wrap: anywhere; }

.ticket-card__footer {
  align-items: flex-end;
  margin-top: auto;
  padding-top: 1rem;
  border-top: 1px solid var(--tv-border-subtle);
}

.ticket-card__footer dl { display: flex; flex-wrap: wrap; margin: 0; gap: 1rem; }
.ticket-card__footer dt { color: var(--tv-text-subtle); font-size: .7rem; font-weight: 600; }
.ticket-card__footer dd { margin: .15rem 0 0; color: var(--tv-text); font-size: .8rem; }

.ticket-actions { display: flex; gap: .5rem; }
.edit-panel { margin: 1.25rem 0; }
.label-row, .edit-actions { display: flex; justify-content: space-between; gap: .75rem; }
.counter { color: var(--tv-text-subtle); font-size: .8rem; }
.counter--warning { color: var(--tv-warning-on-soft); font-weight: 700; }
.counter--danger { color: var(--tv-danger-on-soft); font-weight: 700; }
.edit-actions { justify-content: flex-end; margin-top: .75rem; }

@media (max-width: 899px) {
  .ticket-grid { grid-template-columns: 1fr; }
}

@media (max-width: 575px) {
  .ticket-card__top,
  .ticket-card__footer { align-items: stretch; flex-direction: column; }
  .ticket-actions .btn { flex: 1; }
}
</style>
