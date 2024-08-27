<template>
  <div class="feedback-page">
    <h1>{{ $t("feedbacks") }}</h1>
    <br/>
    <ul class="feedback-list">
      <li v-for="feedback in feedbacks" :key="feedback.id">
        <p>{{ feedback.feedbackContent }}</p>
      </li>
    </ul>

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
  </div>
</template>

<script setup>
import {ref, onMounted} from "vue";
import {useI18n} from "vue-i18n";

const maxToasts = 3;
const {t} = useI18n();
const toasts = ref([]);
let toastHistory = [];
const toastDelay = 3000;
const feedbacks = ref([]);
import axios from "axios";

const fetchFeedbacks = async () => {
  try {
    const response = await axios.get(
        "http://localhost:5005/api/Feedback/listFeedbacks"
    );

    const feedbackDataList = response.data.map((feedback) => ({
      id: feedback.id,
      feedbackContent: feedback.feedbackContent,
    }));

    if (response.status >= 200 && response.status <= 300 && response.data.length > 0) {
      showToast("Geri bildirimler başarıyla yüklendi!", "success");
      feedbacks.value = feedbackDataList;
    } else if(response.data.length <= 0) {
      showToast("Geri bildirim bulunamadı", "danger");
    } else {
      throw new Error(error);
    }
  } catch (error) {
    showToast(
        "Geri bildirimler yüklenirken bir hata oluştu. Lütfen tekrar deneyin.",
        "error"
    );
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
onMounted(fetchFeedbacks);
</script>

<style scoped>
.feedback-page {
  max-width: 900px;
  margin: 3rem auto;
  padding: 2rem;
  background: #f9f9f9;
  border-radius: 8px;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.loading {
  text-align: center;
  font-size: 1.75rem;
  color: #333;
}

.feedback-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.feedback-list li {
  padding: 1rem;
  border-bottom: 1px solid #ddd;
  background: #fff;
  margin-bottom: 1rem;
}

.feedback-list li:last-child {
  border-bottom: none;
}

.feedback-list p {
  margin: 0;
  font-size: 1.125rem;
}

.feedback-list small {
  display: block;
  font-size: 0.875rem;
  color: #555;
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
