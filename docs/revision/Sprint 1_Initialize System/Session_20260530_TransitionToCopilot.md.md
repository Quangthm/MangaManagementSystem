# Project Hand-off & Session Context: Cursor to VS Copilot
**Date:** 2026-05-30
**Project:** Manga Management System
**Tech Stack:** C#, .NET 8, EF Core, MudBlazor, Clean Architecture

## 1. Current State (What was completed)
- **Backend Core:** All 21 Entities, DTOs (`record` types), Repositories (`IUnitOfWork`), and Application Services are 100% complete and registered in DI.
- **UI Integration:** The static Figma-to-MudBlazor UI files have been successfully merged into `src/MangaManagementSystem.Web`.
- **Refactoring:** All UI components now use the correct `MangaManagementSystem.Web` namespace. `_Imports.razor` and `Program.cs` are set up.

## 2. Current Blocker (Priority Fix Required)
Running `dotnet run` or `dotnet build` throws a Dependency Injection exception:
`Unable to resolve service for type 'Microsoft.EntityFrameworkCore.DbContext' while attempting to activate 'MangaManagementSystem.Infrastructure.Repositories.GenericRepository...`

**Suspected Causes:**
1. `GenericRepository.cs` and `UnitOfWork.cs` might be injecting the abstract `DbContext` instead of the specific `ApplicationDbContext`.
2. `services.AddDbContext<ApplicationDbContext>(...)` might be missing or misconfigured in `MangaManagementSystem.Infrastructure/DependencyInjection.cs`.
3. The Web project's `Program.cs` might not be calling the Infrastructure registration correctly.

## 3. Immediate Tasks for Copilot
- **Task 1:** Diagnose and fix the `DbContext` Dependency Injection error to get a green 100% successful build.
- **Task 2:** Once the build succeeds, wire up the `Pages/Mangaka/SeriesList.razor` UI component. Inject `ISeriesService`, call `GetAllSeriesAsync()` in `OnInitializedAsync`, and replace the static placeholder table with dynamic data binding from `IEnumerable<SeriesDto>`.