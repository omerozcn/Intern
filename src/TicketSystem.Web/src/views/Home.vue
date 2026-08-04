<template>
  <section>
    <PageHeader :title="t('dashboard.title')" :description="dashboardDescription">
      <template #actions>
        <RouterLink class="btn btn-primary" :to="isAdmin ? '/adminticket' : '/ticket'">
          <i class="bi" :class="isAdmin ? 'bi-inbox' : 'bi-plus-lg'" aria-hidden="true"></i>
          {{ isAdmin ? t('dashboard.viewAll') : t('dashboard.createTicket') }}
        </RouterLink>
      </template>
    </PageHeader>

    <LoadingState v-if="loading" :message="t('common.loading')" />
    <ErrorState v-else-if="error" :message="error" @retry="loadCounts" />

    <template v-else>
      <div class="kpi-grid" aria-label="Talep özeti">
        <article v-for="item in kpis" :key="item.key" class="surface-card kpi-card">
          <div class="kpi-icon" :class="`kpi-icon--${item.key}`">
            <i :class="item.icon" aria-hidden="true"></i>
          </div>
          <div>
            <p class="kpi-label">{{ item.label }}</p>
            <strong class="kpi-value">{{ item.value }}</strong>
          </div>
        </article>
      </div>

      <div class="surface-card chart-card">
        <div class="chart-heading">
          <div>
            <h2>{{ t('dashboard.distribution') }}</h2>
            <p>{{ t('dashboard.distributionDescription') }}</p>
          </div>
          <RouterLink class="text-link" :to="isAdmin ? '/adminticket' : '/request'">
            {{ t('dashboard.details') }}
            <i class="bi bi-arrow-right" aria-hidden="true"></i>
          </RouterLink>
        </div>
        <div class="chart-wrap">
          <Bar :data="chartData" :options="chartOptions" />
        </div>
      </div>
    </template>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { Bar } from "vue-chartjs";
import {
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LinearScale,
  Tooltip,
} from "chart.js";
import PageHeader from "@/components/PageHeader.vue";
import LoadingState from "@/components/LoadingState.vue";
import ErrorState from "@/components/ErrorState.vue";
import { api } from "@/services/api";
import { useAuthStore } from "@/stores/auth";
import { normalizeTicketCounts, TICKET_STATUS } from "@/utils/tickets";

ChartJS.register(BarElement, CategoryScale, LinearScale, Tooltip, Legend);

const { t } = useI18n();
const auth = useAuthStore();
const loading = ref(true);
const error = ref("");
const counts = ref(normalizeTicketCounts());

const isAdmin = computed(() => auth.user?.role === "Admin");
const dashboardDescription = computed(() =>
  isAdmin.value ? t("dashboard.descriptionAdmin") : t("dashboard.descriptionUser"),
);

const kpis = computed(() => [
  { key: "total", label: t("dashboard.total"), value: counts.value.total, icon: "bi bi-layers" },
  { key: "pending", label: t("status.pending"), value: counts.value.pending, icon: "bi bi-clock" },
  { key: "inProgress", label: t("status.inProgress"), value: counts.value.inProgress, icon: "bi bi-arrow-repeat" },
  { key: "completed", label: t("status.completed"), value: counts.value.completed, icon: "bi bi-check2-circle" },
]);

const chartData = computed(() => ({
  labels: [t("status.pending"), t("status.inProgress"), t("status.completed")],
  datasets: [{
    label: t("dashboard.requests"),
    data: [
      counts.value[TICKET_STATUS.PENDING],
      counts.value[TICKET_STATUS.IN_PROGRESS],
      counts.value[TICKET_STATUS.COMPLETED],
    ],
    backgroundColor: ["#f59e0b", "#0ea5a8", "#15803d"],
    borderRadius: 8,
    maxBarThickness: 64,
  }],
}));

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  scales: { y: { beginAtZero: true, ticks: { precision: 0 } }, x: { grid: { display: false } } },
  plugins: { legend: { display: false }, tooltip: { titleFont: { weight: 600 } } },
}));

async function loadCounts() {
  loading.value = true;
  error.value = "";
  try {
    counts.value = normalizeTicketCounts(await api.get("/api/tickets/status-counts"));
  } catch (requestError) {
    error.value = requestError.message || t("errors.loadDashboard");
  } finally {
    loading.value = false;
  }
}

onMounted(loadCounts);
</script>

<style scoped>
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.kpi-card { display: flex; align-items: center; gap: 1rem; min-height: 126px; padding: 1.25rem; }
.kpi-icon { display: grid; place-items: center; width: 48px; height: 48px; flex: 0 0 48px; border-radius: 14px; color: var(--color-primary); background: var(--color-primary-soft); font-size: 1.25rem; }
.kpi-icon--pending { color: #9a6700; background: #fff7d6; }
.kpi-icon--inProgress { color: #03696b; background: #dff7f6; }
.kpi-icon--completed { color: #166534; background: #dcfce7; }
.kpi-label { margin: 0 0 .25rem; color: var(--color-text-muted); font-size: .875rem; }
.kpi-value { color: var(--color-text); font-size: 1.75rem; line-height: 1; }
.chart-card { padding: 1.5rem; }
.chart-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: 1.25rem; }
.chart-heading h2 { margin: 0 0 .25rem; font-size: 1.125rem; }
.chart-heading p { margin: 0; color: var(--color-text-muted); }
.chart-wrap { height: 320px; }
.text-link { color: var(--color-primary); font-weight: 700; text-decoration: none; white-space: nowrap; }

@media (max-width: 1023px) { .kpi-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
@media (max-width: 575px) {
  .kpi-grid { grid-template-columns: 1fr; }
  .chart-heading { align-items: stretch; flex-direction: column; }
  .chart-wrap { height: 280px; }
}
</style>
