import PrimeVue from "primevue/config";
import "primeicons/primeicons.css";
import "primevue/resources/themes/lara-light-indigo/theme.css";


import Button from "primevue/button";


export const prime = (app) => {

  app.use(PrimeVue);

app.component("PvButton",Button)

};