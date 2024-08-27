import { defineStore } from 'pinia';
import axios from 'axios';

export const useUserStore = defineStore('user', {
  state: () => ({
    isAuthenticated: null,
    userRole: null,
  }),
  actions: {
    async fetchUserRole() {
      const token = sessionStorage.getItem('token');
      if (token) {
        try {
          const response = await axios.post('http://localhost:5005/api/Account/getuserRole', { token: token });
          this.userRole = response.data;
          this.isAuthenticated = true;
          console.log("Role:", this.userRole);
          console.log("Auth :", this.isAuthenticated);
        } catch (error) {
          console.error('Error fetching user role:', error);
          this.isAuthenticated = false;
        }
      } else {
        this.isAuthenticated = false;
      }
    },
  },
});
