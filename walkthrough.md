# 🏛️ Architectural Deep-Dive & Blueprint

This document details the internal mechanics of the **ExpenseTracker** ecosystem, from infrastructure orchestration to the core business logic.

---

## 1. Request Lifecycle: `POST /api/expenses`
When a user interacts with the application, the request travels through several layers of abstraction.

* **Entry Point (Angular UI):** The user submits a form. The `ExpenseStore` (using Angular Signals) dispatches a request to `/api/expenses`.
* **Gateway (Nginx Proxy):** The request hits Nginx on port `80`. Nginx identifies the `/api` path, strips the prefix (as defined in `nginx.conf`), and routes the traffic to the `expense-backend` container on port `8080`.
* **Presentation Layer:** The `ExpensesController` receives the DTO (Data Transfer Object).
* **Security & Validation:** `FluentValidation` checks data integrity. If validation fails, the `CustomExceptionHandler` middleware intercepts the error and returns a clean, RFC-compliant error response.
* **Business Logic (Application):** The `ExpenseService` maps the DTO to a **Domain Entity**.
* **Persistence (Infrastructure):** EF Core receives the entity, translates the change into a SQL `INSERT` statement, and commits it to the **PostgreSQL** instance.



---

## 2. Visual Mental Models

### A. The Full Ecosystem Flow
This diagram illustrates the containerized journey of data across the Docker network.

```mermaid
graph TD
    subgraph "External World"
        User((User))
    end

    subgraph "Network Layer (Docker Network)"
        Proxy[Nginx Proxy:80]
    end

    subgraph "Containerized Backend"
        API[Presentation: API Controllers]
        APP[Application: Services & Validators]
        DOM[Domain: Pure Entities]
        INF[Infrastructure: EF Core & DB Context]
    end

    subgraph "Data Layer"
        DB[(PostgreSQL 16)]
    end

    User -->|HTTP Requests| Proxy
    Proxy -->|Forward| API
    API -->|Validation & Call| APP
    APP -->|Object Mapping| DOM
    APP -->|Persistence logic| INF
    INF -->|SQL Query| DB
```

### B. Clean Architecture (The Dependency Rule)
Dependencies always point inward. The Domain is the stable center; it does not know about the Database, the UI, or Docker.
```mermaid
graph BT
    Presentation["Presentation Layer <br/> API / Controllers"] --> Application
    Infrastructure["Infrastructure Layer <br/> DB Context / SMTP"] --> Application
    Application["Application Layer <br/> Bus. Logic / Interfaces"] --> Domain
    Domain["Domain Layer <br/> Pure Entities"]
    
    style Domain fill:#f9f,stroke:#333,stroke-width:4px
```

### C. Layer Breakdown

| Layer | Responsibility | Key Components |
| :--- | :--- | :--- |
| **Domain** | Core Business Logic | Entities, Value Objects, Logic Exceptions |
| **Application** | Orchestration | Services, DTOs, Validators, Interfaces |
| **Infrastructure** | External Concerns | EF Core, Migrations, Email (SMTP), Logging |
| **Presentation** | Entry Points | Controllers, Middleware, Auth (JWT) |