<template>
  <section>
    <PageHeader :title="t('accounts.title')" :description="t('accounts.description')">
      <template #actions>
        <button type="button" class="btn btn-primary" :aria-expanded="showCreate" aria-controls="create-account-panel" @click="showCreate = !showCreate">
          <i :class="showCreate ? 'bi bi-x-lg' : 'bi bi-person-plus'" aria-hidden="true"></i>
          {{ showCreate ? t('common.close') : t('accounts.add') }}
        </button>
      </template>
    </PageHeader>

    <form v-if="showCreate" id="create-account-panel" class="surface-card account-form" novalidate @submit.prevent="createAccount">
      <div class="form-heading"><h2>{{ t('accounts.createTitle') }}</h2><p>{{ t('accounts.createDescription') }}</p></div>
      <div class="form-grid">
        <div class="form-field">
          <label for="account-first-name" class="form-label">{{ t('fields.firstName') }}</label>
          <input id="account-first-name" v-model.trim="createForm.firstName" class="form-control" :class="{ 'is-invalid': createErrors.firstName }" autocomplete="given-name" :disabled="creating" />
          <div v-if="createErrors.firstName" class="invalid-feedback">{{ createErrors.firstName }}</div>
        </div>
        <div class="form-field">
          <label for="account-last-name" class="form-label">{{ t('fields.lastName') }}</label>
          <input id="account-last-name" v-model.trim="createForm.lastName" class="form-control" :class="{ 'is-invalid': createErrors.lastName }" autocomplete="family-name" :disabled="creating" />
          <div v-if="createErrors.lastName" class="invalid-feedback">{{ createErrors.lastName }}</div>
        </div>
        <div class="form-field">
          <label for="account-email" class="form-label">{{ t('fields.email') }}</label>
          <input id="account-email" v-model.trim="createForm.email" class="form-control" :class="{ 'is-invalid': createErrors.email }" type="email" inputmode="email" autocomplete="email" :disabled="creating" />
          <div v-if="createErrors.email" class="invalid-feedback">{{ createErrors.email }}</div>
        </div>
        <div class="form-field">
          <label for="account-password" class="form-label">{{ t('fields.password') }}</label>
          <input id="account-password" v-model="createForm.password" class="form-control" :class="{ 'is-invalid': createErrors.password }" type="password" autocomplete="new-password" :disabled="creating" />
          <div v-if="createErrors.password" class="invalid-feedback">{{ createErrors.password }}</div>
        </div>
        <div class="form-field">
          <label for="account-role" class="form-label">{{ t('fields.role') }}</label>
          <select id="account-role" v-model="createForm.role" class="form-select" :disabled="creating || !isSuperAdmin">
            <option value="User">{{ t('roles.user') }}</option>
            <!-- Only the platform owner creates administrators. The API enforces the
                 same rule, so hiding the option is convenience, not the control. -->
            <option v-if="isSuperAdmin" value="Admin">{{ t('roles.admin') }}</option>
          </select>
          <div v-if="!isSuperAdmin" class="form-text">{{ t('accounts.userOnlyHint') }}</div>
        </div>

        <div v-if="isSuperAdmin" class="form-field">
          <label for="account-firm" class="form-label">{{ t('fields.firm') }}</label>
          <FirmSelect
            v-model="createForm.firmId"
            input-id="account-firm"
            :exclude-protected="createForm.role !== 'Admin'"
            :disabled="creating"
            :placeholder="t('accounts.selectFirm')"
          />
          <div v-if="createErrors.firmId" class="invalid-feedback d-block">{{ createErrors.firmId }}</div>
          <!-- The firm is what makes an administrator a super administrator, so it has
               to be chosen deliberately rather than assigned behind the scenes. -->
          <div v-if="createForm.role === 'Admin'" class="form-text">{{ t('accounts.adminFirmHint') }}</div>
        </div>
        <div v-else class="form-field">
          <span class="form-label d-block">{{ t('fields.firm') }}</span>
          <p class="form-text mb-0">{{ ownFirmName }}</p>
        </div>
      </div>
      <div class="form-actions">
        <button type="button" class="btn btn-outline-secondary" :disabled="creating" @click="resetCreate">{{ t('common.cancel') }}</button>
        <button type="submit" class="btn btn-primary" :disabled="creating">
          <span v-if="creating" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
          {{ t('accounts.create') }}
        </button>
      </div>
    </form>

    <div class="surface-card toolbar">
      <div class="search-field">
        <i class="bi bi-search" aria-hidden="true"></i>
        <label class="visually-hidden" for="account-search">{{ t('common.search') }}</label>
        <input id="account-search" v-model.trim="search" class="form-control" type="search" :placeholder="t('accounts.searchPlaceholder')" />
      </div>
      <label class="filter-field"><span>{{ t('fields.role') }}</span><select v-model="roleFilter" class="form-select"><option value="all">{{ t('common.all') }}</option><option value="User">{{ t('roles.user') }}</option><option value="Admin">{{ t('roles.admin') }}</option></select></label>
      <label class="filter-field"><span>{{ t('fields.firm') }}</span>
        <FirmSelect v-model="firmFilter" input-id="firm-filter" :placeholder="t('common.all')" />
        <button v-if="firmFilter" type="button" class="btn btn-sm btn-link px-0" @click="firmFilter = ''">{{ t('common.clear') }}</button>
      </label>
    </div>

    <SkeletonList v-if="loading" :rows="4" />
    <ErrorState v-else-if="error" :message="error" @retry="load" />
    <EmptyState v-else-if="!users.length" icon="bi-people" :title="t('accounts.emptyTitle')" :message="t('accounts.emptyDescription')" />

    <template v-else>
      <div class="surface-card table-card desktop-table">
        <table class="table align-middle mb-0">
          <thead><tr><th scope="col">{{ t('accounts.person') }}</th><th scope="col">{{ t('fields.email') }}</th><th scope="col">{{ t('fields.role') }}</th><th scope="col">{{ t('fields.firm') }}</th><th scope="col" class="text-end">{{ t('common.actions') }}</th></tr></thead>
          <tbody class="tv-stagger">
            <tr v-for="user in users" :key="user.id">
              <td><div class="person-cell"><span>{{ initials(user) }}</span><div><strong>{{ user.firstName }} {{ user.lastName }}</strong><small>@{{ user.userName }}</small></div></div></td>
              <td>{{ user.email }}</td>
              <td><span class="role-badge" :class="{ admin: user.role === 'Admin' }">{{ t(user.role === 'Admin' ? 'roles.admin' : 'roles.user') }}</span></td>
              <td>{{ user.firmName || t('common.notAvailable') }}</td>
              <td class="text-end"><div class="row-actions"><button v-if="canManage(user)" type="button" class="btn btn-sm btn-outline-secondary" @click="openEdit(user)"><i class="bi bi-pencil" aria-hidden="true"></i><span class="visually-hidden">{{ t('common.edit') }}</span></button><button v-if="canManage(user)" type="button" class="btn btn-sm btn-outline-danger" @click="askDelete(user)"><i class="bi bi-trash" aria-hidden="true"></i><span class="visually-hidden">{{ t('common.delete') }}</span></button></div></td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="mobile-cards">
        <article v-for="user in users" :key="user.id" class="surface-card user-card">
          <div class="person-cell"><span>{{ initials(user) }}</span><div><strong>{{ user.firstName }} {{ user.lastName }}</strong><small>{{ user.email }}</small></div></div>
          <dl><div><dt>{{ t('fields.role') }}</dt><dd><span class="role-badge" :class="{ admin: user.role === 'Admin' }">{{ t(user.role === 'Admin' ? 'roles.admin' : 'roles.user') }}</span></dd></div><div><dt>{{ t('fields.firm') }}</dt><dd>{{ user.firmName || t('common.notAvailable') }}</dd></div></dl>
          <div class="card-actions"><button v-if="canManage(user)" type="button" class="btn btn-outline-secondary" @click="openEdit(user)"><i class="bi bi-pencil" aria-hidden="true"></i> {{ t('common.edit') }}</button><button v-if="canManage(user)" type="button" class="btn btn-outline-danger" @click="askDelete(user)"><i class="bi bi-trash" aria-hidden="true"></i> {{ t('common.delete') }}</button></div>
        </article>
      </div>
    </template>

    <AppModal
      v-model:open="editOpen"
      :title="t('accounts.editTitle')"
      :description="selectedUser?.email ?? ''"
      size="lg"
      :busy="saving"
      initial-focus="#edit-first-name"
      @close="closeEdit"
    >
      <form v-if="selectedUser" id="edit-account-form" novalidate @submit.prevent="saveAccount">
        <div class="form-grid dialog-grid">
          <div class="form-field"><label for="edit-first-name" class="form-label">{{ t('fields.firstName') }}</label><input id="edit-first-name" v-model.trim="editForm.firstName" class="form-control" :disabled="saving" /></div>
          <div class="form-field"><label for="edit-last-name" class="form-label">{{ t('fields.lastName') }}</label><input id="edit-last-name" v-model.trim="editForm.lastName" class="form-control" :disabled="saving" /></div>
          <div class="form-field dialog-span"><label for="edit-email" class="form-label">{{ t('fields.email') }}</label><input id="edit-email" v-model.trim="editForm.email" class="form-control" type="email" :disabled="saving" /></div>
          <div class="form-field"><label for="edit-role" class="form-label">{{ t('fields.role') }}</label><select id="edit-role" v-model="editForm.role" class="form-select" :disabled="saving || !isSuperAdmin"><option value="User">{{ t('roles.user') }}</option><option v-if="isSuperAdmin" value="Admin">{{ t('roles.admin') }}</option></select></div>
          <div v-if="isSuperAdmin" class="form-field"><label for="edit-firm" class="form-label">{{ t('fields.firm') }}</label><FirmSelect v-model="editForm.firmId" input-id="edit-firm" :exclude-protected="editForm.role !== 'Admin'" :disabled="saving" :placeholder="t('accounts.selectFirm')" /></div>
          <div v-else class="form-field"><span class="form-label d-block">{{ t('fields.firm') }}</span><p class="form-text mb-0">{{ ownFirmName }}</p></div>
        </div>
        <p v-if="editError" class="form-error" role="alert">{{ editError }}</p>
      </form>

      <template #footer>
        <button type="button" class="btn btn-outline-secondary" :disabled="saving" @click="editOpen = false">{{ t('common.cancel') }}</button>
        <button type="submit" form="edit-account-form" class="btn btn-primary" :disabled="saving">
          <span v-if="saving" class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
          {{ t('common.saveChanges') }}
        </button>
      </template>
    </AppModal>

    <PaginationBar
      :page="page"
      :total-pages="totalPages"
      :total-count="totalCount"
      :has-previous="hasPrevious"
      :has-next="hasNext"
      :busy="loading"
      @change="goToPage"
    />

    <ConfirmDialog v-model:open="confirmOpen" :title="t('accounts.deleteTitle')" :message="t('accounts.deleteMessage', { name: selectedForDelete ? `${selectedForDelete.firstName} ${selectedForDelete.lastName}` : '' })" :confirm-label="t('common.delete')" variant="danger" :busy="deleting" @confirm="deleteAccount" />
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import PageHeader from "@/components/PageHeader.vue";
import SkeletonList from "@/components/SkeletonList.vue";
import EmptyState from "@/components/EmptyState.vue";
import ErrorState from "@/components/ErrorState.vue";
import AppModal from "@/components/AppModal.vue";
import ConfirmDialog from "@/components/ConfirmDialog.vue";
import PaginationBar from "@/components/PaginationBar.vue";
import FirmSelect from "@/components/FirmSelect.vue";
import { usePagedList } from "@/composables/usePagedList";
import { api } from "@/services/api";
import { useAuthStore } from "@/stores/auth";
import { useToastStore } from "@/stores/toast";
import { meetsPasswordPolicy } from "@/utils/password";

const { t, locale } = useI18n();
const auth = useAuthStore();
const isSuperAdmin = computed(() => auth.isSuperAdmin);
const ownFirmName = computed(() => auth.user?.firm?.name || t("common.notAvailable"));
const toast = useToastStore();

const roleFilter = ref("all");
const firmFilter = ref("");
const showCreate = ref(false);
const creating = ref(false);
const createForm = reactive({ firstName: "", lastName: "", email: "", password: "", role: "User", firmId: "" });
const createErrors = reactive({ firstName: "", lastName: "", email: "", password: "", firmId: "" });
const editOpen = ref(false);
const selectedUser = ref(null);
const editForm = reactive({ firstName: "", lastName: "", email: "", role: "User", firmId: "" });
const editError = ref("");
const saving = ref(false);
const selectedForDelete = ref(null);
const confirmOpen = ref(false);
const deleting = ref(false);


/* The firm stays selectable for both roles now: an administrator's firm is what decides
   whether they are a super administrator, so clearing it on role change would hide the
   single most consequential field on the form. Only the protected-firm filter changes. */
watch(() => createForm.role, () => { createErrors.firmId = ""; });

function normalizeUser(item) {
  return { id: item.id ?? item.Id, userName: item.userName ?? item.name ?? "", firstName: item.firstName ?? "", lastName: item.lastName ?? "", email: item.email ?? "", role: item.role ?? "User", firmId: Number(item.firm?.id ?? item.firmId) || null, firmName: item.firm?.name ?? item.firmName ?? "" };
}
// Role and firm filters run server side so they cover every page, not just the visible one.
const listParams = computed(() => ({
  role: roleFilter.value === "all" ? "" : roleFilter.value,
  firmId: firmFilter.value || "",
}));

const {
  items: users,
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
} = usePagedList("/api/users", {
  params: listParams,
  map: (rows) => rows.map(normalizeUser),
});

function initials(user) { return `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.toLocaleUpperCase(locale.value === "tr" ? "tr-TR" : "en-US"); }
function validEmail(value) { return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value); }

/* Administrator accounts belong to the platform owner. A non-super administrator gets
   no edit or delete control on one at all, which is clearer than offering a button that
   the API answers with 403. */
function canManage(user) {
  return isSuperAdmin.value || user.role !== "Admin";
}

/* A non-super administrator has no firm field: the server pins new accounts to the
   caller's own firm regardless, and sending it here just keeps the request honest. */
function effectiveFirmId() {
  return isSuperAdmin.value ? Number(createForm.firmId) : Number(auth.user?.firm?.id) || 0;
}


function validateCreate() {
  createErrors.firstName = createForm.firstName.length >= 2 ? "" : t("validation.required");
  createErrors.lastName = createForm.lastName.length >= 2 ? "" : t("validation.required");
  createErrors.email = validEmail(createForm.email) ? "" : t("validation.email");
  createErrors.password = meetsPasswordPolicy(createForm.password) ? "" : t("validation.minLength", { min: 12 });
  // Required for every role now. The API has always demanded a real firm id — sending
  // null for administrators is why creating one from this screen used to fail outright.
  createErrors.firmId = isSuperAdmin.value && !createForm.firmId ? t("validation.selectFirm") : "";
  return Object.values(createErrors).every((value) => !value);
}
function resetCreate() { Object.assign(createForm, { firstName: "", lastName: "", email: "", password: "", role: "User", firmId: "" }); Object.keys(createErrors).forEach((key) => { createErrors[key] = ""; }); showCreate.value = false; }
async function createAccount() {
  if (!validateCreate() || creating.value) return;
  creating.value = true;
  try {
    await api.post("/api/users", { firstName: createForm.firstName, lastName: createForm.lastName, email: createForm.email, password: createForm.password, role: createForm.role, firmId: effectiveFirmId() });
    resetCreate(); await load(); toast.success(t("accounts.created"));
  } catch (requestError) { toast.error(requestError.message || t("errors.createAccount")); }
  finally { creating.value = false; }
}

function openEdit(user) {
  selectedUser.value = user; Object.assign(editForm, { firstName: user.firstName, lastName: user.lastName, email: user.email, role: user.role, firmId: user.firmId ?? "" }); editError.value = ""; editOpen.value = true;
}
function closeEdit() { selectedUser.value = null; editError.value = ""; }
async function saveAccount() {
  if (!selectedUser.value || saving.value) return;
  if (!editForm.firstName || !editForm.lastName || !validEmail(editForm.email) || (editForm.role === "User" && !editForm.firmId)) { editError.value = t("validation.checkFields"); return; }
  saving.value = true;
  try {
    await api.put(`/api/users/${selectedUser.value.id}`, { id: selectedUser.value.id, firstName: editForm.firstName, lastName: editForm.lastName, email: editForm.email, role: editForm.role, firmId: editForm.role === "User" ? Number(editForm.firmId) : null });
    editOpen.value = false; await load(); toast.success(t("accounts.updated"));
  } catch (requestError) { toast.error(requestError.message || t("errors.updateAccount")); }
  finally { saving.value = false; }
}

function askDelete(user) { selectedForDelete.value = user; confirmOpen.value = true; }
async function deleteAccount() {
  if (!selectedForDelete.value || deleting.value) return;
  deleting.value = true;
  try { await api.delete(`/api/users/${selectedForDelete.value.id}`); confirmOpen.value = false; selectedForDelete.value = null; await load(); toast.success(t("accounts.deleted")); }
  catch (requestError) { toast.error(requestError.message || t("errors.deleteAccount")); }
  finally { deleting.value = false; }
}

onMounted(load);
</script>

<style scoped>
.account-form { margin-bottom: 1rem; padding: 1.5rem; }
.form-heading h2 { margin: 0 0 .25rem; font-size: 1.15rem; }
.form-heading p { margin: 0 0 1.25rem; color: var(--tv-text-muted); }
.form-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 1rem; }
.form-actions { display: flex; justify-content: flex-end; gap: .75rem; margin-top: 1.25rem; }
/* align-items: start keeps each control at its natural height. Stretching made the
   search wrapper as tall as the row, which dragged its absolutely positioned
   magnifier icon down to the vertical centre of that tall box. */
.toolbar { display: grid; grid-template-columns: minmax(240px, 1fr) 180px 220px; align-items: start; gap: .75rem; margin-bottom: 1rem; padding: .75rem; }
.search-field { position: relative; }
.search-field i { position: absolute; top: 50%; left: 1rem; transform: translateY(-50%); color: var(--tv-text-muted); }
.search-field input { padding-left: 2.6rem; }
.filter-field { display: flex; align-items: center; gap: .5rem; margin: 0; }
.filter-field span { color: var(--tv-text-muted); font-size: .75rem; font-weight: 700; white-space: nowrap; }
.table-card { overflow: hidden; }
.table th { padding: 1rem; color: var(--tv-text-muted); background: var(--tv-surface-sunken); font-size: .75rem; text-transform: uppercase; }
.table td { padding: .9rem 1rem; }
.person-cell { display: flex; align-items: center; gap: .75rem; min-width: 0; }
.person-cell > span { display: grid; place-items: center; width: 40px; height: 40px; flex: 0 0 40px; border-radius: 50%; color: var(--tv-accent); background: var(--tv-accent-soft); font-size: .75rem; font-weight: 800; }
.person-cell strong, .person-cell small { display: block; overflow-wrap: anywhere; }
.person-cell small { color: var(--tv-text-muted); font-size: .75rem; }
.role-badge { display: inline-flex; padding: .3rem .6rem; border-radius: 999px; color: var(--tv-text-muted); background: var(--tv-surface-muted); font-size: .75rem; font-weight: 700; }
.role-badge.admin { color: var(--tv-accent-on-soft); background: var(--tv-accent-soft); }
.row-actions { display: flex; justify-content: flex-end; gap: .4rem; }
.mobile-cards { display: none; gap: 1rem; }
.user-card { padding: 1rem; }
.user-card dl { display: grid; grid-template-columns: 1fr 1fr; gap: .75rem; margin: 1rem 0; }
.user-card dt { color: var(--tv-text-muted); font-size: .7rem; }
.user-card dd { margin: .2rem 0 0; overflow-wrap: anywhere; }
.card-actions { display: flex; gap: .5rem; }
.card-actions .btn { flex: 1; }
.app-dialog { width: min(calc(100% - 2rem), 700px); max-height: calc(100dvh - 2rem); padding: 0; border: 0; border-radius: 18px; background: transparent; }
.app-dialog::backdrop { background: var(--tv-overlay); }
.dialog-card { padding: 1.5rem; border-radius: 18px; background: var(--tv-surface-2); }
.dialog-header { display: flex; justify-content: space-between; gap: 1rem; margin-bottom: 1.25rem; }
.dialog-header h2 { margin: .2rem 0 0; font-size: 1.25rem; }
.eyebrow { color: var(--tv-text-muted); font-size: .75rem; }
.icon-button { display: grid; place-items: center; width: 44px; height: 44px; border: 0; border-radius: 12px; color: var(--tv-text-muted); background: var(--tv-bg-page); }
.dialog-grid { grid-template-columns: 1fr 1fr; }
.dialog-span { grid-column: 1 / -1; }
.form-error { color: var(--bs-danger); }
@media (max-width: 1023px) { .form-grid { grid-template-columns: 1fr 1fr; } .toolbar { grid-template-columns: 1fr 1fr; } .search-field { grid-column: 1 / -1; } }
@media (max-width: 767px) { .desktop-table { display: none; } .mobile-cards { display: grid; } }
@media (max-width: 575px) { .form-grid, .dialog-grid, .toolbar { grid-template-columns: 1fr; } .search-field, .dialog-span { grid-column: auto; } .form-actions { flex-direction: column-reverse; } .form-actions .btn { width: 100%; } }
</style>
