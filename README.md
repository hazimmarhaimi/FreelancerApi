FreelancerAPI Project Documentation
Overview
The FreelancerAPI is a RESTful API built with ASP.NET Core 8.0, designed to manage freelancer data with CRUD operations, search functionality, caching, and JWT-based authentication. This document provides a detailed overview of the project, refactors it using Clean Architecture principles, and includes well-documented code snippets to enhance maintainability and scalability.

Purpose: Manage freelancer profiles, including their skills, hobbies, and archival status, with efficient caching and secure access.
Technologies: ASP.NET Core 8.0, Entity Framework Core, IMemoryCache, JWT Authentication.
Key Features:
CRUD operations for freelancers.
Wildcard search by username or email.
Archiving/unarchiving freelancers.
Caching with IMemoryCache.
JWT-based authentication.
Current Date and Time: 04:44 PM +08 on Tuesday, May 20, 2025 (relevant for token expiration).
Project Structure
The current structure of FreelancerAPI mixes concerns within the FreelancersController. We'll refactor it into Clean Architecture to separate concerns, improve maintainability, and enhance testability.

Current Structure
Entities: Freelancer, Skillset, Hobby (in Entities folder).
Data Access: ApplicationDbContext, IRepository<T> (in Data and Repositories folders).
Controllers: FreelancersController (in Controllers folder).
DTOs: FreelancerDto, CreateFreelancerDto, UpdateFreelancerDto (in Dtos folder).
Refactored Structure with Clean Architecture
Clean Architecture organizes the project into concentric layers, ensuring dependencies flow inward and the business logic remains independent of external systems.

Layers
Entities (Core):
Core business models and rules.
Independent of frameworks.
Use Cases (Application Layer):
Application-specific business logic.
Orchestrates interactions between entities and external systems.
Interface Adapters (Controllers):
Converts data between use cases and external systems (e.g., HTTP requests).
Frameworks and Drivers (Infrastructure):
External systems like databases, caching, and web frameworks.
Directory Structure
text

Copy
FreelancerAPI/
├── Entities/
│   ├── Freelancer.cs
│   ├── Skillset.cs
│   └── Hobby.cs
├── UseCases/
│   ├── IGetFreelancerUseCase.cs
│   ├── GetFreelancerUseCase.cs
│   ├── ICreateFreelancerUseCase.cs
│   └── CreateFreelancerUseCase.cs
├── Controllers/
│   └── FreelancersController.cs
├── Dtos/
│   ├── FreelancerDto.cs
│   ├── CreateFreelancerDto.cs
│   └── UpdateFreelancerDto.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Repositories/
│   ├── IRepository.cs
│   └── EfRepository.cs
└── Program.cs
