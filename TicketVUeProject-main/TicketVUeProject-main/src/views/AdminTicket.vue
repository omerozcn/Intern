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
  <div class="container">
    <div class="d-flex justify-content-between align-items-center mb-3">
      <span class="title">{{ $t("tickets") }}</span>
      <div class="filter-buttons">
        <button
            @click="filterTickets(1)"
            class="btn btn-warning"
            style="color: white"
        >
          {{ $t("status_1") }}
        </button>
        <button
            @click="filterTickets(2)"
            class="btn btn-info"
            style="color: white"
        >
          {{ $t("status_2") }}
        </button>
        <button @click="filterTickets(3)" class="btn btn-success">
          {{ $t("status_3") }}
        </button>
        <button @click="filterTickets(null)" class="btn btn-secondary">
          {{ $t("all") }}
        </button>
      </div>
    </div>

    <hr/>

    <div class="mb-3">
      <ol class="list-group list-group-numbered">
        <li
            v-for="ticket in filteredTickets"
            :key="ticket.id"
            class="list-group-item d-flex justify-content-between align-items-start"
        >
          <div class="ms-2 me-auto">
            <div class="fw-bold text-break">
              <strong>{{ $t("product") }}: </strong> {{ ticket.productName }}
            </div>
            <div class="fw-bold text-break">
              <strong>{{ $t("firmName") }}: </strong> {{ ticket.firmName }}
            </div>
            <div class="fw-bold text-break">
              <strong>{{ $t("Talep") }}: </strong> {{ ticket.description }}
            </div>
            <div class="fw-bold text-break">
              <strong>{{ $t("response") }}: </strong>
              {{ ticket.answer || $t("no_response") }}
            </div>
            <div class="fw-bold text-break">
              {{ $t("date") }}:
              {{ ticket.date ? ticket.date.toLocaleString() : "N/A" }}
            </div>
          </div>
          <div class="ms-auto d-flex align-items-center">
            <span
                :class="['badge rounded-pill', ticketStatusClass(ticket.status)]"
            >
              {{ ticketStatusText(ticket.status) }}
            </span>
            <div class="dropdown ms-2">
              <button
                  class="btn btn-secondary"
                  type="button"
                  data-bs-toggle="dropdown"
                  aria-expanded="false"
                  :disabled="ticket.status === 3"
              >
                <i class="bi bi-gear-fill"></i>
              </button>
              <ul class="dropdown-menu">
                <li>
                  <a
                      class="dropdown-item"
                      href="#"
                      @click="updateTicketStatus(ticket, 1)"
                  >
                    {{ $t("status_1") }}
                  </a>
                </li>
                <li>
                  <a
                      class="dropdown-item"
                      href="#"
                      @click="updateTicketStatus(ticket, 2)"
                  >
                    {{ $t("status_2") }}
                  </a>
                </li>
                <li>
                  <a
                      class="dropdown-item"
                      href="#"
                      @click="showResponseModal(ticket)"
                  >
                    {{ $t("status_3") }}
                  </a>
                </li>
              </ul>
            </div>
          </div>
        </li>
      </ol>
    </div>

    <div
        v-if="selectedTicket"
        class="modal fade"
        tabindex="-1"
        id="responseModal"
    >
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t("provide_response") }}</h5>
            <button
                type="button"
                class="btn-close"
                data-bs-dismiss="modal"
                aria-label="Close"
            ></button>
          </div>
          <div class="modal-body">
            <textarea
                v-model="ticketanswer"
                class="form-control"
                rows="3"
                placeholder="Enter your response"
            ></textarea>
          </div>
          <div class="modal-footer">
            <button
                type="button"
                class="btn btn-secondary"
                data-bs-dismiss="modal"
            >
              {{ $t("close") }}
            </button>
            <button
                type="button"
                class="btn btn-primary"
                @click="submitResponse"
            >
              {{ $t("complete_process") }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import {ref, onMounted, computed, nextTick} from "vue";
import axios from "axios";
import {useI18n} from "vue-i18n";

export default {
  setup() {
    const content = ref("");
    const tickets = ref([]);
    const filterBy = ref(null);
    const selectedTicket = ref(null);
    const ticketanswer = ref("");
    const toasts = ref([]);
    const newStatus = ref(null);
    const response = ref("");
    const maxToasts = 3;
    const {t} = useI18n();
    let toastHistory = [];
    const toastDelay = 3000;

    const fetchTickets = async () => {
      try {
        const response = await axios.get(
            "http://localhost:5005/api/Ticket/listTicket"
        );

        tickets.value = response.data.map((ticket) => ({
          id: ticket.id,
          newProduct: Boolean(ticket.newProduct),
          description: ticket.description || "No Description",
          date: ticket.created ? new Date(ticket.created) : null,
          answer: ticket.answer,
          status: ticket.status,
          firmName: ticket.firmName,
          productName: ticket.productName,
          createdBy: ticket.createdBy
        }));

        tickets.value.sort((a, b) => {
          if (a.status !== b.status) {
            return a.status === 1 ? -1 : (b.status === 1 ? 1 : (
                a.status === 2 ? -1 : (b.status === 2 ? 1 : 0)));
          }
          if (a.newProduct !== b.newProduct) {
            return a.newProduct ? -1 : 1;
          }
          if (a.date !== b.date) {
            return a.date > b.date ? -1 : 1;
          }
          return 0;
        });


        if (tickets.value.length === 0) {
          showToast("Hiç talep bulunamadı.", "info");
        } else {
          showToast("Talepler başarıyla yüklendi!", "success");
        }

        return tickets.value;
      } catch (error) {
        console.error("Talepler yüklenirken bir hata oluştu: ", error);
        showToast(
            "Talepler yüklenirken bir hata oluştu. Lütfen tekrar deneyin.",
            "error"
        );
        return [];
      }
    };

    const handleClick = async () => {
      if (User.value) {
        await addDoc(collection(db, "tickets"), {
          userName: User.value.displayName,
          content: content.value,
          firmName: "Company Name",
          product: "Product Name",
          date: serverTimestamp(),
          status: "pending",
          response: "",
        });
        fetchTickets();
      }
    };

    const handleDelete = async (id) => {
      try {
        const response = await axios.delete(
            `http://localhost:5005/api/Ticket/deleteTicket/${id}`
        );

        await fetchTickets();
      } catch (error) {
        console.error("Error deleting ticket:", error);
      }
    };

    const updateTicketStatus = async (ticket, newStatus) => {
      try {
        if (ticket.status !== 3) {
          const response = await axios.put(
              `http://localhost:5005/api/Ticket/updateStatus/${ticket.id}`,
              {
                status: newStatus,
              }
          );
          fetchTickets();
          showToast(
              newStatus === 3 ? t("ticket_completed") : t("ticket_updated"), "success"
          );
        }
      } catch (error) {
        console.error("Error updating ticket status:", error);
        showToast(t("ticket_update_failed"));
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

    const ticketStatusClass = (status) =>
        ({
          1: "bg-warning",
          2: "bg-info",
          3: "bg-success",
        }[status]);

    const ticketStatusText = (status) =>
        ({
          1: t("Beklemede"),
          2: t("İşlemde"),
          3: t("Tamamlandı"),
        }[status]);

    const filteredTickets = computed(() =>
        filterBy.value
            ? tickets.value.filter((ticket) => ticket.status === filterBy.value)
            : tickets.value
    );

    const filterTickets = (status) => {
      filterBy.value = status;
    };

    const showResponseModal = (ticket) => {
      selectedTicket.value = ticket;
      response.value = ticket.response || "";
      const modal = new bootstrap.Modal(
          document.getElementById("responseModal")
      );
      modal.show();
    };

    const submitResponse = async () => {
      if (selectedTicket.value) {
        try {
          const response = await axios.put(
              `http://localhost:5005/api/Ticket/updateTicket/${selectedTicket.value.id}`,
              {
                answer: ticketanswer.value,
                status: 3,
              }
          );

          await fetchTickets();

          const modal = bootstrap.Modal.getInstance(
              document.getElementById("responseModal")
          );
          if (modal) {
            modal.hide();
          } else {
            console.error("Modal instance not found.");
          }

          selectedTicket.value = null;
          response.value = "";

          showToast(t("ticket_completed"), "success");
        } catch (error) {
          console.error("Error updating ticket response:", error);
          showToast(t("ticket_update_failed"), "error");
        }
      }
    };

    onMounted(async () => {
      await fetchTickets();
    });

    return {
      content,
      handleClick,
      tickets,
      ticketanswer,
      handleDelete,
      updateTicketStatus,
      ticketStatusClass,
      ticketStatusText,
      filteredTickets,
      filterTickets,
      showResponseModal,
      selectedTicket,
      response,
      submitResponse,
      showToast,
      toasts,
      removeToast,
    };
  },
};
</script>

<style scoped>
.container {
  max-width: 1300px;
  padding: 20px;
  background-color: #f8f9fa;
  border-radius: 8px;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.title {
  font-size: 2rem;
  font-weight: bold;
  color: #333;
}

.filter-buttons {
  display: flex;
  gap: 0.5rem;
}

.filter-buttons .btn {
  border-radius: 50px;
  padding: 0.5rem 1rem;
}

.list-group-item {
  border: 1px solid #ddd;
  border-radius: 8px;
  margin-bottom: 10px;
}

.bg-warning {
  background-color: #ffc107 !important;
}

.bg-info {
  background-color: #56d9ed !important;
}

.bg-success {
  background-color: #14da42 !important;
}

.modal-dialog-centered {
  display: flex;
  align-items: center;
}

.modal-content {
  max-height: 80vh;
  overflow-y: auto;
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
