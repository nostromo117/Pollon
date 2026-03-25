# Pollon

Pollon is a modern, distributed Content Management System (CMS) and Delivery platform built with **.NET Aspire**. It demonstrates a decoupled architecture between content management (Backoffice) and content delivery (Frontend).

## Architecture

The project is composed of several microservices coordinated by **.NET Aspire**:

- **Pollon.AppHost**: The orchestration project that manages service discovery and infrastructure.
- **Pollon.Backoffice.Api**: A REST API built with **Marten** and **PostgreSQL** for managing dynamic content types and items.
- **Pollon.Backoffice.Web**: A **Blazor** application (with **MudBlazor** Material Design UI) for all administrative tasks.
- **Pollon.Media.Api**: A dedicated microservice for media asset management. It handles file uploads and serves binary content (images, etc.) stored in **PostgreSQL** via Marten. Acts as an internal CDN, completely decoupled from the Backoffice business logic. The storage layer is abstracted behind `IMediaStorageService`, enabling future migration to cloud providers (AWS S3, Azure Blob Storage) without changing the consumers.
- **Pollon.Content.Api**: A delivery API using **SQL Server** and **EntityFramework Core** for high-performance read models. It synchronizes with the backoffice via events.
- **Pollon.Frontend.Web**: A customer-facing **Blazor** site with SEO-friendly routing and hierarchical slugs.
- **Pollon.Contracts**: Shared models and events used for communication between services.
- **Pollon.ServiceDefaults**: Standard Aspire service defaults (observability, health checks).

## Technologies

- **System Orchestration**: .NET Aspire
- **Database (Backoffice + Media)**: PostgreSQL + Marten (Document Database)
- **Database (Content)**: SQL Server (Read Models)
- **Messaging**: Wolverine + RabbitMQ (Event-driven synchronization)
- **UI (Backoffice)**: [MudBlazor](https://mudblazor.com/) (Material Design for Blazor)
- **UI (Frontend)**: Blazor Server

## Prerequisites

To run this project, you need the following installed:

1. **.NET 10 SDK** (or the latest version compatible with the project).
2. **.NET Aspire Workload**: Install via `dotnet workload install aspire`.
3. **Docker Desktop**: Required to run SQL Server, PostgreSQL, and RabbitMQ containers.
4. **Git**: For version control.
5. **EF Core Tooling**: `dotnet tool install --global dotnet-ef`.

## Getting Started

1. Clone the repository.
2. Ensure Docker Desktop is running.
3. Open the solution in **Visual Studio 2022** (17.10+) or **VS Code**.
4. Set `Pollon.AppHost` as the startup project and run (F5).
5. Alternatively, run from the command line:
   ```bash
   dotnet run --project Pollon.AppHost/Pollon.AppHost.csproj
   ```

## Key Features

- **Dynamic Content Types**: Create custom content structures (Text, Number, Date, Boolean, RichText, Image) via UI, backed by a strongly-typed `ContentFieldType` enum.
- **Media Management**: Upload images directly from the Backoffice. Files are stored in the dedicated `Pollon.Media.Api` and served transparently to both the Backoffice and Frontend via lightweight reverse proxies.
- **Business Validation**: Server-side validation for slugs and system names.
- **SEO Optimization**: Automatic and manual slug generation for content items.
- **Event Sourcing**: Changes in the backoffice are published to RabbitMQ and consumed by the Content API to update read models.
