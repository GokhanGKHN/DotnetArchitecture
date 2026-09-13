# 🏛️ DotnetArchitecture

Kurumsal standartlarda geliştirilmiş, **Clean Architecture**, **Domain-Driven Design (DDD)** ve **CQRS** prensiplerini uygulayan modern bir **.NET 10** web API referans mimarisidir.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14.0-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4?logo=nuget&logoColor=white)](https://docs.microsoft.com/ef/core/)
[![MediatR](https://img.shields.io/badge/MediatR-CQRS-blue)](https://github.com/jbogard/MediatR)
[![Redis](https://img.shields.io/badge/Redis-7--Alpine-DC382D?logo=redis&logoColor=white)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-MSSQL%20%26%20Redis-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/)
[![Tests](https://img.shields.io/badge/Tests-58%20Passed-brightgreen?logo=xunit&logoColor=white)](https://github.com/)
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
  - [6. Specification Pattern (ISpecification ve Evaluator)](#6-specification-pattern-ispecification-ve-evaluator)
  - [7. Canlılık ve Hazırlık Sağlık Denetimleri (Health Checks)](#7-canlılık-ve-hazırlık-sağlık-denetimleri-health-checks)
  - [8. Dahili İstek Sınırlama (Rate Limiting - RFC 6585)](#8-dahili-istek-sınırlama-rate-limiting---rfc-6585)
  - [9. Otomatik Denetim İzi (Audit Logging) ve Mantıksal Silme (Soft Delete)](#9-otomatik-denetim-izi-audit-logging-ve-mantıksal-silme-soft-delete)
  - [10. Dağıtık Önbellekleme (Distributed Caching with Redis & IDistributedCache)](#10-dağıtık-önbellekleme-distributed-caching-with-redis--idistributedcache)
  - [11. Idempotency Turnikesi (Idempotent Consumer Pattern)](#11-idempotency-turnikesi-idempotent-consumer-pattern)
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
        Middlewares["Global Exception Handler & Rate Limiter"]
        HealthChecks["Liveness & Readiness Probes (SQL + Redis)"]
        BackgroundWorker["Outbox Background Worker"]
    end

    subgraph Application ["⚡ Application Katmanı (CQRS)"]
        Commands["Commands & Queries (MediatR)"]
        Behaviors["Pipeline Turnikeleri (Performance, Idempotency, Caching, Validation)"]
        Events["Domain Event Handlers"]
        AppInterfaces["Interfaces (IUnitOfWork, ICacheService, IIdempotencyService, Repositories)"]
    end

    subgraph Persistence ["🗄️ Persistence Katmanı"]
        EFCore["Entity Framework Core (SQL Server)"]
        RedisCache["DistributedCacheService & IdempotencyService (Redis)"]
        Interceptors["AuditableEntityInterceptor (Audit & Soft Delete)"]
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
| **🧠 Domain** | Saf C#, Rich Domain Model, Aggregate Root (`Order`), Domain Events (`OrderCreatedDomainEvent`), Enums (`UserRole`, `OrderStatus`), Encapsulation, BaseEntity (`IAuditableEntity`, `ISoftDeletable`). Dış bağımlılık içermez. |
| **⚡ Application** | CQRS Mimarisi (Commands & Queries), MediatR, DTO Sınıfları, Pipeline Turnikeleri (`PerformanceBehavior`, `IdempotencyBehavior`, `CachingBehavior`, `ValidationBehavior`), FluentValidation, Soyutlamalar (`ICacheService`, `IIdempotencyService`, `IUnitOfWork`, `IProductRepository`, `ICurrentUserService`). |
| **🗄️ Persistence** | Entity Framework Core, SQL Server Express, StackExchange.Redis (`IDistributedCache`), `DistributedCacheService`, `IdempotencyService`, `AuditableEntityInterceptor`, Global Query Filters, Repository Pattern, Unit of Work, Outbox Tablosu, PBKDF2 Şifreleme ve JWT Token Üretici. |
| **🌐 WebApi** | İnce Controller'lar (`AuthController`, `ProductsController`, `OrdersController`), Scalar UI & OpenAPI Dokümantasyonu, RFC 7807/9110 `ProblemDetails`, HTTP `Idempotency-Key` başlık desteği, .NET 10 Rate Limiting, Health Checks (SQL + Redis), `ProcessOutboxMessagesBackgroundService` (Hosted Service). |

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
[ Turnike 2: IdempotencyBehavior ] ──► Mükerrer komutsa (aynı Idempotency-Key) DB'ye gitmeden kayıtlı yanıtı döner (1 ms).
   │                                   Eşzamanlı devam eden işlem varsa 409 Conflict fırlatır.
   ▼
[ Turnike 3: CachingBehavior ] ─────► İstek 'ICacheableQuery' ise Redis / Dağıtık önbelleğe bakar (Cache HIT: 1 ms).
   │                                   İstek 'ICacheInvalidator' ise işlem bitince ilgili önbellekleri temizler.
   ▼
[ Turnike 4: ValidationBehavior ] ──► FluentValidation kurallarını denetler. Hatalıysa DB'ye gitmeden 400 döner.
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
- Validasyon hataları (`400 BadRequest`), iş kuralı ihlalleri (`400 BadRequest`), eşzamanlı istek çakışması (`409 Conflict`), bulunamadı durumları (`404 NotFound`) standart `application/problem+json` formatında döndürülür.

### 6. Specification Pattern (ISpecification ve Evaluator)
Repository arayüzlerini yüzlerce özel sorgu metoduyla (`GetByNameAndPriceAndCategory...`) kirletmek yerine, sorgu mantığını (Filtre, Eager Loading `Include`, Sıralama ve Sayfalama) kapsülleyen **Domain-Driven Design (DDD)** deseni:
- **`ISpecification<T>` & `BaseSpecification<T>`:** Filtre (`Criteria`), sıralama (`OrderBy`, `OrderByDescending`), ilişkiler (`Includes`) ve sayfalama (`Skip`, `Take`) kurallarını güçlü tipli nesneler halinde tanımlar (örn: `ProductsFilterSpecification`, `OrderWithItemsSpecification`).
- **`SpecificationEvaluator<T>`:** EF Core sorgu ağacını (`IQueryable<T>`) arka planda dinamik olarak inşa eder; EF Core detaylarının Application veya Controller katmanına sızmasını (leak) engeller.

### 7. Canlılık ve Hazırlık Sağlık Denetimleri (Health Checks)
Cloud-native, Kubernetes ve Docker orkestrasyon standartlarına tam uyumlu yerleşik sağlık kontrolü uç noktaları:
- **`/health` (Kapsamlı JSON Sağlık Raporu):** API, SQL Server ve Redis bağlantı durumunu, milisaniye gecikmelerini ve ayrıntılı bileşen listesini döndürür (`HealthCheckResponseWriter`).
- **`/health/live` (Liveness Probe):** Yalnızca API sürecinin ayakta olup olmadığını denetler (Yanıt: `Healthy`). Süreç çökerse konteyner orkestratörü süreci yeniden başlatır.
- **`/health/ready` (Readiness Probe):** SQL Server ve Redis veritabanı bağlantılarını denetler. Kritik bağımlılıklar yanıt vermiyorsa yük dengeleyici (Load Balancer) bu sunucuya kullanıcı trafiği yönlendirmez.

### 8. Dahili İstek Sınırlama (Rate Limiting - RFC 6585)
.NET 10 yerleşik `Microsoft.AspNetCore.RateLimiting` middleware'i ile API uç noktaları kaba kuvvet (Brute-Force) ve DoS saldırılarına karşı korunur:
- **`AuthPolicy` (Sıkı Güvenlik):** `/api/auth/login` ve `/api/auth/register` uç noktalarında IP başına 1 dakikada maksimum **10 istek** (Kuyruk: 0). Parola deneme botlarını anında durdurur.
- **`GeneralPolicy` (Kaynak Koruma):** Genel API uç noktalarında (`Products`) IP başına 1 dakikada maksimum **100 istek**.
- **Özelleştirilmiş 429 Yanıtı:** Limit aşıldığında istemciye standart `Retry-After: 60` HTTP başlığı ve RFC 7807/9110 `application/problem+json` formatında hata mesajı döndürülür.

### 9. Otomatik Denetim İzi (Audit Logging) ve Mantıksal Silme (Soft Delete)
EF Core `SaveChangesInterceptor` altyapısı sayesinde kod tekrarı olmaksızın merkezi veri güvenliği ve denetimi:
- **`AuditableEntityInterceptor`:** Bir varlık eklendiğinde veya güncellendiğinde `CreatedAtUtc`, `CreatedBy`, `LastModifiedAtUtc`, `LastModifiedBy` alanları `ICurrentUserService` üzerinden (JWT taleplerinden) otomatik doldurulur.
- **Mantıksal Silme (Soft Delete):** `context.Remove(entity)` çağrıldığında fiziksel `DELETE` sorgusu engellenir; durum otomatik olarak `Modified` yapılarak `IsDeleted = true`, `DeletedAtUtc`, `DeletedBy` atanır.
- **Global Query Filter:** `AppDbContext.OnModelCreating` aşamasında tanımlanan filtre ile sistem genelindeki tüm sorgularda silinmiş kayıtlar SQL seviyesinde (`WHERE IsDeleted = 0`) otomatik filtrelenir; gerektiğinde `.IgnoreQueryFilters()` ile geçmiş kayıtlar denetlenebilir.

### 10. Dağıtık Önbellekleme (Distributed Caching with Redis & IDistributedCache)
Mikroservis ve çoklu konteyner (Multi-replica) ortamlarında sunucuların bellek senkronizasyonu kaybını önleyen kurumsal dağıtık önbellek mimarisi:
- **Dependency Inversion:** `Application` katmanı somut Redis kütüphanesini bilmez; saf `ICacheService` arayüzüne bağımlıdır.
- **`DistributedCacheService`:** `Persistence` katmanında `Microsoft.Extensions.Caching.StackExchangeRedis` (`IDistributedCache`) üzerinden JSON UTF-8 byte dizisi serileştirmesiyle çalışır.
- **Hata Toleransı (Graceful Degradation / Resilience):** Redis sunucusu geçici olarak kapalı veya erişilemez olduğunda sistem exception fırlatıp kullanıcı işlemini çökertmez; sessizce Cache Miss davranışı sergileyerek doğrudan veritabanından veri çekmeye devam eder.
- **Otomatik Geçersiz Kılma (Cache Invalidation):** `ICacheInvalidator` uygulayan komutlar çalıştığında (`CreateProductCommand`), ilgili önbellek anahtarları otomatik olarak temizlenir.

### 11. Idempotency Turnikesi (Idempotent Consumer Pattern)
Özellikle ödeme alma, sipariş verme veya stok düşme gibi hassas ve durumu değiştiren işlemlerde ağ gecikmeleri veya kullanıcının mükerrer buton tıklamalarından kaynaklanan **çift sipariş ve çift para çekme riskini** ortadan kaldırır:
- **`Idempotency-Key` HTTP Başlığı:** İstemci (Web/Mobil) istek başlığında benzersiz bir UUID gönderir (`Idempotency-Key: 9b1deb4d...`).
- **`IdempotencyBehavior`:** Komut `IIdempotentCommand` arayüzünü uyguluyorsa turnike devreye girer.
  - **İlk Çağrı (`FirstRun`):** Kilit alınır, sipariş veritabanına yazılır, stok düşülür ve nihai sonuç 24 saat süreyle Redis/Bellek deposuna kaydedilir.
  - **Mükerrer Çağrı (`AlreadyProcessed` / Replay):** Handler ve veritabanı operasyonları **kesinlikle çalıştırılmaz**, stok tekrar düşülmez; önceki yanıt 1 ms içinde doğrudan döndürülür!
  - **Eşzamanlı Çağrı (`InProgress` / Çakışma):** İlk istek henüz bitmemişken aynı anahtarla paralel ikinci bir istek gelirse `IdempotencyConflictException` fırlatılır ve istemciye `409 Conflict` dönülür.
  - **Hata Güvenliği (Rollback):** Eğer işlem bir validasyon veya veritabanı hatasıyla sonlanırsa kilit derhal serbest bırakılır (`ReleaseAsync`), böylece istemci düzeltme yapıp aynı anahtarla tekrar deneyebilir.

---

## 🧪 Otomatik Test Mimarisi (Unit & Integration Tests)

Projede katmanların bağımsızlığını ve güvenilirliğini garanti altına alan **58 adet otomatik test** bulunmaktadır (`xUnit`, `FluentAssertions`, `NSubstitute` ve `WebApplicationFactory`):

```text
Test Projeleri Dağılımı:
├── 🧠 DotnetArchitecture.Domain.UnitTests      (10 Test) -> Varlık kuralları, stok düşme, Domain Events
├── ⚡ DotnetArchitecture.Application.UnitTests (22 Test) -> CQRS Handler'ları, CachingBehavior, IdempotencyBehavior, Specifications, Validation
└── 🌐 DotnetArchitecture.IntegrationTests     (26 Test) -> WebApplicationFactory + InMemory DB (Auth, RBAC, HealthChecks, RateLimiting, Interceptors, DistributedCache, Idempotency)
```

Tüm testleri tek komutla koşturmak için:
```bash
dotnet test
```

Örnek Test Çıktısı:
```text
Passed!  - Failed: 0, Passed: 10, Skipped: 0 - DotnetArchitecture.Domain.UnitTests.dll (58 ms)
Passed!  - Failed: 0, Passed: 22, Skipped: 0 - DotnetArchitecture.Application.UnitTests.dll (134 ms)
Passed!  - Failed: 0, Passed: 26, Skipped: 0 - DotnetArchitecture.IntegrationTests.dll (1 s)

Toplam 58 Testin 58'i de BAŞARILI! ✅
```

---

## 📁 Proje Dizin Yapısı

```text
DotnetArchitecture/
├── src/
│   ├── DotnetArchitecture.Domain/           # Çekirdek Domain Katmanı
│   │   ├── Common/                         # BaseEntity, IAuditableEntity, ISoftDeletable, IDomainEvent
│   │   ├── Entities/                       # User, Product, Order, OrderItem
│   │   └── Enums/                          # UserRole, OrderStatus
│   │
│   ├── DotnetArchitecture.Application/      # CQRS & İş Mantığı Katmanı
│   │   ├── Behaviors/                      # Performance, Idempotency, Caching, Validation Turnikeleri
│   │   ├── Common/
│   │   │   ├── Exceptions/                 # IdempotencyConflictException (409 Conflict)
│   │   │   ├── Idempotency/                # IIdempotentCommand, IIdempotencyService, IdempotencyCheckResult
│   │   │   ├── Specifications/             # ISpecification, BaseSpecification
│   │   │   └── PagedResponse<T>
│   │   ├── Features/
│   │   │   ├── Auth/                       # Register, Login (Commands, DTOs, Validators)
│   │   │   ├── Products/                   # CreateProduct, GetAllProducts, Specifications
│   │   │   └── Orders/                     # CreateOrder, GetOrderById, Outbox Events, Specifications
│   │   └── Interfaces/                     # IUnitOfWork, ICacheService, ICurrentUserService, IProductRepository vb.
│   │
│   ├── DotnetArchitecture.Persistence/      # Altyapı & Veritabanı Katmanı
│   │   ├── Configurations/                 # Fluent API Entity Eşlemeleri (EF Core)
│   │   ├── Context/                        # AppDbContext (Global Query Filters)
│   │   ├── Interceptors/                   # AuditableEntityInterceptor (Audit & Soft Delete)
│   │   ├── Migrations/                     # EF Core Veritabanı Göçleri
│   │   ├── Outbox/                         # OutboxMessage Entity
│   │   ├── Repositories/                   # UnitOfWork, Repository Implementasyonları
│   │   ├── Services/                       # IdempotencyService, DistributedCacheService, PasswordHasher, JwtTokenGenerator
│   │   └── Specifications/                 # SpecificationEvaluator (EF Core Queryable Oluşturucu)
│   │
│   └── DotnetArchitecture.WebApi/           # API Sunum Katmanı
│       ├── BackgroundServices/             # ProcessOutboxMessagesBackgroundService
│       ├── Common/                         # HealthCheckResponseWriter (Standart JSON Raporu)
│       ├── Controllers/                    # Auth, Products, Orders Controller'ları
│       ├── Middlewares/                    # GlobalExceptionHandler (ProblemDetails & 409 Conflict)
│       ├── Services/                       # CurrentUserService (IHttpContextAccessor ile Claims Erişimi)
│       └── DotnetArchitecture.WebApi.http  # Kapsamlı API Test İstekleri
│
├── tests/
│   ├── DotnetArchitecture.Domain.UnitTests/      # Domain Birim Testleri
│   │   └── Entities/                           # OrderTests, ProductTests, UserTests
│   ├── DotnetArchitecture.Application.UnitTests/ # CQRS, Turnike ve Specification Birim Testleri
│   │   ├── Behaviors/                          # ValidationBehavior, CachingBehavior, IdempotencyBehavior Testleri
│   │   ├── Features/                           # Handler ve Validator Testleri
│   │   └── Specifications/                     # ProductsFilter ve OrderWithItems Testleri
│   └── DotnetArchitecture.IntegrationTests/     # Gerçek HTTP API Entegrasyon Testleri
│       ├── Common/CustomWebApplicationFactory   # İzole Test Veritabanı Yapılandırması
│       ├── Controllers/                         # Auth, Products, OrdersIdempotency ve HealthChecks Testleri
│       ├── Interceptors/                        # AuditableEntityInterceptorTests (Audit & Soft Delete)
│       ├── Middlewares/                         # RateLimitingTests (429 Too Many Requests)
│       └── Services/                            # DistributedCacheService, IdempotencyService Testleri
│
├── .dockerignore                                # Docker derleme hariç tutma kuralları
├── .env.example                                 # Docker Compose ortam değişkenleri şablonu
├── docker-compose.yml                           # MSSQL + Redis + WebApi çoklu konteyner orkestrasyonu
└── README.md
```

---

## 🚀 Kurulum ve Çalıştırma

Projeyi çalıştırmak için iki pratik yöntem bulunmaktadır:

### 🌟 Seçenek A: Docker Compose ile Tek Komutta Çalıştırma (Önerilen)

Sisteminizde yalnızca Docker yüklü olması yeterlidir. SQL Server, Redis ve WebApi birbirine bağlı ve sağlıklı olarak otomatik ayağa kalkar:

```bash
# 1. Depoyu klonlayıp dizine geçin
git clone https://github.com/GokhanGKHN/DotnetArchitecture.git
cd DotnetArchitecture

# 2. MSSQL, Redis ve WebApi'yi arka planda başlatın
docker compose up -d
```

> [!TIP]
> **Otomatik Sağlık Denetimi & Migration:** WebApi servisi, SQL Server ve Redis'in ayağa kalkıp sorgu kabul etmesini (`healthcheck: service_healthy`) bekler. Başlatıldığında veritabanı tablolarını (`AppDbContext.Database.MigrateAsync`) otomatik oluşturur!

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

#### 1. SQL Server ve Redis Konteynerlerini Başlatın
```bash
# MSSQL
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourPassword123." \
   -p 1433:1433 --name mssql_express -d \
   mcr.microsoft.com/mssql/server:2022-latest

# Redis
docker run -d --name redis_dev -p 6379:6379 redis:7-alpine
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
5. **Idempotency ile Güvenli Sipariş:**
   - `Idempotency-Key` başlığıyla `POST /api/orders` (İstek 10 kez art arda atılsa bile sipariş sadece 1 kez oluşturulur, stok yalnızca 1 kez düşülür ve aynı sipariş kimliği anında döndürülür!)
6. **Sağlık Denetimleri (Health Checks):**
   - `GET /health` (API, SQL Server ve Redis bileşenlerinin milisaniye gecikmeli ayrıntılı JSON durum raporu)
   - `GET /health/live` (Konteyner Liveness probu -> `Healthy`)
   - `GET /health/ready` (Konteyner Readiness probu -> `Healthy`)
7. **Kaba Kuvvet (Brute-Force) İstek Sınırlama:**
   - `POST /api/auth/login` (1 dakika içinde 11 kez istek atıldığında: `429 Too Many Requests` ve `Retry-After: 60`)
8. **Dağıtık Önbellekleme & Invalidation:**
   - `GET /api/products` (Redis'e yazma/okuma ve Admin yeni ürün eklediğinde önbelleğin anında silinmesi)
