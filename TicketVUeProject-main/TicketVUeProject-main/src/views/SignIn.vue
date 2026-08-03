<template>
  <div class="auth-page">
    <section class="auth-card" aria-labelledby="login-title">
      <div class="auth-brand">
        <img :src="logo" alt="Turkuvaz" />
        <span>{{ t('app.name') }}</span>
      </div>

      <template v-if="!forgotMode">
        <div class="auth-heading">
          <p class="eyebrow">{{ t('app.tagline') }}</p>
          <h1 id="login-title">{{ t('auth.signIn.title') }}</h1>
          <p>{{ t('auth.signIn.description') }}</p>
        </div>

        <form novalidate @submit.prevent="submitLogin">
          <div class="form-field">
            <label for="login-email" class="form-label">{{ t('auth.signIn.email') }}</label>
            <input
              id="login-email"
              v-model.trim="credentials.email"
              class="form-control"
              :class="{ 'is-invalid': errors.email }"
              type="email"
              autocomplete="username"
              inputmode="email"
              :aria-describedby="errors.email ? 'login-email-error' : undefined"
              :disabled="busy"
            />
            <div v-if="errors.email" id="login-email-error" class="invalid-feedback">{{ errors.email }}</div>
          </div>

          <div class="form-field">
            <label for="login-password" class="form-label">{{ t('auth.signIn.password') }}</label>
            <div class="password-field">
              <input
                id="login-password"
                v-model="credentials.password"
                class="form-control"
                :class="{ 'is-invalid': errors.password }"
                :type="showPassword ? 'text' : 'password'"
                autocomplete="current-password"
                :aria-describedby="errors.password ? 'login-password-error' : undefined"
                :disabled="busy"
              />
              <button
                type="button"
                class="password-toggle"
                :aria-label="showPassword ? t('auth.signIn.hidePassword') : t('auth.signIn.showPassword')"
                :aria-pressed="showPassword"
                @click="showPassword = !showPassword"
              >
                <i :class="showPassword ? 'bi bi-eye-slash' : 'bi bi-eye'" aria-hidden="true"></i>
              </button>
            </div>
            <div v-if="errors.password" id="login-password-error" class="invalid-feedback d-block">{{ errors.password }}</div>
          </div>

          <div class="auth-row">
            <button type="button" class="link-button" @click="openForgot">{{ t('auth.signIn.forgotPassword') }}</button>
          </div>

          <button type="submit" class="btn btn-primary btn-lg w-100" :disabled="busy">
            <span v-if="busy" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
            {{ busy ? t('common.loading') : t('auth.signIn.submit') }}
          </button>
        </form>
      </template>

      <template v-else>
        <div class="auth-heading">
          <p class="eyebrow">{{ t('app.tagline') }}</p>
          <h1 id="login-title">{{ t('auth.forgot.title') }}</h1>
          <p>{{ forgotSent ? t('auth.forgot.accepted') : t('auth.forgot.description') }}</p>
        </div>

        <div v-if="forgotSent" class="notice notice--success" role="status">
          <i class="bi bi-envelope-check" aria-hidden="true"></i>
          <span>{{ t('auth.forgot.accepted') }}</span>
        </div>

        <form v-else novalidate @submit.prevent="submitForgotPassword">
          <div class="form-field">
            <label for="forgot-email" class="form-label">{{ t('auth.signIn.email') }}</label>
            <input
              id="forgot-email"
              v-model.trim="forgotEmail"
              class="form-control"
              :class="{ 'is-invalid': forgotError }"
              type="email"
              autocomplete="email"
              inputmode="email"
              :disabled="busy"
            />
            <div v-if="forgotError" class="invalid-feedback">{{ forgotError }}</div>
          </div>
          <button type="submit" class="btn btn-primary btn-lg w-100" :disabled="busy">
            <span v-if="busy" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
            {{ busy ? t('common.sending') : t('auth.forgot.submit') }}
          </button>
        </form>

        <button type="button" class="btn btn-link auth-back" :disabled="busy" @click="closeForgot">
          <i class="bi bi-arrow-left" aria-hidden="true"></i>
          {{ t('common.cancel') }}
        </button>
      </template>
    </section>
  </div>
</template>

<script setup>
import { reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import logo from "@/assets/Turkuvaz Logo.png";
import { api } from "@/services/api";
import { useAuthStore } from "@/stores/auth";
import { useToastStore } from "@/stores/toast";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const toast = useToastStore();

const credentials = reactive({ email: "", password: "" });
const errors = reactive({ email: "", password: "" });
const showPassword = ref(false);
const busy = ref(false);
const forgotMode = ref(false);
const forgotEmail = ref("");
const forgotError = ref("");
const forgotSent = ref(false);

function validEmail(value) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function validateLogin() {
  errors.email = validEmail(credentials.email) ? "" : t("validation.email");
  errors.password = credentials.password ? "" : t("validation.required");
  return !errors.email && !errors.password;
}

async function submitLogin() {
  if (!validateLogin() || busy.value) return;
  busy.value = true;
  try {
    await auth.login({ ...credentials });
    const returnUrl = typeof route.query.returnUrl === "string" && route.query.returnUrl.startsWith("/")
      ? route.query.returnUrl
      : "/";
    await router.replace(returnUrl);
  } catch (error) {
    toast.error(error.message || t("errors.login"));
  } finally {
    busy.value = false;
  }
}

function openForgot() {
  forgotEmail.value = credentials.email;
  forgotError.value = "";
  forgotSent.value = false;
  forgotMode.value = true;
}

function closeForgot() {
  forgotMode.value = false;
  forgotSent.value = false;
}

async function submitForgotPassword() {
  forgotError.value = validEmail(forgotEmail.value) ? "" : t("validation.email");
  if (forgotError.value || busy.value) return;
  busy.value = true;
  try {
    await api.post("/api/account/forgot-password", { email: forgotEmail.value });
    forgotSent.value = true;
  } catch (error) {
    toast.error(error.message || t("errors.generic"));
  } finally {
    busy.value = false;
  }
}
</script>

<style scoped>
.auth-page { min-height: 100dvh; display: grid; place-items: center; padding: 1.5rem; background: radial-gradient(circle at top right, #d9f3f1 0, transparent 36%), var(--color-page); }
.auth-card { width: min(100%, 460px); padding: clamp(1.5rem, 5vw, 2.5rem); border: 1px solid var(--color-border); border-radius: 20px; background: #fff; box-shadow: 0 24px 60px rgba(15, 42, 51, .11); }
.auth-brand { display: flex; align-items: center; gap: .75rem; margin-bottom: 2rem; color: var(--color-text); font-weight: 800; }
.auth-brand img { width: 46px; height: 46px; object-fit: contain; }
.auth-heading { margin-bottom: 1.75rem; }
.auth-heading h1 { margin: .2rem 0 .5rem; font-size: clamp(1.75rem, 7vw, 2.25rem); }
.auth-heading p:not(.eyebrow) { margin: 0; color: var(--color-text-muted); }
.eyebrow { margin: 0; color: var(--color-primary); font-size: .75rem; font-weight: 800; letter-spacing: .08em; text-transform: uppercase; }
.form-field { margin-bottom: 1.25rem; }
.password-field { position: relative; }
.password-field .form-control { padding-right: 3.25rem; }
.password-toggle { position: absolute; inset: 0 .25rem 0 auto; width: 44px; border: 0; border-radius: 10px; color: var(--color-text-muted); background: transparent; }
.auth-row { display: flex; justify-content: flex-end; margin: -.25rem 0 1.25rem; }
.link-button { min-height: 44px; padding: .5rem 0; border: 0; color: var(--color-primary); background: none; font-weight: 700; }
.notice { display: flex; gap: .75rem; padding: 1rem; border-radius: 12px; }
.notice--success { color: #166534; background: #dcfce7; }
.auth-back { display: inline-flex; align-items: center; gap: .5rem; margin-top: 1rem; }
</style>
