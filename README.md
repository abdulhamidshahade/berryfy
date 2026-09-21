# <img align="absmiddle" width="50" height="50" alt="1790012305130_1_-removebg-preview" src="https://github.com/user-attachments/assets/6392ae45-0de5-4f5f-b804-6f84094f172d" /> Berryfy

<img width="3072" height="2076" alt="1790010321390(1)" src="https://github.com/user-attachments/assets/b8bd1c66-c3cf-4e59-889a-8523358c8c97" />



A modern, full-stack **e-commerce platform** built with **.NET 9** and **Next.js 15**.  
Berryfy delivers a complete online shopping experience — product catalog, cart, checkout, wishlist, coupons, order management, simulated payments, and a full admin dashboard — all containerized with Docker.

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Next.js 15](https://img.shields.io/badge/Next.js-15.1.8-000000?style=flat&logo=next.js)](https://nextjs.org/)
[![React 19](https://img.shields.io/badge/React-19.0-61DAFB?style=flat&logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.0-3178C6?style=flat&logo=typescript)](https://www.typescriptlang.org/)
[![Bootstrap 5](https://img.shields.io/badge/Bootstrap-5.3.6-7952B3?style=flat&logo=bootstrap)](https://getbootstrap.com/)
[![EF Core](https://img.shields.io/badge/EF_Core-9.0.4-512BD4?style=flat&logo=dotnet)](https://learn.microsoft.com/en-us/ef/core/)

---

## 🌐 Live Demo

**[demo.berryfy.org](https://demo.berryfy.org/auth/login)** — sign in to explore the storefront or the admin dashboard.

---

## 🏗️ Architecture

Berryfy follows a **layered architecture** with strict separation of concerns. Each layer depends only on the layer below it; the domain has no external dependencies except cross-cutting libraries.

### Backend (.NET 9)

| Project | Responsibility |
|---------|----------------|
| **Berryfy.Domain** | Entities, repository interfaces, domain constants, and identity models. No dependencies on infrastructure. |
| **Berryfy.Application** | Services, DTOs, AutoMapper profiles, FluentValidation rules, and JWT token helpers. Depends only on Domain. |
| **Berryfy.Infrastructure** | EF Core `DbContext`, repository implementations, SQL Server, Identity stores, health checks, Zoho SMTP. |
| **Berryfy.API** | ASP.NET Core controller-based REST API. OpenAPI + Scalar docs, JWT auth, CORS, rate limiting, health endpoint, data seeding on startup. |

### Frontend (Next.js 15)

| Directory | Responsibility |
|-----------|----------------|
| `src/app/` | App Router pages, layouts, and Next.js API route handlers |
| `src/components/` | React components — admin, auth, cart, checkout, product, shared UI |
| `src/lib/services/` | API client services that call the .NET backend |
| `src/lib/actions/` | Next.js Server Actions for mutations and form handling |
| `src/middleware.ts` | Edge middleware — JWT refresh, auth-guarded routes, session cookie management |
| `src/types/` | TypeScript type definitions shared across the app |

---

## ✨ Features

### 🛒 E-Commerce Core
- Product catalog with categories, images, search, sorting, and pagination
- Shopping cart — session-based for guests, user-linked on login
- Full checkout flow with saved shipping/billing info
- Wishlist — create, share, duplicate, and bulk-manage items
- Coupon system — percentage & fixed discounts, assignable to all / specific / new users
- Simulated payment processing with PDF receipt download
- Real-time inventory tracking with reserve/release/confirm on checkout and low-stock alerts

### 🔐 Authentication & Authorization
- JWT access tokens (2-hour expiry) with rotating refresh tokens (7-day expiry)
- Role-based authorization — **User**, **Admin**, **SuperAdmin**
- Email confirmation via 6-digit OTP code (Zoho SMTP)
- Forgot password & secure reset via emailed token
- User profile management, change password, lock/unlock accounts
- Transparent token refresh in Next.js middleware (no page reload required)

### 📊 Admin Dashboard
- Analytics — sales revenue, order statistics, traffic charts
- Product & category CRUD with image management
- Order management — update status, cancel, refund
- Inventory — adjust stock, view change history, low-stock notifications
- Coupon lifecycle — create, assign to all / specific / new users, disable per user
- User management — view, lock/unlock, assign roles
- Payment records — search, filter by status/date range, totals
- Shop settings & admin wishlist overview

### 🚀 Technical Highlights
- **Controller-based REST API** with 12 resource controllers and 100+ endpoints
- **Standardized response envelope** (`isSuccess`, `statusCode`, `statusMessage`, `data`, `errors`)
- **Server-Side Rendering** via Next.js App Router with React Server Components
- **Server Actions** for secure form handling and data mutations
- **FluentValidation** — declarative, testable validation rules on every request DTO
- **AutoMapper** — zero-boilerplate entity → DTO mapping across all layers
- **Serilog** — structured request logging
- **Health check** at `/health` — SQL Server connectivity verified on startup
- **Docker Compose** — one command spins up API, frontend, and SQL Server

---

## 🛠️ Tech Stack

### Backend

| Package | Version |
|---------|---------|
| .NET / ASP.NET Core | 9.0 |
| Entity Framework Core | 9.0.4 |
| EF Core SQL Server | 9.0.4 |
| ASP.NET Core Identity | 9.0.4 |
| JWT Bearer | 9.0.4 |
| Microsoft.AspNetCore.OpenApi | 9.0.2 |
| Scalar.AspNetCore | 1.2.61 |
| AutoMapper | 16.1.1 |
| FluentValidation.AspNetCore | 11.3.1 |
| Serilog.AspNetCore | 10.0.0 |
| Microsoft.IdentityModel.Tokens | 8.16.0 |
| Microsoft.Extensions.Diagnostics.HealthChecks | 9.0.7 |
| Newtonsoft.Json | 13.0.3 |

### Frontend

| Package | Version |
|---------|---------|
| Next.js | 15.1.8 |
| React / React DOM | 19.0.0 |
| TypeScript | ^5 |
| Bootstrap | 5.3.6 |
| Bootstrap Icons | 1.13.1 |
| React Bootstrap | 2.10.10 |
| date-fns | 4.1.0 |
| uuid | 11.1.0 |
| formidable | 3.5.1 |

### Infrastructure

| Component | Detail |
|-----------|--------|
| Database | SQL Server 2022 (Docker image `mcr.microsoft.com/mssql/server:2022-latest`) |
| Email | Zoho SMTP — OTP confirmation codes, password resets, order notifications |
| Containerization | Docker + Docker Compose 3.9 |

---

## 📁 Project Structure

```text
BlueBerry24/
├── Berryfy.API/                    # ASP.NET Core Web API
│   ├── Controllers/                # 12 resource controllers
│   ├── Program.cs                  # App entry point — DI, middleware, seeding
│   ├── appsettings.json            # Base configuration
│   └── Dockerfile                  # API container image
├── Berryfy.Application/            # Application layer
│   ├── Services/
│   │   ├── Concretes/              # Service implementations
│   │   └── Interfaces/             # Service contracts
│   ├── Dtos/                       # Request / response DTOs
│   └── Mapping/                    # AutoMapper profiles
├── Berryfy.Domain/                 # Domain layer (no infra dependencies)
│   ├── Entities/
│   │   ├── AuthEntities/           # ApplicationUser, ApplicationRole
│   │   ├── ProductEntities/        # Product, Category, ProductCategory
│   │   ├── OrderEntities/          # Order, OrderItem, OrderTotal
│   │   ├── ShoppingCartEntities/   # Cart, CartItem, CartCoupon
│   │   ├── CouponEntities/         # Coupon, UserCoupon
│   │   ├── WishlistEntities/       # Wishlist, WishlistItem
│   │   ├── ShopEntities/           # Shop
│   │   ├── CheckoutEntities/       # UserCheckoutInfo
│   │   ├── InventoryEntities/      # InventoryLog
│   │   └── PaymentEntities/        # Payment
│   ├── Repositories/               # Repository interfaces
│   └── Constants/                  # OrderStatus, PaymentStatus, CartStatus, roles …
├── Berryfy.Infrastructure/         # Infrastructure layer
│   ├── Data/                       # EF Core DbContext
│   ├── Repositories/               # Repository implementations
│   └── Migrations/                 # EF Core migrations (auto-applied on startup)
├── Berryfy.Tests/                  # xUnit test project
├── Berryfy.Web/                    # Next.js 15 frontend
│   ├── src/
│   │   ├── app/                    # App Router pages & API routes
│   │   ├── components/             # React components
│   │   ├── lib/
│   │   │   ├── actions/            # Server Actions
│   │   │   ├── services/           # API client services
│   │   │   └── utils/              # Helpers & formatters
│   │   ├── types/                  # TypeScript definitions
│   │   └── middleware.ts           # Edge middleware (auth guard, token refresh)
│   ├── public/                     # Static assets
│   ├── Dockerfile                  # Frontend container image
│   └── package.json
├── docker-compose.yml              # Compose — API + frontend + SQL Server
├── .env.example                    # Environment variable template
└── README.md
```

---

## 🌐 Frontend Routes

### Public / Storefront

| Route | Description |
|-------|-------------|
| `/` | Home — hero, featured products, categories, stats |
| `/products` | Product catalog with search, filter, sort & pagination |
| `/products/[id]` | Product detail page |
| `/categories` | Category browser |
| `/categories/[id]` | Products by category |
| `/cart` | Shopping cart |
| `/checkout` | Checkout — shipping info & summary |
| `/checkout/order` | Order review |
| `/payment` | Payment processing |
| `/payment/success` | Order confirmation |
| `/wishlist` | Public wishlist view |

### Authentication

| Route | Description |
|-------|-------------|
| `/auth/login` | Sign in |
| `/auth/register` | Create account |
| `/auth/confirm-email` | 6-digit OTP verification |
| `/auth/resend-confirmation` | Resend OTP code |
| `/auth/forgot-password` | Request password reset |
| `/auth/reset-password` | Reset password via emailed token |

### User Account (protected)

| Route | Description |
|-------|-------------|
| `/profile` | Profile overview |
| `/profile/edit` | Edit name & username |
| `/profile/change-password` | Change password |
| `/profile/payments` | Payment history |
| `/profile/payments/[id]` | Payment detail & receipt |
| `/profile/wishlist` | Manage wishlist |
| `/profile/coupons` | My coupons |
| `/orders` | Order history |
| `/orders/[id]` | Order detail |

### Admin Dashboard (Admin / SuperAdmin only)

| Route | Description |
|-------|-------------|
| `/admin` | Dashboard overview |
| `/admin/analytics` | Sales & revenue analytics |
| `/admin/reports` | Business reports |
| `/admin/Traffic` | Traffic overview |
| `/admin/products` | Product list |
| `/admin/products/create` | Create product |
| `/admin/products/update/[id]` | Edit product |
| `/admin/products/delete/[id]` | Delete product |
| `/admin/categories` | Category list |
| `/admin/categories/create` | Create category |
| `/admin/categories/update/[id]` | Edit category |
| `/admin/categories/delete/[id]` | Delete category |
| `/admin/orders` | Order management |
| `/admin/orders/[id]` | Order detail & status update |
| `/admin/customers` | Customer list |
| `/admin/users` | User management |
| `/admin/role-management` | Assign & manage roles |
| `/admin/coupons` | Coupon list |
| `/admin/coupons/create` | Create coupon |
| `/admin/coupons/update/[id]` | Edit coupon |
| `/admin/coupons/delete/[id]` | Delete coupon |
| `/admin/coupons/add-to-user` | Assign coupon to a user |
| `/admin/coupons/add-to-all-users` | Assign to all users |
| `/admin/coupons/add-to-specific-users` | Assign to specific users |
| `/admin/coupons/add-to-new-users` | Assign to new users |
| `/admin/coupons/[id]/users` | Users holding a coupon |
| `/admin/coupons/[id]/add-user` | Add user to coupon |
| `/admin/payments` | Payment records |
| `/admin/payments/[id]` | Payment detail |
| `/admin/payments/update/[id]` | Update payment status |
| `/admin/payments/delete/[id]` | Delete payment |
| `/admin/payments/refund/[id]` | Process refund |
| `/admin/inventory` | Inventory overview |
| `/admin/inventory/edit/[id]` | Adjust stock |
| `/admin/inventory/history/[id]` | Stock change history |
| `/admin/wishlists` | Wishlist admin overview |
| `/admin/settings` | Shop settings |

---

## 🔌 API Controllers

| Controller | Base Route | Description |
|------------|-----------|-------------|
| `AuthController` | `api/auth` | Register, login, logout, me, refresh token, email confirmation, password reset, user CRUD, lock/unlock |
| `ProductsController` | `api/products` | Paginated list, by id, by name, create, update, delete, existence checks |
| `CategoriesController` | `api/categories` | List, by id, by name, create, update, delete, existence checks |
| `ShoppingCartsController` | `api/shopping-carts` | Current cart, add/update/remove items, apply/remove coupon, checkout, complete, clear |
| `OrdersController` | `api/orders` | Create, by id, by user, by reference, by status, admin list, update status, cancel, refund, process |
| `PaymentsController` | `api/payments` | Process, verify, refund, by id, by user, my payments, paginated, stats, search, update status |
| `CouponsController` | `api/coupons` | CRUD, by code, assign to all/specific/new users, user coupon lists, disable per user |
| `InventoriesController` | `api/inventories` | Stock check, reserve, release, confirm deduction, add stock, adjust, low-stock list, history, notifications |
| `WishlistsController` | `api/wishlists` | CRUD, default wishlist, add/remove/bulk items, share, duplicate, admin stats |
| `RoleManagementController` | `api/role-managements` | Create/rename/delete roles, assign/remove per user, bulk assign, initialize defaults, stats |
| `ShopsController` | `api/shops` | Get shop settings, update shop settings |
| `UserCheckoutInfoController` | `api/UserCheckoutInfo` | Get, save checkout info, save billing info, delete |

### Response Envelope

All endpoints return a standardized envelope:

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "statusMessage": "Operation completed successfully",
  "data": {},
  "errors": []
}
```

### Authorization Levels

| Policy | Roles |
|--------|-------|
| `[AllRoles]` | Any authenticated user |
| `[UserAndAbove]` | User, Admin, SuperAdmin |
| `[AdminAndAbove]` | Admin, SuperAdmin |
| `[SuperAdminOnly]` | SuperAdmin only |

---

## ⚡ Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/) with npm
- SQL Server (local or Docker)
- Docker & Docker Compose (for the one-command setup)

---

### Option A — Docker Compose (recommended)

```bash
git clone https://github.com/abdulhamidshahade/berryfy.git berryfy
cd berryfy

# Copy the environment template and fill in your secrets (never commit .env)
cp .env.example .env
```

Edit `.env` with your values, then:

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:30305 |
| API | http://localhost:7105 |
| SQL Server | localhost:11433 |

EF Core migrations and data seeding run automatically when the API starts.

---

### Option B — Manual Setup

#### 1. Clone

```bash
git clone https://github.com/abdulhamidshahade/berryfy.git berryfy
cd berryfy
```

#### 2. Backend

```bash
# Restore packages
dotnet restore

# Set required user secrets (or edit appsettings.Development.json)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=.;Database=Berryfy;Trusted_Connection=True;MultipleActiveResultSets=true" \
  --project Berryfy.API

dotnet user-secrets set "ApiSettings:JwtOptions:SecretKey" "your-min-32-char-secret" \
  --project Berryfy.API

# Apply migrations (auto-applied on startup too)
dotnet ef database update \
  --startup-project Berryfy.API \
  --project Berryfy.Infrastructure

# Start the API
dotnet run --project Berryfy.API
```

API is available at `https://localhost:7105`.  
Health check: `https://localhost:7105/health`

#### 3. Frontend

```bash
cd Berryfy.Web
npm install
```

Create `Berryfy.Web/.env.local`:

```bash
API_BASE_URL=https://localhost:7105/api
API_BASE_AUTH=https://localhost:7105/api/auth
NEXTAUTH_URL=http://localhost:3000
COOKIE_SECURE=false
```

```bash
npm run dev
```

Frontend is available at `http://localhost:3000`.

---

## 🔧 Environment Variables

### `.env` (Docker Compose — from `.env.example`)

```bash
# SQL Server SA password — must be strong (uppercase + lowercase + digits + symbols, min 8 chars)
SA_PASSWORD=YourStrongSaPassword123!

# JWT secret key — minimum 32 characters
ApiSettings__JwtOptions__SecretKey=your-jwt-secret-key-min-32-chars-here

# Zoho SMTP — for OTP codes, password resets, and notifications
GmailSettings__Email=your-account@zohomail.com
GmailSettings__Password=your-zoho-app-password

# CORS — semicolon-separated list of allowed browser origins
Cors__Origins=http://localhost:30305;http://frontend:30305

# Cookie security (set false for plain HTTP, true when behind HTTPS)
COOKIE_SECURE=false
```

### `.env.local` (Frontend development)

```bash
API_BASE_URL=https://localhost:7105/api
API_BASE_AUTH=https://localhost:7105/api/auth
NEXTAUTH_URL=http://localhost:3000
COOKIE_SECURE=false
```

---

## 🔒 Security

- JWT access tokens with 2-hour expiry; refresh tokens with 7-day expiry and rotation
- Role-based authorization enforced at the controller/action level
- Email confirmation required before first login (6-digit OTP via Zoho SMTP)
- CORS configured from environment — no wildcards in production
- HTTPS enforcement and HSTS configurable via `Security:*` settings
- Input validation on every request DTO via FluentValidation
- Passwords hashed by ASP.NET Core Identity (PBKDF2)
- `.env` file is git-ignored — only `.env.example` is committed

---

## 📊 Performance

- Next.js Server-Side Rendering and React Server Components for fast initial load
- Edge middleware handles token refresh without an extra round-trip
- EF Core queries with `AsNoTracking()` for read operations
- Inventory uses a reserve/confirm pattern to prevent overselling under concurrent load
- Health check endpoint used by Docker Compose to delay dependent services until the DB is ready

---

## 🧪 Testing

A `Berryfy.Tests` xUnit project is included. Tests for services, repositories, and controllers are in progress.

---

## 🚀 Deployment

### Docker (production)

```bash
# On your server
git clone https://github.com/abdulhamidshahade/berryfy.git berryfy
cd berryfy
cp .env.example .env   # fill in production secrets
docker compose up -d --build
```

### Manual publish

```bash
# API
dotnet publish Berryfy.API -c Release -o ./publish/api

# Frontend
cd Berryfy.Web
npm run build
npm start
```

### Database migration script (for managed SQL instances)

```bash
dotnet ef migrations script \
  --project Berryfy.Infrastructure \
  --startup-project Berryfy.API \
  --output migration.sql
```

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Commit using conventional commits: `git commit -m 'feat: add amazing feature'`
4. Push and open a Pull Request

---

## 📄 License

Distributed under the MIT License.

---

## 👤 Developer

**Abdulhamid Shahade**  
📧 [shahade.abdulhamid@gmail.com](mailto:shahade.abdulhamid@gmail.com)  
🔗 [linkedin.com/in/abdulhamidshahade](https://linkedin.com/in/abdulhamidshahade)  
💻 [github.com/abdulhamidshahade](https://github.com/abdulhamidshahade)
