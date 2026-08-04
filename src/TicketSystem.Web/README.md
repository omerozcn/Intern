# TicketSystem.Web

Turkuvaz Talep Sistemi'nin Vue 3 istemcisi. Kurulum, ortam değişkenleri ve
sistemin tamamına dair belgeler depo kökündeki [README](../../README.md)
dosyasındadır.

## Komutlar

| Komut | Ne yapar |
| --- | --- |
| `npm ci` | Bağımlılıkları `package-lock.json`'a birebir sadık kurar |
| `npm run dev` | Geliştirme sunucusunu açar (`http://localhost:5173`) |
| `npm run build` | Üretim paketini `dist/` altına üretir |
| `npm run preview` | Üretilmiş paketi yerelde sunar |
| `npm run lint` | ESLint |
| `npm test` | Vitest birim testleri (tek seferlik) |
| `npm run test:watch` | Vitest izleme kipinde |
| `npm run test:e2e` | Playwright uçtan uca testleri; API ve istemciyi kendisi başlatır |

## Yapı

| Klasör | İçerik |
| --- | --- |
| `src/views/` | Rota bileşenleri (`*View.vue`) |
| `src/components/` | Paylaşılan bileşenler |
| `src/composables/` | Yeniden kullanılabilir kompozisyon fonksiyonları |
| `src/services/` | `api` istemcisi |
| `src/stores/` | Pinia store'ları |
| `src/i18n/` | Türkçe/İngilizce çeviriler |
| `e2e/` | Playwright senaryoları (Vitest bunları yüklemez) |

`/api` istekleri geliştirmede Vite proxy'si üzerinden backend'e gider; hedefi
`.env.example` içindeki `VITE_API_PROXY_TARGET` belirler.
