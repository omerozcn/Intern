<template>
  <section>
    <PageHeader :title="t('services.title')" :description="t('services.description')" />

    <form class="surface-card create-bar" novalidate @submit.prevent="createProduct">
      <div class="create-field">
        <label for="new-product" class="form-label">{{ t('services.newName') }}</label>
        <input id="new-product" v-model.trim="newProductName" class="form-control" :class="{ 'is-invalid': createError }" :placeholder="t('services.newPlaceholder')" :disabled="creating" />
        <div v-if="createError" class="invalid-feedback">{{ createError }}</div>
      </div>
      <button class="btn btn-primary" type="submit" :disabled="creating">
        <span v-if="creating" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
        <i v-else class="bi bi-plus-lg" aria-hidden="true"></i>
        {{ t('services.add') }}
      </button>
    </form>

    <div class="list-toolbar">
      <div class="search-field">
        <i class="bi bi-search" aria-hidden="true"></i>
        <label class="visually-hidden" for="service-search">{{ t('common.search') }}</label>
        <input id="service-search" v-model.trim="query" class="form-control" type="search" :placeholder="t('services.searchPlaceholder')" />
      </div>
      <span>{{ t('services.count', { count: filteredProducts.length }) }}</span>
    </div>

    <LoadingState v-if="loading" :message="t('common.loading')" />
    <ErrorState v-else-if="error" :message="error" @retry="loadAll" />
    <EmptyState v-else-if="!filteredProducts.length" icon="bi-box-seam" :title="t('services.emptyTitle')" :message="t('services.emptyDescription')" />

    <div v-else class="service-grid">
      <article v-for="product in filteredProducts" :key="product.id" class="surface-card service-card">
        <div class="service-head">
          <div class="service-title">
            <span class="service-icon"><i class="bi bi-box-seam" aria-hidden="true"></i></span>
            <div>
              <span class="service-id">#{{ product.id }}</span>
              <h2 v-if="editingId !== product.id">{{ product.name }}</h2>
              <label v-else class="visually-hidden" :for="`edit-product-${product.id}`">{{ t('services.editName') }}</label>
              <input v-if="editingId === product.id" :id="`edit-product-${product.id}`" v-model.trim="editName" class="form-control form-control-sm" :disabled="savingId === product.id" />
            </div>
          </div>
          <div class="icon-actions">
            <button v-if="editingId !== product.id" type="button" class="icon-button" :aria-label="t('services.editAria', { name: product.name })" @click="startEdit(product)"><i class="bi bi-pencil" aria-hidden="true"></i></button>
            <button v-else type="button" class="icon-button icon-button--success" :aria-label="t('common.save')" :disabled="savingId === product.id" @click="saveEdit(product)"><i class="bi bi-check-lg" aria-hidden="true"></i></button>
            <button v-if="editingId === product.id" type="button" class="icon-button" :aria-label="t('common.cancel')" :disabled="savingId === product.id" @click="cancelEdit"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
            <button v-else type="button" class="icon-button icon-button--danger" :aria-label="t('services.deleteAria', { name: product.name })" @click="askDeleteProduct(product)"><i class="bi bi-trash" aria-hidden="true"></i></button>
          </div>
        </div>

        <div class="assignments">
          <div class="section-label"><span>{{ t('services.assignedFirms') }}</span><strong>{{ product.firms.length }}</strong></div>
          <div v-if="product.firms.length" class="firm-tags">
            <span v-for="firm in product.firms" :key="firm.relationId" class="firm-tag">
              {{ firm.name }}
              <button type="button" :aria-label="t('services.removeFirmAria', { firm: firm.name, product: product.name })" @click="askDeleteRelation(product, firm)">
                <i class="bi bi-x" aria-hidden="true"></i>
              </button>
            </span>
          </div>
          <p v-else class="empty-inline">{{ t('services.noFirms') }}</p>

          <form class="assign-form" @submit.prevent="assignFirm(product)">
            <label class="visually-hidden" :for="`firm-${product.id}`">{{ t('services.selectFirm') }}</label>
            <select :id="`firm-${product.id}`" v-model="assignmentSelections[product.id]" class="form-select form-select-sm" :disabled="assigningId === product.id">
              <option value="">{{ t('services.selectFirm') }}</option>
              <option v-for="firm in availableFirms(product)" :key="firm.id" :value="firm.id">{{ firm.name }}</option>
            </select>
            <button type="submit" class="btn btn-sm btn-outline-primary" :disabled="assigningId === product.id || !assignmentSelections[product.id]">
              <span v-if="assigningId === product.id" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
              {{ t('services.assign') }}
            </button>
          </form>
        </div>
      </article>
    </div>

    <ConfirmDialog
      v-model:open="confirmOpen"
      :title="confirmTitle"
      :message="confirmMessage"
      :confirm-label="t('common.remove')"
      variant="danger"
      :busy="removing"
      @confirm="confirmRemoval"
    />
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import PageHeader from "@/components/PageHeader.vue";
import LoadingState from "@/components/LoadingState.vue";
import EmptyState from "@/components/EmptyState.vue";
import ErrorState from "@/components/ErrorState.vue";
import ConfirmDialog from "@/components/ConfirmDialog.vue";
import { api } from "@/services/api";
import { useToastStore } from "@/stores/toast";

const { t, locale } = useI18n();
const toast = useToastStore();
const products = ref([]);
const firms = ref([]);
const relations = ref([]);
const loading = ref(true);
const error = ref("");
const query = ref("");
const newProductName = ref("");
const createError = ref("");
const creating = ref(false);
const editingId = ref(null);
const editName = ref("");
const savingId = ref(null);
const assigningId = ref(null);
const assignmentSelections = reactive({});
const confirmOpen = ref(false);
const confirmTarget = ref(null);
const removing = ref(false);

const groupedProducts = computed(() => products.value.map((product) => ({
  ...product,
  firms: relations.value
    .filter((relation) => Number(relation.productId) === product.id)
    .map((relation) => ({
      relationId: Number(relation.id),
      id: Number(relation.firmId),
      name: relation.firmName ?? firms.value.find((firm) => firm.id === Number(relation.firmId))?.name ?? t("common.notAvailable"),
    })),
})));

const filteredProducts = computed(() => {
  const needle = query.value.toLocaleLowerCase(locale.value === "tr" ? "tr-TR" : "en-US");
  return groupedProducts.value.filter((product) => !needle || `${product.name} ${product.firms.map((firm) => firm.name).join(" ")}`.toLocaleLowerCase(locale.value === "tr" ? "tr-TR" : "en-US").includes(needle));
});

const confirmTitle = computed(() => confirmTarget.value?.type === "product" ? t("services.deleteTitle") : t("services.removeFirmTitle"));
const confirmMessage = computed(() => confirmTarget.value?.type === "product"
  ? t("services.deleteMessage", { name: confirmTarget.value?.product.name ?? "" })
  : t("services.removeFirmMessage", { firm: confirmTarget.value?.firm.name ?? "", product: confirmTarget.value?.product.name ?? "" }));

async function loadAll() {
  loading.value = true;
  error.value = "";
  try {
    const [productRows, firmRows, relationRows] = await Promise.all([
      api.get("/api/Product/listProduct"),
      api.get("/api/Firm/listFirm"),
      api.get("/api/Firmproduct/listfirmProduct"),
    ]);
    products.value = (productRows ?? []).map((item) => ({ id: Number(item.id), name: item.name }));
    firms.value = (firmRows ?? []).map((item) => ({ id: Number(item.id), name: item.name ?? item.firmName }));
    relations.value = relationRows ?? [];
  } catch (requestError) {
    error.value = requestError.message || t("errors.loadServices");
  } finally {
    loading.value = false;
  }
}

function availableFirms(product) { return firms.value.filter((firm) => !product.firms.some((assigned) => assigned.id === firm.id)); }

async function createProduct() {
  createError.value = newProductName.value.length >= 2 ? "" : t("validation.nameLength");
  if (createError.value || creating.value) return;
  creating.value = true;
  try {
    await api.post("/api/Product/createProduct", { name: newProductName.value });
    newProductName.value = "";
    await loadAll();
    toast.success(t("services.created"));
  } catch (requestError) {
    toast.error(requestError.message || t("errors.createService"));
  } finally { creating.value = false; }
}

function startEdit(product) { editingId.value = product.id; editName.value = product.name; }
function cancelEdit() { editingId.value = null; editName.value = ""; }
async function saveEdit(product) {
  if (editName.value.length < 2 || savingId.value) { toast.warning(t("validation.nameLength")); return; }
  savingId.value = product.id;
  try {
    await api.put(`/api/Product/updateProduct/${product.id}`, { id: product.id, name: editName.value });
    cancelEdit();
    await loadAll();
    toast.success(t("services.updated"));
  } catch (requestError) { toast.error(requestError.message || t("errors.updateService")); }
  finally { savingId.value = null; }
}

async function assignFirm(product) {
  const firmId = Number(assignmentSelections[product.id]);
  if (!firmId || assigningId.value) return;
  assigningId.value = product.id;
  try {
    await api.post("/api/Firmproduct/createfirmProduct", { firmId, productId: product.id });
    assignmentSelections[product.id] = "";
    await loadAll();
    toast.success(t("services.assignmentUpdated"));
  } catch (requestError) { toast.error(requestError.message || t("errors.assignService")); }
  finally { assigningId.value = null; }
}

function askDeleteProduct(product) { confirmTarget.value = { type: "product", product }; confirmOpen.value = true; }
function askDeleteRelation(product, firm) { confirmTarget.value = { type: "relation", product, firm }; confirmOpen.value = true; }
async function confirmRemoval() {
  if (!confirmTarget.value || removing.value) return;
  removing.value = true;
  try {
    if (confirmTarget.value.type === "product") await api.delete(`/api/Product/deleteProduct/${confirmTarget.value.product.id}`);
    else await api.delete(`/api/Firmproduct/deletefirmProduct/${confirmTarget.value.firm.relationId}`);
    confirmOpen.value = false;
    confirmTarget.value = null;
    await loadAll();
    toast.success(t("services.removeSuccess"));
  } catch (requestError) { toast.error(requestError.message || t("errors.removeService")); }
  finally { removing.value = false; }
}

onMounted(loadAll);
</script>

<style scoped>
.create-bar { display: flex; align-items: end; gap: 1rem; margin-bottom: 1.5rem; padding: 1rem; }
.create-field { flex: 1; }
.list-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 1rem; margin-bottom: 1rem; }
.list-toolbar > span { color: var(--color-text-muted); white-space: nowrap; }
.search-field { position: relative; width: min(100%, 420px); }
.search-field i { position: absolute; top: 50%; left: 1rem; transform: translateY(-50%); color: var(--color-text-muted); }
.search-field input { padding-left: 2.6rem; }
.service-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1rem; }
.service-card { min-width: 0; padding: 1.25rem; }
.service-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; }
.service-title { display: flex; align-items: center; min-width: 0; gap: .75rem; }
.service-icon { display: grid; place-items: center; width: 42px; height: 42px; flex: 0 0 42px; border-radius: 12px; color: var(--color-primary); background: var(--color-primary-soft); }
.service-id { color: var(--color-text-muted); font-size: .7rem; }
.service-title h2 { margin: .1rem 0 0; overflow-wrap: anywhere; font-size: 1.05rem; }
.icon-actions { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: .35rem; }
.icon-button { display: grid; place-items: center; width: 40px; height: 40px; border: 1px solid var(--color-border); border-radius: 10px; color: var(--color-text-muted); background: white; }
.icon-button--danger { color: #b42318; }
.icon-button--success { color: #15803d; }
.assignments { margin-top: 1.25rem; padding-top: 1rem; border-top: 1px solid var(--color-border); }
.section-label { display: flex; justify-content: space-between; color: var(--color-text-muted); font-size: .8rem; font-weight: 700; }
.firm-tags { display: flex; flex-wrap: wrap; gap: .4rem; margin-top: .75rem; }
.firm-tag { display: inline-flex; align-items: center; gap: .25rem; padding: .35rem .35rem .35rem .65rem; border-radius: 999px; color: #03696b; background: var(--color-primary-soft); font-size: .8rem; font-weight: 700; }
.firm-tag button { display: grid; place-items: center; width: 28px; height: 28px; border: 0; border-radius: 50%; color: inherit; background: transparent; }
.empty-inline { margin: .75rem 0; color: var(--color-text-muted); font-size: .85rem; }
.assign-form { display: flex; gap: .5rem; margin-top: .9rem; }
.assign-form select { min-width: 0; }
@media (max-width: 899px) { .service-grid { grid-template-columns: 1fr; } }
@media (max-width: 575px) { .create-bar, .list-toolbar { align-items: stretch; flex-direction: column; } .create-bar .btn, .search-field { width: 100%; } .service-head { align-items: stretch; flex-direction: column; } .icon-actions { justify-content: flex-start; } .assign-form { flex-direction: column; } }
</style>
