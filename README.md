# Turkuvaz Talep Sistemi

Vue 3 istemcisi ve .NET 8 API'sinden oluşan rol tabanlı talep yönetim sistemi. Kullanıcılar hizmet talebi açar ve kendi taleplerini takip eder; yöneticiler talepleri yanıtlar, firma/hizmet tanımlarını ve hesapları yönetir.

## Proje yapısı

```
TicketSystem.sln
├── src/
│   ├── TicketSystem.Api/          .NET 8 Web API (EF Core, ASP.NET Identity, JWT)
│   └── TicketSystem.Web/          Vue 3 SPA (Vite, Pinia, vue-router, vue-i18n)
├── tests/
│   └── TicketSystem.Api.Tests/    xUnit entegrasyon testleri (WebApplicationFactory)
├── .github/workflows/ci.yml       Backend, frontend ve uçtan uca işler
└── docker-compose.yml             SQL Server + API konteynerleri
```

API projesinin ad alanları `TicketSystem.*` olarak kalır; `.Api` soneki yalnızca derlemeyi çözümden ayırt eder ve `RootNamespace` ile açıkça sabitlenmiştir.

| API klasörü | İçerik |
| --- | --- |
| `Controllers/` | HTTP uçları |
| `Data/` | `ApplicationDbContext` ve geliştirme verisi tohumlama |
| `Dtos/` | İstek/yanıt tipleri |
| `Models/` | EF Core varlıkları |
| `Interfaces/` + `Repositories/` | Veri erişim sözleşmeleri ve uygulamaları |
| `Services/` | Token üretimi, e-posta, güvenlik damgası doğrulama |
| `Mappers/` | Varlık ↔ DTO dönüşümleri |
| `Configuration/` | Seçenek sınıfları (JWT, SMTP, CORS, hız sınırı) |
| `Security/` | Rol ve talep sabitleri |
| `Migrations/` | EF Core migration'ları |

## Gereksinimler

- .NET 8 SDK
- Node.js 22.12 veya üzeri
- SQL Server / LocalDB (ya da Docker)
- Parola sıfırlama e-postaları için bir SMTP hesabı

## Güvenli yerel yapılandırma

Bağlantı dizesi, JWT imzalama anahtarı ve SMTP kimlik bilgileri izlenen ayar dosyalarına yazılmaz. API klasöründe User Secrets kullanın:

> Aşağıdaki `<...>` ifadeleri **yer tutucudur, birebir yazılmamalıdır.** Örneğin
> `<connection-string>` metnini olduğu gibi kaydederseniz uygulama açılışta
> `Format of the initialization string does not conform to specification` hatasıyla durur.
>
> **Bağlantı dizesi satırı geliştirme için gerekli değildir:** `appsettings.Development.json`
> zaten bir LocalDB varsayılanı taşır. Yalnızca farklı bir sunucuya bağlanacaksanız verin.

```powershell
cd src/TicketSystem.Api
dotnet user-secrets set "Jwt:SigningKey" "<en-az-32-byte-rastgele-anahtar>"
dotnet user-secrets set "Smtp:Host" "<smtp-host>"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:UserName" "<smtp-user>"
dotnet user-secrets set "Smtp:Password" "<smtp-password>"
dotnet user-secrets set "Smtp:FromEmail" "<from-address>"
```

Dağıtım ortamında aynı anahtarları environment secret olarak verin; örneğin `Jwt__SigningKey` ve `ConnectionStrings__DefaultConnection`. Depoda daha önce kullanılmış SQL/JWT değerlerini ilgili dış sistemlerde ayrıca döndürün.

### Veritabanı bağlantısı

`appsettings.json` içindeki bağlantı dizesi **boştur** — depoda makineye özel hiçbir değer tutulmaz. Bağlantı çalıştığı ortamdan gelir:

| Ortam | Kaynak |
| --- | --- |
| Geliştirme (Windows) | `appsettings.Development.json` içindeki LocalDB varsayılanı, ya da User Secrets ile geçersiz kılınır |
| Docker | `docker-compose.yml` tarafından verilen `ConnectionStrings__DefaultConnection` |
| Dağıtım | `ConnectionStrings__DefaultConnection` environment değişkeni |

Bağlantı hiçbir kaynaktan gelmezse uygulama açılışta `ConnectionStrings__DefaultConnection must be configured.` hatasıyla durur.

## Çalıştırma

### Docker ile — tek komut (.NET SDK ve Node gerektirmez)

```bash
cp .env.example .env   # MSSQL_SA_PASSWORD ve JWT_SIGNING_KEY doldurun
```

```bash
docker compose up -d --build
```

Üç konteyner sırayla ayağa kalkar ve her biri bir sonrakini bekler:

| Servis | Adres | Rolü |
| --- | --- | --- |
| `web` | **http://localhost:5173** | nginx; derlenmiş SPA'yı sunar ve `/api` isteklerini API'ye yönlendirir |
| `api` | http://localhost:5005 · [Swagger](http://localhost:5005/swagger) | .NET 8 API; `/health` veritabanı bağlantısını da doğrular |
| `db` | localhost:14330 | SQL Server 2022; veri `mssql-data` volume'ünde kalıcı |

**Uygulamayı açmak için tek adres: http://localhost:5173** — SPA ve API aynı origin üzerinden konuştuğu için CORS devreye girmez. Veritabanı portu yalnızca SSMS/sqlcmd bağlamak için yayınlanır ve makinede kurulu bir SQL Server ile çakışmasın diye 1433 yerine 14330'dadır.

Durum: `docker compose ps` · Günlükler: `docker compose logs -f api` · Durdurmak: `docker compose down` · Veriyi de silmek: `docker compose down -v`

> **Tohumlama açık gelir.** `Seed__DevelopmentAccounts` bu yığında `true`'dur: migration'ları uygular ve aşağıdaki iki hesabı oluşturur. Erişilebilir bir ortama koyacaksanız `.env` içinde `SEED_DEVELOPMENT_ACCOUNTS=false` yapın (ve migration'ları ayrıca uygulayın) ya da en azından `SEED_ADMIN_PASSWORD` / `SEED_USER_PASSWORD` değerlerini değiştirin.

### Yerel olarak

API:

```powershell
cd src/TicketSystem.Api
dotnet restore
dotnet run --launch-profile http
```

Frontend başka bir terminalde:

```powershell
cd src/TicketSystem.Web
npm ci
npm run dev
```

Vite, `/api` isteklerini varsayılan olarak `http://localhost:5005` adresine yönlendirir. Farklı bir adres için `.env.example` dosyasını `.env.local` olarak kopyalayın.

## Geliştirme hesapları

`Development` ortamında uygulama açılışta veritabanını migrate eder ve iki test hesabı hazırlar:

| Rol | E-posta | Parola |
| --- | --- | --- |
| Admin | `admin.test@turkuvaz.local` | `AdminTest!2026` |
| User | `user.test@turkuvaz.local` | `UserTest!2026` |

Bu hesaplar yalnızca `Development` ortamında oluşturulur.

## API sözleşmesi

Kaynak adları çoğul ve küçük harflidir; işlem HTTP fiiliyle ifade edilir. Tüm liste uçları `page`, `pageSize` (en fazla 100) ve `search` sorgu parametrelerini kabul eder ve `{ items, page, pageSize, totalCount, totalPages }` döner.

### Kimlik doğrulama — `/api/auth`

| Uç | Erişim | Açıklama |
| --- | --- | --- |
| `POST /api/auth/login` | Herkes | Oturum açar, JWT döner (dakikada 5 deneme sınırı) |
| `POST /api/auth/logout` | Oturum | Güvenlik damgasını tazeleyip mevcut tokenları geçersizler |
| `GET /api/auth/me` | Oturum | Oturum açan hesabın profili |
| `POST /api/auth/forgot-password` | Herkes | Sıfırlama bağlantısı gönderir; hesap yoksa da aynı yanıtı verir |
| `POST /api/auth/reset-password` | Herkes | Token ile yeni parola belirler |
| `POST /api/auth/change-password` | Oturum | Mevcut parolayla değiştirir |

### Hesap yönetimi — `/api/users`

| Uç | Erişim |
| --- | --- |
| `GET /api/users` · `GET /api/users/{id}` | Admin |
| `POST /api/users` · `PUT /api/users/{id}` · `DELETE /api/users/{id}` | Admin |

### Talepler — `/api/tickets`

| Uç | Erişim | Açıklama |
| --- | --- | --- |
| `GET /api/tickets` | Admin | Tüm talepler |
| `GET /api/tickets/mine` | User | Kendi talepleri |
| `GET /api/tickets/{id}` | Admin, User | Yönetici hepsini, kullanıcı yalnızca kendi talebini görür |
| `POST /api/tickets` | User | Yeni talep |
| `PUT /api/tickets/{id}` | Admin | Yanıt ve durumu birlikte günceller |
| `PUT /api/tickets/{id}/status` | Admin | Yalnızca durum; tamamlamak için önceden yanıt yazılmış olmalı |
| `PUT /api/tickets/{id}/description` | User | Yalnızca `pending` durumundayken |
| `DELETE /api/tickets/{id}` | User | Yalnızca `pending` durumundayken |
| `GET /api/tickets/status-counts` | Admin, User | Duruma göre sayım; kullanıcı için kendi talepleri |

### Firmalar, hizmetler ve atamalar

| Uç | Erişim | Açıklama |
| --- | --- | --- |
| `GET/POST /api/firms`, `GET/PUT/DELETE /api/firms/{id}` | Admin | `TURKUVAZ` korumalı sistem firmasıdır |
| `GET/POST /api/products`, `PUT/DELETE /api/products/{id}` | Admin | Talep geçmişi olan hizmet silinemez |
| `GET /api/products/mine` | User | Kullanıcının firmasına atanmış hizmetler |
| `GET/POST /api/firm-products`, `DELETE /api/firm-products/{id}` | Admin | Firma–hizmet atamaları |
| `GET /api/feedback` | Admin | Geri bildirim kutusu |
| `POST /api/feedback` | User | Geri bildirim gönder |

Hata yanıtları `application/problem+json` biçimindedir. Benzersizlik ihlalleri 409, doğrulama hataları 400 döner.

## Doğrulama

```powershell
dotnet build TicketSystem.sln
dotnet test TicketSystem.sln

cd src/TicketSystem.Web
npm run lint
npm test
npm run build
npm run test:e2e
```

API'yi Swagger üzerinden denemek için `dotnet run --launch-profile http` çalıştırıp `http://localhost:5005/swagger` adresini açın.

> **Uçtan uca testler kendi sunucularını başlatır.** Playwright, 5173 ve 5005 portlarında çalışan bir şey bulursa onu yeniden kullanır — Docker yığını ayaktayken `npm run test:e2e` çalıştırırsanız testler o konteynerlere bağlanır ve giriş hız sınırına (dakikada 5) takılıp `429` alır; test paketi sekizden fazla kez giriş yapar. Uçtan uca testleri çalıştırmadan önce `docker compose down` deyin.

### Testler

| Katman | Komut | Kapsam |
| --- | --- | --- |
| Backend entegrasyon | `dotnet test` | Gerçek API'yi tek kullanımlık bir veritabanına karşı çalıştırır: kimlik doğrulama, rol izolasyonu, talep yaşam döngüsü, sayfalama ve doğrulama kuralları |
| Frontend birim | `npm test` | API istemcisi, auth store, ortak bileşenler, sayfalama composable'ı |
| Uçtan uca | `npm run test:e2e` | Playwright her iki sunucuyu da başlatıp gerçek tarayıcıda giriş ve talep akışlarını dener |

Backend testleri gerçek bir SQL Server'a ihtiyaç duyar; repository katmanı açık serializable transaction kullandığı için in-memory sağlayıcı yeterli değildir. Varsayılan olarak LocalDB kullanılır. Farklı bir sunucu için `TEST_DB_CONNECTION` ortam değişkenini `{DATABASE}` yer tutucusuyla verin:

```powershell
$env:TEST_DB_CONNECTION = "Server=localhost,1433;Database={DATABASE};User Id=sa;Password=<parola>;TrustServerCertificate=True"
```

### Sürekli entegrasyon

`.github/workflows/ci.yml` her push ve pull request'te backend, frontend ve uçtan uca testleri çalıştırır. SQL Server, GitHub Actions servis konteyneri olarak sağlanır. SA parolasını `CI_SQL_PASSWORD` deposu gizli değeriyle geçersiz kılabilirsiniz.

## Migration geçmişi notu

İlk migration'ın adı `deneme` iken `InitialCreate` olarak değiştirildi. Migration kimliği veritabanındaki `__EFMigrationsHistory` tablosunda saklandığı için, elinde bu değişiklikten **önce** oluşturulmuş bir veritabanı olanların şu güncellemeyi bir kez çalıştırması gerekir:

```sql
UPDATE __EFMigrationsHistory
   SET MigrationId = '20250511121219_InitialCreate'
 WHERE MigrationId = '20250511121219_deneme';
```

Çalıştırılmazsa EF ilk migration'ı uygulanmamış sayar ve dolu veritabanına yeniden uygulamaya çalışır. Sıfırdan oluşturulan veritabanları etkilenmez.
