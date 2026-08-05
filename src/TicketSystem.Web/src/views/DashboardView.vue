<template>
  <section>
    <PageHeader :title="t('dashboard.title')" :description="dashboardDescription">
      <template #actions>
        <RouterLink class="btn btn-primary" :to="isAdmin ? '/admin/tickets' : '/tickets/new'">
          <i class="bi" :class="isAdmin ? 'bi-inbox' : 'bi-plus-lg'" aria-hidden="true"></i>
          {{ isAdmin ? t('dashboard.viewAll') : t('dashboard.createTicket') }}
        </RouterLink>
      </template>
    </PageHeader>

    <LoadingState v-if="loading" :message="t('common.loading')" />
    <ErrorState v-else-if="error" :message="error" @retry="loadAll" />

    <template v-else>
      <!-- role="group" is required for the label to be exposed: a bare div is not a
           name-required element, so screen readers drop aria-label on it. -->
      <div class="kpi-grid tv-stagger" role="group" :aria-label="t('dashboard.summaryLabel')">
        <StatCard
          v-for="item in kpis"
          :key="item.key"
          :label="item.label"
          :value="item.value"
          :icon="item.icon"
          :tone="item.tone"
          :share="item.share"
          :hint="item.hint"
          :to="item.to"
        />
      </div>

      <div class="panel-row">
        <div class="surface-card chart-card">
          <div class="chart-heading">
            <div>
              <h2>{{ t('dashboard.distribution') }}</h2>
              <p>{{ t('dashboard.distributionDescription') }}</p>
            </div>
            <RouterLink class="text-link" :to="isAdmin ? '/admin/tickets' : '/tickets'">
              {{ t('dashboard.details') }}
              <i class="bi bi-arrow-right" aria-hidden="true"></i>
            </RouterLink>
          </div>

          <div class="chart-body">
            <!--
              Chart.js gives the canvas role="img", which axe then requires to
              have an accessible name. Rather than label it and have the same
              three numbers announced twice, the whole chart is hidden: the
              status rail beside it is a complete text equivalent, and it is the
              reading that actually works without sight.
            -->
            <div class="chart-wrap" aria-hidden="true">
              <Doughnut :data="chartData" :options="chartOptions" />
              <div class="chart-centre">
                <strong class="tv-tabular">{{ formattedTotal }}</strong>
                <small>{{ completionLabel }}</small>
              </div>
            </div>

            <!-- Text-first reading of the same three numbers. Three values are
                 parts of a whole, which is why this is a proportion rail and the
                 chart above is a doughnut rather than the previous bar chart. -->
            <ul class="status-rail">
              <li v-for="item in distribution" :key="item.key">
                <span class="status-rail__dot" :class="`status-rail__dot--${item.key}`" aria-hidden="true"></span>
                <span class="status-rail__label">{{ item.label }}</span>
                <span class="status-rail__value tv-tabular">{{ item.value }}</span>
                <span class="status-rail__track" aria-hidden="true">
                  <span
                    class="status-rail__fill"
                    :class="`status-rail__fill--${item.key}`"
                    :style="{ width: `${item.percent}%` }"
                  ></span>
                </span>
              </li>
            </ul>
          </div>
        </div>

        <div class="surface-card queue-card">
          <!-- The heading is role-branched, not just the description. To an admin
               these are work items in their queue; to a requester they are their
               own tickets waiting on someone else, and "Sıradaki işler" would be
               telling them to do something they cannot do. -->
          <SectionHeading
            icon="bi-hourglass-split"
            :title="isAdmin ? t('dashboard.queueTitleAdmin') : t('dashboard.queueTitleUser')"
            :description="isAdmin ? t('dashboard.queueDescriptionAdmin') : t('dashboard.queueDescriptionUser')"
          />
          <TicketQueue :tickets="queue" :empty-message="t('dashboard.queueEmpty')" />
        </div>
      </div>

      <div v-if="isAdmin && oldest.length" v-reveal class="surface-card queue-card queue-card--wide">
        <SectionHeading
          icon="bi-clock-history"
          :title="t('dashboard.oldestTitle')"
          :description="t('dashboard.oldestDescription')"
        />
        <TicketQueue :tickets="oldest" :empty-message="t('dashboard.queueEmpty')" show-age />
      </div>

      <!-- The firm and service catalogues belong to the platform owner, so these counts
           only exist for a super administrator; for anyone else the endpoints answer
           403 and the strip would just show zeroes. -->
      <template v-if="isSuperAdmin">
        <h2 v-reveal class="section-title">{{ t('dashboard.scaleTitle') }}</h2>
        <div class="kpi-grid">
          <StatCard
            v-for="(item, index) in scale"
            :key="item.key"
            v-reveal="index * 70"
            :label="item.label"
            :value="item.value"
            :icon="item.icon"
            :to="item.to"
            :loading="item.loading"
          />
        </div>
      </template>

      <template v-else-if="!isAdmin">
        <h2 v-reveal class="section-title">{{ t('dashboard.quickActions') }}</h2>
        <div class="kpi-grid">
          <StatCard
            v-reveal
            :label="t('dashboard.myServices')"
            :value="myServiceCount"
            icon="bi-box-seam"
            :loading="secondaryLoading"
          />
          <AppCard
            v-for="(action, index) in quickLinks"
            :key="action.to"
            v-reveal="(index + 1) * 70"
            :to="action.to"
            padding="md"
          >
            <span class="quick-icon" aria-hidden="true"><i :class="['bi', action.icon]"></i></span>
            <strong class="quick-label">{{ action.label }}</strong>
          </AppCard>
        </div>
      </template>
    </template>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { Doughnut } from "vue-chartjs";
import {
  ArcElement,
  Chart as ChartJS,
  DoughnutController,
  Legend,
  Tooltip,
} from "chart.js";

import AppCard from "@/components/AppCard.vue";
import ErrorState from "@/components/ErrorState.vue";
import LoadingState from "@/components/LoadingState.vue";
import PageHeader from "@/components/PageHeader.vue";
import SectionHeading from "@/components/SectionHeading.vue";
import StatCard from "@/components/StatCard.vue";
import TicketQueue from "@/components/TicketQueue.vue";
import { tokenEpoch } from "@/composables/useTheme";
import { api } from "@/services/api";
import { useAuthStore } from "@/stores/auth";
import { readChartTokens } from "@/utils/chartTheme";
import { normalizeTicketCounts, TICKET_STATUS } from "@/utils/tickets";

ChartJS.register(ArcElement, DoughnutController, Tooltip, Legend);

const { t, locale } = useI18n();
const auth = useAuthStore();

const loading = ref(true);
const secondaryLoading = ref(true);
const error = ref("");
const counts = ref(normalizeTicketCounts());
const queue = ref([]);
const oldest = ref([]);
const myServiceCount = ref(0);
const scaleCounts = ref({ firms: 0, services: 0, accounts: 0, feedback: 0 });

const isAdmin = computed(() => auth.user?.role === "Admin");
const isSuperAdmin = computed(() => auth.isSuperAdmin);
const dashboardDescription = computed(() =>
  isAdmin.value ? t("dashboard.descriptionAdmin") : t("dashboard.descriptionUser"),
);

const numberFormat = computed(
  () => new Intl.NumberFormat(locale.value === "tr" ? "tr-TR" : "en-US"),
);
const formattedTotal = computed(() => numberFormat.value.format(counts.value.total));

function share(value) {
  return counts.value.total ? value / counts.value.total : 0;
}

function shareHint(value) {
  if (!counts.value.total) return "";
  return t("dashboard.shareOfTotal", { percent: Math.round(share(value) * 100) });
}

const listRoute = computed(() => (isAdmin.value ? "/admin/tickets" : "/tickets"));

const kpis = computed(() => [
  {
    key: "total",
    label: t("dashboard.total"),
    value: counts.value.total,
    icon: "bi-layers",
    tone: "neutral",
    share: null,
    hint: "",
    to: listRoute.value,
  },
  ...[
    { key: TICKET_STATUS.PENDING, tone: "pending", icon: "bi-clock" },
    { key: TICKET_STATUS.IN_PROGRESS, tone: "inProgress", icon: "bi-arrow-repeat" },
    { key: TICKET_STATUS.COMPLETED, tone: "completed", icon: "bi-check2-circle" },
  ].map(({ key, tone, icon }) => ({
    key,
    label: t(`status.${key}`),
    value: counts.value[key],
    icon,
    tone,
    share: share(counts.value[key]),
    hint: shareHint(counts.value[key]),
    to: { path: listRoute.value, query: { status: key } },
  })),
]);

// Intl rather than a literal '%': Turkish writes %100 and English 100%, and
// hardcoding either gets one of them wrong.
const percentFormat = computed(
  () =>
    new Intl.NumberFormat(locale.value === "tr" ? "tr-TR" : "en-US", {
      style: "percent",
      maximumFractionDigits: 0,
    }),
);

const completionLabel = computed(() => {
  const ratio = counts.value.total
    ? counts.value[TICKET_STATUS.COMPLETED] / counts.value.total
    : 0;
  return `${percentFormat.value.format(ratio)} ${t("dashboard.completionRate")}`;
});

const distribution = computed(() =>
  [TICKET_STATUS.PENDING, TICKET_STATUS.IN_PROGRESS, TICKET_STATUS.COMPLETED].map((key) => ({
    key,
    label: t(`status.${key}`),
    value: counts.value[key],
    percent: Math.round(share(counts.value[key]) * 100),
  })),
);

const scale = computed(() => [
  { key: "firms", label: t("dashboard.firms"), value: scaleCounts.value.firms, icon: "bi-buildings", to: "/firms", loading: secondaryLoading.value },
  { key: "services", label: t("dashboard.services"), value: scaleCounts.value.services, icon: "bi-box-seam", to: "/services", loading: secondaryLoading.value },
  { key: "accounts", label: t("dashboard.accounts"), value: scaleCounts.value.accounts, icon: "bi-people", to: "/accounts", loading: secondaryLoading.value },
  { key: "feedback", label: t("dashboard.feedback"), value: scaleCounts.value.feedback, icon: "bi-chat-left-dots", to: "/admin/feedback", loading: secondaryLoading.value },
]);

const quickLinks = computed(() => [
  { to: "/tickets/new", label: t("dashboard.createTicket"), icon: "bi-plus-lg" },
  { to: "/tickets", label: t("tickets.myTitle"), icon: "bi-inbox" },
  { to: "/feedback/new", label: t("feedback.sendTitle"), icon: "bi-chat-square-text" },
]);

// Reading tokenEpoch is what makes these recompute after a theme flip; canvas
// keeps whatever colours it was handed, so the chart has to be told again.
const palette = computed(() => {
  void tokenEpoch.value;
  return readChartTokens();
});

const prefersReducedMotion = () =>
  globalThis.matchMedia?.("(prefers-reduced-motion: reduce)").matches ?? false;

const chartData = computed(() => ({
  labels: [t("status.pending"), t("status.inProgress"), t("status.completed")],
  datasets: [{
    label: t("dashboard.requests"),
    data: [
      counts.value[TICKET_STATUS.PENDING],
      counts.value[TICKET_STATUS.IN_PROGRESS],
      counts.value[TICKET_STATUS.COMPLETED],
    ],
    backgroundColor: [palette.value.pending, palette.value.inProgress, palette.value.completed],
    borderWidth: 0,
    spacing: 2,
  }],
}));

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  cutout: "68%",
  // CSS cannot reach a canvas, so the reduced-motion escape has to be here.
  animation: prefersReducedMotion()
    ? false
    : { duration: 900, easing: "easeOutCubic", delay: (ctx) => ctx.dataIndex * 90 },
  plugins: {
    legend: { display: false },
    tooltip: { titleFont: { weight: 600 } },
  },
}));

async function loadCounts() {
  counts.value = normalizeTicketCounts(await api.get("/api/tickets/status-counts"));
}

function totalOf(result) {
  return result?.status === "fulfilled" ? (result.value?.totalCount ?? 0) : 0;
}

/* Everything below the KPI row is supplementary. Promise.allSettled keeps one
   failing panel — a 403 on a list the role cannot read, say — from blanking the
   whole dashboard. */
async function loadSecondary() {
  secondaryLoading.value = true;
  const pendingPath = isAdmin.value ? "/api/tickets" : "/api/tickets/mine";
  const pendingQuery = `${pendingPath}?status=pending&page=1&pageSize=5`;

  try {
    if (isAdmin.value) {
      // The catalogue counts are only requested when they can actually be read, so a
      // normal administrator's dashboard does not fire four requests that 403.
      const scaleQueries = isSuperAdmin.value
        ? [
            api.get("/api/firms?page=1&pageSize=1"),
            api.get("/api/products?page=1&pageSize=1"),
            api.get("/api/users?page=1&pageSize=1"),
            api.get("/api/feedback?page=1&pageSize=1"),
          ]
        : [];

      const [pending, firms, services, accounts, feedback] = await Promise.allSettled([
        api.get(pendingQuery),
        ...scaleQueries,
      ]);

      queue.value = pending.status === "fulfilled" ? (pending.value?.items ?? []) : [];
      scaleCounts.value = {
        firms: totalOf(firms),
        services: totalOf(services),
        accounts: totalOf(accounts),
        feedback: totalOf(feedback),
      };

      /* Tickets are ordered by status then newest-first, so the last page of the
         pending filter holds the oldest pending tickets. Two cheap requests turn
         that ordering into an ageing view without a new endpoint. */
      const totalPages = pending.status === "fulfilled" ? (pending.value?.totalPages ?? 0) : 0;
      if (totalPages > 1) {
        const tail = await Promise.allSettled([
          api.get(`${pendingPath}?status=pending&page=${totalPages}&pageSize=5`),
        ]);
        oldest.value =
          tail[0].status === "fulfilled" ? [...(tail[0].value?.items ?? [])].reverse() : [];
      } else {
        oldest.value = [];
      }
    } else {
      const [pending, products] = await Promise.allSettled([
        api.get(pendingQuery),
        api.get("/api/products/mine"),
      ]);
      queue.value = pending.status === "fulfilled" ? (pending.value?.items ?? []) : [];
      // /api/products/mine returns a bare array, not a PagedResult.
      myServiceCount.value = products.status === "fulfilled" ? (products.value?.length ?? 0) : 0;
    }
  } finally {
    secondaryLoading.value = false;
  }
}

async function loadAll() {
  loading.value = true;
  error.value = "";
  try {
    await loadCounts();
    loading.value = false;
    await loadSecondary();
  } catch (requestError) {
    error.value = requestError.message || t("errors.loadDashboard");
    loading.value = false;
  }
}

onMounted(loadAll);
</script>

<style scoped>
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.panel-row {
  display: grid;
  grid-template-columns: minmax(0, 1.35fr) minmax(0, 1fr);
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.chart-card,
.queue-card {
  padding: 1.5rem;
}

.queue-card--wide {
  margin-bottom: 1.5rem;
}

.chart-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.25rem;
}

.chart-heading h2 {
  margin: 0 0 0.25rem;
  color: var(--tv-text-strong);
  font-size: 1.125rem;
}

.chart-heading p {
  margin: 0;
  color: var(--tv-text-muted);
}

.chart-body {
  display: grid;
  align-items: center;
  gap: 1.5rem;
  grid-template-columns: minmax(0, 200px) minmax(0, 1fr);
}

.chart-wrap {
  position: relative;
  height: 200px;
}

.chart-centre {
  position: absolute;
  inset: 0;
  display: grid;
  place-content: center;
  text-align: center;
}

.chart-centre strong {
  display: block;
  color: var(--tv-text-strong);
  font-size: 1.75rem;
  font-weight: 800;
  line-height: 1.1;
}

.chart-centre small {
  color: var(--tv-text-muted);
  font-size: 0.75rem;
}

.status-rail {
  display: grid;
  margin: 0;
  padding: 0;
  gap: 0.85rem;
  list-style: none;
}

.status-rail li {
  display: grid;
  align-items: center;
  gap: 0.5rem;
  grid-template-columns: auto minmax(0, 1fr) auto;
}

.status-rail__dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
}

.status-rail__dot--pending { background: var(--tv-chart-pending); }
.status-rail__dot--inProgress { background: var(--tv-chart-inProgress); }
.status-rail__dot--completed { background: var(--tv-chart-completed); }

.status-rail__label {
  color: var(--tv-text-muted);
  font-weight: 600;
}

.status-rail__value {
  color: var(--tv-text-strong);
  font-weight: 800;
}

.status-rail__track {
  overflow: hidden;
  height: 6px;
  grid-column: 1 / -1;
  border-radius: 999px;
  background: var(--tv-chart-track);
}

.status-rail__fill {
  display: block;
  height: 100%;
  border-radius: inherit;
  transition: width var(--tv-duration-slower) var(--tv-ease);
}

.status-rail__fill--pending { background: var(--tv-chart-pending); }
.status-rail__fill--inProgress { background: var(--tv-chart-inProgress); }
.status-rail__fill--completed { background: var(--tv-chart-completed); }

.section-title {
  margin: 0 0 1rem;
  color: var(--tv-text-strong);
  font-size: 1.125rem;
  font-weight: 750;
}

.quick-icon {
  display: grid;
  width: 44px;
  height: 44px;
  place-items: center;
  border-radius: 14px;
  background: var(--tv-accent-soft);
  color: var(--tv-accent-on-soft);
  font-size: 1.2rem;
}

.quick-label {
  color: var(--tv-text-strong);
  font-size: 1rem;
  font-weight: 700;
}

.text-link {
  color: var(--tv-accent);
  font-weight: 700;
  text-decoration: none;
  white-space: nowrap;
}

@media (max-width: 1199px) {
  .panel-row { grid-template-columns: minmax(0, 1fr); }
}

@media (max-width: 1023px) {
  .kpi-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}

@media (max-width: 767px) {
  .chart-body { grid-template-columns: minmax(0, 1fr); }
}

@media (max-width: 575px) {
  .kpi-grid { grid-template-columns: 1fr; }
  .chart-heading { align-items: stretch; flex-direction: column; }
}
</style>
