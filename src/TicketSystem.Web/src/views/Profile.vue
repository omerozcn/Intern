<template>
  <section>
    <PageHeader :title="t('profile.title')" :description="t('profile.description')" />

    <div class="profile-layout">
      <article class="surface-card profile-card">
        <div class="profile-summary">
          <span class="avatar">{{ initials }}</span>
          <div><h2>{{ fullName }}</h2><p>@{{ auth.user?.userName }}</p><span class="role-badge" :class="{ admin: auth.user?.role === 'Admin' }">{{ t(auth.user?.role === 'Admin' ? 'roles.admin' : 'roles.user') }}</span></div>
        </div>
        <dl class="profile-details">
          <div><dt><i class="bi bi-envelope" aria-hidden="true"></i>{{ t('fields.email') }}</dt><dd>{{ auth.user?.email }}</dd></div>
          <div><dt><i class="bi bi-buildings" aria-hidden="true"></i>{{ t('fields.firm') }}</dt><dd>{{ auth.user?.firm?.name || t('common.notAvailable') }}</dd></div>
          <div><dt><i class="bi bi-person-badge" aria-hidden="true"></i>{{ t('fields.role') }}</dt><dd>{{ t(auth.user?.role === 'Admin' ? 'roles.admin' : 'roles.user') }}</dd></div>
        </dl>
      </article>

      <form class="surface-card password-card" novalidate @submit.prevent="changePassword">
        <div class="section-heading"><span><i class="bi bi-shield-lock" aria-hidden="true"></i></span><div><h2>{{ t('profile.passwordTitle') }}</h2><p>{{ t('profile.passwordDescription') }}</p></div></div>
        <div class="form-field">
          <label for="current-password" class="form-label">{{ t('fields.currentPassword') }}</label>
          <div class="password-field"><input id="current-password" v-model="passwordForm.currentPassword" class="form-control" :class="{ 'is-invalid': passwordErrors.currentPassword }" :type="showPasswords ? 'text' : 'password'" autocomplete="current-password" :disabled="busy" /><button type="button" class="password-toggle" :aria-label="t('auth.signIn.showPassword')" @click="showPasswords = !showPasswords"><i :class="showPasswords ? 'bi bi-eye-slash' : 'bi bi-eye'" aria-hidden="true"></i></button></div>
          <div v-if="passwordErrors.currentPassword" class="invalid-feedback d-block">{{ passwordErrors.currentPassword }}</div>
        </div>
        <div class="form-field">
          <label for="profile-new-password" class="form-label">{{ t('fields.newPassword') }}</label>
          <input id="profile-new-password" v-model="passwordForm.newPassword" class="form-control" :class="{ 'is-invalid': passwordErrors.newPassword }" :type="showPasswords ? 'text' : 'password'" autocomplete="new-password" :disabled="busy" />
          <div v-if="passwordErrors.newPassword" class="invalid-feedback">{{ passwordErrors.newPassword }}</div>
          <div v-else class="form-text">{{ t('auth.reset.passwordHint') }}</div>
        </div>
        <div class="form-field">
          <label for="profile-confirm-password" class="form-label">{{ t('fields.confirmPassword') }}</label>
          <input id="profile-confirm-password" v-model="passwordForm.confirmPassword" class="form-control" :class="{ 'is-invalid': passwordErrors.confirmPassword }" :type="showPasswords ? 'text' : 'password'" autocomplete="new-password" :disabled="busy" />
          <div v-if="passwordErrors.confirmPassword" class="invalid-feedback">{{ passwordErrors.confirmPassword }}</div>
        </div>
        <button class="btn btn-primary" type="submit" :disabled="busy"><span v-if="busy" class="spinner-border spinner-border-sm" aria-hidden="true"></span>{{ busy ? t('common.saving') : t('profile.changePassword') }}</button>
      </form>
    </div>
  </section>
</template>

<script setup>
import { computed, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import PageHeader from "@/components/PageHeader.vue";
import { api } from "@/services/api";
import { useAuthStore } from "@/stores/auth";
import { useToastStore } from "@/stores/toast";
import { meetsPasswordPolicy } from "@/utils/password";

const { t, locale } = useI18n();
const router = useRouter();
const auth = useAuthStore();
const toast = useToastStore();
const passwordForm = reactive({ currentPassword: "", newPassword: "", confirmPassword: "" });
const passwordErrors = reactive({ currentPassword: "", newPassword: "", confirmPassword: "" });
const showPasswords = ref(false);
const busy = ref(false);
const fullName = computed(() => `${auth.user?.firstName ?? ""} ${auth.user?.lastName ?? ""}`.trim());
const initials = computed(() => `${auth.user?.firstName?.[0] ?? ""}${auth.user?.lastName?.[0] ?? ""}`.toLocaleUpperCase(locale.value === "tr" ? "tr-TR" : "en-US"));

function validate() {
  passwordErrors.currentPassword = passwordForm.currentPassword ? "" : t("validation.required");
  passwordErrors.newPassword = meetsPasswordPolicy(passwordForm.newPassword) ? "" : t("validation.minLength", { min: 12 });
  passwordErrors.confirmPassword = passwordForm.confirmPassword === passwordForm.newPassword ? "" : t("validation.passwordMismatch");
  return Object.values(passwordErrors).every((value) => !value);
}

async function changePassword() {
  if (!validate() || busy.value) return;
  busy.value = true;
  try {
    await api.post("/api/account/change-password", { currentPassword: passwordForm.currentPassword, newPassword: passwordForm.newPassword });
    toast.success(t("profile.passwordUpdated"));
    await auth.logout();
    await router.replace({ path: "/sign-in", query: { passwordChanged: "1" } });
  } catch (error) { toast.error(error.message || t("errors.changePassword")); }
  finally { busy.value = false; }
}
</script>

<style scoped>
.profile-layout { display: grid; grid-template-columns: minmax(280px, 1fr) minmax(0, 1.5fr); align-items: start; gap: 1.5rem; }
.profile-card, .password-card { padding: clamp(1.25rem, 3vw, 2rem); }
.profile-summary { display: flex; align-items: center; gap: 1rem; padding-bottom: 1.5rem; border-bottom: 1px solid var(--color-border); }
.avatar { display: grid; place-items: center; width: 72px; height: 72px; flex: 0 0 72px; border-radius: 20px; color: white; background: linear-gradient(135deg, var(--color-primary), #155e75); font-size: 1.25rem; font-weight: 800; }
.profile-summary h2 { margin: 0; font-size: 1.25rem; }
.profile-summary p { margin: .2rem 0 .5rem; color: var(--color-text-muted); }
.role-badge { display: inline-flex; padding: .3rem .6rem; border-radius: 999px; color: #334155; background: #e2e8f0; font-size: .75rem; font-weight: 700; }
.role-badge.admin { color: #03696b; background: var(--color-primary-soft); }
.profile-details { display: grid; gap: 1.25rem; margin: 1.5rem 0 0; }
.profile-details dt { display: flex; align-items: center; gap: .5rem; color: var(--color-text-muted); font-size: .75rem; }
.profile-details dd { margin: .25rem 0 0 1.5rem; overflow-wrap: anywhere; font-weight: 700; }
.section-heading { display: flex; gap: .75rem; margin-bottom: 1.5rem; }
.section-heading > span { display: grid; place-items: center; width: 44px; height: 44px; flex: 0 0 44px; border-radius: 13px; color: var(--color-primary); background: var(--color-primary-soft); }
.section-heading h2 { margin: 0 0 .25rem; font-size: 1.1rem; }
.section-heading p { margin: 0; color: var(--color-text-muted); }
.form-field { margin-bottom: 1.25rem; }
.password-field { position: relative; }
.password-field input { padding-right: 3.25rem; }
.password-toggle { position: absolute; inset: 0 .25rem 0 auto; width: 44px; border: 0; border-radius: 10px; color: var(--color-text-muted); background: transparent; }
@media (max-width: 767px) { .profile-layout { grid-template-columns: 1fr; } .password-card .btn { width: 100%; } }
</style>
