<template>

  <div class="container">
    <div class="input-group mb-4">
      <select
          v-model="selectedProduct"
          @change="handleProductChange"
          class="form-select"
          aria-label="Select a product"
      >
        <option value="" disabled>{{ $t("ticket.select_product") }}</option>
        <option value="0">Yeni Ürün Talebi</option>
        <option v-for="product in products" :key="product.id" :value="product">
          {{ product.productName }}
        </option>
      </select>
    </div>

    <div v-if="newProductRequest || selectedProduct" class="mb-4">
      <form @submit.prevent="handleClick">
        <div class="form-floating">
          <textarea
              class="form-control"
              placeholder="{{ $t('ticket.enter_request') }}"
              id="floatingTextarea2"
              style="height: 100px"
              v-model="description"
          ></textarea>
          <label for="floatingTextarea2">{{ $t("ticket.your_request") }}</label>
          <div v-if="trimmedRequest.length < 30" class="character-limit">
            {{ trimmedRequest.length }} / 30
          </div>
        </div>

        <div class="d-grid gap-2 col-6 mx-auto mb-4">
          <button
              class="btn btn-primary"
              type="submit"
              :disabled="!selectedProduct || trimmedRequest.length < 30"
              style="
              background-color: rgb(100, 116, 139);
              border-color: rgb(100, 116, 139);
              margin-top: 30px;
            "
          >
            {{ $t("ticket.add_ticket") }}
          </button>
        </div>
      </form>
    </div>

    <hr/>
    <h1 class="mb-4">{{ $t("ticket.current_tickets") }}</h1>

    <h2 class="mb-4" v-if="newProductTickets.length">Yeni Ürün Talepleri</h2>
    <div class="mb-4" v-if="newProductTickets.length">
      <ol class="list-group list-group-numbered">
        <li
            v-for="ticket in newProductTickets"
            :key="ticket.id"
            class="list-group-item d-flex justify-content-between align-items-start"
            style="margin-bottom: 10px"
        >
          <div class="ms-2 me-auto text-break">
            <div class="fw-bold text-break">
              <strong>{{ $t("ticket.product") }}</strong
              >{{ "Yeni Ürün Talebi" }}
            </div>
            <div class="text-break">
              <strong>{{ $t("ticket.ticket") }}</strong
              >{{ ticket.description }}
            </div>
            <div>
              <strong>{{ $t("ticket.response") }}</strong>
              {{ ticket.answer || $t("ticket.no_response_yet") }}
            </div>
            <div>
              {{ $t("ticket.date") }}
              {{ ticket.date ? ticket.date.toDate().toLocaleString() : "N/A" }}
            </div>
          </div>
          <span
              :class="['badge rounded-pill', ticketStatusClass(ticket.status)]"
          >
            {{ $t(`status.${ticket.status}`) }}
          </span>
          <span
              v-if="ticket.status !== 'completed'"
              @click="editTicket(ticket)"
              class="badge bgen-warning mx-2"
          >
            <i class="bi bi-pencil"></i>
          </span>
          <span
              @click="handleDelete(ticket.id)"
              class="badge bg-danger mx-2"
              :disabled="ticket.status === 3"
          >
            <i class="bi bi-x cursor-pointer"></i>
          </span>
        </li>
      </ol>
    </div>

    <div
        class="modal fade"
        id="editModal"
        tabindex="-1"
        aria-labelledby="editModalLabel"
        aria-hidden="true"
    >
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="editModalLabel">Edit Ticket</h5>
            <button
                type="button"
                class="btn-close"
                data-bs-dismiss="modal"
                aria-label="Close"
            ></button>
          </div>
          <div class="modal-body">
            <textarea
                v-model="editRequest"
                class="form-control"
                placeholder="Edit your request"
            ></textarea>
            <label for="floatingTextarea2">{{
                $t("ticket.your_request")
              }}</label>
            <div v-if="trimmedRequest.length < 30" class="character-limit">
              {{ trimmedRequest.length }} / 30
            </div>
          </div>
          <div class="modal-footer">
            <button
                type="button"
                class="btn btn-secondary"
                data-bs-dismiss="modal"
            >
              Close
            </button>
            <button type="button" class="btn btn-primary" @click="submitEdit">
              Save changes
            </button>
          </div>
        </div>
      </div>
    </div>

    <h2 class="mb-4" v-if="existingProductTickets.length">
      Mevcut Ürün Talepleri
    </h2>
    <div class="mb-4" v-if="existingProductTickets.length">
      <ol class="list-group list-group-numbered">
        <li
            v-for="ticket in existingProductTickets"
            :key="ticket.id"
            class="list-group-item d-flex justify-content-between align-items-start"
            style="margin-bottom: 10px"
        >
          <div class="ms-2 me-auto text-break">
            <div class="fw-bold text-break">
              <strong>{{ $t("product") }}: </strong> {{ ticket.productName }}
            </div>
            <div class="fw-bold text-break">
              <strong>{{ $t("ticket.ticket") }}: </strong> {{ ticket.description }}
            </div>
            <div>
              <strong>{{ $t("ticket.response") }}</strong>
              {{ ticket.answer || $t("ticket.no_response_yet") }}
            </div>
            <div>
              {{ $t("ticket.date") }}
              {{ ticket.date ? ticket.date.toDate().toLocaleString() : "N/A" }}
            </div>
          </div>
          <span
              :class="['badge rounded-pill', ticketStatusClass(ticket.status)]"
          >
            {{ $t(`status.${ticket.status}`) }}
          </span>
          <span
              v-if="ticket.status !== 'completed'"
              @click="editTicket(ticket)"
              class="badge bgen-warning mx-2"
          >
            <i class="bi bi-pencil"></i>
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
  </div>
</template>

<script>
import {ref, computed, onMounted} from "vue";
import axios from "axios";

export default {
  setup() {
    const description = ref("");
    const tickets = ref([]);
    const products = ref([]);
    const selectedProduct = ref("");
    const selectedTicket = ref(null);
    const answer = ref("");
    const editRequest = ref("");
    const toasts = ref([]);
    const maxToasts = 3;
    let toastHistory = [];
    const toastDelay = 3000;
    const newProductRequest = ref(false);
    const newProductTickets = ref([]);
    const existingProductTickets = ref([]);
    const trimmedRequest = computed(() =>
        description.value.replace(/\s+/g, "").trim()
    );
    const trimmedEditRequest = computed(() =>
        editRequest.value.replace(/\s+/g, "").trim()
    );

    const handleProductChange = () => {
      newProductRequest.value = selectedProduct.value === "1";
    };

    const handleClick = async () => {
      let newProducts = false;
      if(selectedProduct.value === "0")
        newProducts = true;
      console.log(selectedProduct)
      const query = {
        description: description.value,
        newProduct: newProducts,
        createdBy: sessionStorage.getItem("firstName"),
        status: 1,
        firmName: sessionStorage.getItem("firmName"),
        productId: (selectedProduct.value.id - 1)
      }
      console.log(query.productId);
      try {
        const response = await axios.post('http://localhost:5005/api/Ticket/createTicket',
            query
        );

        selectedProduct.value = "";
        fetchTickets();
        newProductRequest.value = false;
        return response;
      } catch (error) {
        console.error('Error creating ticket: ', error);
      }
    };

    const fetchTickets = async () => {
      try {
        tickets.value = [];
        newProductTickets.value = [];
        existingProductTickets.value = [];

        const response = await axios.get(
            "http://localhost:5005/api/Ticket/listTicket"
        );
          tickets.value = response.data.map((ticket) => ({
            id: ticket.id,
            newProduct: ticket.newProduct,
            description: ticket.description,
            date: ticket.updated ? new Date(ticket.updated) : null,
            answer: ticket.answer,
            status: ticket.status || 1,
            firmName: ticket.firmName,
            productName: ticket.productName,
          }));

        if (tickets.value.length === 0 || "") {
          showToast("Hiç talep bulunamadı.", "info");
        } else {
          showToast("Talepler başarıyla yüklendi!", "success");
        }

        return tickets.value;
      } catch (error) {
        console.error("Error fetching tickets:", error);
      }
    };

    const fetchProducts = async () => {
      const firmname = sessionStorage.getItem("firmName");
      try {
        const productResponse = await axios.get(
            `http://localhost:5005/api/FirmProduct/listProductsByFirm/${firmname}`
        );
        const productsData = productResponse.data;

        products.value = productsData.map((product) => ({
          id: product.id,
          productName: product.productName,
        }));
        console.log(products.value);

        showToast("Ürünler başarıyla yüklendi.", "success");
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

    const handleDelete = async (id) => {
      await deleteDoc(doc(db, "tickets", id));
      fetchTickets();
    };

    const updateTicketStatus = async (ticket, newStatus) => {
      if (ticket.status !== "completed") {
        await updateDoc(doc(db, "tickets", ticket.id), {status: newStatus});
        fetchTickets();
      }
    };

    const editTicket = (ticket) => {
      if (ticket.status !== "completed") {
        selectedTicket.value = ticket;
        editRequest.value = ticket.description;
        const modalElement = document.getElementById("editModal");
        const modal = new bootstrap.Modal(modalElement);
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
          description: editRequest.value,
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
      await fetchTickets();
      await fetchProducts();
    });

    const ticketStatusClass = (status) => {
      return {
        "bg-warning": status === "pending",
        "bg-info": status === "inProgress",
        "bg-success": status === "completed",
      };
    };

    return {
      description,
      handleClick,
      tickets,
      products,
      selectedProduct,
      handleDelete,
      updateTicketStatus,
      selectedTicket,
      editTicket,
      fetchTickets,
      fetchProducts,
      editRequest,
      submitEdit,
      removeToast,
      showToast,
      ticketStatusClass,
      trimmedRequest,
      trimmedEditRequest,
      newProductRequest,
      handleProductChange,
      newProductTickets,
      existingProductTickets,
    };
  },
};
</script>

<style scoped>
.container {
  max-width: 1200px;
  margin: auto;
  padding: 20px;
  background-color: #f8f9fa;
  border-radius: 10px;
  box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
}

h1 {
  font-size: 2rem;
  font-weight: 600;
  color: #333;
}

input,
select,
textarea {
  border-radius: 5px;
}

.form-select,
.form-control {
  border: 1px solid #ced4da;
  border-radius: 5px;
}

.form-control:focus,
.form-select:focus {
  border-color: #007bff;
  box-shadow: 0 0 0 0.2rem rgba(38, 143, 255, 0.25);
}

.btn-primary {
  background-color: #007bff;
  border-color: #007bff;
  color: #fff;
  border-radius: 5px;
  transition: background-color 0.3s;
}

.btn-primary:hover {
  background-color: #0056b3;
}

.character-limit {
  font-size: 14px;
  color: #6c757d;
}

.modal-dialog {
  display: flex;
  align-items: center;
}

.modal-content {
  border-radius: 10px;
  overflow: hidden;
}

.modal-header {
  background-color: #3a4856;
  color: #fff;
}

.modal-body {
  padding: 20px;
}

.bg-warning {
  background-color: #ffc107 !important;
}

.bg-info {
  background-color: #17a2b8 !important;
}

.bg-success {
  background-color: #28a745 !important;
}

.badge {
  border-radius: 50px;
  padding: 0.5rem 1rem;
}

.badge.bg-warning {
  background-color: #ffc107;
}

.badge.bg-danger {
  background-color: #dc3545;
}

.bgen-warning {
  background-color: gray;
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
