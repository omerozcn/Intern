<template>
  <section>
    <PageHeader :title="t('firms.title')" :description="t('firms.description')" />

    <form class="surface-card create-bar" novalidate @submit.prevent="createFirm">
      <div class="create-field">
        <label for="new-firm" class="form-label">{{ t('firms.newName') }}</label>
        <input id="new-firm" v-model.trim="newFirmName" class="form-control" :class="{ 'is-invalid': createError }" :placeholder="t('firms.newPlaceholder')" :disabled="creating" />
        <div v-if="createError" class="invalid-feedback">{{ createError }}</div>
      </div>
      <button class="btn btn-primary" type="submit" :disabled="creating">
        <span v-if="creating" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
        <i v-else class="bi bi-plus-lg" aria-hidden="true"></i>
        {{ t('firms.add') }}
      </button>
    </form>

    <div class="list-toolbar">
      <div class="search-field">
        <i class="bi bi-search" aria-hidden="true"></i>
        <label class="visually-hidden" for="firm-search">{{ t('common.search') }}</label>
        <input id="firm-search" v-model.trim="search" class="form-control" type="search" :placeholder="t('firms.searchPlaceholder')" />
      </div>
      <span>{{ t('firms.count', { count: totalCount }) }}</span>
    </div>

    <LoadingState v-if="loading" :message="t('common.loading')" />
    <ErrorState v-else-if="error" :message="error" @retry="load" />
    <EmptyState v-else-if="!firms.length" icon="bi-buildings" :title="t('firms.emptyTitle')" :message="t('firms.emptyDescription')" />

    <div v-else class="firm-grid">
      <article v-for="firm in firms" :key="firm.id" class="surface-card firm-card">
        <div class="firm-main">
          <span class="firm-icon"><i class="bi bi-buildings" aria-hidden="true"></i></span>
          <div class="firm-content">
            <span class="firm-id">#{{ firm.id }}</span>
            <div v-if="editingId === firm.id" class="edit-row">
              <label class="visually-hidden" :for="`edit-firm-${firm.id}`">{{ t('firms.editName') }}</label>
              <input :id="`edit-firm-${firm.id}`" v-model.trim="editName" class="form-control form-control-sm" :disabled="saving" />
              <button type="button" class="icon-button icon-button--success" :aria-label="t('common.save')" :disabled="saving" @click="saveEdit(firm)"><i class="bi bi-check-lg" aria-hidden="true"></i></button>
              <button type="button" class="icon-button" :aria-label="t('common.cancel')" :disabled="saving" @click="cancelEdit"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
            </div>
            <template v-else>
              <div class="name-row"><h2>{{ firm.name }}</h2><span v-if="firm.isProtected" class="protected-badge"><i class="bi bi-shield-lock" aria-hidden="true"></i> {{ t('firms.protected') }}</span></div>
              <p>{{ t('firms.servicesAssigned', { count: firm.productCount }) }}</p>
            </template>
          </div>
        </div>
        <div v-if="editingId !== firm.id" class="firm-actions">
          <button type="button" class="btn btn-sm btn-outline-secondary" :disabled="firm.isProtected" @click="startEdit(firm)"><i class="bi bi-pencil" aria-hidden="true"></i> {{ t('common.edit') }}</button>
          <button type="button" class="btn btn-sm btn-outline-danger" :disabled="firm.isProtected" @click="askDelete(firm)"><i class="bi bi-trash" aria-hidden="true"></i> {{ t('common.delete') }}</button>
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

    <ConfirmDialog v-model:open="confirmOpen" :title="t('firms.deleteTitle')" :message="t('firms.deleteMessage', { name: selectedFirm?.name ?? '' })" :confirm-label="t('common.delete')" variant="danger" :busy="deleting" @confirm="deleteFirm" />
  </section>
</template>

<script setup>
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import PageHeader from "@/components/PageHeader.vue";
import LoadingState from "@/components/LoadingState.vue";
import EmptyState from "@/components/EmptyState.vue";
import ErrorState from "@/components/ErrorState.vue";
import ConfirmDialog from "@/components/ConfirmDialog.vue";
import PaginationBar from "@/components/PaginationBar.vue";
import { usePagedList } from "@/composables/usePagedList";
import { api } from "@/services/api";
import { useToastStore } from "@/stores/toast";

const { t } = useI18n();
const toast = useToastStore();

// The list endpoint reports the assigned-service count and the protected flag, so the
// client no longer fetches the assignment list just to derive them.
const {
  items: firms,
  page,
  totalPages,
  totalCount,
  hasPrevious,
  hasNext,
  search,
  loading,
  error,
  load,
  goToPage,
} = usePagedList("/api/firms", {
  map: (rows) => rows.map((item) => ({
    id: Number(item.id),
    name: item.name ?? "",
    productCount: Number(item.productCount ?? 0),
    isProtected: Boolean(item.isProtected),
  })),
});

const newFirmName = ref("");
const createError = ref("");
const creating = ref(false);
const editingId = ref(null);
const editName = ref("");
const saving = ref(false);
const selectedFirm = ref(null);
const confirmOpen = ref(false);
const deleting = ref(false);

async function createFirm() {
  createError.value = newFirmName.value.length >= 2 ? "" : t("validation.nameLength");
  if (createError.value || creating.value) return;
  creating.value = true;
  try {
    await api.post("/api/firms", { name: newFirmName.value });
    newFirmName.value = "";
    await load();
    toast.success(t("firms.created"));
  } catch (requestError) { toast.error(requestError.message || t("errors.createFirm")); }
  finally { creating.value = false; }
}

function startEdit(firm) { if (!firm.isProtected) { editingId.value = firm.id; editName.value = firm.name; } }
function cancelEdit() { editingId.value = null; editName.value = ""; }
async function saveEdit(firm) {
  if (editName.value.length < 2 || saving.value) { toast.warning(t("validation.nameLength")); return; }
  saving.value = true;
  try {
    await api.put(`/api/firms/${firm.id}`, { name: editName.value });
    cancelEdit();
    await load();
    toast.success(t("firms.updated"));
  } catch (requestError) { toast.error(requestError.message || t("errors.updateFirm")); }
  finally { saving.value = false; }
}

function askDelete(firm) { if (!firm.isProtected) { selectedFirm.value = firm; confirmOpen.value = true; } }
async function deleteFirm() {
  if (!selectedFirm.value || deleting.value) return;
  deleting.value = true;
  try {
    await api.delete(`/api/firms/${selectedFirm.value.id}`);
    confirmOpen.value = false;
    selectedFirm.value = null;
    await load();
    toast.success(t("firms.deleted"));
  } catch (requestError) { toast.error(requestError.message || t("errors.deleteFirm")); }
  finally { deleting.value = false; }
}

onMounted(load);
</script>

<style scoped>
.create-bar { display: flex; align-items: end; gap: 1rem; margin-bottom: 1.5rem; padding: 1rem; }
.create-field { flex: 1; }
.list-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 1rem; margin-bottom: 1rem; }
.list-toolbar > span { color: var(--color-text-muted); white-space: nowrap; }
.search-field { position: relative; width: min(100%, 420px); }
.search-field i { position: absolute; top: 50%; left: 1rem; transform: translateY(-50%); color: var(--color-text-muted); }
.search-field input { padding-left: 2.6rem; }
.firm-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1rem; }
.firm-card { display: flex; align-items: center; justify-content: space-between; min-width: 0; gap: 1rem; padding: 1.25rem; }
.firm-main { display: flex; align-items: center; min-width: 0; gap: .75rem; }
.firm-icon { display: grid; place-items: center; width: 44px; height: 44px; flex: 0 0 44px; border-radius: 13px; color: var(--color-primary); background: var(--color-primary-soft); }
.firm-content { min-width: 0; }
.firm-id { color: var(--color-text-muted); font-size: .7rem; }
.name-row, .edit-row { display: flex; align-items: center; gap: .5rem; }
.name-row h2 { margin: .1rem 0 0; overflow-wrap: anywhere; font-size: 1rem; }
.firm-content p { margin: .25rem 0 0; color: var(--color-text-muted); font-size: .8rem; }
.protected-badge { display: inline-flex; align-items: center; gap: .25rem; padding: .25rem .5rem; border-radius: 999px; color: #03696b; background: var(--color-primary-soft); font-size: .7rem; font-weight: 700; white-space: nowrap; }
.firm-actions { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: .4rem; }
.icon-button { display: grid; place-items: center; width: 38px; height: 38px; border: 1px solid var(--color-border); border-radius: 10px; color: var(--color-text-muted); background: white; }
.icon-button--success { color: #15803d; }
@media (max-width: 899px) { .firm-grid { grid-template-columns: 1fr; } }
@media (max-width: 575px) { .create-bar, .list-toolbar, .firm-card { align-items: stretch; flex-direction: column; } .create-bar .btn, .search-field { width: 100%; } .firm-actions { justify-content: stretch; } .firm-actions .btn { flex: 1; } .name-row { align-items: flex-start; flex-direction: column; } }
</style>
