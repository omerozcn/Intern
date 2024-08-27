import { createApp } from 'vue';
import App from './App.vue';
import router from './router';
import { getAuth, onAuthStateChanged } from 'firebase/auth';
import { initializeApp } from "firebase/app";
import { getFirestore, collection, getDocs } from 'firebase/firestore';
import { prime } from './prime/prime';
import { createPinia } from 'pinia';
import axios from 'axios';
import { createI18n } from "vue-i18n";
import en from "./i18n/en.json";
import tr from "./i18n/tr.json";



const app = createApp(App)
const pinia = createPinia()


prime(app)
const firebaseConfig = {
    apiKey: "AIzaSyCzw_qB-FEKH_bjLlTmAIjLMYJSB2NJyfI",
    authDomain: "vue-project-d235e.firebaseapp.com",
    projectId: "vue-project-d235e",
    storageBucket: "vue-project-d235e.appspot.com",
    messagingSenderId: "79097250000",
    appId: "1:79097250000:web:54f17e221357cf0f61ded5"
};

axios.interceptors.request.use(
    (config) => {
        const token = sessionStorage.getItem('token');
        if (token) {
            config.headers['Authorization'] = `Bearer ${token}`;
        }
        return config;
    },
    (error) => {
        if (error.response.status === 401) {
            sessionStorage.removeItem('token');
            window.location.href = '/sign-in';
        }
        return Promise.reject(error);
    }
);

axios.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response && error.response.status === 401) {
            sessionStorage.removeItem('token');
            router.push('/sign-in'); // Yönlendirme işlemi
        }
        return Promise.reject(error);
    }
);

const firebaseApp = initializeApp(firebaseConfig);
const db = getFirestore(firebaseApp);


onAuthStateChanged(getAuth(), (displayName) => {
    if (!app) {
       
    }
});

async function fetchData() {
    const querySnapshot = await getDocs(collection(db, 'your-collection-name'));
    querySnapshot.forEach((doc) => {
        console.log(`${doc.id} => ${doc.data()}`);
    });
}
//i18 MultiLanguage
const messages = {
    en,
    tr,
    
  };
const i18n = createI18n({
    locale: "tr",
    fallbackLocale: "tr",
    legacy: false,
    messages,
  });

app.use(pinia);
app.use(router);
app.use(i18n);
fetchData();
app.mount('#app');
export { db };