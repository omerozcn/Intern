# Turkuvaz Talep Sistemi

Vue 3 istemcisi ve .NET 8 API'sinden oluşan rol tabanlı talep yönetim sistemi.

## Gereksinimler

- .NET 8 SDK
- Node.js 22.12 veya üzeri
- SQL Server / LocalDB
- Parola sıfırlama e-postaları için bir SMTP hesabı

## Güvenli yerel yapılandırma

Bağlantı dizesi, JWT imzalama anahtarı ve SMTP kimlik bilgileri izlenen ayar dosyalarına yazılmaz. Backend klasöründe User Secrets kullanın:

```powershell
cd TicketSystem/deneme2
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>"
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

### Docker ile (LocalDB gerektirmez)

```powershell
cp .env.example .env   # MSSQL_SA_PASSWORD ve JWT_SIGNING_KEY doldurun
docker compose up -d --build
```

SQL Server konteyneri sağlıklı duruma gelene kadar API başlatılmaz. API `http://localhost:5005` adresinde açılır ve migration'ları uygulayıp geliştirme hesaplarını oluşturur. Veri `mssql-data` adlı volume'de kalıcıdır.

Durdurmak için `docker compose down`, veriyi de silmek için `docker compose down -v`.

### Yerel olarak

API:

```powershell
cd TicketSystem/deneme2
dotnet restore
dotnet run --launch-profile http
```

Frontend başka bir terminalde:

```powershell
cd TicketVUeProject-main/TicketVUeProject-main
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

## Doğrulama

```powershell
cd TicketSystem/deneme2
dotnet build

cd ../../TicketVUeProject-main/TicketVUeProject-main
npm run lint
npm test
npm run build
```

API'yi Swagger üzerinden denemek için `dotnet run --launch-profile http` çalıştırıp `http://localhost:5005/swagger` adresini açın.

### Henüz kurulmamış olanlar

- Backend'de otomatik test projesi yok; `dotnet test` çalıştıracak bir hedef bulunmuyor.
- `@playwright/test` bağımlılığı kurulu ama `playwright.config.js` ve e2e senaryoları henüz yazılmadı, bu yüzden `npm run test:e2e` çalışmaz.
- CI yapılandırması (`.github/workflows`) yok.
