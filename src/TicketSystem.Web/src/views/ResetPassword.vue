<template>
  <div class="auth-page">
    <section class="auth-card" aria-labelledby="reset-title">
      <RouterLink class="brand-link" to="/sign-in">
        <img :src="logo" alt="Turkuvaz" />
        <span>{{ t('app.name') }}</span>
      </RouterLink>
      <h1 id="reset-title">{{ t('auth.reset.title') }}</h1>
      <p class="intro">{{ t('auth.reset.description') }}</p>

      <ErrorState v-if="!hasResetParameters" :message="t('auth.reset.invalidLink')" />

      <form v-else novalidate @submit.prevent="submit">
        <div class="form-field">
          <label for="new-password" class="form-label">{{ t('auth.reset.newPassword') }}</label>
          <div class="password-field">
            <input
              id="new-password"
              v-model="form.newPassword"
              class="form-control"
              :class="{ 'is-invalid': errors.newPassword }"
              :type="showPassword ? 'text' : 'password'"
              autocomplete="new-password"
              :disabled="busy || complete"
            />
            <button type="button" class="password-toggle" :aria-label="t('auth.signIn.showPassword')" @click="showPassword = !showPassword">
              <i :class="showPassword ? 'bi bi-eye-slash' : 'bi bi-eye'" aria-hidden="true"></i>
            </button>
          </div>
          <div v-if="errors.newPassword" class="invalid-feedback d-block">{{ errors.newPassword }}</div>
          <div class="form-text">{{ t('validation.minLength', { min: 12 }) }}</div>
        </div>

        <div class="form-field">
          <label for="confirm-password" class="form-label">{{ t('auth.reset.confirmPassword') }}</label>
          <input
            id="confirm-password"
            v-model="form.confirmPassword"
            class="form-control"
            :class="{ 'is-invalid': errors.confirmPassword }"
            :type="showPassword ? 'text' : 'password'"
            autocomplete="new-password"
            :disabled="busy || complete"
          />
          <div v-if="errors.confirmPassword" class="invalid-feedback">{{ errors.confirmPassword }}</div>
        </div>

        <div v-if="complete" class="notice" role="status">
          <i class="bi bi-check-circle" aria-hidden="true"></i>
          <span>{{ t('auth.reset.success') }}</span>
        </div>

        <button v-if="!complete" class="btn btn-primary btn-lg w-100" type="submit" :disabled="busy">
          <span v-if="busy" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
          {{ busy ? t('common.saving') : t('auth.reset.submit') }}
        </button>
        <RouterLink v-else class="btn btn-primary btn-lg w-100" to="/sign-in">{{ t('auth.signIn.submit') }}</RouterLink>
      </form>
    </section>
  </div>
</template>

<script setup>
import { computed, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import logo from "@/assets/Turkuvaz Logo.png";
import ErrorState from "@/components/ErrorState.vue";
import { api } from "@/services/api";
import { useToastStore } from "@/stores/toast";
import { meetsPasswordPolicy } from "@/utils/password";

const { t } = useI18n();
const route = useRoute();
const toast = useToastStore();
const form = reactive({ newPassword: "", confirmPassword: "" });
const errors = reactive({ newPassword: "", confirmPassword: "" });
const showPassword = ref(false);
const busy = ref(false);
const complete = ref(false);

const email = computed(() => typeof route.query.email === "string" ? route.query.email : "");
const token = computed(() => typeof route.query.token === "string" ? route.query.token : "");
const hasResetParameters = computed(() => Boolean(email.value && token.value));

function validate() {
  errors.newPassword = meetsPasswordPolicy(form.newPassword) ? "" : t("validation.minLength", { min: 12 });
  errors.confirmPassword = form.confirmPassword === form.newPassword ? "" : t("validation.passwordMismatch");
  return !errors.newPassword && !errors.confirmPassword;
}

async function submit() {
  if (!validate() || busy.value) return;
  busy.value = true;
  try {
    await api.post("/api/auth/reset-password", {
      email: email.value,
      token: token.value,
      newPassword: form.newPassword,
    });
    complete.value = true;
  } catch (error) {
    toast.error(error.message || t("errors.resetPassword"));
  } finally {
    busy.value = false;
  }
}
</script>

<style scoped>
.auth-page { min-height: 100dvh; display: grid; place-items: center; padding: 1.5rem; background: var(--color-page); }
.auth-card { width: min(100%, 460px); padding: clamp(1.5rem, 5vw, 2.5rem); border: 1px solid var(--color-border); border-radius: 20px; background: white; box-shadow: 0 24px 60px rgba(15, 42, 51, .11); }
.brand-link { display: flex; align-items: center; gap: .75rem; margin-bottom: 2rem; color: var(--color-text); font-weight: 800; text-decoration: none; }
.brand-link img { width: 46px; height: 46px; object-fit: contain; }
h1 { margin-bottom: .5rem; }
.intro { margin-bottom: 1.75rem; color: var(--color-text-muted); }
.form-field { margin-bottom: 1.25rem; }
.password-field { position: relative; }
.password-field .form-control { padding-right: 3.25rem; }
.password-toggle { position: absolute; inset: 0 .25rem 0 auto; width: 44px; border: 0; border-radius: 10px; color: var(--color-text-muted); background: transparent; }
.notice { display: flex; gap: .75rem; margin-bottom: 1rem; padding: 1rem; border-radius: 12px; color: #166534; background: #dcfce7; }
</style>
