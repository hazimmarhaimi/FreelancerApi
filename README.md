# FreelancerAPI

Welcome to **FreelancerAPI**, a robust and secure RESTful API built with **ASP.NET Core 8.0** for managing freelancer profiles. This API provides CRUD operations, search, archiving, caching, and JWT-based authentication — all implemented using **Clean Architecture** principles to ensure high maintainability, testability, and scalability.

> 🕓 **Current Date & Time:** 04:47 PM (GMT+8), Tuesday, May 20, 2025  
> 🔗 **Repository:** [GitHub – FreelancerAPI](https://github.com/yourusername/FreelancerAPI)

---

## 📚 Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Usage](#usage)
- [API Endpoints](#api-endpoints)
- [Configuration](#configuration)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)

---

## 🚀 Features

- ✅ **CRUD Operations** – Create, Read, Update, Delete freelancer profiles
- 🔍 **Search** – Wildcard search by username or email
- 🗂️ **Archiving** – Archive/unarchive freelancer profiles
- ⚡ **Caching** – In-memory caching with `IMemoryCache` for performance boost
- 🔐 **Authentication** – JWT-based secure API access
- 🧱 **Clean Architecture** – Separation of concerns across layers:
  - Entities (Domain)
  - Use Cases (Application)
  - Infrastructure (Data & Services)
  - API (Presentation Layer)

---

## 🏗️ Architecture

This project follows **Clean Architecture** and organizes code into:
- `Domain`: Core entities and interfaces
- `Application`: Business rules and use cases
- `Infrastructure`: Data access (EF Core, caching)
- `API`: Controllers and middleware

This structure supports scalability, unit testing, and clean separation of concerns.

---

## 🛠️ Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022+ or VS Code
- SQL Server (or in-memory DB for testing)
- Postman (optional, for API testing)

---

## ⚙️ Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/FreelancerAPI.git
   cd FreelancerAPI
