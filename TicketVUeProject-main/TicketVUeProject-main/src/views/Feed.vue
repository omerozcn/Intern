<template>
  <div class="forgot-password-container">
    <h2>{{ $t("forgotPassword.title") }}</h2>

    <p>
      <strong>{{ $t("forgotPassword.email") }}:</strong> {{ User.email }}
    </p>

    <div class="password-change">
      <div class="input-group mb-3">
        <input
            type="password"
            class="form-control"
            :placeholder="$t('forgotPassword.newPassword')"
            v-model="newPassword"
        />
      </div>

      <button @click="changePassword" class="btn btn-primary">
        {{ $t("forgotPassword.updatePassword") }}
      </button>
      <p v-if="passwordMessage" class="password-message">
        {{ passwordMessage }}
      </p>
    </div>
  </div>
</template>

<script>
export default {
  data() {
    return {
      user: {
        email: "Kullanıcı Mail", // mail
      },
      newPassword: "",
      passwordMessage: "",
    };
  },
  methods: {
    changePassword() {
      if (this.newPassword) {
        this.passwordMessage = this.$t("forgotPassword.passwordUpdated");
      } else {
        this.passwordMessage = this.$t("forgotPassword.passwordMismatch");
      }
    },
  },
};
</script>

<style scoped>
.forgot-password-container {
  max-width: 400px;
  margin: 0 auto;
  padding: 20px;
  border: 1px solid #ccc;
  border-radius: 8px;
}

.input-group.mb-3 {
  margin-bottom: 15px;
}

button {
  width: 100%;
  padding: 10px;
  background-color: #007bff;
  color: #fff;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

button:hover {
  background-color: #0056b3;
}

.password-message {
  margin-top: 10px;
  color: red;
}
</style>
