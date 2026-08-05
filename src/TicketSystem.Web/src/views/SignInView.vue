<template>
  <div class="auth-page">
    <!--
      Brand canvas, desktop only. aria-hidden because every word in it is
      marketing copy that repeats nothing the form needs; a screen reader user
      lands straight on the heading and the fields.
    -->
    <aside class="auth-aside d-none d-lg-flex" aria-hidden="true">
      <span class="auth-aside__blob auth-aside__blob--one"></span>
      <span class="auth-aside__blob auth-aside__blob--two"></span>

      <div class="auth-aside__content">
        <img :src="logo" alt="" class="auth-aside__logo" />
        <h2>{{ t('auth.brand.title') }}</h2>
        <p>{{ t('auth.brand.subtitle') }}</p>
        <ul>
          <li v-for="point in brandPoints" :key="point">
            <i class="bi bi-check-circle-fill" aria-hidden="true"></i>
            {{ point }}
          </li>
        </ul>
      </div>
    </aside>

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
import { computed, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import logo from "@/assets/turkuvaz-logo.webp";
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

const brandPoints = computed(() => [
  t("auth.brand.pointOne"),
  t("auth.brand.pointTwo"),
  t("auth.brand.pointThree"),
]);

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
    await api.post("/api/auth/forgot-password", { email: forgotEmail.value });
    forgotSent.value = true;
  } catch (error) {
    toast.error(error.message || t("errors.generic"));
  } finally {
    busy.value = false;
  }
}
</script>

<style scoped>
/* The .auth-layout wrapper already centres this and paints the gradient. Repeating
   an opaque background here painted a white column over that gradient, because
   this element is only as wide as the centred grid track. */
.auth-page { display: grid; place-items: center; width: 100%; }

/* Below 992px this collapses back to exactly the previous centred card — the
   split is a desktop affordance and mobile behaviour is unchanged. */
@media (min-width: 992px) {
  .auth-page {
    width: min(100%, 1040px);
    align-items: stretch;
    gap: 2.5rem;
    grid-template-columns: minmax(0, 1fr) minmax(0, 460px);
  }
}

.auth-aside {
  position: relative;
  flex-direction: column;
  justify-content: center;
  overflow: hidden;
  padding: clamp(2rem, 4vw, 3rem);
  border-radius: 24px;
  background:
    radial-gradient(120% 80% at 0 0, var(--tv-sidebar-glow), transparent 60%),
    linear-gradient(160deg, var(--tv-sidebar-from), var(--tv-sidebar-to));
  color: var(--tv-white);
}

/* Slow ambient drift. Decorative only, and stopped entirely under
   prefers-reduced-motion at the bottom of this block. */
.auth-aside__blob {
  position: absolute;
  border-radius: 50%;
  filter: blur(48px);
  opacity: .5;
  pointer-events: none;
}

.auth-aside__blob--one {
  top: -60px;
  right: -40px;
  width: 260px;
  height: 260px;
  background: var(--tv-teal-400);
  animation: tv-float 9s ease-in-out infinite;
}

.auth-aside__blob--two {
  bottom: -80px;
  left: -50px;
  width: 300px;
  height: 300px;
  background: var(--tv-teal-700);
  animation: tv-float 12s ease-in-out infinite reverse;
}

.auth-aside__content { position: relative; z-index: 1; }
.auth-aside__logo { width: auto; height: 34px; margin-bottom: 2rem; padding: 5px 10px; border-radius: 10px; background: var(--tv-white); }
.auth-aside h2 { margin: 0 0 .75rem; font-size: clamp(1.5rem, 2.4vw, 2rem); font-weight: 800; letter-spacing: -.02em; }
.auth-aside > .auth-aside__content > p { margin: 0 0 2rem; color: var(--tv-teal-100); }
.auth-aside ul { display: grid; margin: 0; padding: 0; gap: .85rem; list-style: none; }
.auth-aside li { display: flex; align-items: flex-start; gap: .6rem; color: var(--tv-teal-100); font-weight: 600; }
.auth-aside li .bi { color: var(--tv-teal-300); }

.auth-card { width: min(100%, 460px); padding: clamp(1.5rem, 5vw, 2.5rem); border: 1px solid var(--tv-border); border-radius: 20px; background: var(--tv-auth-card-bg); box-shadow: var(--tv-shadow-lg); }
/* Redundant beside the brand canvas on desktop, so it only shows where the
   canvas does not. */
.auth-brand { display: flex; align-items: center; gap: .75rem; margin-bottom: 2rem; color: var(--tv-text); font-weight: 800; }
/* Wordmark, not an icon: a fixed square rendered it about 11px tall. */
.auth-brand img { width: auto; height: 32px; }
.auth-heading { margin-bottom: 1.75rem; }
.auth-heading h1 { margin: .2rem 0 .5rem; color: var(--tv-text-strong); font-size: clamp(1.75rem, 7vw, 2.25rem); }
.auth-heading p:not(.eyebrow) { margin: 0; color: var(--tv-text-muted); }
.eyebrow { margin: 0; color: var(--tv-accent); font-size: .75rem; font-weight: 800; letter-spacing: .08em; text-transform: uppercase; }
.form-field { margin-bottom: 1.25rem; }
.password-field { position: relative; }
.password-field .form-control { padding-right: 3.25rem; }
.password-toggle { position: absolute; inset: 0 .25rem 0 auto; width: 44px; border: 0; border-radius: 10px; color: var(--tv-text-muted); background: transparent; }
.auth-row { display: flex; justify-content: flex-end; margin: -.25rem 0 1.25rem; }
.link-button { min-height: 44px; padding: .5rem 0; border: 0; color: var(--tv-accent); background: none; font-weight: 700; }
.notice { display: flex; gap: .75rem; padding: 1rem; border-radius: 12px; }
.notice--success { color: var(--tv-success-on-soft); background: var(--tv-success-soft); }
.auth-back { display: inline-flex; align-items: center; gap: .5rem; margin-top: 1rem; }

@media (min-width: 992px) {
  .auth-brand { display: none; }
}

@media (prefers-reduced-motion: reduce) {
  .auth-aside__blob { animation: none !important; }
}
</style>
