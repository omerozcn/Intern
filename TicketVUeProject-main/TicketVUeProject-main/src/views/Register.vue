<template>
  <main class="form-register m-auto">
    <div class="container">
      <div class="row mb-5">
        <div class="col-md-6">
          <div
              class="register-form p-4 rounded"
              style="
              background-color: rgba(var(--bs-light-rgb), var(--bs-bg-opacity));
              --bs-bg-opacity: 1;
            "
          >
            <h2 class="section-title mb-4">{{ $t("createAccount") }}</h2>
            <div class="input-group mb-3">
              <input
                  type="text"
                  class="form-control"
                  :placeholder="$t('firstName')"
                  v-model="firstName"
              />
            </div>
            <div class="input-group mb-3">
              <input
                  type="text"
                  class="form-control"
                  :placeholder="$t('lastName')"
                  v-model="lastName"
              />
            </div>
            <div class="input-group mb-3">
              <input
                  type="email"
                  class="form-control"
                  :placeholder="$t('email')"
                  v-model="email"
              />
            </div>
            <div class="input-group mb-3">
              <input
                  type="password"
                  class="form-control"
                  :placeholder="$t('password')"
                  v-model="password"
              />
            </div>
            <div class="input-group mb-3">
              <select
                  class="form-control"
                  v-model="selectedFirm"
                  :disabled="role === 'Admin'"
              >
                <option value="0" disabled>{{ $t("selectFirm") }}</option>
                <option v-for="firm in firms" :key="firm.id" :value="firm.id" :disabled="firm.firmName === 'TURKUVAZ'">
                  {{ firm.firmName }}
                </option>
              </select>
            </div>
            <div class="input-group mb-3">
              <select class="form-control" v-model="role">
                <option value="User">{{ $t("User") }}</option>
                <option value="Admin">{{ $t("Admin") }}</option>
              </select>
            </div>
            <PvButton
                :label="$t('createAccount')"
                icon="pi pi-User"
                severity="secondary"
                class="rounded w-30"
                @click="register"
            />
          </div>
        </div>
        <div class="col-md-6">
          <div class="Admin-list p-4 rounded bg-light">
            <h2 class="section-title mb-4">{{ $t("AdminList") }}</h2>
            <table class="table">
              <thead>
              <tr>
                <th scope="col">{{ $t("firstName") }}</th>
                <th scope="col">{{ $t("lastName") }}</th>
                <th scope="col">{{ $t("email") }}</th>
                <th scope="col">{{ $t("role") }}</th>
                <th scope="col">{{ $t("action") }}</th>
              </tr>
              </thead>
              <tbody>
              <tr v-for="Admin in Admins" :key="Admin.id">
                <td>{{ Admin.firstName }}</td>
                <td>{{ Admin.lastName }}</td>
                <td>{{ Admin.email }}</td>
                <td>{{ Admin.role }}</td>
                <td>
                  <button
                      @click="editUser(Admin)"
                      class="btn btn-warning btn-sm mx-2"
                      style="
                        background-color: #64748b;
                        color: white;
                        border-color: #64748b;
                      "
                  >
                    <span class="bi bi-gear-fill"></span>
                  </button>
                  <button
                      @click="deleteUser(Admin)"
                      class="btn btn-danger btn-sm"
                  >
                    <i class="bi bi-x"></i>
                  </button>
                </td>
              </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
      <div class="btn-group mb-3">
        <button
            @click="clearFilter"
            class="btn btn-outline-secondary"
            style="background-color: #64748b; border-color: #64748b; color: white"
        >
          {{ $t("allUsers") }}
        </button>
        <button
            type="button"
            class="btn btn-danger dropdown-toggle"
            data-bs-toggle="dropdown"
            aria-expanded="false"
            style="background-color: #64748b; border-color: #64748b"
        >
          {{ $t("filterFirms") }}
        </button>
        <ul class="dropdown-menu">
          <li v-for="firm in firms" :key="firm.firmName">
            <button class="dropdown-item" @click="filterByFirm(firm.firmName)">
              {{ firm.firmName }}
            </button>
          </li>
        </ul>
      </div>
      <div class="User-list p-4 rounded bg-light">
        <h2 class="section-title mb-4">{{ $t("UserList") }}</h2>
        <table class="table table-bordered">
          <thead>
          <tr>
            <th scope="col">{{ $t("firstName") }}</th>
            <th scope="col">{{ $t("lastName") }}</th>
            <th scope="col">{{ $t("email") }}</th>
            <th scope="col">{{ $t("firmName") }}</th>
            <th scope="col">{{ $t("role") }}</th>
            <th scope="col">{{ $t("action") }}</th>
          </tr>
          </thead>
          <tbody>
          <tr v-for="User in filteredUsers" :key="User.id">
            <td>{{ User.firstName }}</td>
            <td>{{ User.lastName }}</td>
            <td>{{ User.email }}</td>
            <td>{{ User.firmName }}</td>
            <td>{{ User.role }}</td>
            <td>
              <button
                  @click="editUser(User)"
                  class="btn btn-warning btn-sm mx-2"
                  style="
                    background-color: #64748b;
                    color: white;
                    border-color: #64748b;
                  "
              >
                <span class="bi bi-gear-fill"></span>
              </button>
              <button @click="deleteUser(User)" class="btn btn-danger btn-sm">
                <i class="bi bi-x"></i>
              </button>
            </td>
          </tr>
          </tbody>
        </table>
      </div>
    </div>

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

    <div
        v-if="isEditModalVisible"
        class="modal fade show"
        tabindex="-1"
        style="display: block"
    >
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t("edit") }}</h5>
            <button
                type="button"
                class="btn-close"
                @click="closeEditModal"
            ></button>
          </div>
          <div class="modal-body">
            <div class="input-group mb-3">
              <input
                  type="text"
                  class="form-control"
                  :placeholder="$t('firstName')"
                  v-model="editName"
              />
            </div>
            <div class="input-group mb-3">
              <input
                  type="text"
                  class="form-control"
                  :placeholder="$t('lastName')"
                  v-model="editlastName"
              />
            </div>
            <div class="input-group mb-3">
              <input
                  type="email"
                  class="form-control"
                  :placeholder="$t('email')"
                  v-model="editEmail"
              />
            </div>
            <div class="input-group mb-3">
              <select class="form-control" v-model="editRole">
                <option value="" disabled>{{ $t("selectRole") }}</option>
                <option value="User">{{ $t("User") }}</option>
                <option value="Admin">{{ $t("Admin") }}</option>
              </select>
            </div>
            <button @click="submitEdit" class="btn btn-primary w-100">
              {{ $t("update") }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </main>
</template>

<script setup>
import {ref, onMounted} from "vue";
import {useRouter} from "vue-router";
import axios from "axios";

const router = useRouter();

const userName = ref("");
const firstName = ref("");
const lastName = ref("");
const email = ref("");
const password = ref("");
const firms = ref([]);
const selectedFirm = ref(0);
const role = ref("User");
const Users = ref([]);
const filteredUsers = ref([]);
const Admins = ref([]);
const editName = ref("");
const editlastName = ref("");
const editEmail = ref("");
const editRole = ref("");
const editUserId = ref(null);
const deleteUserId = ref(null);
const isEditModalVisible = ref(false);
const firmNameMap = ref({});
const toasts = ref([]);
const maxToasts = 3;
const firmId = ref(null);
let toastHistory = [];
const toastDelay = 3000;

const fetchFirms = async () => {
  try {
    const response = await axios.get("http://localhost:5005/api/Firm/listFirm");
    const allFirms = response.data.map((firm) => ({
      id: firm.id,
      firmName: firm.name,
    }));

    firms.value = allFirms.filter(firm => firm.firmName !== 'TURKUVAZ');

    firmNameMap.value = allFirms.reduce((map, firm) => {
      map[firm.id] = firm.firmName;
      return map;
    }, {});
  } catch (error) {
    console.error("Error fetching firms:", error);
    showToast(
        "Firmalar yüklenirken bir hata oluştu. Lütfen tekrar deneyin.",
        "error"
    );
  }
};

const fetchUsers = async () => {
  try {
    const response = await axios.get(
        "http://localhost:5005/api/account/listUsers"
    );

    const UserDataList = response.data.map((User) => ({
      Id: User.id,
      userName: User.userName,
      firstName: User.firstName,
      lastName: User.lastName,
      email: User.email,
      role: User.role,
      firmName: User.firmName,
    }));
    Admins.value = [];
    Users.value = [];

    UserDataList.forEach((user) => {
      if (user.role === "Admin") {
        Admins.value.push(user);
      } else if (user.role === "User") {
        Users.value.push(user);
      }
    });

    Admins.value.sort((a, b) => a.firstName.localeCompare(b.firstName));
    Users.value.sort((a, b) => a.firstName.localeCompare(b.firstName));

    showToast("Kullanıcılar başarıyla yüklendi!", "success");
    return {Admins: Admins.value, Users: Users.value};
  } catch (error) {
    console.error("Error fetching Users:", error);
    showToast(
        "Kullanıcılar yüklenirken bir hata oluştu. Lütfen tekrar deneyin.",
        "error"
    );
    throw error;
  }
};

const register = async () => {
  if (
      !firstName.value ||
      !lastName.value ||
      !email.value ||
      !password.value ||
      !role.value
  ) {
    showToast("Lütfen tüm alanları doldurduğunuzdan emin olun.", "warning");
    return;
  }

  if (role.value === "User" && selectedFirm.value === 99) {
    showToast("Kullanıcı rolüyle Turkuvaz firmasını seçemezsiniz.", "warning");
    return;
  }

  if(role.value === "Admin") {
    selectedFirm.value = 99;
  }
  const registerModel = {
    firstName: firstName.value,
    lastName: lastName.value,
    email: email.value,
    password: password.value,
    role: role.value,
    firmId: selectedFirm.value,
  };

  try {
    const response = await axios.post(
        "http://localhost:5005/api/account/register",
        registerModel
    );
    if (response.status == 200 && response.status <= 300) {
      showToast("Kayıt başarılı!", "success");
      firstName.value = "";
      lastName.value = "";
      email.value = "";
      password.value = "";
    }
    setTimeout(async () => {
      await fetchFirms();
    }, 3000);
  } catch (error) {
    console.error("Error during registration:", error);
    showToast(
        "Kayıt sırasında bir hata oluştu. Lütfen tekrar deneyin.",
        "error"
    );
  }
  await fetchUsers();
};

const editUser = (user) => {
  editUserId.value = user.Id;
  editName.value = user.firstName;
  editlastName.value = user.lastName;
  editEmail.value = user.email;
  editRole.value = user.role;
  console.log(editName.value);
  isEditModalVisible.value = true;
};

const submitEdit = async () => {
  try {
    const response = await axios.put(
        `http://localhost:5005/api/account/updateAccount/${editUserId.value}`,
        {
          id: editUserId.value,
          firstName: editName.value,
          lastName: editlastName.value,
          email: editEmail.value,
          role: editRole.value,
        },
        {
          headers: {
            "Content-Type": "application/json",
          },
        }
    );
    console.log(response.data);
    if (response.status >= 200 && response.status < 300) {
      showToast("Kullanıcı başarıyla güncellendi!", "success");
      isEditModalVisible.value = false;
      await fetchUsers();
    } else {
      throw new Error("Kullanıcı güncellenirken bir hata oluştu");
    }
  } catch (error) {
    console.error("Error updating User:", error);
    showToast(
        "Kullanıcı güncellenirken bir hata oluştu. Lütfen tekrar deneyin.",
        "error"
    );
  }
};

const deleteUser = async (user) => {
  try {
    const response = await axios.delete(
        `http://localhost:5005/api/account/deleteUser/${user.Id}`
    );
    setTimeout( async () => await fetchFirms(), 2000);
    showToast("Kullanıcı başarıyla silindi!", "success");
  } catch (error) {
    console.error("Error deleting User:", error);
    showToast(
        "Kullanıcı silinirken bir hata oluştu. Lütfen tekrar deneyin.",
        "error"
    );
  }
};

const filterByFirm = (firmName) => {
  filteredUsers.value = Users.value.filter(
      (User) => User.firmName === firmName
  );
  showToast(`Firmaya göre filtre uygulandı: ${firmName}`, "success");
};

const clearFilter = () => {
  filteredUsers.value = Users.value;
};

const closeEditModal = () => {
  isEditModalVisible.value = false;
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
  await fetchFirms();
  await fetchUsers();
  clearFilter();
});
</script>

<style scoped>
.main {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.register-form,
.Admin-list {
  background: #fff;
  box-shadow: 0 0 15px rgba(0, 0, 0, 0.1);
  border-radius: 10px;
}

.input-group {
  margin-bottom: 1rem;
}

.section-title {
  font-size: 1.5rem;
  font-weight: bold;
  color: #333;
  margin-bottom: 1rem;
}

.btn {
  transition: background-color 0.3s ease;
}

.btn:hover {
  background-color: #0056b3;
}

.btnen-warning {
  background-color: white;
}

.table {
  margin-top: 1rem;
}

.table th,
.table td {
  vertical-align: middle;
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

.table th {
  background-color: #f8f9fa;
}

.modal-body .input-group {
  margin-bottom: 1rem;
}
</style>
