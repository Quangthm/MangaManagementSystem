@workspace Please create a new Markdown file at `docs/revision/Session_20260530_TransitionToCursor.md`. This file will serve as the master context and hand-off document for another AI assistant (Cursor) to continue the development of this project.

Populate the file with the exact following content:

# Project Hand-off & Session Context
**Project:** Manga Management System
**Tech Stack:** C#, .NET 8, Entity Framework Core, SQL Server
**Architecture:** Clean Architecture (Domain, Application, Infrastructure, Web)
**Developer Profile for AI Context:** The developer has a strong background in Java EE (MVC2) and is currently transitioning to C# and the .NET framework. Please provide clear, structured explanations when introducing .NET-specific concepts and maintain a focus on Enterprise best practices.

## 1. Current State (🟢 SUCCESS)
- **Database:** Initialized on SQL Server (`LONGPC\SQLEXPRESS`) using EF Core Migrations. 21 tables created across 3 schemas (`auth`, `manga`, `audit`).
- **Data Access Layer:** Implemented the Generic Repository pattern (`IGenericRepository`, `GenericRepository`) and the Unit of Work pattern (`IUnitOfWork`, `UnitOfWork`).
- **Application Layer:** 
  - All 21 DTOs have been created using concise C# 9+ `record` syntax.
  - Application Services for Core Entities (`SeriesService`, `ChapterService`, `UserService`) are fully implemented and registered in Dependency Injection.

## 2. Strict Coding Rules & Constraints (MUST FOLLOW)
1. **DTOs:** Always use C# `record` types. Apply Data Annotation validations (`[Required]`, `[MaxLength]`) directly in the constructor. Exclude audit properties (CreatedAt, UpdatedBy) and navigation objects from Create/Update DTOs. Ensure `PasswordHash` is never exposed in Read DTOs.
2. **Repositories:** Do NOT call `SaveChanges()` or `SaveChangesAsync()` inside any Repository. Repositories only add/update/delete states in the `ApplicationDbContext`.
3. **Application Services:** 
   - ONLY inject `IUnitOfWork`. Do NOT inject specific repositories or `ApplicationDbContext` directly into services.
   - Responsible for mapping DTOs to Entities manually (AutoMapper is not used yet).
   - Responsible for calling `await _unitOfWork.SaveChangesAsync()`.

## 3. Future Work (Next Steps for Cursor AI)
- **Immediate Task:** Scaffold the Application Services for the remaining entities (e.g., `ChapterPageService`, `FileResourceService`, `SeriesProposalService`) following the exact same pattern as `SeriesService`.
- **Subsequent Task:** Begin setting up the Presentation Layer (Blazor UI integration or Web API Controllers) to expose these services.

## 4. Resume Procedure
1. Open the solution in Cursor.
2. Index the codebase (Cmd/Ctrl + Enter in Cursor Chat) so Cursor understands the `IUnitOfWork` and `record` DTO structures.
3. Review `src/MangaManagementSystem.Application/Services/SeriesService.cs` as the golden standard template for upcoming services.
4. Run `dotnet build` to ensure the project compiles successfully before writing new code.