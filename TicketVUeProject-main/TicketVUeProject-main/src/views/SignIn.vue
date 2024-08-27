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
  <main class="form-signin m-auto w-100">
    <div class="row">
      <div class="col-12">
        <p class="lead mb-4" style="margin-top: 85px">
          {{ $t("login.title") }}
        </p>
        <div class="form-floating mb-3">
          <input
              type="email"
              class="form-control form-control-lg"
              id="floatingInput"
              placeholder="{{ $t('login.email') }}"
              v-model="email"
              required
          />
          <label for="floatingInput">{{ $t("login.email") }}</label>
        </div>
        <div class="form-floating mb-4">
          <input
              type="password"
              class="form-control form-control-lg"
              id="floatingPassword"
              placeholder="{{ $t('login.password') }}"
              v-model="password"
              @keyup.enter="signIn"
          />
          <label for="floatingPassword">{{ $t("login.password") }}</label>
        </div>
        <button
            @click="showForgotPasswordModal"
            class="btn btn-link mt-3 w-100 forgot-password-link"
        >
          {{ $t("login.forgot_password") }}
        </button>
        <PvButton
            :label="$t('login.sign_in')"
            icon="pi pi-User"
            severity="secondary"
            class="rounded w-75"
            @click="signIn"
        />
      </div>
    </div>

    <div
        v-if="errMsg"
        class="toast align-items-center text-bg-danger border-0 show"
        role="alert"
    >
      <div class="d-flex">
        <div class="toast-body">{{ errMsg }}</div>
        <button
            type="button"
            class="btn-close btn-close-white me-2 m-auto"
            @click="errMsg = ''"
            aria-label="Close"
        ></button>
      </div>
    </div>

    <div
        v-if="isForgotPasswordModalVisible"
        class="modal fade show"
        tabindex="-1"
        style="display: block"
    >
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">
              {{ $t("login.forgot_password_modal_title") }}
            </h5>
            <button
                type="button"
                class="btn-close"
                @click="hideForgotPasswordModal"
            ></button>
          </div>
          <div class="modal-body">
            <p>{{ $t("login.forgot_password_modal_body") }}</p>
            <input
                type="email"
                class="form-control"
                v-model="forgotPasswordEmail"
                placeholder="E-Mail"
            />
          </div>
          <div class="modal-footer">
            <button
                type="button"
                class="btn btn-secondary"
                @click="hideForgotPasswordModal"
            >
              {{ $t("login.cancel") }}
            </button>
            <button
                type="button"
                class="btn btn-primary"
                @click="resetPassword"
            >
              {{ $t("login.reset_password") }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </main>
</template>

<script setup>
import {ref} from "vue";
import {useRouter} from "vue-router";
import axios from "axios";
import { useUserStore } from '../stores/UserStore.js';

const email = ref("");
const password = ref("");
const errMsg = ref("");
const token = sessionStorage.getItem("token");
const toasts = ref([]);
const store = useUserStore();
const isAuthenticated = ref();
const userRole = ref();
const isForgotPasswordModalVisible = ref(false);
const forgotPasswordEmail = ref("");
const maxToasts = 3;
let toastHistory = [];
const toastDelay = 3000;

const router = useRouter();


const signIn = async () => {
  const loginDto = {email: email.value, password: password.value};
  try {
    const response = await axios.post(
        "http://localhost:5005/api/account/login",
        loginDto
    );

    if (response.data.token) {
      sessionStorage.setItem("token", response.data.token);
      sessionStorage.setItem("email", response.data.email);
      sessionStorage.setItem("userName", response.data.userName);
      sessionStorage.setItem("firstName", response.data.firstName);
      sessionStorage.setItem("lastName", response.data.lastName);
      sessionStorage.setItem("firmName", response.data.firmName);
      showToast("Giriş başarılı! Ana sayfaya yönlendiriliyorsunuz...", "info");

      setTimeout(async () => {
        await store.fetchUserRole();
        isAuthenticated.value = store.isAuthenticated;
        userRole.value = store.userRole;
        await router.push("/");
      }, 2000);
    }
  } catch (e) {
    if (e.response && e.response.status === 401) {

      showToast("E-posta veya şifre hatalı. Lütfen tekrar deneyin.", "error");
    } else if (e.response && e.response.status === 400 || (email.value || password.value === null) ) {

      showToast("Giriş bilgileri eksik veya hatalı. Lütfen tüm alanları doldurun ve tekrar deneyin.", "warning");
    } else if (e.response && e.response.status === 500) {

      showToast("Sunucu hatası! Lütfen daha sonra tekrar deneyin.", "warning");
    } else {

      showToast("Giriş sırasında bir hata oluştu. Lütfen tekrar deneyin.", "error");
    }
    console.error("Error during login:", e);
  }
};

const showForgotPasswordModal = () => {
  isForgotPasswordModalVisible.value = true;
};

const hideForgotPasswordModal = () => {
  isForgotPasswordModalVisible.value = false;
  forgotPasswordEmail.value = "";
};

const resetPassword = async () => {
  if (!forgotPasswordEmail.value) {
    errMsg.value = $t("login.error.generic");
    return;
  }

  try {
    await sendPasswordResetEmail(auth, forgotPasswordEmail.value);
    hideForgotPasswordModal();
    alert($t("login.password_reset_email_sent"));
  } catch (error) {
    handleAuthError(error);
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


</script>

<style scoped>
.form-signin {
  text-align: center;
  font-family: "Arial", sans-serif;
  font-size: 1rem;
  margin: 180px auto 0;
  max-width: 400px;
}

.lead {
  font-size: 1.25rem;
  margin-bottom: 1.5rem;
}

.form-floating {
  position: relative;
  margin-bottom: 1.5rem;
}

.btn-link {
  color: #007bff;
  font-weight: bold;
}

.btn-link:hover {
  color: #0056b3;
}

.toast {
  background-color: #f8d7da;
  color: #721c24;
}

.modal.show {
  display: block;
  background-color: rgba(0, 0, 0, 0.5);
}

.modal-content {
  border-radius: 0.375rem;
}

.modal-header .btn-close {
  filter: invert(1);
}

.forgot-password-link {
  display: block;
  text-align: center;
  margin-top: 1rem;
  font-size: 0.9rem;
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
