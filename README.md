# 🏛️ DotnetArchitecture

Kurumsal standartlarda geliştirilmiş, **Clean Architecture**, **Domain-Driven Design (DDD)** ve **CQRS** prensiplerini uygulayan modern bir **.NET 10** web API referans mimarisidir.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14.0-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4?logo=nuget&logoColor=white)](https://docs.microsoft.com/ef/core/)
[![MediatR](https://img.shields.io/badge/MediatR-CQRS-blue)](https://github.com/jbogard/MediatR)
[![Docker](https://img.shields.io/badge/Docker-MSSQL-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/_/microsoft-mssql-server)
[![Tests](https://img.shields.io/badge/Tests-25%20Passed-brightgreen?logo=xunit&logoColor=white)](https://github.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## 📑 İçindekiler
- [Mimari Genel Bakış](#-mimari-genel-bakış)
- [Katmanlar ve Teknolojiler](#-katmanlar-ve-kullanılan-teknolojiler)
- [Öne Çıkan Kurumsal Desenler](#-öne-çıkan-kurumsal-desenler)
  - [1. MediatR Pipeline Turnikeleri (Chain of Responsibility)](#1-mediatr-pipeline-turnikeleri-chain-of-responsibility)
  - [2. Transactional Outbox Pattern & Background Worker](#2-transactional-outbox-pattern--background-worker)
  - [3. Veritabanı Düzeyinde Sayfalama, Sıralama ve Filtreleme](#3-veritabanı-düzeyinde-sayfalama-sıralama-ve-filtreleme)
  - [4. JWT Kimlik Doğrulama & Rol Yetkilendirme (RBAC)](#4-jwt-kimlik-doğrulama--rol-yetkilendirme-rbac)
  - [5. Standart Hata Yönetimi (RFC 7807 / 9110 ProblemDetails)](#5-standart-hata-yönetimi-rfc-7807--9110-problemdetails)
- [🧪 Otomatik Test Mimarisi (Unit & Integration Tests)](#-otomatik-test-mimarisi-unit--integration-tests)
- [Proje Dizin Yapısı](#-proje-dizin-yapısı)
- [Kurulum ve Çalıştırma](#-kurulum-ve-çalıştırma)
- [API Test Senaryoları (.http Dosyası)](#-api-test-senaryoları-http-dosyası)

---

## 🧭 Mimari Genel Bakış

Proje, bağımlılıkların yalnızca içe doğru aktığı **Onion / Clean Architecture** prensibine dayanır. Domain katmanı en merkezdedir ve hiçbir dış kütüphaneye veya veritabanı aracına bağımlı değildir.

```mermaid
flowchart TD
    subgraph WebApi ["🌐 WebApi Katmanı"]
        Controllers["İnce Controller'lar"]
        Middlewares["Global Exception Handler"]
        BackgroundWorker["Outbox Background Worker"]
    end

    subgraph Application ["⚡ Application Katmanı (CQRS)"]
        Commands["Commands & Queries (MediatR)"]
        Behaviors["Pipeline Behaviors (Performance, Caching, Validation)"]
        Events["Domain Event Handlers"]
        AppInterfaces["Interfaces (IUnitOfWork, Repositories)"]
    end

    subgraph Persistence ["🗄️ Persistence Katmanı"]
        EFCore["Entity Framework Core (SQL Server)"]
        Repositories["Repository & UnitOfWork Implementasyonları"]
        OutboxTable["OutboxMessages Tablosu"]
        AuthServices["PBKDF2 Hasher & JWT Generator"]
    end

    subgraph Domain ["🧠 Domain Katmanı (Çekirdek)"]
        Entities["Entities (User, Product, Order, OrderItem)"]
        AggregateRoots["Aggregate Roots & Invariants"]
        DomainEvents["Domain Events"]
        Enums["Enums (UserRole, OrderStatus)"]
    end

    WebApi --> Application
    WebApi --> Persistence
    Persistence --> Application
    Application --> Domain
    Persistence --> Domain
```

---

## 🏗️ Katmanlar ve Kullanılan Teknolojiler

| Katman | Sorumluluk ve Teknolojiler |
| :--- | :--- |
| **🧠 Domain** | Saf C#, Rich Domain Model, Aggregate Root (`Order`), Domain Events (`OrderCreatedDomainEvent`), Enums (`UserRole`, `OrderStatus`), Encapsulation ve İş Kuralları (Invariants). Dış bağımlılık içermez. |
| **⚡ Application** | CQRS Mimarisi (Commands & Queries), MediatR, DTO Sınıfları, Pipeline Turnikeleri (`PerformanceBehavior`, `CachingBehavior`, `ValidationBehavior`), FluentValidation, `PagedResponse<T>` ve Arayüzler (`IUnitOfWork`, `IProductRepository`, `IUserRepository` vb.). |
| **🗄️ Persistence** | Entity Framework Core, SQL Server Express (Docker), Fluent API Varlık Konfigürasyonları, Repository Pattern, Unit of Work, Transactional Outbox Tablosu (`OutboxMessages`), PBKDF2 Şifreleme ve JWT Token Üretici. |
| **🌐 WebApi** | İnce Controller'lar (`AuthController`, `ProductsController`, `OrdersController`), Scalar UI & OpenAPI Dokümantasyonu, `IExceptionHandler` ile RFC 7807/9110 `ProblemDetails` hata formatı, JWT Bearer Yetkilendirme ve `ProcessOutboxMessagesBackgroundService` (Hosted Service). |

---

## 🌟 Öne Çıkan Kurumsal Desenler

### 1. MediatR Pipeline Turnikeleri (Chain of Responsibility)
Tüm istekler (Command ve Query) Handler'a ulaşmadan önce zincirleme turnikelerden geçer:

```text
HTTP İstek
   │
   ▼
[ Turnike 1: PerformanceBehavior ] ──► Kronometreyi başlatır. İşlem 500 ms'yi aşarsa uyarı loglar.
   │
   ▼
[ Turnike 2: CachingBehavior ] ─────► İstek 'ICacheableQuery' ise önce önbelleğe bakar (Cache HIT: 1 ms).
   │                                   İstek 'ICacheInvalidator' ise işlem bitince ilgili önbellekleri temizler.
   ▼
[ Turnike 3: ValidationBehavior ] ──► FluentValidation kurallarını denetler. Hatalıysa DB'ye gitmeden 400 döner.
   │
   ▼
[ Handler ] ─────────────────────────► İş mantığı ve veritabanı operasyonu çalışır.
```

### 2. Transactional Outbox Pattern & Background Worker
Veritabanına kayıt atarken aynı anda e-posta veya bildirim göndermenin yarattığı **Dual-Write** riskini ortadan kaldırır:
- **Tek Transaction:** Sipariş oluşturulduğunda `Order`, `OrderItems`, stok güncellemesi ve `OrderCreatedDomainEvent` JSON'ı **tek bir SQL transaction'ı** içinde `OutboxMessages` tablosuna yazılır. İstemciye anında `200 OK` dönülür (~15-25 ms).
- **Garantili Dağıtım (At-Least-Once Delivery):** Arka planda çalışan `ProcessOutboxMessagesBackgroundService` kuyruktan işlenmemiş olayları okur, MediatR üzerinden dinleyicilere (E-posta, Depo ERP) iletir ve mesajı `ProcessedOnUtc` olarak işaretler. Ağ veya servis kesintilerinde veri ve bildirim kaybı sıfıra iner.

### 3. Veritabanı Düzeyinde Sayfalama, Sıralama ve Filtreleme
- Bellek tüketimini önlemek amacıyla EF Core `Skip()` ve `Take()` kullanılarak SQL Server üzerinde doğrudan **`OFFSET / FETCH NEXT`** sorguları çalıştırılır.
- İstemciye yalnızca filtrelenmiş veriler değil; `TotalCount`, `TotalPages`, `HasPreviousPage`, `HasNextPage` gibi zengin sayfalama üst verilerini içeren `PagedResponse<T>` döndürülür.
- Parametrelere özel dinamik cache anahtarları (`products-p1-s10-qApple-byprice-descTrue`) ile yüksek performans sağlanır.

### 4. JWT Kimlik Doğrulama & Rol Yetkilendirme (RBAC)
- **Kriptografik Güvenlik:** Şifreler `Rfc2898DeriveBytes.Pbkdf2` algoritmasıyla 100.000 iterasyon ve rastgele 128-bit kriptografik tuz (salt) ile hash'lenir. Zamanlama saldırılarına karşı `FixedTimeEquals` koruması mevcuttur.
- **Rol Koruması:** `[Authorize(Roles = "Admin")]` ile ürün ekleme gibi kritik operasyonlar korunur; `[Authorize]` ile sipariş işlemleri sadece oturum açmış kullanıcılara açılır.

### 5. Standart Hata Yönetimi (RFC 7807 / 9110 ProblemDetails)
- `IExceptionHandler` arayüzü ile merkezi ve güvenli hata yakalama.
- Validasyon hataları, iş kuralı ihlalleri (`InvalidOperationException`) ve bulunamadı durumları standart `application/problem+json` formatında döndürülür.

---

## 🧪 Otomatik Test Mimarisi (Unit & Integration Tests)

Projede katmanların bağımsızlığını ve güvenilirliğini garanti altına alan **25 adet otomatik test** bulunmaktadır (`xUnit`, `FluentAssertions`, `NSubstitute` ve `WebApplicationFactory`):

```text
Test Projeleri Dağılımı:
├── 🧠 DotnetArchitecture.Domain.UnitTests      (10 Test) -> Varlık kuralları, stok düşme, Domain Events
├── ⚡ DotnetArchitecture.Application.UnitTests (8 Test)  -> CQRS Handler'ları, FluentValidation, Turnike denetimleri
└── 🌐 DotnetArchitecture.IntegrationTests     (7 Test)  -> WebApplicationFactory + İzole InMemory DB (Auth, RBAC 401/403/200)
```

Tüm testleri tek komutla koşturmak için:
```bash
dotnet test
```

Örnek Test Çıktısı:
```text
Passed!  - Failed: 0, Passed: 10, Skipped: 0 - DotnetArchitecture.Domain.UnitTests.dll (63 ms)
Passed!  - Failed: 0, Passed:  8, Skipped: 0 - DotnetArchitecture.Application.UnitTests.dll (140 ms)
Passed!  - Failed: 0, Passed:  7, Skipped: 0 - DotnetArchitecture.IntegrationTests.dll (937 ms)

Toplam 25 Testin 25'i de BAŞARILI! ✅
```

---

## 📁 Proje Dizin Yapısı

```text
DotnetArchitecture/
├── src/
│   ├── DotnetArchitecture.Domain/           # Çekirdek Domain Katmanı
│   │   ├── Common/                         # BaseEntity, IDomainEvent
│   │   ├── Entities/                       # User, Product, Order, OrderItem
│   │   └── Enums/                          # UserRole, OrderStatus
│   │
│   ├── DotnetArchitecture.Application/      # CQRS & İş Mantığı Katmanı
│   │   ├── Behaviors/                      # Performance, Caching, Validation Turnikeleri
│   │   ├── Common/                         # PagedResponse<T>
│   │   ├── Features/
│   │   │   ├── Auth/                       # Register, Login (Commands, DTOs, Validators)
│   │   │   ├── Products/                   # CreateProduct, GetAllProducts (Paging & Cache)
│   │   │   └── Orders/                     # CreateOrder, GetOrderById, Outbox Events
│   │   └── Interfaces/                     # IUnitOfWork, IProductRepository, IUserRepository vb.
│   │
│   ├── DotnetArchitecture.Persistence/      # Altyapı & Veritabanı Katmanı
│   │   ├── Configurations/                 # Fluent API Entity Eşlemeleri (EF Core)
│   │   ├── Context/                        # AppDbContext
│   │   ├── Migrations/                     # EF Core Veritabanı Göçleri
│   │   ├── Outbox/                         # OutboxMessage Entity
│   │   ├── Repositories/                   # UnitOfWork, Repository Implementasyonları
│   │   └── Services/                       # PBKDF2 PasswordHasher, JwtTokenGenerator
│   │
│   └── DotnetArchitecture.WebApi/           # API Sunum Katmanı
│       ├── BackgroundServices/             # ProcessOutboxMessagesBackgroundService
│       ├── Controllers/                    # Auth, Products, Orders Controller'ları
│       ├── Middlewares/                    # GlobalExceptionHandler (ProblemDetails)
│       └── DotnetArchitecture.WebApi.http  # Kapsamlı API Test İstekleri
│
├── tests/
│   ├── DotnetArchitecture.Domain.UnitTests/      # Domain Birim Testleri
│   │   └── Entities/                           # OrderTests, ProductTests, UserTests
│   ├── DotnetArchitecture.Application.UnitTests/ # CQRS ve Turnike Birim Testleri
│   │   ├── Behaviors/                          # ValidationBehavior Mock Testleri
│   │   └── Features/                           # Handler ve Validator Testleri
│   └── DotnetArchitecture.IntegrationTests/     # Gerçek HTTP API Entegrasyon Testleri
│       ├── Common/CustomWebApplicationFactory   # İzole Test Veritabanı Yapılandırması
│       └── Controllers/                         # AuthController ve ProductsController Testleri
│
├── .dockerignore                                # Docker derleme hariç tutma kuralları
├── .env.example                                 # Docker Compose ortam değişkenleri şablonu
├── docker-compose.yml                           # MSSQL + WebApi çoklu konteyner orkestrasyonu
└── README.md
```

---

## 🚀 Kurulum ve Çalıştırma

Projeyi çalıştırmak için iki pratik yöntem bulunmaktadır:

### 🌟 Seçenek A: Docker Compose ile Tek Komutta Çalıştırma (Önerilen)

Sisteminizde yalnızca Docker yüklü olması yeterlidir. SQL Server ve WebApi birbirine bağlı olarak otomatik ayağa kalkar:

```bash
# 1. Depoyu klonlayıp dizine geçin
git clone https://github.com/GokhanGKHN/DotnetArchitecture.git
cd DotnetArchitecture

# 2. MSSQL ve WebApi'yi arka planda başlatın
docker compose up -d
```

> [!TIP]
> **Otomatik Sağlık Denetimi & Migration:** WebApi servisi, SQL Server'ın ayağa kalkıp sorgu kabul etmesini (`healthcheck: service_healthy`) bekler. Başlatıldığında veritabanı tablolarını (`AppDbContext.Database.MigrateAsync`) otomatik oluşturur!

API arayüzüne anında erişin:
👉 `http://localhost:5294/scalar/v1`

Konteynerleri durdurmak için:
```bash
docker compose down
```

---

### 💻 Seçenek B: Yerel Geliştirme (Local CLI) ile Çalıştırma

Eğer yerel makinenizde geliştirme yapmak isterseniz:

#### Gereksinimler
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker Desktop](https://www.docker.com/) veya yüklü bir Docker motoru
- `dotnet-ef` CLI aracı (`dotnet tool install --global dotnet-ef`)

#### 1. Yalnızca SQL Server Konteynerini Başlatın
```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourPassword123." \
   -p 1433:1433 --name mssql_express -d \
   mcr.microsoft.com/mssql/server:2022-latest
```

#### 2. Güvenli Bağlantı Dizesini (User Secrets) Tanımlayın
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=DotnetArchitectureDb;User Id=sa;Password=YourPassword123.;TrustServerCertificate=True;" --project src/DotnetArchitecture.WebApi
```

#### 3. Veritabanı Tablolarını Oluşturun (Migration)
```bash
dotnet ef database update --project src/DotnetArchitecture.Persistence --startup-project src/DotnetArchitecture.WebApi
```

#### 4. Uygulamayı Ayağa Kaldırın
```bash
dotnet run --project src/DotnetArchitecture.WebApi
```

Uygulama başladığında Scalar API arayüzüne tarayıcınızdan erişebilirsiniz:
👉 `http://localhost:5294/scalar/v1`

---

## 🧪 API Test Senaryoları (.http Dosyası)

Manuel API testlerini çalıştırmak için `src/DotnetArchitecture.WebApi/DotnetArchitecture.WebApi.http` dosyasını VS Code (REST Client) veya Rider ile açıp istekleri doğrudan gönderebilirsiniz:

1. **Kullanıcı Kaydı & Giriş:**
   - `POST /api/auth/register` (Admin veya Member rolüyle kayıt)
   - `POST /api/auth/login` (Giriş yaparak JWT Token alma)
2. **Yetkilendirme Denetimi:**
   - Token olmadan `POST /api/products` (Sonuç: `401 Unauthorized`)
   - Admin Token ile `POST /api/products` (Sonuç: `200 OK` & Cache Invalidation)
3. **Önbellek & Sayfalama:**
   - `GET /api/products?pageNumber=1&pageSize=2` (1. İstek: Cache Miss -> DB'den SQL Sayfalama)
   - `GET /api/products?pageNumber=1&pageSize=2` (2. İstek: Cache Hit -> 0-1 ms!)
   - `GET /api/products?pageSize=500` (Validation Turnikesi -> DB'ye gitmeden 400 Bad Request)
4. **Outbox Pattern ile Sipariş:**
   - `POST /api/orders` (Sipariş anında onaylanır, arka plandaki worker 5 saniye içinde e-posta ve depo bildirimlerini dağıtır)
