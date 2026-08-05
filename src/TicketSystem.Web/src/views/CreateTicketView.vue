<template>
  <section>
    <PageHeader :title="t('ticket.createTitle')" :description="t('ticket.createDescription')" />

    <div class="form-layout">
      <form class="surface-card ticket-form" novalidate @submit.prevent="submit">
        <fieldset class="request-type">
          <legend>{{ t('ticket.productType') }}</legend>
          <label class="type-option" :class="{ active: !form.newProduct }">
            <input v-model="form.newProduct" type="radio" :value="false" :disabled="busy" />
            <span class="type-icon"><i class="bi bi-box-seam" aria-hidden="true"></i></span>
            <span>
              <strong>{{ t('ticket.existingProduct') }}</strong>
              <small>{{ t('ticket.existingProductHint') }}</small>
            </span>
          </label>
          <label class="type-option" :class="{ active: form.newProduct }">
            <input v-model="form.newProduct" type="radio" :value="true" :disabled="busy" />
            <span class="type-icon"><i class="bi bi-stars" aria-hidden="true"></i></span>
            <span>
              <strong>{{ t('ticket.newProduct') }}</strong>
              <small>{{ t('ticket.newProductHint') }}</small>
            </span>
          </label>
        </fieldset>

        <div v-if="!form.newProduct" class="form-field">
          <label for="ticket-product" class="form-label">{{ t('fields.product') }}</label>
          <LoadingState v-if="productsLoading" compact :message="t('common.loading')" />
          <ErrorState v-else-if="productsError" compact :message="productsError" @retry="loadProducts" />
          <template v-else>
            <select
              id="ticket-product"
              v-model="form.productId"
              class="form-select"
              :class="{ 'is-invalid': errors.productId }"
              :disabled="busy || !products.length"
            >
              <option value="" disabled>{{ t('ticket.selectProduct') }}</option>
              <option v-for="product in products" :key="product.id" :value="product.id">{{ product.name }}</option>
            </select>
            <div v-if="errors.productId" class="invalid-feedback">{{ errors.productId }}</div>
            <p v-if="!products.length" class="form-text">{{ t('ticket.noProducts') }}</p>
          </template>
        </div>

        <div class="form-field">
          <div class="label-row">
            <label for="ticket-description" class="form-label">{{ t('fields.description') }}</label>
            <span :class="['character-count', { invalid: descriptionLength > 280 }]">{{ descriptionLength }}/280</span>
          </div>
          <textarea
            id="ticket-description"
            v-model="form.description"
            class="form-control"
            :class="{ 'is-invalid': errors.description }"
            rows="7"
            maxlength="280"
            :placeholder="form.newProduct ? t('ticket.newPlaceholder') : t('ticket.descriptionPlaceholder')"
            :aria-describedby="errors.description ? 'ticket-description-error' : 'ticket-description-help'"
            :disabled="busy"
          ></textarea>
          <div v-if="errors.description" id="ticket-description-error" class="invalid-feedback">{{ errors.description }}</div>
          <div v-else id="ticket-description-help" class="form-text">{{ t('ticket.descriptionHelp') }}</div>
        </div>

        <div class="form-actions">
          <RouterLink class="btn btn-outline-secondary" to="/tickets">{{ t('ticket.viewMine') }}</RouterLink>
          <button class="btn btn-primary" type="submit" :disabled="busy || productsLoading">
            <span v-if="busy" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
            <i v-else class="bi bi-send" aria-hidden="true"></i>
            {{ busy ? t('common.sending') : t('ticket.submit') }}
          </button>
        </div>
      </form>

      <aside class="surface-card info-card">
        <div class="info-icon"><i class="bi bi-lightbulb" aria-hidden="true"></i></div>
        <h2>{{ t('ticket.tipsTitle') }}</h2>
        <ul>
          <li>{{ t('ticket.tipOne') }}</li>
          <li>{{ t('ticket.tipTwo') }}</li>
          <li>{{ t('ticket.tipThree') }}</li>
        </ul>
        <div v-if="auth.user?.firm" class="firm-note">
          <span>{{ t('fields.firm') }}</span>
          <strong>{{ auth.user.firm.name }}</strong>
        </div>
      </aside>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import PageHeader from "@/components/PageHeader.vue";
import LoadingState from "@/components/LoadingState.vue";
import ErrorState from "@/components/ErrorState.vue";
import { api } from "@/services/api";
import { useAuthStore } from "@/stores/auth";
import { useToastStore } from "@/stores/toast";

const { t } = useI18n();
const auth = useAuthStore();
const toast = useToastStore();
const products = ref([]);
const productsLoading = ref(true);
const productsError = ref("");
const busy = ref(false);
const form = reactive({ description: "", newProduct: false, productId: "" });
const errors = reactive({ description: "", productId: "" });
const descriptionLength = computed(() => form.description.length);

watch(() => form.newProduct, (newProduct) => {
  errors.productId = "";
  if (newProduct) form.productId = "";
});

async function loadProducts() {
  productsLoading.value = true;
  productsError.value = "";
  try {
    const result = await api.get("/api/products/mine");
    products.value = (result ?? []).map((item) => ({
      id: Number(item.id ?? item.productId),
      name: item.name ?? item.productName,
    })).filter((item) => item.id && item.name);
  } catch (error) {
    productsError.value = error.message || t("errors.loadProducts");
  } finally {
    productsLoading.value = false;
  }
}

function validate() {
  const textLength = form.description.trim().length;
  errors.description = textLength < 30 || textLength > 280 ? t("validation.descriptionLength") : "";
  errors.productId = !form.newProduct && !form.productId ? t("validation.selectProduct") : "";
  return !errors.description && !errors.productId;
}

async function submit() {
  if (!validate() || busy.value) return;
  busy.value = true;
  try {
    await api.post("/api/tickets", {
      description: form.description.trim(),
      newProduct: form.newProduct,
      productId: form.newProduct ? null : Number(form.productId),
    });
    form.description = "";
    form.productId = "";
    form.newProduct = false;
    toast.success(t("ticket.created"));
  } catch (error) {
    toast.error(error.message || t("errors.createTicket"));
  } finally {
    busy.value = false;
  }
}

onMounted(loadProducts);
</script>

<style scoped>
.form-layout { display: grid; grid-template-columns: minmax(0, 2fr) minmax(260px, 1fr); align-items: start; gap: 1.5rem; }
.ticket-form, .info-card { padding: clamp(1.25rem, 3vw, 2rem); }
.request-type { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .75rem; margin: 0 0 1.5rem; padding: 0; border: 0; }
.request-type legend { grid-column: 1 / -1; margin-bottom: .5rem; font-size: .875rem; font-weight: 700; }
.type-option { position: relative; display: flex; align-items: center; gap: .75rem; min-height: 92px; padding: 1rem; border: 1px solid var(--tv-border); border-radius: 14px; cursor: pointer; }
.type-option.active { border-color: var(--tv-accent); background: var(--tv-accent-soft); box-shadow: 0 0 0 1px var(--tv-accent); }
.type-option input { position: absolute; width: 1px; height: 1px; opacity: 0; }
.type-option:focus-within { outline: 3px solid var(--tv-focus); outline-offset: 2px; }
.type-option strong, .type-option small { display: block; }
.type-option small { margin-top: .2rem; color: var(--tv-text-muted); }
.type-icon { display: grid; place-items: center; width: 42px; height: 42px; flex: 0 0 42px; border-radius: 12px; color: var(--tv-accent-on-soft); background: var(--tv-surface-1); font-size: 1.1rem; }
.form-field { margin-bottom: 1.5rem; }
.label-row { display: flex; justify-content: space-between; gap: 1rem; }
.character-count { color: var(--tv-text-muted); font-size: .8rem; }
.character-count.invalid { color: var(--bs-danger); }
.form-actions { display: flex; justify-content: flex-end; gap: .75rem; padding-top: .5rem; }
.info-icon { display: grid; place-items: center; width: 48px; height: 48px; margin-bottom: 1rem; border-radius: 14px; color: var(--tv-accent); background: var(--tv-accent-soft); font-size: 1.25rem; }
.info-card h2 { font-size: 1.1rem; }
.info-card ul { display: grid; gap: .75rem; padding-left: 1.25rem; color: var(--tv-text-muted); }
.firm-note { display: flex; flex-direction: column; gap: .2rem; margin-top: 1.5rem; padding-top: 1.25rem; border-top: 1px solid var(--tv-border); }
.firm-note span { color: var(--tv-text-muted); font-size: .8rem; }
@media (max-width: 767px) { .form-layout { grid-template-columns: 1fr; } .request-type { grid-template-columns: 1fr; } .form-actions { flex-direction: column-reverse; } .form-actions .btn { width: 100%; } }
</style>
