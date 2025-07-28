# LangLearn Backend

LangLearn is a language learning platform that allows users to create decks of flashcards and grammar resources to aid in their language study. This repository contains the **LangLearn Backend** — a minimal API built with **.NET 9**, **Entity Framework Core**, **SQLite**, and **JWT-based authentication**.

For frontend code, visit the [LangLearn Frontend repository](https://github.com/PavelNikolaichev/langlearn-frontend)
## Features

- User registration and authentication (JWT)
- CRUD operations for:
  - **Decks**: Collections of flashcards
  - **Flashcards**: Question-answer pairs tied to a deck
  - **Grammar Sets**: Themed sets of grammar entries
  - **Grammar Entries**: Individual grammar rules/examples within a set

## Tech Stack

- .NET 9 Minimal API
- Entity Framework Core
- SQLite (file-based) / In-Memory provider (for tests)
- JWT Authentication (Bearer)
- FluentValidation for request validation
- xUnit for integration testing

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- (Optional) [SQLite CLI](https://www.sqlite.org/download.html)

## Configuration

Copy or modify `appsettings.json` / `appsettings.Development.json` to set:

```json
{
  "Jwt": {
    "Key": "<YourSuperSecretKeyHere>"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=LangLearn.db"
  }
}
```

Alternatively, set the `Jwt__Key` and `ConnectionStrings__DefaultConnection` environment variables or dotnet secrets.

## Getting Started

1. **Restore dependencies**
   ```bash
   dotnet restore
   ```

2. **Run database migrations**
   ```bash
   dotnet tool install --global dotnet-ef # if not installed
   dotnet ef database update --project LangLearn.Backend/LangLearn.Backend.csproj
   ```

3. **Run the API**
   ```bash
   dotnet run --project LangLearn.Backend/LangLearn.Backend.csproj
   ```

4. **Explore Swagger UI**
   Navigate to `https://localhost:5001/swagger` (or HTTP port) to view and test endpoints.

## Testing

The solution includes integration tests using an in-memory EF Core provider.

```bash
dotnet test LangLearn.Backend.Tests/LangLearn.Backend.Tests.csproj
```
