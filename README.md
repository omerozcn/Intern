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
cd TicketSystem
dotnet build TickettSystem.sln
dotnet test TickettSystem.sln

cd ../TicketVUeProject-main/TicketVUeProject-main
npm run lint
npm test
npm run build
npm run test:e2e
```

API'yi Swagger üzerinden denemek için `dotnet run --launch-profile http` çalıştırıp `http://localhost:5005/swagger` adresini açın.

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
