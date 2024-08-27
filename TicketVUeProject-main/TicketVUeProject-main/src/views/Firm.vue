<template>
  <div class="container my-4">
    <div class="input-group mb-4">
      <div class="form-floating w-100">
        <textarea
            class="form-control"
            :placeholder="$t('firmName')"
            id="floatingTextarea2"
            v-model="newfirmName"
        ></textarea>
        <label for="floatingTextarea2">{{ $t("firmName") }}</label>
      </div>
    </div>
    <div class="d-grid gap-2 col-6 mx-auto mb-4">
      <button
          @click="handleFirmaClick"
          class="btn btn-primary"
          type="button"
          style="background-color: #64748b; border-color: #64748b"
      >
        {{ $t("add_firm") }}
      </button>
    </div>
    <hr/>
    <h1 class="my-4">{{ $t("Firms") }}</h1>
    <div class="mb-3">
      <ol class="list-group list-group-numbered">
        <li
            v-for="firm in firms"
            :key="firm.id"
            class="list-group-item d-flex justify-content-between align-items-start"
        >
          <div class="w-75">
            <div v-if="editFirmaId === firm.id && firm.firmName !== 'TURKUVAZ'">
              <input
                  v-model="editfirmName"
                  type="text"
                  class="form-control"
                  :placeholder="$t('new_firmName')"
              />
              <div class="mt-2">
                <button
                    @click="handleUpdateFirma(firm.id)"
                    class="btn btn-success btn-sm"
                >
                  {{ $t("update") }}
                </button>
                <button
                    @click="cancelEdit"
                    class="btn btn-secondary btn-sm ms-2"
                >
                  {{ $t("cancel") }}
                </button>
              </div>
            </div>
            <div v-else>
              <span>{{ firm.firmName }}</span>
              <button
                  @click="startEdit(firm)"
                  class="btn btnen-warning btn-sm ms-2"
              >
                <i class="bi bi-gear-fill"></i>
              </button>
              <div
                  v-if="firm.assignedusersIds && firm.assignedusersIds.length"
                  class="mt-2"
              >
                <strong>{{ $t("employee_name") }}:</strong>
                <ul>
                  <li v-for="UsersId in firm.assignedusersIds" :key="UsersId">
                    {{ getusers(UsersId).name }}
                    {{ getusers(UsersId).lastName }}
                    <button
                        @click="removeusersFromFirma(firm, UsersId)"
                        class="btn btn-sm btn-danger ms-2"
                    >
                      <i class=""></i>
                    </button>
                  </li>
                </ul>
              </div>
            </div>
          </div>
          <div>
            <button @click="handleDelete(firm)"
                    class="btn btn-danger ms-2"
                    :disabled="firm.firmName === 'TURKUVAZ'">
              <i class="bi bi-x"></i>

            </button>
          </div>
        </li>
      </ol>
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
  </div>
</template>

<script>
import {ref, onMounted, computed} from "vue";

import {useI18n} from "vue-i18n";
import axios from "axios";

export default {
  setup() {
    const newfirmName = ref("");
    const firms = ref([]);
    const Users = ref([]);
    const currentusers = ref(null);
    const deleteFirmId = ref(null);
    const editFirmaId = ref(null);
    const editfirmName = ref("");
    const firmNameMap = ref({});
    const toasts = ref([]);
    const maxToasts = 3;
    let toastHistory = [];
    const toastDelay = 3000;
    const {t} = useI18n();

    const fetchFirms = async () => {
      try {
        const response = await axios.get(
            "http://localhost:5005/api/Firm/listFirm"
        );
        firms.value = response.data.map((firm) => ({
          id: firm.id,
          firmName: firm.name,
        }));

        firms.value.sort((a, b) => {
          if (a.firmName < b.firmName) return -1;
          if (a.firmName > b.firmName) return 1;
          return 0;
        });

        firmNameMap.value = firms.value.reduce((map, firm) => {
          map[firm.id] = firm.firmName;
          return map;
        }, {});
        showToast("Firmalar başarıyla listelendi.", "success");
      } catch (error) {
        console.error("Error fetching firms:", error);
        showToast("Firmaları getirirken bir hata oluştu.", "error");
      }
    };

    const handleFirmaClick = async () => {
      if (!newfirmName.value.trim()) {
        showToast("Firma adı boş olamaz.", "warning");
        return;
      }

      try {
        const newFirmData = {name: newfirmName.value.trim()};

        const existingFirm = firms.value.find(
            (firm) =>
                firm.firmName.toLowerCase() === newFirmData.name.toLowerCase()
        );

        if (existingFirm) {
          showToast("Bu isimde bir firma zaten mevcut!", "error");
          newfirmName.value = "";
          return;
        }

        const response = await axios.post(
            "http://localhost:5005/api/Firm/createFirm",
            newFirmData
        );

        if (response.status >= 200 && response.status <= 300) {
          showToast("Firma başarıyla oluşturuldu!", "success");
          newfirmName.value = "";
          await fetchFirms();
        }
        return response;
      } catch (error) {
        console.error(error);
        showToast(
            "Firma oluşturulurken bir hata oluştu. Lütfen tekrar deneyin.",
            "error"
        );
      }
    };

    const handleDelete = async (firm) => {
      if (firm.firmName === 'TURKUVAZ') {
        showToast("Bu firma silinemiyor.", "warning");
        return;
      }
      try {
        await axios.delete(
            `http://localhost:5005/api/Firm/deleteFirm/${firm.id}`
        );
        showToast("Firma başarıyla silindi!", "success");
        await fetchFirms();
      } catch (error) {
        console.error("Error deleting firm:", error);
        showToast("Firma silinirken bir hata oluştu.", "error");
      }
    };

    const startEdit = (firm) => {
      if (firm.firmName === 'TURKUVAZ') return showToast("Bu firmanın ismini değiştiremezsiniz.", "warning");;
      editFirmaId.value = firm.id;
      editfirmName.value = firm.firmName;
    };

    const handleUpdateFirma = async () => {
      if (editfirmName.value === 'TURKUVAZ') {
        showToast("Bu firmanın ismini değiştiremezsiniz.", "warning");
        return;
      }
      try {
        const response = await axios.put(
            `http://localhost:5005/api/Firm/updateFirm/${editFirmaId.value}`,
            {id: editFirmaId.value, name: editfirmName.value},
            {headers: {"Content-Type": "application/json"}}
        );

        if (response.status >= 200 && response.status < 300) {
          showToast("Firma başarıyla güncellendi!", "success");
          cancelEdit();
          await fetchFirms();
        } else {
          throw new Error("Failed to update firm");
        }
      } catch (error) {
        showToast(
            "Firma güncellenirken bir hata oluştu. Lütfen tekrar deneyin.",
            "error"
        );
        console.error(error);
      }
    };
    const cancelEdit = () => {
      editFirmaId.value = null;
      editfirmName.value = "";
    };

    // const removeusersFromFirma = async (firma, UsersId) => {
    //   const firmaRef = doc(db, "firms", firma.id);
    //   const updatedAssignedusers = firma.assignedusersIds.filter(
    //     (id) => id !== UserId
    //   );
    //   await updateDoc(firmaRef, { assignedusersIds: updatedAssignedusers });
    //   showToast(t("employee_removed"));

    //   fetchFirms();
    // };

    const getusers = (UsersId) => {
      return Users.value.find((user) => user.id === UsersId) || {};
    };

    const filteredfirms = computed(() => {
      if (!currentusers.value) return firms.value;
      return firms.value.filter((firma) =>
          firma.assignedusersIds.includes(currentusers.value.id)
      );
    });

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
    });

    return {
      newfirmName,
      firms,
      Users,
      currentusers,
      handleFirmaClick,
      handleDelete,
      getusers,
      editFirmaId,
      editfirmName,
      startEdit,
      handleUpdateFirma,
      cancelEdit,
      filteredfirms,
      toasts,
      showToast,
      removeToast,
      removeToast: (index) => toasts.value.splice(index, 1),
    };
  },
};
</script>

<style scoped>
.container {
  max-width: 900px;
  margin: auto;
  padding: 20px;
  background-color: #f8f9fa;
  border-radius: 10px;
  box-shadow: 0 0 15px rgba(0, 0, 0, 0.1);
}

.input-group {
  border-radius: 10px;
  border: 1px solid #ced4da;
}

.form-floating label {
  color: #495057;
}

.form-control {
  border: 1px solid #ced4da;
  border-radius: 10px;
  transition: border-color 0.3s ease;
}

.form-control:focus {
  border-color: #00bcd4;
  box-shadow: 0 0 0 0.2rem rgba(0, 188, 212, 0.25);
}

.btn-primary {
  background-color: #00bcd4;
  border-color: #00bcd4;
  color: #ffffff;
  border-radius: 5px;
  transition: background-color 0.3s ease;
}

.btn-primary:hover {
  background-color: #00a2c5;
}

.btn-warning {
  background-color: #ffc107;
  border-color: #ffc107;
  color: black;
  border-radius: 5px;
  transition: background-color 0.3s ease;
}

.btn-warning:hover {
  background-color: #e0a800;
}

.btn-danger {
  background-color: #dc3545;
  border-color: #dc3545;
  border-radius: 5px;
  transition: background-color 0.3s ease;
}

.btn-danger:hover {
  background-color: #c82333;
}

.btn-secondary {
  background-color: #6c757d;
  border-color: #6c757d;
  border-radius: 5px;
  transition: background-color 0.3s ease;
}

.btn-secondary:hover {
  background-color: #5a6268;
}

.list-group-item {
  background-color: #ffffff;
  border: 1px solid #dee2e6;
  border-radius: 5px;
  margin-bottom: 10px;
  padding: 15px;
  box-shadow: 0 0 5px rgba(0, 0, 0, 0.1);
}

.list-group-item div {
  text-align: left;
}

h1 {
  font-size: 2rem;
  font-weight: 600;
  color: #343a40;
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

.btnen-warning {
  background-color: white;
}
</style>
