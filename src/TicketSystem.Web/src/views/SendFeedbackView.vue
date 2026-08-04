<template>
  <section>
    <PageHeader :title="t('feedback.sendTitle')" :description="t('feedback.sendDescription')" />

    <div class="feedback-layout">
      <div class="surface-card feedback-card">
        <div v-if="submitted" class="success-state" role="status">
          <span><i class="bi bi-check2" aria-hidden="true"></i></span>
          <h2>{{ t('feedback.thankYou') }}</h2>
          <p>{{ t('feedback.thankYouDescription') }}</p>
          <button type="button" class="btn btn-outline-primary" @click="reset">{{ t('feedback.sendAnother') }}</button>
        </div>

        <form v-else novalidate @submit.prevent="submit">
          <div class="form-field">
            <div class="label-row">
              <label for="feedback-content" class="form-label">{{ t('feedback.message') }}</label>
              <span :class="{ invalid: form.content.length > 1000 }">{{ form.content.length }}/1000</span>
            </div>
            <textarea
              id="feedback-content"
              v-model="form.content"
              class="form-control"
              :class="{ 'is-invalid': contentError }"
              rows="9"
              maxlength="1000"
              :placeholder="t('feedback.placeholder')"
              :aria-describedby="contentError ? 'feedback-error' : 'feedback-help'"
              :disabled="busy"
            ></textarea>
            <div v-if="contentError" id="feedback-error" class="invalid-feedback">{{ contentError }}</div>
            <div v-else id="feedback-help" class="form-text">{{ t('feedback.messageHint') }}</div>
          </div>
          <div class="form-actions">
            <button class="btn btn-primary" type="submit" :disabled="busy">
              <span v-if="busy" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
              <i v-else class="bi bi-send" aria-hidden="true"></i>
              {{ busy ? t('common.sending') : t('feedback.submit') }}
            </button>
          </div>
        </form>
      </div>

      <aside class="surface-card privacy-card">
        <span class="privacy-icon"><i class="bi bi-chat-heart" aria-hidden="true"></i></span>
        <h2>{{ t('feedback.whyTitle') }}</h2>
        <p>{{ t('feedback.whyDescription') }}</p>
        <ul>
          <li><i class="bi bi-check-circle" aria-hidden="true"></i>{{ t('feedback.pointOne') }}</li>
          <li><i class="bi bi-check-circle" aria-hidden="true"></i>{{ t('feedback.pointTwo') }}</li>
          <li><i class="bi bi-check-circle" aria-hidden="true"></i>{{ t('feedback.pointThree') }}</li>
        </ul>
      </aside>
    </div>
  </section>
</template>

<script setup>
import { reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import PageHeader from "@/components/PageHeader.vue";
import { api } from "@/services/api";
import { useToastStore } from "@/stores/toast";

const { t } = useI18n();
const toast = useToastStore();
const form = reactive({ content: "" });
const contentError = ref("");
const busy = ref(false);
const submitted = ref(false);

async function submit() {
  const length = form.content.trim().length;
  contentError.value = length < 10 || length > 1000 ? t("validation.feedbackLength") : "";
  if (contentError.value || busy.value) return;
  busy.value = true;
  try {
    await api.post("/api/feedback", { feedbackContent: form.content.trim() });
    submitted.value = true;
    form.content = "";
  } catch (error) {
    toast.error(error.message || t("errors.sendFeedback"));
  } finally {
    busy.value = false;
  }
}

function reset() { submitted.value = false; contentError.value = ""; }
</script>

<style scoped>
.feedback-layout { display: grid; grid-template-columns: minmax(0, 2fr) minmax(260px, 1fr); align-items: start; gap: 1.5rem; }
.feedback-card, .privacy-card { padding: clamp(1.25rem, 3vw, 2rem); }
.label-row { display: flex; justify-content: space-between; gap: 1rem; }
.label-row span { color: var(--color-text-muted); font-size: .8rem; }
.label-row span.invalid { color: var(--bs-danger); }
.form-actions { display: flex; justify-content: flex-end; margin-top: 1.25rem; }
.privacy-icon, .success-state > span { display: grid; place-items: center; width: 52px; height: 52px; border-radius: 15px; color: var(--color-primary); background: var(--color-primary-soft); font-size: 1.35rem; }
.privacy-card h2 { margin: 1rem 0 .5rem; font-size: 1.1rem; }
.privacy-card p { color: var(--color-text-muted); }
.privacy-card ul { display: grid; gap: .75rem; margin: 1.25rem 0 0; padding: 0; list-style: none; }
.privacy-card li { display: flex; gap: .6rem; color: var(--color-text-muted); }
.privacy-card li i { color: var(--color-primary); }
.success-state { display: grid; justify-items: center; min-height: 350px; align-content: center; text-align: center; }
.success-state > span { color: #166534; background: #dcfce7; }
.success-state h2 { margin: 1rem 0 .5rem; }
.success-state p { max-width: 460px; margin: 0 0 1.25rem; color: var(--color-text-muted); }
@media (max-width: 767px) { .feedback-layout { grid-template-columns: 1fr; } .form-actions .btn { width: 100%; } }
</style>
