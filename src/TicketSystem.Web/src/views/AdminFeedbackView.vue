<template>
  <section>
    <PageHeader :title="t('feedback.adminTitle')" :description="t('feedback.adminDescription')" />

    <div class="list-toolbar">
      <div class="search-field">
        <i class="bi bi-search" aria-hidden="true"></i>
        <label class="visually-hidden" for="feedback-search">{{ t('common.search') }}</label>
        <input id="feedback-search" v-model.trim="search" class="form-control" type="search" :placeholder="t('feedback.searchPlaceholder')" />
      </div>
      <span>{{ t('feedback.count', { count: totalCount }) }}</span>
    </div>

    <SkeletonList v-if="loading" :rows="4" />
    <ErrorState v-else-if="error" :message="error" @retry="load" />
    <EmptyState v-else-if="!feedback.length" icon="bi-chat-square-text" :title="t('feedback.emptyTitle')" :message="t('feedback.emptyDescription')" />

    <div v-else class="feedback-grid tv-stagger">
      <article v-for="item in feedback" :key="item.id" class="surface-card feedback-item">
        <div class="quote-icon"><i class="bi bi-quote" aria-hidden="true"></i></div>
        <p>{{ item.feedbackContent }}</p>
        <span>#{{ item.id }}</span>
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
  </section>
</template>

<script setup>
import { onMounted } from "vue";
import { useI18n } from "vue-i18n";
import PageHeader from "@/components/PageHeader.vue";
import SkeletonList from "@/components/SkeletonList.vue";
import EmptyState from "@/components/EmptyState.vue";
import ErrorState from "@/components/ErrorState.vue";
import PaginationBar from "@/components/PaginationBar.vue";
import { usePagedList } from "@/composables/usePagedList";

const { t } = useI18n();

const {
  items: feedback,
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
} = usePagedList("/api/feedback", {
  map: (rows) => rows.map((item) => ({
    id: Number(item.id),
    feedbackContent: item.feedbackContent ?? "",
  })),
});

onMounted(load);
</script>

<style scoped>
.list-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 1rem; margin-bottom: 1rem; }
.list-toolbar > span { color: var(--color-text-muted); white-space: nowrap; }
.search-field { position: relative; width: min(100%, 420px); }
.search-field i { position: absolute; top: 50%; left: 1rem; transform: translateY(-50%); color: var(--color-text-muted); }
.search-field input { padding-left: 2.6rem; }
.feedback-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 1rem; }
.feedback-item { min-width: 0; padding: 1.25rem; }
.quote-icon { display: grid; place-items: center; width: 40px; height: 40px; border-radius: 12px; color: var(--color-primary); background: var(--color-primary-soft); font-size: 1.25rem; }
.feedback-item p { min-height: 84px; margin: 1rem 0; white-space: pre-wrap; overflow-wrap: anywhere; }
.feedback-item > span { color: var(--color-text-muted); font-size: .75rem; }
@media (max-width: 1023px) { .feedback-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
@media (max-width: 575px) { .list-toolbar { align-items: stretch; flex-direction: column; } .search-field { width: 100%; } .feedback-grid { grid-template-columns: 1fr; } }
</style>
