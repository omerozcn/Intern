<template>
  <div v-if="toasts.length > 0" class="toast-container">
    <div
        v-for="(toast, index) in toasts"
        :key="index"
        :class="['toast', toast.type, 'show']"
        role="alert"
        aria-live="assertive"
        aria-atomic="true"
    >
      <div class="toast-body">
        {{ toast.message }}
        <button class="close-btn" @click="removeToast(index)">&times;</button>
      </div>
    </div>
  </div>
  <main style="background-color: white">
    <div v-if="userRole == 'Admin'">
      <div
          class="admin-dashboard"
          style="background-color: white; border-color: black"
      >
        <h2>{{ $t("total_requests") }}</h2>
        <div class="dashboard-item">
          <div class="info-box-container">
            <div class="info-box">
              <h3>{{ $t("total_requests") }}</h3>
              <p>{{ totalRequests }}</p>
            </div>
            <div class="info-box">
              <h3>{{ $t("pending_requests") }}</h3>
              <p>{{ pendingRequests }}</p>
            </div>
            <div class="info-box">
              <h3>{{ $t("in_progress_requests") }}</h3>
              <p>{{ inProgressRequests }}</p>
            </div>
            <div class="info-box">
              <h3>{{ $t("completed_requests") }}</h3>
              <p>{{ completedRequests }}</p>
            </div>
          </div>
          <div class="chart-container">
            <div class="chart-wrapper">
              <Bar :data="chartData" :options="chartOptions"></Bar>
            </div>
            <div class="chart-wrapper">
              <Doughnut
                  :data="doughnutData"
                  :options="doughnutOptions"
              ></Doughnut>
            </div>
          </div>
          <router-link
              to="/adminticket"
              class="btn btn-primary"
              style="
              margin-top: 50px;
              background-color: #64748b;
              border-color: #64748b;
            "
          >{{ $t("view_requests") }}
          </router-link
          >
        </div>
      </div>
    </div>
    <div v-else-if="isAuthenticated">
      <p>{{ $t("User_dashboard_message") }}</p>
      <div class="User-dashboard">
        <h2>{{ $t("your_total_requests") }}</h2>
        <div class="dashboard-item">
          <div class="info-box-container">
            <div class="info-box">
              <h3>{{ $t("total_requests") }}</h3>
              <p>{{ totalRequests }}</p>
            </div>
            <div class="info-box">
              <h3>{{ $t("pending_requests") }}</h3>
              <p>{{ pendingRequests }}</p>
            </div>
            <div class="info-box">
              <h3>{{ $t("in_progress_requests") }}</h3>
              <p>{{ inProgressRequests }}</p>
            </div>
            <div class="info-box">
              <h3>{{ $t("completed_requests") }}</h3>
              <p>{{ completedRequests }}</p>
            </div>
          </div>

          <router-link
              to="/request"
              class="btn btn-primary"
              style="
              margin-top: 50px;
              background-color: #64748b;
              border-color: #64748b;
            "
          >{{ $t("view_my_requests") }}
          </router-link
          >
        </div>
      </div>
    </div>

    <div v-else>
      <p>{{ $t("please_sign_in") }}</p>
      <router-link
          to="/sign-in"
          class="btn btn-outline-success sign-in-button"
          type="submit"
      >
        <i class="bi bi-box-arrow-in-right"></i>
        <span>⠀{{ $t("sign_in") }}</span>
      </router-link>
    </div>
  </main>
</template>

<script setup>
import {ref, onMounted} from "vue";

import {Bar, Doughnut} from "vue-chartjs";
import {
  Chart as ChartJS,
  BarElement,
  CategoryScale,
  LinearScale,
  Title,
  Tooltip,
  Legend,
  ArcElement,
} from "chart.js";
import {useI18n} from "vue-i18n";
import {useRouter} from "vue-router";
import {useUserStore} from "@/stores/UserStore.js";
import {storeToRefs} from "pinia";

ChartJS.register(
    BarElement,
    CategoryScale,
    LinearScale,
    Title,
    Tooltip,
    Legend,
    ArcElement
);

const {t} = useI18n();
const router = useRouter();
const totalRequests = ref(0);
const pendingRequests = ref(0);
const inProgressRequests = ref(0);
const store = useUserStore();
const { isAuthenticated, userRole } = storeToRefs(store);
const completedRequests = ref(0);
const feedbackText = ref("");
const toasts = ref([]);
const maxToasts = 3;
let toastHistory = [];
const toastDelay = 3000;

const doughnutData = ref({
  labels: [t("status_1"), t("status_2"), t("status_3")],
  datasets: [
    {
      label: t("requests"),
      data: [4, 8, 0],
      backgroundColor: ["#f39c12", "#2980b9", "#27ae60"],
    },
  ],
});

const doughnutOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: "top",
    },
    title: {
      display: true,
      text: t("request_distribution"),
    },
  },
};

const chartData = ref({
  labels: [t("status_1"), t("status_2"), t("status_3")],
  datasets: [
    {
      label: t("requests"),
      data: [4, 8, 0],
      backgroundColor: ["#f39c12", "#2980b9", "#27ae60"],
    },
  ],
});

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: "top",
    },
    title: {
      display: true,
      text: t("request_distribution"),
    },
  },
};

const submitFeedback = async () => {
  if (feedbackText.value.trim() === "") {
    alert(t("feedback_empty_error"));
    return;
  }

  try {
    feedbackText.value = "";
    alert(t("feedback_submitted_success"));
  } catch (error) {
    console.error("Error submitting feedback: ", error);
    alert(t("feedback_submission_error"));
  }
};

const showToast = (message, type) => {
  const currentTime = Date.now();
  const isMessageRecent = toastHistory.some(item =>
      item.message === message && (currentTime - item.timestamp) < toastDelay
  );

  if (isMessageRecent) return;

  if (toasts.value.length >= maxToasts) {
    removeToast(0);
  }

  toasts.value.push({ message, type });

  toastHistory.push({ message, timestamp: currentTime });

  toastHistory = toastHistory.filter(item => currentTime - item.timestamp < toastDelay);

  setTimeout(() => removeToast(0), 1800);
};

const removeToast = (index) => {
  const toast = document.querySelectorAll(".toast")[index];
  if (toast) {
    toast.classList.add("hide");
    setTimeout(() => {
      toasts.value.splice(index, 1);
    }, 600);
  }
};

onMounted(async () => {
  await store.fetchUserRole();
});
</script>

<style scoped>
main {
  display: flex;
  justify-content: center;
  align-items: center;
  flex-direction: column;
  text-align: center;
  padding: 2rem;
  min-height: 100vh;
  background-color: #f5f5f5;
}

.sign-in-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem 1rem;
  font-size: 1rem;
  font-weight: bold;
  color: #28a745;
  border: 2px solid #28a745;
  border-radius: 0.25rem;
  text-decoration: none;
  transition: background-color 0.3s ease, color 0.3s ease;
}

.sign-in-button:hover {
  background-color: #28a745;
  color: #fff;
}

.admin-dashboard,
.User-dashboard {
  width: 100%;
  max-width: 1600px;
  margin: 2rem auto;
  padding: 1rem;
  border: 1px solid #ddd;
  border-radius: 0.5rem;
  background-color: #fff;
}

.dashboard-item {
  margin-bottom: 2rem;
}

.dashboard-item h3 {
  margin: 0;
}

.dashboard-item p {
  margin: 0.5rem 0;
  font-size: 1.5rem;
}

.info-box-container {
  display: flex;
  justify-content: space-around;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 2rem;
}

.info-box {
  flex: 1;
  min-width: 200px;
  padding: 1rem;
  background-color: #f0f0f0;
  border-radius: 0.5rem;
  text-align: center;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.info-box h3 {
  margin: 0 0 0.5rem 0;
  font-size: 1.25rem;
}

.info-box p {
  margin: 0;
  font-size: 1.5rem;
  font-weight: bold;
}

.feedback-section {
  margin-top: 2rem;
  padding: 1rem;
  border: 1px solid #ddd;
  border-radius: 0.5rem;
  background-color: #f0f0f0;
}

.feedback-textarea {
  width: 100%;
  min-height: 150px;
  padding: 0.5rem;
  font-size: 1rem;
  border: 1px solid #ccc;
  border-radius: 0.25rem;
  resize: vertical;
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.75rem 1.5rem;
  font-size: 1.25rem;
  font-weight: bold;
  color: #fff;
  background-color: #007bff;
  border: 2px solid #007bff;
  border-radius: 0.25rem;
  text-decoration: none;
  transition: background-color 0.3s ease, color 0.3s ease;
}

.btn-primary:hover {
  background-color: #0056b3;
  border-color: #00408a;
}

.chart-container {
  display: flex;
  justify-content: space-around;
  flex-wrap: wrap;
  gap: 2rem;
  width: 100%;
}

.chart-wrapper {
  flex: 1;
  min-width: 600px;
  max-width: 800px;
  height: 500px;
}

.toast-container {
  position: fixed;
  top: 1rem;
  right: 1rem;
  z-index: 1050;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  pointer-events: none;
}

.toast {
  margin-top: 0.5rem;
  padding: 1rem 1.5rem;
  border-radius: 0.5rem;
  color: #fff;
  font-size: 1rem;
  background: linear-gradient(135deg, #6a11cb, #2575fc);
  opacity: 0;
  transform: translateX(100%) scale(0.9);
  transition: opacity 0.6s ease, transform 0.6s ease, box-shadow 0.6s ease,
  transform 0.3s ease-in-out,
    /* Added transition for scaling */ background 1s ease; /* Smooth background transition */
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.2);
  pointer-events: all;
}

.toast.success {
  background: linear-gradient(135deg, #4caf50, #81c784);
}

.toast.error {
  background: linear-gradient(135deg, #f44336, #e57373);
}

.toast.show {
  opacity: 1;
  transform: translateX(0) scale(1);
}

.toast.hide {
  opacity: 0;
  transform: translateX(100%) scale(0.8); /* Shrinks while fading out */
}

.toast::before {
  content: "";
  position: absolute;
  top: 50%;
  left: 0;
  transform: translateY(-50%);
  width: 5px;
  height: 100%;
  border-radius: 0.5rem 0 0 0.5rem;
  background-color: rgba(255, 255, 255, 0.4);
}

.toast .close-btn {
  position: absolute;
  top: 0.5rem;
  right: 0.5rem;
  background: none;
  border: none;
  color: rgba(255, 255, 255, 0.8);
  font-size: 1.2rem;
  cursor: pointer;
  transition: color 0.3s ease;
}

.toast .close-btn:hover {
  color: #fff;
}
</style>
