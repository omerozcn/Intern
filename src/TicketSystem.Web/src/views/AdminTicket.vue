<template>
  <section>
    <PageHeader :title="t('adminTickets.title')" :description="t('adminTickets.description')" />

    <div class="toolbar surface-card">
      <div class="search-field">
        <i class="bi bi-search" aria-hidden="true"></i>
        <label class="visually-hidden" for="ticket-search">{{ t('common.search') }}</label>
        <input id="ticket-search" v-model.trim="search" class="form-control" type="search" :placeholder="t('adminTickets.searchPlaceholder')" />
      </div>
      <div class="filter-bar" role="group" :aria-label="t('tickets.filterLabel')">
        <button
          v-for="filter in filters"
          :key="filter.value"
          type="button"
          class="filter-chip"
          :class="{ active: activeFilter === filter.value }"
          :aria-pressed="activeFilter === filter.value"
          @click="activeFilter = filter.value"
        >{{ filter.label }} <span>{{ filter.count }}</span></button>
      </div>
    </div>

    <LoadingState v-if="loading" :message="t('common.loading')" />
    <ErrorState v-else-if="error" :message="error" @retry="load" />
    <EmptyState v-else-if="!tickets.length" icon="bi-inbox" :title="t('adminTickets.emptyTitle')" :message="t('adminTickets.emptyMessage')" />

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
          <tbody>
            <tr v-for="ticket in tickets" :key="ticket.id">
              <td>
                <span class="ticket-id">#{{ ticket.id }} · {{ ticket.createdBy }}</span>
                <p class="description-cell">{{ ticket.description }}</p>
              </td>
              <td>{{ ticket.firmName || t('common.notAvailable') }}</td>
              <td>{{ ticket.productName || t('tickets.newProductRequest') }}</td>
              <td>{{ formatDate(ticket.created) }}</td>
              <td><StatusBadge :status="ticket.status" /></td>
              <td class="text-end">
                <button type="button" class="btn btn-sm btn-outline-primary" :aria-label="t('adminTickets.manageAria', { id: ticket.id })" @click="openManage(ticket)">
                  <i class="bi bi-sliders" aria-hidden="true"></i>
                  {{ t('adminTickets.manage') }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="mobile-cards">
        <article v-for="ticket in tickets" :key="ticket.id" class="surface-card mobile-ticket">
          <div class="mobile-ticket__head"><span>#{{ ticket.id }}</span><StatusBadge :status="ticket.status" /></div>
          <h2>{{ ticket.productName || t('tickets.newProductRequest') }}</h2>
          <p>{{ ticket.description }}</p>
          <dl>
            <div><dt>{{ t('fields.firm') }}</dt><dd>{{ ticket.firmName || t('common.notAvailable') }}</dd></div>
            <div><dt>{{ t('adminTickets.requester') }}</dt><dd>{{ ticket.createdBy }}</dd></div>
            <div><dt>{{ t('tickets.created') }}</dt><dd>{{ formatDate(ticket.created) }}</dd></div>
          </dl>
          <button type="button" class="btn btn-outline-primary w-100" @click="openManage(ticket)">
            <i class="bi bi-sliders" aria-hidden="true"></i> {{ t('adminTickets.manage') }}
          </button>
        </article>
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

    <dialog ref="manageDialog" class="app-dialog" @close="resetDialog">
      <form v-if="selectedTicket" class="dialog-card" method="dialog" @submit.prevent="saveTicket">
        <div class="dialog-header">
          <div>
            <span class="ticket-id">#{{ selectedTicket.id }}</span>
            <h2>{{ t('adminTickets.dialogTitle') }}</h2>
          </div>
          <button type="button" class="icon-button" :aria-label="t('common.close')" @click="closeManage">
            <i class="bi bi-x-lg" aria-hidden="true"></i>
          </button>
        </div>
        <div class="dialog-context">
          <strong>{{ selectedTicket.productName || t('tickets.newProductRequest') }}</strong>
          <p>{{ selectedTicket.description }}</p>
        </div>
        <div class="form-field">
          <label for="admin-status" class="form-label">{{ t('fields.status') }}</label>
          <select id="admin-status" v-model="manageForm.status" class="form-select" :disabled="saving">
            <option :value="TICKET_STATUS.PENDING">{{ t('status.pending') }}</option>
            <option :value="TICKET_STATUS.IN_PROGRESS">{{ t('status.inProgress') }}</option>
            <option :value="TICKET_STATUS.COMPLETED">{{ t('status.completed') }}</option>
          </select>
        </div>
        <div class="form-field">
          <div class="label-row">
            <label for="admin-answer" class="form-label">{{ t('adminTickets.answer') }}</label>
            <span>{{ manageForm.answer.length }}/1000</span>
          </div>
          <textarea
            id="admin-answer"
            v-model="manageForm.answer"
            class="form-control"
            :class="{ 'is-invalid': answerError }"
            rows="6"
            maxlength="1000"
            :disabled="saving"
          ></textarea>
          <div v-if="answerError" class="invalid-feedback">{{ answerError }}</div>
          <div v-else class="form-text">{{ t('adminTickets.answerHint') }}</div>
        </div>
        <div class="dialog-actions">
          <button type="button" class="btn btn-outline-secondary" :disabled="saving" @click="closeManage">{{ t('common.cancel') }}</button>
          <button type="submit" class="btn btn-primary" :disabled="saving">
            <span v-if="saving" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
            {{ t('common.saveChanges') }}
          </button>
        </div>
      </form>
    </dialog>
  </section>
</template>

<script setup>
import { computed, nextTick, onMounted, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import PageHeader from "@/components/PageHeader.vue";
import StatusBadge from "@/components/StatusBadge.vue";
import LoadingState from "@/components/LoadingState.vue";
import EmptyState from "@/components/EmptyState.vue";
import ErrorState from "@/components/ErrorState.vue";
import PaginationBar from "@/components/PaginationBar.vue";
import { usePagedList } from "@/composables/usePagedList";
import { api } from "@/services/api";
import { useToastStore } from "@/stores/toast";
import { normalizeTicket, TICKET_STATUS } from "@/utils/tickets";

const { t, locale } = useI18n();
const toast = useToastStore();

const activeFilter = ref("all");
const selectedTicket = ref(null);
const manageDialog = ref(null);
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
} = usePagedList("/api/Ticket/listTicket", {
  params: listParams,
  map: (rows) => rows.map(normalizeTicket),
});

const filters = computed(() => [
  { value: "all", label: t("common.all"), count: statusCounts.all },
  { value: TICKET_STATUS.PENDING, label: t("status.pending"), count: statusCounts.pending },
  { value: TICKET_STATUS.IN_PROGRESS, label: t("status.inProgress"), count: statusCounts.inProgress },
  { value: TICKET_STATUS.COMPLETED, label: t("status.completed"), count: statusCounts.completed },
]);

function formatDate(value) {
  return value ? new Intl.DateTimeFormat(locale.value === "tr" ? "tr-TR" : "en-US", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value)) : t("common.notAvailable");
}

/** Tab counts cover every page, so they come from the dedicated summary endpoint. */
async function loadStatusCounts() {
  try {
    const rows = await api.get("/api/Ticket/ticketstatuscount");
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

async function openManage(ticket) {
  selectedTicket.value = ticket;
  manageForm.status = ticket.status;
  manageForm.answer = ticket.answer;
  answerError.value = "";
  await nextTick();
  manageDialog.value?.showModal();
}

function closeManage() { manageDialog.value?.close(); }
function resetDialog() {
  selectedTicket.value = null;
  manageForm.status = TICKET_STATUS.PENDING;
  manageForm.answer = "";
  answerError.value = "";
}

async function saveTicket() {
  answerError.value = manageForm.status === TICKET_STATUS.COMPLETED && !manageForm.answer.trim()
    ? t("validation.answerRequired")
    : "";
  if (answerError.value || saving.value || !selectedTicket.value) return;
  saving.value = true;
  try {
    await api.put(`/api/Ticket/updateTicket/${selectedTicket.value.id}`, {
      status: manageForm.status,
      answer: manageForm.answer.trim() || null,
    });
    closeManage();
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
.toolbar { display: flex; align-items: center; justify-content: space-between; gap: 1rem; margin-bottom: 1rem; padding: .75rem; }
.search-field { position: relative; flex: 1 1 280px; max-width: 420px; }
.search-field i { position: absolute; top: 50%; left: 1rem; transform: translateY(-50%); color: var(--color-text-muted); }
.search-field .form-control { padding-left: 2.6rem; }
.filter-bar { display: flex; flex-wrap: wrap; gap: .4rem; }
.filter-chip { min-height: 40px; padding: .45rem .7rem; border: 1px solid transparent; border-radius: 999px; color: var(--color-text-muted); background: transparent; font-weight: 700; }
.filter-chip span { margin-left: .25rem; }
.filter-chip.active { border-color: var(--color-primary); color: var(--color-primary); background: var(--color-primary-soft); }
.table-card { overflow: hidden; }
.table th { padding: 1rem; color: var(--color-text-muted); background: #f8fafc; font-size: .75rem; letter-spacing: .03em; text-transform: uppercase; }
.table td { padding: 1rem; }
.ticket-id { color: var(--color-text-muted); font-size: .75rem; }
.description-cell { max-width: 430px; margin: .3rem 0 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.mobile-cards { display: none; gap: 1rem; }
.mobile-ticket { padding: 1.1rem; }
.mobile-ticket__head { display: flex; align-items: center; justify-content: space-between; gap: 1rem; }
.mobile-ticket h2 { margin: 1rem 0 .5rem; font-size: 1rem; }
.mobile-ticket > p { color: var(--color-text-muted); overflow-wrap: anywhere; }
.mobile-ticket dl { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .75rem; }
.mobile-ticket dt { color: var(--color-text-muted); font-size: .7rem; }
.mobile-ticket dd { margin: .15rem 0 0; font-size: .85rem; overflow-wrap: anywhere; }
.app-dialog { width: min(calc(100% - 2rem), 620px); max-height: calc(100dvh - 2rem); padding: 0; border: 0; border-radius: 18px; background: transparent; box-shadow: 0 24px 70px rgba(15, 23, 42, .25); }
.app-dialog::backdrop { background: rgba(15, 23, 42, .55); backdrop-filter: blur(2px); }
.dialog-card { padding: 1.5rem; border-radius: 18px; background: white; }
.dialog-header { display: flex; justify-content: space-between; gap: 1rem; }
.dialog-header h2 { margin: .2rem 0 0; font-size: 1.25rem; }
.icon-button { display: grid; place-items: center; width: 44px; height: 44px; border: 0; border-radius: 12px; color: var(--color-text-muted); background: var(--color-page); }
.dialog-context { margin: 1.25rem 0; padding: 1rem; border-radius: 12px; background: var(--color-page); }
.dialog-context p { margin: .35rem 0 0; color: var(--color-text-muted); white-space: pre-wrap; overflow-wrap: anywhere; }
.form-field { margin-bottom: 1.25rem; }
.label-row { display: flex; justify-content: space-between; gap: 1rem; }
.label-row span { color: var(--color-text-muted); font-size: .8rem; }
.dialog-actions { display: flex; justify-content: flex-end; gap: .75rem; }
@media (max-width: 900px) { .toolbar { align-items: stretch; flex-direction: column; } .search-field { max-width: none; } }
@media (max-width: 767px) { .desktop-table { display: none; } .mobile-cards { display: grid; } }
@media (max-width: 480px) { .mobile-ticket dl { grid-template-columns: 1fr; } .dialog-actions { flex-direction: column-reverse; } .dialog-actions .btn { width: 100%; } }
</style>
