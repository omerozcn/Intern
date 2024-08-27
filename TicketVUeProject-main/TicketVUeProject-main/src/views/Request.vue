<template>
  <div class="container">
    <div class="header">
      <h1 class="mb-4">{{ $t("ticket.current_tickets") }}</h1>
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

    <h2 class="mb-4" v-if="filteredTickets.length === 0">
      {{ $t("ticket.existing_product_requests") }}
    </h2>
    <div class="mb-4" v-if="filteredTickets.length > 0">
      <ol class="list-group list-group-numbered">
        <li
            v-for="ticket in filteredTickets"
            :key="ticket.id"
            class="list-group-item d-flex justify-content-between align-items-start mb-2"
        >
          <div class="ms-2 me-auto text-break">
            <div class="fw-bold text-break">
              <strong>{{ $t("product") }}: </strong> {{ ticket.productName }}
            </div>
            <div class="text-break">
              <strong>{{ $t("ticket.ticket") }}: </strong>{{ ticket.description }}
            </div>
            <div>
              <strong>{{ $t("ticket.response") }}: </strong>
              {{ ticket.answer || $t("ticket.no_response_yet") }}
            </div>
            <div>
              {{ $t("ticket.date") }}:
              {{
                ticket.created
                    ? ticket.created.toLocaleString()
                    : "N/A"
              }}
            </div>
          </div>
          <span
              :class="['badge rounded-pill', ticketStatusClass(ticket.status)]"
          >
            {{ ticketStatusText(ticket.status) }}
          </span>
          <span
              @click="handleDelete(ticket.id)"
              class="badge bg-danger mx-2"
              :disabled="ticket.status === 3"
          >
            <i class="bi bi-x"></i>
          </span>
        </li>
      </ol>
    </div>

<!--    <h2 class="mb-4" v-if="filteredTickets.length === null">-->
<!--      {{ $t("ticket.new_product_requests") }}-->
<!--    </h2>-->
<!--    <div class="mb-4" v-if="filteredTickets.length > 0">-->
<!--      <ol class="list-group list-group-numbered">-->
<!--        <li-->
<!--            v-for="ticket in filteredTickets"-->
<!--            :key="ticket.id"-->
<!--            class="list-group-item d-flex justify-content-between align-items-start mb-2"-->
<!--        >-->
<!--          <div class="ms-2 me-auto text-break">-->
<!--            <div class="fw-bold text-break">-->
<!--              <strong>{{ $t("product") }}: </strong> {{ ticket.productName }}-->
<!--            </div>-->
<!--            <div class="text-break">-->
<!--              <strong>{{ $t("ticket.request") }}: </strong>{{ ticket.description }}-->
<!--            </div>-->
<!--            <div>-->
<!--              <strong>{{ $t("ticket.response") }}: </strong>-->
<!--              {{ ticket.answer || $t("ticket.no_response_yet") }}-->
<!--            </div>-->
<!--            <div>-->
<!--              {{ $t("ticket.date") }}:-->
<!--              {{ ticket.date ? ticket.date.toLocaleString() : "N/A" }}-->
<!--            </div>-->
<!--          </div>-->
<!--          <span-->
<!--              :class="['badge rounded-pill', ticketStatusClass(ticket.status)]"-->
<!--          >-->
<!--            {{ $t(`status.${ticket.status}`) }}-->
<!--          </span>-->
<!--          <span-->
<!--              @click="handleDelete(ticket.id)"-->
<!--              class="badge bg-danger mx-2"-->
<!--              :disabled="ticket.status === 3"-->
<!--          >-->
<!--            <i class="bi bi-x"></i>-->
<!--          </span>-->
<!--        </li>-->
<!--      </ol>-->
<!--    </div>-->
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
import {ref, computed, onMounted} from "vue";
import {useI18n} from "vue-i18n";
import axios from "axios";

export default {
  setup() {
    const {t} = useI18n();
    const request = ref("");
    const tickets = ref([]);
    const products = ref([]);
    const toasts = ref([]);
    const filterBy = ref(null);
    const selectedProduct = ref("");
    const selectedTicket = ref(null);
    const response = ref("");
    const editRequest = ref("");
    const maxToasts = 3;
    const firmName = ref("");
    const trimmedRequest = computed(() =>
        request.value.replace(/\s+/g, "").trim()
    );
    const trimmedEditRequest = computed(() =>
        editRequest.value.replace(/\s+/g, "").trim()
    );
    const statusFilter = ref(null);
    let toastHistory = [];
    const toastDelay = 3000;

    const handleClick = async () => {
      if (
          User.value &&
          selectedProduct.value &&
          trimmedRequest.value.length >= 30
      ) {
        try {
          await axios.post("https://localhost:5005/api/Ticket/createTicket", {
            id: User.value.uid,
            request: request.value,
            date: new Date().toISOString(),
            status: 1,
            product: selectedProduct.value,
          });
          request.value = "";
          selectedProduct.value = "";
          fetchTickets();
        } catch (error) {
          console.error("Error adding ticket:", error);
        }
      }
    };

    const fetchTickets = async () => {
      console.log("ftftft")
      let usertoken = sessionStorage.getItem("token")
      try {
        const response = await axios.get(`http://localhost:5005/api/Ticket/listByUserId`, {
          headers: {
            Authorization: `Bearer ${usertoken}`
          }
        });
        console.log("response: ", response.data);

        tickets.value = response.data.map((ticket) => ({
          id: ticket.id,
          newProduct: ticket.newProduct,
          description: ticket.description || "No Description",
          date: ticket.updated ? new Date(ticket.updated) : null,
          answer: ticket.answer,
          status: ticket.status,
          created: ticket.created ? new Date(ticket.created) : null,
          createdBy: ticket.createdBy,
          firmName: ticket.firmName,
          productName: ticket.productName,
        }));

        tickets.value.sort((a, b) => {
          if (a.status !== b.status) {
            return a.status === 2 ? -1 : (b.status === 2 ? 1 : (
                a.status === 1 ? -1 : (b.status === 1 ? 1 : 0)));
          }
          if (a.date !== b.date) {
            return a.date > b.date ? -1 : 1;
          }
          if (a.newProduct !== b.newProduct) {
            return a.newProduct ? -1 : 1;
          }
          return 0;
        });

        filteredTickets.value = tickets.value;
        console.log("BİM: ", filteredTickets);

        if (tickets.value.length === 0) {
          showToast("Hiç talep bulunamadı.", "info");
        } else {
          showToast("Talepler başarıyla yüklendi!", "success");
        }
      } catch (error) {
        console.error("Talepler yüklenirken bir hata oluştu: ", error);
        showToast(
            "Talepler yüklenirken bir hata oluştu. Lütfen tekrar deneyin.",
            "error"
        );
        return [];
      }
    };

    const fetchProducts = async () => {
      try {
        const productResponse = await axios.get(
            "http://localhost:5005/api/Product/listProduct"
        );
        const productsData = productResponse.data;
        const productsMap = {};

        productsData.forEach((product) => {
          productsMap[product.id] = {
            id: product.id,
            name: product.name,
            birthDate: product.birthDate,
            birthDateFormatted: product.birthDateFormatted,
            Firms: [],
          };
        });

        const productsWithFirms = Object.values(productsMap);
        products.value = productsWithFirms;
      } catch (error) {
        console.error(
            "Veri çekme sırasında bir hata oluştu:",
            error.response?.data || error.message
        );
        showToast(
            "Veri çekme sırasında bir hata oluştu. Lütfen tekrar deneyin.",
            "error"
        );
      }
    };

    const handleDelete = async (ticket) => {
      console.log("Ticket: ", ticket);
      try {
        const response = await axios.delete(
            `http://localhost:5005/api/Ticket/deleteTicket/${ticket}`
        );
        await fetchProducts();
        showToast("Talep başarıyla silindi!", "success");
      } catch (error) {
        console.error(
            "Talep silinirken bir hata oluştu:",
            error.response?.data || error.message
        );
        showToast(
            "Talep silinirken bir hata oluştu. Lütfen tekrar deneyin.",
            "error"
        );
      }
    };

    const updateTicketStatus = async (ticket, newStatus) => {
      if (ticket.status !== "completed") {
        await updateDoc(doc(db, "tickets", ticket.id), {status: newStatus});
        fetchTickets();
      }
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
        await updateDoc(doc(db, "tickets", selectedTicket.value.id), {
          response: response.value,
          status: "completed",
        });
        fetchTickets();
        const modal = bootstrap.Modal.getInstance(
            document.getElementById("responseModal")
        );
        modal.hide();
        selectedTicket.value = null;
        response.value = "";
      }
    };

    const editTicket = (ticket) => {
      if (ticket.status !== "completed") {
        selectedTicket.value = ticket;
        editRequest.value = ticket.request;
        const modal = new bootstrap.Modal(document.getElementById("editModal"));
        modal.show();
      }
    };

    const submitEdit = async () => {
      if (
          selectedTicket.value &&
          trimmedEditRequest.value.length >= 30 &&
          selectedTicket.value.status !== "completed"
      ) {
        await updateDoc(doc(db, "tickets", selectedTicket.value.id), {
          request: editRequest.value,
        });
        fetchTickets();
        const modal = bootstrap.Modal.getInstance(
            document.getElementById("editModal")
        );
        modal.hide();
        selectedTicket.value = null;
        editRequest.value = "";
      }
    };

    const filteredTickets = computed(() =>
        filterBy.value
            ? tickets.value.filter(ticket => ticket.status === filterBy.value)
            : tickets.value
    );

    const filterTickets = (status) => {
      filterBy.value = status;
    };

    const newProductTickets = computed(() => {
      return tickets.value.filter((ticket) =>
          products.value.some((product) => product.id === ticket.product.id)
      );
    });

    // const existingProductTickets = computed(() => {
    //   return tickets.value.filter(
    //       (ticket) =>
    //           !products.value.some((product) => product.id === ticket.product.id)
    //   );
    // });

    // const filteredNewProductTickets = computed(() => {
    //   return filteredTickets.value.filter((ticket) =>
    //       newProductTickets.value.some((t) => t.id === ticket.id)
    //   );
    // });

    // const filteredExistingProductTickets = computed(() => {
    //   return filteredTickets.value.filter((ticket) =>
    //       existingProductTickets.value.some((t) => t.id === ticket.id)
    //   );
    // });

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

    onMounted(() => {
      fetchTickets();
      fetchProducts();
    });

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

    return {
      request,
      handleClick,
      tickets,
      toasts,
      products,
      selectedProduct,
      handleDelete,
      updateTicketStatus,
      showResponseModal,
      selectedTicket,
      response,
      submitResponse,
      removeToast,
      editTicket,
      editRequest,
      submitEdit,
      filterTickets,
      filteredTickets,
      ticketStatusClass,
      ticketStatusText,
      trimmedRequest,
      trimmedEditRequest,
      newProductTickets,
      // existingProductTickets,
      // filteredNewProductTickets,
      // filteredExistingProductTickets,
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
