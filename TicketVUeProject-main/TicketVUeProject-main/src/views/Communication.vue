<template>
  <div class="main">
    <div class="feedback-section">
      <h2>{{ $t("submit_feedback") }}</h2>
      <textarea
          v-model="newfeedbackContent"
          placeholder="Enter your feedback here..."
          class="feedback-textarea"
      ></textarea>
      <button
          @click="submitFeedback"
          class="btn btn-primary"
          :style="{ backgroundColor: '#64748b', borderColor: '#64748b' }"
      >
        {{ $t("submit") }}
      </button>
    </div>
    <!-- Toast Mesajları -->
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

<script>
import {ref} from "vue";
import axios from "axios";

export default {

  setup() {
    const newfeedbackContent = ref();
    const toasts = ref([]);
    const maxToasts = 3;
    let toastHistory = [];
    const toastDelay = 3000;

    const showToast1 = async () => {
      showToast("Geri bildiriminizi yazın.", "success");
      return;
    }
    const submitFeedback = async () => {
      try {
        if (
            !newfeedbackContent.value ||
            newfeedbackContent.value.trim() === ""
        ) {
          showToast(
              "Geri bildirim içeriği boş olamaz. Lütfen bir şeyler yazın.",
              "warning"
          );
          return;
        }
        const newFeedbackData = {feedbackContent: newfeedbackContent.value};

        const response = await axios.post(
            "http://localhost:5005/api/Feedback/createFeedback",
            newFeedbackData
        );
        showToast("Geri bildirim başarıyla gönderildi!", "success");
      } catch (error) {
        console.error(
            "Geri bildirim oluşturulurken bir hata oluştu:",
            error.response?.data || error.message
        );

        if (error.response?.data?.errors) {
          console.error("Doğrulama Hataları:", error.response.data.errors);
        }
        console.error("Tam Hata Detayları:", error);

        showToast(
            "Geri bildirim gönderilirken bir hata oluştu. Lütfen tekrar deneyin.",
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

    return {
      submitFeedback,
      showToast1,
      showToast,
      newfeedbackContent,
      toasts,
    };
  },
};
</script>

<style scoped>
.main {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background-color: white;
  padding: 3rem;
}

.feedback-section {
  padding: 3rem;
  border: 1px solid #ddd;
  border-radius: 0.5rem;
  background-color: #f0f0f0;
  width: 100%;
  max-width: 1000px;
  text-align: center;
}

.feedback-section h2 {
  font-size: 2.5rem;
  margin-bottom: 2rem;
}

.feedback-textarea {
  width: 100%;
  min-height: 250px;
  padding: 1.5rem;
  font-size: 1.5rem;
  border: 1px solid #ccc;
  border-radius: 0.25rem;
  resize: vertical;
  margin-bottom: 2rem;
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 1.5rem 3rem;
  font-size: 1.75rem;
  font-weight: bold;
  color: #fff;
  border-radius: 0.25rem;
  text-decoration: none;
  transition: background-color 0.3s ease, color 0.3s ease;
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

.btn-primary:hover {
  background-color: #0056b3;
  border-color: #00408a;
}
</style>
