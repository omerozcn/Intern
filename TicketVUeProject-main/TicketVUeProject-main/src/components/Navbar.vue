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
  <nav
    class="navbar navbar-expand-lg navbar-dark bg-dark fixed-top"
  >
    <div class="container-fluid">
      <a class="navbar-brand" href="/">{{ t("customer_request_form") }}</a>
      <button
        class="navbar-toggler"
        type="button"
        data-bs-toggle="collapse"
        data-bs-target="#navbarNav"
        aria-controls="navbarNav"
        aria-expanded="false"
        aria-label="Toggle navigation"
      >
        <span class="navbar-toggler-icon"></span>
      </button>
      <div class="collapse navbar-collapse" id="navbarNav">
        <ul class="navbar-nav ms-auto">
          <li class="nav-item" v-if="isAuthenticated">
            <a class="nav-link" aria-current="page" href="/">
              <span class="bi bi-house-door"></span> {{ t("home") }}
            </a>
          </li>
          <li class="nav-item" v-if="isAuthenticated">
            <a class="nav-link" aria-current="page" href="/ticket">
              <span class="bi bi-file-earmark-plus"></span> {{ t("create_request") }}
            </a>
          </li>
          <li class="nav-item" v-if="isAuthenticated">
            <a class="nav-link" aria-current="page" href="/request">
              <span class="bi bi-card-list"></span> {{ t("my_requests") }}
            </a>
          </li>
          <li class="nav-item" v-if="isAuthenticated && userRole === 'Admin'">
            <a class="nav-link" aria-current="page" href="/adminticket">
              <span class="bi bi-card-list"></span> {{ t("requests") }}
            </a>
          </li>
          <li class="nav-item" v-if="isAuthenticated && userRole === 'Admin'">
            <a class="nav-link" aria-current="page" href="/product">
              <span class="bi bi-file-earmark-code"></span> {{ t("products") }}
            </a>
          </li>
          <li class="nav-item" v-if="isAuthenticated">
            <a class="nav-link" aria-current="page" href="/communication">
              <span  class="bi bi-chat-right-dots"></span> {{ t("communication") }}
            </a>
          </li>
          <li class="nav-item" v-if="isAuthenticated && userRole === 'Admin'">
            <a class="nav-link" aria-current="page" href="/feedback">
              <span  class="bi bi-chat-right-dots"></span> {{ t("feedback") }}
            </a>
          </li>
          <li class="nav-item" v-if="isAuthenticated && userRole === 'Admin'">
            <a class="nav-link" aria-current="page" href="/firm">
              <span class="bi bi-people"></span> {{ t("firmName") }}
            </a>
          </li>
          <li class="nav-item" v-if="isAuthenticated && userRole === 'Admin'">
            <a class="nav-link" aria-current="page" href="/register">
              <span class="bi bi-person-plus"></span> {{ t("create_account") }}
            </a>
          </li>
          <li class="nav-item dropdown cursor-pointer">
            <a
              class="nav-link dropdown-"
              href="#"
              id="languageDropdown"
              role="button"
              data-bs-toggle="dropdown"
              aria-expanded="false"
            >
              {{ currentLanguage }}
            </a>
            <ul class="dropdown-menu" aria-labelledby="languageDropdown">
              <li>
                <a class="dropdown-item" @click="changeLanguage('tr')">Türkçe</a>
              </li>
              <li>
                <a class="dropdown-item" @click="changeLanguage('en')">İngilizce</a>
              </li>
            </ul>
          </li>
          <li class="nav-item" v-if="isAuthenticated">
            <a class="nav-link" aria-current="page" href="/profile">
              <span class="bi bi-person"></span> {{ t("")  }}
            </a>
          </li>
          <li class="nav-item">
            <router-link
              to="/sign-in"
              class="nav-link btn btn-outline-success"
              type="submit"
            >
              <i class="bi bi-box-arrow-in-right"></i>
              <span class="d-lg-none d-inline">{{ t("sign_in") }}</span>
            </router-link>
          </li>
          <li class="nav-item" v-if="isAuthenticated">
            <button
              class="nav-link btn btn-outline-danger"
              type="button"
              @click.prevent="handleLogout"
            >
              <i class="bi bi-box-arrow-right"></i>
              <span class="d-lg-none d-inline">{{ t("sign_out") }}</span>
            </button>
          </li>
        </ul>
      </div>
    </div>
  </nav>

</template>

<script>
import { ref, computed, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useUserStore } from '../stores/UserStore.js';

export default {
  setup() {
    const { t, locale } = useI18n();
    const toasts = ref([]);
    const maxToasts = 3;
    let toastHistory = [];
    const toastDelay = 3000;
    const store = useUserStore();
    const router = useRouter();
    const isAuthenticated = ref();
    const userRole = ref();
    const currentLanguage = computed(() => locale.value.toUpperCase());

    const handleLogout = async () => {
      sessionStorage.clear();
      isAuthenticated.value = false;
      showToast("Çıkış yapılıyor.", "warning")
      setTimeout(() => {
        router.push("/sign-in");
      }, 2000);
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

    const changeLanguage = (lang) => {
      locale.value = lang;
    };

    onMounted(async () => {
      await store.fetchUserRole();
      isAuthenticated.value = store.isAuthenticated;
      userRole.value = store.userRole;
    });

    return {
      t,
      toasts,
      isAuthenticated,
      userRole,
      currentLanguage,
      changeLanguage,
      handleLogout,
    };
  },
};
</script>

<style scoped>
.navbar {
  background-color: #3a4856;
}

.nav-link {
  display: flex;
  align-items: center;
}

.nav-link i, .nav-link span.bi {
  margin-right: 12px; 
}

.nav-link i {
  transition: color 0.3s ease, transform 0.3s ease;
}

.nav-link:hover span {
  color: #f0f0f0;
  transform: scale(1.2);
  cursor: pointer;
}

.navbar-toggler {
  border: none;
}

.navbar-toggler:focus {
  outline: none;
  box-shadow: none;
}

.cursor-pointer {
  cursor: pointer;
}

.dropdown-item {
  cursor: pointer;
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
