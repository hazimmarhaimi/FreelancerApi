FreelancerAPI





Welcome to FreelancerAPI, a robust RESTful API built with ASP.NET Core 8.0 to manage freelancer profiles, including CRUD operations, search functionality, caching, and JWT-based authentication. This project is designed with Clean Architecture principles to ensure maintainability, testability, and scalability.

Current Date and Time: 04:47 PM +08 on Tuesday, May 20, 2025.
Repository: https://github.com/yourusername/FreelancerAPI
Table of Contents
Features
Architecture
Getting Started
Prerequisites
Installation
Usage
API Endpoints
Configuration
Contributing
License
Contact
Features
CRUD Operations: Create, read, update, and delete freelancer profiles.
Search Functionality: Wildcard search by username or email.
Archiving: Archive or unarchive freelancers.
Caching: In-memory caching with IMemoryCache for performance.
Authentication: Secure access with JWT tokens (valid until 05:47 PM +08 on May 20, 2025, by default).
Clean Architecture: Separated layers for entities, use cases, controllers, and infrastructure.
Architecture
The FreelancerAPI follows Clean Architecture to ensure a decoupled and maintainable codebase:

Layers
Entities (Core):
Business models (e.g., Freelancer, Skillset, Hobby) with core rules.
Independent of frameworks or databases.
Use Cases (Application Layer):
Application-specific logic (e.g., GetFreelancerUseCase, CreateFreelancerUseCase).
Defines interfaces for external systems.
Interface Adapters (Controllers):
Translates HTTP requests to use case calls (e.g., FreelancersController).
Frameworks and Drivers (Infrastructure):
External implementations (e.g., ApplicationDbContext, IMemoryCache, EF Core).
