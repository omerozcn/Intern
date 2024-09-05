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
  <div class="container mt-5">
    <div class="input-group mb-3">
      <input
          v-model="newProduct"
          type="text"
          class="form-control"
          :placeholder="$t('service_name')"
      />
    </div>
    <div class="d-grid gap-2 col-6 mx-auto">
      <button
          @click="addProduct"
          class="btn btn-outline-secondary"
          type="button"
      >
        {{ $t("add_service") }}
      </button>
    </div>
    <hr/>
    <h1>{{ $t("products") }}</h1>
    <div class="mb-3">
      <ol class="list-group list-group-numbered">
        <li
            v-for="product in products"
            :key="product.id"
            class="list-group-item d-flex justify-content-between align-items-center"
        >
          <div class="ms-2 me-auto">
            <div v-if="!product.editing">{{ product.name }}</div>
            <div v-else>
              <input v-model="product.name" class="form-control"/>
            </div>
            <ul>
              <li v-for="(firm, index) in firmProductData" :key="index">
                {{ firm.firmName }}
                <button
                    @click="removeFirmFromProduct(firm.id)"
                    class="btn btn-sm btn-danger ms-2"
                >
                  <i class=""></i>
                </button>
              </li>
            </ul>
          </div>
          <div>
            <button
                type="button"
                class="btn btn-primary"
                @click="selectProduct(product)"
                data-bs-toggle="modal"
                data-bs-target="#exampleModal"
            >
              +
            </button>
            <button
                v-if="!product.editing"
                @click="startEditing(product)"
                class="btn btn-outline-secondary ms-2"
                style="background-color: #6c757d; color: white"
            >
              <i class="bi bi-gear-fill"></i>
            </button>
            <button
                v-else
                @click="saveEditing(product)"
                class="btn btn-success ms-2"
            >
              {{ $t("save") }}
            </button>
            <button @click="handleDelete(product)" class="btn btn-danger ms-2">
              <i class="bi bi-x"></i>
            </button>
          </div>
        </li>
      </ol>
    </div>

    <div
        class="modal fade"
        id="exampleModal"
        tabindex="-1"
        aria-labelledby="exampleModalLabel"
        aria-hidden="true"
    >
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="exampleModalLabel">
              {{ $t("add_firm") }}
            </h5>
            <button
                type="button"
                class="btn-close"
                data-bs-dismiss="modal"
                aria-label="Close"
            ></button>
          </div>
          <div class="modal-body">
            <select
                v-model="selectedFirm"
                class="form-select"
                aria-label="Select Firm"
            >
              <option disabled selected>{{ $t("select_firm") }}</option>
              <option v-for="firm in firms" :key="firm.id" :value="firm.id">
                {{ firm.firmName }}
              </option>
            </select>
          </div>
          <div class="modal-footer">
            <button
                type="button"
                class="btn btn-secondary"
                data-bs-dismiss="modal"
            >
              {{ $t("close") }}
            </button>
            <button type="button" class="btn btn-primary" @click="saveFirm">
              {{ $t("add_firm") }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import {ref, onMounted} from "vue";
import {useI18n} from "vue-i18n";
import axios from "axios";

export default {
  setup() {
    const {t} = useI18n();
    const newProduct = ref("");
    const products = ref([]);
    const editProductId = ref(null);
    const editProductName = ref();
    const firms = ref([]);
    const firmProductData = ref();
    const ProductId = ref(null);
    const firmNameMap = ref({});
    const selectedFirm = ref("");
    const selectedProduct = ref(null);
    const toasts = ref([]);
    const maxToasts = 3;
    let toastHistory = [];
    const toastDelay = 3000;

    const fetchProducts = async () => {
      try {
        const productResponse = await axios.get(
            "http://localhost:5005/api/Product/listProduct"
        );
        const productsData = productResponse.data;
        // const firmProductResponse = await axios.get(
        //   "http://localhost:5005/api/Firmproduct/listfirmProduct"
        // );
        // firmProductData.value = firmProductResponse.data;

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
        // firmProductData.value.forEach(({ productId, firmName }) => {
        //   if (productsMap[productId]) {
        //     productsMap[productId].Firms.push(firmName);
        //   }
        // });

        const productsWithFirms = Object.values(productsMap);
        products.value = productsWithFirms;
        showToast("Ürünler başarıyla yüklendi.", "success");
      } catch (error) {
        console.error(
            "Veri çekme sırasında bir hata oluştu:",
            error.response?.data || error.message
        );
        showToast(
            "Ürünler listeleirken bir hata oluştu. Lütfen tekrar deneyin.",
            "error"
        );
      }
    };

    const fetchFirms = async () => {
      try {
        const response = await axios.get(
            "http://localhost:5005/api/Firm/listFirm"
        );
        firms.value = response.data.map((firm) => ({
          id: firm.id,
          firmName: firm.name,
        }));
        firmNameMap.value = firms.value.reduce((map, firm) => {
          map[firm.id] = firm.firmName;
          return map;
        }, {});
        showToast("Firmalar başarıyla yüklendi!", "success"); // Başarı mesajı
      } catch (error) {
        console.error("Error fetching firms:", error);
        showToast("Firmalar yüklenirken bir hata oluştu.", "error"); // Hata mesajı
        throw error;
      }
    };

    const handleDelete = async (product) => {
      try {
        const response = await axios.delete(
            `http://localhost:5005/api/Product/deleteProduct/${product.id}`
        );
        await fetchProducts();
        showToast("Ürün başarıyla silindi!", "success");
      } catch (error) {
        console.error(
            "Ürün silinirken bir hata oluştu:",
            error.response?.data || error.message
        );
        showToast(
            "Ürün silinirken bir hata oluştu. Lütfen tekrar deneyin.",
            "error"
        );
      }
    };

    const addProduct = async () => {
      try {
        if (!newProduct.value || newProduct.value.trim() === "") {
          showToast("Ürün adı boş olamaz. Lütfen bir isim girin.", "warning");
          return;
        }
        const newProductData = {
          name: newProduct.value.trim(),
        };

        const existingProduct = products.value.find(
            (product) =>
                product.name.toLowerCase() === newProductData.name.toLowerCase()
        );

        if (existingProduct) {
          showToast("Bu isimde bir ürün zaten mevcut!", "error");
          return;
        }

        const response = await axios.post(
            "http://localhost:5005/api/Product/createProduct",
            newProductData
        );
        showToast("Ürün başarıyla oluşturuldu!", "success");
        await fetchProducts();
      } catch (error) {
        console.error(
            "Ürün oluşturulurken bir hata oluştu:",
            error.response?.data || error.message
        );
        showToast(
            "Ürün oluşturulurken bir hata oluştu. Lütfen tekrar deneyin.",
            "error"
        );
      }
    };

    const selectProduct = (product) => {
      selectedProduct.value = product.id;
    };

    const saveFirm = async () => {
      if (!selectedFirm.value || !selectedProduct.value) {
        console.error("Hem firma hem de ürün seçilmelidir");
        showToast("Hem firma hem de ürün seçilmelidir", "error");
        return;
      }

      const firmproductData = {
        FirmId: selectedFirm.value,
        ProductId: selectedProduct.value,
      };

      try {
        const response = await axios.post(
            "http://localhost:5005/api/Firmproduct/createfirmProduct",
            firmproductData
        );

        showToast("Firma başarıyla ürünle ilişkilendirildi!", "success");

        fetchProducts();
        fetchFirms();
      } catch (error) {
        console.error(
            "Firma ile ürün ilişkilendirilirken bir hata oluştu:",
            error
        );
        showToast(
            "Firma ile ürün ilişkilendirilirken bir hata oluştu.",
            "error"
        );
      }
    };

    const removeFirmFromProduct = async (firmProductId) => {
      if (!firmProductId) {
        console.error("Firma Ürün ID'si tanımlı değil");
        return;
      }

      try {
        await axios.delete(
            `http://localhost:5005/api/Firmproduct/deletefirmProduct/${firmProductId}`
        );
        fetchProducts();
        showToast("Firma ürün bağlantısı başarıyla silindi!", "success");
      } catch (error) {
        console.error(
            "Firma ürün bağlantısı silinirken bir hata oluştu:",
            error.response?.data.errors || error.message
        );
        showToast(
            "Firma ürün bağlantısı silinirken bir hata oluştu. Lütfen tekrar deneyin.",
            "error"
        );
      }
    };

    const startEditing = (product) => {
      editProductId.value = product.id;
      editProductName.value = product.name;

      product.editing = true;
    };

    const saveEditing = async (product) => {
      // Boş veri kontrolü
      if (!product.name.trim()) {
        showToast("Ürün adı boş olamaz.", "error");
        return; // İşlemi durdur
      }

      try {
        var response = await axios.put(
            `http://localhost:5005/api/Product/updateProduct/${product.id}`,
            {
              id: product.id,
              name: product.name,
            },
            {
              headers: {
                "Content-Type": "application/json",
              },
            }
        );
        product.editing = false;

        if (response.status >= 200 && response.status < 300) {
          await fetchProducts();
          showToast("Ürün başarıyla güncellendi!", "success");
        } else {
          product.editing = false;
          throw new Error("Ürün güncellenirken bir hata oluştu");
        }
      } catch (error) {
        product.editing = false;
        console.error("Ürün güncellenirken bir hata oluştu:", error);
        showToast(
            "Ürün güncellenirken bir hata oluştu. Lütfen tekrar deneyin.",
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

    onMounted(() => {
      fetchProducts();
      fetchFirms();
    });

    return {
      newProduct,
      products,
      firms,
      selectedFirm,
      addProduct,
      handleDelete,
      selectProduct,
      saveFirm,
      firmProductData,
      removeFirmFromProduct,
      startEditing,
      saveEditing,
      toasts,
      removeToast,
      showToast,
      t,
    };
  },
};
</script>

<style scoped>
.container {
  max-width: 800px;
  margin-top: 20px;
  background-color: #f8f9fa;
  padding: 20px;
  border-radius: 10px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

h1 {
  text-align: center;
  margin-bottom: 20px;
  font-size: 24px;
  font-weight: bold;
  color: #343a40;
}

.list-group-item {
  margin-bottom: 10px;
  border: 1px solid #ddd;
  border-radius: 5px;
}

.modal-body {
  margin-bottom: 20px;
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
