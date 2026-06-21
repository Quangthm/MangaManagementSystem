# MangaManagementSystem C# Backend Naming and Coding Conventions

**Project:** Manga Creation Workflow and Publishing Management System  
**Target stack:** .NET 8, ASP.NET Core Web API, Clean Architecture, SQL Server stored procedures, Cloudinary integration  
**Purpose:** Provide one team convention for C# backend code based on Microsoft Learn and C# Corner naming guidance, adapted for this project.

## 1. Source priority

Use the following priority when rules differ:

1. **Project-specific backend rules** — because this project uses Clean Architecture, stored procedures, SQL Server GUID IDs, Cloudinary, and .NET 8.
2. **Microsoft Learn C# coding conventions** — official modern .NET/C# guidance.
3. **C# Corner naming conventions** — useful naming checklist, but some parts are older or internally inconsistent.

## 2. Project-specific backend decisions

These conventions are not only style rules. They prevent confusion while the backend is being converted to the new GUID-based database schema.

### 2.1 ID and GUID naming

| Item | Convention | Example |
|---|---|---|
| Database-backed GUID IDs in C# | Use `Guid` | `public Guid UserId { get; set; }` |
| Method parameters for IDs | camelCase + entity name + `Id` | `Guid userId`, `Guid seriesId` |
| DTO properties for IDs | PascalCase + entity name + `Id` | `UserId`, `FileResourceId` |
| SQL parameters in C# | Use the SQL parameter name exactly as expected by the stored procedure | `@user_id`, `@target_user_id` |
| SQL parameter type for GUID | `SqlDbType.UniqueIdentifier` | `new SqlParameter("@user_id", SqlDbType.UniqueIdentifier)` |
| Audit ID exception | Keep the type that matches the DB audit ID, not `Guid` | `long AuditEventId` if DB uses `BIGINT` |

**Rule:** Do not use `int` or `long` for entity IDs that are now `uniqueidentifier` in the database. The exception is audit ID if the audit table still uses a numeric identity.

### 2.2 Clean Architecture namespace pattern

Use namespaces that match the project layer and folder structure.

```csharp
namespace MangaManagementSystem.Domain.Entities;
namespace MangaManagementSystem.Application.DTOs.Auth;
namespace MangaManagementSystem.Application.Interfaces.Repositories;
namespace MangaManagementSystem.Infrastructure.Persistence.Repositories;
namespace MangaManagementSystem.API.Controllers;
```

Use **file-scoped namespaces** for normal new C# files.

```csharp
namespace MangaManagementSystem.Application.DTOs.Auth;

public sealed class RegisterRequestDto
{
    public required string Username { get; init; }
}
```

### 2.3 Backend type naming

| Type | Convention | Example |
|---|---|---|
| Entity | Singular PascalCase noun | `User`, `Series`, `FileResource` |
| DTO | PascalCase + `Dto`, `RequestDto`, or `ResponseDto` | `RegisterRequestDto`, `UserProfileResponseDto` |
| Interface | `I` + PascalCase | `IUserRepository`, `ICloudinaryStorageService` |
| Repository | Entity + `Repository` | `UserRepository`, `FileResourceRepository` |
| Service | Feature + `Service` | `AuthService`, `RegistrationService` |
| Controller | Feature + `Controller` | `AuthController`, `AdminUsersController` |
| Validator | Target + `Validator` | `RegisterRequestValidator` |
| Options/config class | Feature + `Options` or `Settings` | `CloudinarySettings`, `GoogleAuthOptions` |
| Exception | Meaning + `Exception` | `UserNotFoundException` |

### 2.4 Stored procedure wrapper naming

C# method names should be readable and business-focused. SQL procedure names may remain database-style.

```csharp
Task<Guid> CreateUserWithOptionalPortfolioAsync(
    CreateUserWithOptionalPortfolioCommand command,
    CancellationToken cancellationToken);

Task ChangeUserStatusAsync(
    Guid adminUserId,
    Guid targetUserId,
    UserStatusCode newStatusCode,
    string? reason,
    CancellationToken cancellationToken);
```

Use the stored procedure name exactly when calling SQL:

```csharp
command.CommandText = "auth.usp_Admin_ChangeUserStatus";
command.CommandType = CommandType.StoredProcedure;
```

### 2.5 Async method naming

All methods that return `Task` or `Task<T>` should end with `Async`.

```csharp
Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
```

Exception: event handlers or framework-required method names may follow framework conventions.

### 2.6 CancellationToken placement

Put `CancellationToken cancellationToken` as the final parameter.

```csharp
Task<IReadOnlyList<UserSummaryDto>> GetPendingUsersAsync(
    int page,
    int pageSize,
    CancellationToken cancellationToken);
```

### 2.7 Avoid overloading methods with unclear meanings

Prefer descriptive names over ambiguous overloads.

Good:

```csharp
GetByEmailAsync(string email, CancellationToken cancellationToken)
GetByUsernameAsync(string username, CancellationToken cancellationToken)
```

Avoid:

```csharp
GetAsync(string value, CancellationToken cancellationToken)
```

## 3. Naming conventions

### 3.1 Casing summary

| Code element | Convention | Example |
|---|---|---|
| Namespace | PascalCase | `MangaManagementSystem.Application.Services` |
| Class / record / struct | PascalCase | `SeriesBoardPoll`, `FileUploadResultDto` |
| Interface | `I` + PascalCase | `IUserRepository` |
| Method | PascalCase | `ChangeUserStatusAsync` |
| Property | PascalCase | `DisplayName`, `PortfolioFileId` |
| Public constant | PascalCase | `MaxPortfolioFileSizeBytes` |
| Enum type | PascalCase, singular unless flags | `UserStatusCode` |
| Flags enum type | PascalCase, plural | `FileAccessPermissions` |
| Enum value | PascalCase, no prefix | `PendingApproval`, `Active`, `Rejected` |
| Method parameter | camelCase | `targetUserId`, `newStatusCode` |
| Local variable | camelCase | `uploadedFile`, `userStatus` |
| Private field | `_camelCase` | `_userRepository`, `_cloudinaryService` |
| Private static field | `_camelCase` | `_cachedStatuses` |
| Private static readonly field | `_camelCase` or `s_camelCase`; choose one project-wide | `_allowedFileTypes` |

Recommended project choice: use `_camelCase` for private fields, including injected dependencies.

```csharp
private readonly IUserRepository _userRepository;
private readonly IFileStorageService _fileStorageService;
```

### 3.2 Classes

Use PascalCase. Use nouns or noun phrases. Do not add prefixes or underscores.

Good:

```csharp
public sealed class UserRepository
public sealed class FileResource
public sealed class RegisterRequestDto
```

Avoid:

```csharp
public sealed class clsUserRepository
public sealed class user_repository
public sealed class User_Repository
```

### 3.3 Methods

Use PascalCase. Use verb or verb phrase names.

Good:

```csharp
CreateUserWithOptionalPortfolioAsync
ChangeUserStatusAsync
UploadPortfolioAsync
CalculateSha256HashAsync
```

Avoid all-caps method names.

```csharp
// Avoid
CREATEUSER()
GETUSERBYID()
```

### 3.4 Parameters and local variables

Use camelCase. Use meaningful names based on purpose, not type.

Good:

```csharp
Guid targetUserId
string portfolioFileName
int pageSize
```

Avoid Hungarian notation and type-only names.

```csharp
// Avoid
string strName
Guid guidUser
int intCount
```

### 3.5 Properties

Use PascalCase. Do not use `Get` or `Set` prefixes for properties.

Good:

```csharp
public string DisplayName { get; init; }
public Guid? PortfolioFileId { get; init; }
```

Avoid:

```csharp
public string GetDisplayName { get; set; }
public Guid? SetPortfolioFileId { get; set; }
```

### 3.6 Interfaces

Use `I` followed by PascalCase.

Good:

```csharp
public interface IUserRepository
public interface IFileStorageService
public interface ICurrentUserService
```

### 3.7 Enums

Use PascalCase for enum type names and enum values. Use a singular enum type name unless it is a flags enum.

Good:

```csharp
public enum UserStatusCode
{
    PendingApproval,
    Active,
    Rejected,
    Disabled
}
```

Avoid suffixes like `Enum`, `Flag`, or `Flags` in enum type names.

```csharp
// Avoid
public enum UserStatusEnum
public enum PermissionFlags
```

### 3.8 Events and event arguments

Use verb-based event names. Use `EventArgs` suffix for event argument classes.

```csharp
public event EventHandler<UserStatusChangedEventArgs>? UserStatusChanged;
```

For normal event handlers, use parameters named `sender` and `e`.

```csharp
private void OnUserStatusChanged(object? sender, UserStatusChangedEventArgs e)
{
}
```

### 3.9 Assemblies and projects

Use root project name + layer name.

```text
MangaManagementSystem.Domain
MangaManagementSystem.Application
MangaManagementSystem.Infrastructure
MangaManagementSystem.API
MangaManagementSystem.Web
```

## 4. Type usage conventions

### 4.1 Use C# language keywords for built-in types

Use C# keywords instead of .NET runtime type names.

Good:

```csharp
string username;
int pageSize;
long fileSizeBytes;
bool isDeleted;
```

Avoid:

```csharp
String username;
Int32 pageSize;
Int64 fileSizeBytes;
Boolean isDeleted;
```

Use the correct type for the data. For example, use `long` for file sizes if the database uses `BIGINT`, and use `Guid` for IDs stored as `uniqueidentifier`.

### 4.2 `var` usage

Use `var` only when the type is obvious from the right-hand side.

Good:

```csharp
var user = new User();
var fileSizeBytes = 1024L;
var command = connection.CreateCommand();
```

Use explicit types when the right-hand side does not clearly show the type.

```csharp
User? user = await _userRepository.GetByEmailAsync(email, cancellationToken);
int pendingCount = await _userRepository.CountPendingUsersAsync(cancellationToken);
```

Use `var` freely for LINQ projections where anonymous or complex generic types improve readability.

### 4.3 Strings

Use string interpolation for short string composition.

```csharp
var message = $"User {username} is pending approval.";
```

Use `StringBuilder` when appending many strings in loops or large text generation.

Use raw string literals for large JSON, SQL snippets, or multi-line test data when it improves readability.

```csharp
var detailJson = """
{
  "action": "USER_STATUS_CHANGED"
}
""";
```

### 4.4 Object and collection initialization

Use object initializers when they make construction clearer.

```csharp
var dto = new UserProfileResponseDto
{
    UserId = user.UserId,
    Username = user.Username,
    DisplayName = user.DisplayName
};
```

Use collection expressions where supported and clear.

```csharp
string[] allowedPurposes =
[
    "SERIES_PROPOSAL",
    "SERIES_COVER",
    "CHAPTER_PAGE_VERSION",
    "TASK_REFERENCE",
    "EDITORIAL_ATTACHMENT",
    "REGISTRATION_PORTFOLIO",
    "USER_AVATAR"
];
```

## 5. Formatting and layout

### 5.1 Indentation

Use four spaces for indentation. Do not use tab characters.

### 5.2 Braces

Use Allman braces: opening braces go on their own line.

```csharp
if (user.StatusCode == UserStatusCode.PendingApproval)
{
    return false;
}
```

### 5.3 One statement per line

Good:

```csharp
user.StatusCode = UserStatusCode.Active;
user.UpdatedAtUtc = DateTime.UtcNow;
```

Avoid:

```csharp
user.StatusCode = UserStatusCode.Active; user.UpdatedAtUtc = DateTime.UtcNow;
```

### 5.4 One declaration per line

Good:

```csharp
Guid userId;
Guid roleId;
```

Avoid:

```csharp
Guid userId, roleId;
```

### 5.5 Blank lines

Use one blank line between methods and between larger logical sections.

```csharp
public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
{
    // ...
}

public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
{
    // ...
}
```

### 5.6 Line length

Microsoft documentation recommends short lines for documentation readability. For this backend project, use a practical team limit:

- Preferred: keep lines readable and split long expressions.
- Recommended maximum: around 120 characters.
- Hard rule: do not sacrifice clarity just to fit a line limit.

### 5.7 Parentheses in complex expressions

Use parentheses to make complex boolean logic clear.

```csharp
if ((user.StatusCode == UserStatusCode.Active) && (user.RoleName == RoleNames.Admin))
{
    return true;
}
```

## 6. File layout

Use this order for normal C# files:

1. `using` directives
2. File-scoped namespace
3. Class/record/interface declaration
4. Constants/static readonly fields
5. Private readonly fields
6. Constructor
7. Public properties
8. Public methods
9. Private helper methods

Example:

```csharp
using MangaManagementSystem.Application.Interfaces.Repositories;
using MangaManagementSystem.Domain.Entities;

namespace MangaManagementSystem.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Users.FindAsync([userId], cancellationToken).AsTask();
    }
}
```

## 7. Comment conventions

### 7.1 Prefer readable code first

Do not comment obvious code. Rename variables or extract methods instead.

### 7.2 Use single-line comments for short explanations

```csharp
// Cloudinary upload already succeeded, so attempt cleanup if SQL fails.
```

### 7.3 Comment style

- Put comments on a separate line, not at the end of a code line.
- Start comment text with an uppercase letter.
- End full-sentence comments with a period.
- Put one space after `//`.

### 7.4 XML comments

Use XML comments for public APIs, complex service contracts, or team-facing interfaces.

```csharp
/// <summary>
/// Changes a user's account status through the database status-change workflow.
/// </summary>
Task ChangeUserStatusAsync(
    Guid adminUserId,
    Guid targetUserId,
    UserStatusCode newStatusCode,
    string? reason,
    CancellationToken cancellationToken);
```

## 8. Error handling conventions

### 8.1 Catch only what you can handle

Catch specific exception types when you can add useful handling, cleanup, logging, or domain-specific response behavior.

Good:

```csharp
try
{
    await _userRepository.ChangeUserStatusAsync(
        adminUserId,
        targetUserId,
        newStatusCode,
        reason,
        cancellationToken);
}
catch (SqlException ex) when (IsDuplicateKeyViolation(ex))
{
    throw new DuplicateUserException("The username or email is already reserved.", ex);
}
```

Avoid catching `Exception` just to hide errors.

```csharp
// Avoid
catch (Exception)
{
    return false;
}
```

### 8.2 Preserve stack traces

Use `throw;` instead of `throw ex;` when rethrowing.

### 8.3 Use `using` declarations for disposables

```csharp
await using SqlConnection connection = new(_connectionString);
await using SqlCommand command = connection.CreateCommand();
```

## 9. Async and LINQ conventions

### 9.1 Use async/await for I/O-bound operations

Database calls, Cloudinary calls, file reads, and HTTP calls should be asynchronous.

```csharp
public async Task<FileUploadResultDto> UploadAsync(
    Stream fileStream,
    string fileName,
    CancellationToken cancellationToken)
{
    // ...
}
```

### 9.2 LINQ variable names

Use meaningful names that describe the query result.

Good:

```csharp
var pendingUsers = users.Where(user => user.StatusCode == UserStatusCode.PendingApproval);
```

Avoid:

```csharp
var data = users.Where(x => x.StatusCode == UserStatusCode.PendingApproval);
```

### 9.3 Anonymous type property names

Use PascalCase aliases when projecting anonymous types.

```csharp
var userSummaries = users.Select(user => new
{
    UserId = user.UserId,
    DisplayName = user.DisplayName
});
```

## 10. Backend-specific examples

### 10.1 Good repository method

```csharp
public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
{
    return await _dbContext.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(user => user.UserId == userId, cancellationToken);
}
```

### 10.2 Good stored procedure call shape

```csharp
public async Task ChangeUserStatusAsync(
    Guid adminUserId,
    Guid targetUserId,
    UserStatusCode newStatusCode,
    string? reason,
    CancellationToken cancellationToken)
{
    await using SqlConnection connection = new(_connectionString);
    await using SqlCommand command = connection.CreateCommand();

    command.CommandText = "auth.usp_Admin_ChangeUserStatus";
    command.CommandType = CommandType.StoredProcedure;

    command.Parameters.Add(new SqlParameter("@admin_user_id", SqlDbType.UniqueIdentifier)
    {
        Value = adminUserId
    });

    command.Parameters.Add(new SqlParameter("@target_user_id", SqlDbType.UniqueIdentifier)
    {
        Value = targetUserId
    });

    command.Parameters.Add(new SqlParameter("@new_status_code", SqlDbType.NVarChar, 30)
    {
        Value = newStatusCode.ToString().ToUpperInvariant()
    });

    command.Parameters.Add(new SqlParameter("@reason", SqlDbType.NVarChar, 500)
    {
        Value = string.IsNullOrWhiteSpace(reason) ? DBNull.Value : reason.Trim()
    });

    await connection.OpenAsync(cancellationToken);
    await command.ExecuteNonQueryAsync(cancellationToken);
}
```

## 11. Conflicts and resolution decisions

| Topic | Microsoft Learn | C# Corner | Conflict? | Project decision |
|---|---|---|---|---|
| Built-in type names | Use C# keywords like `string`, `int` instead of runtime names like `System.String`, `System.Int32`. | Use native data types like `int` instead of `Int32`. | No major conflict. | Use C# keywords. Use `Guid` for GUID IDs because it has no C# keyword. |
| Class naming | Not heavily emphasized on this page, but examples use PascalCase. | PascalCase, nouns/noun phrases, no prefixes, no underscores. | No conflict. | Use PascalCase singular nouns for entities and services. |
| Method naming | Examples use PascalCase. | PascalCase, max 7 parameters. | Minor gap: Microsoft page does not mention max parameter count. | Use PascalCase. Prefer request/command DTOs when parameter lists become long. |
| Parameters and locals | Microsoft gives semantic variable naming and `var` guidance. | camelCase, no Hungarian notation, no underscores. | No conflict. | Use camelCase and meaningful names. |
| Private fields | Microsoft page does not define a private field naming rule here. | Says private member variables should use `_camelCase`, but later says field names should be PascalCase with no underscores. | Yes, C# Corner conflicts with itself. | Use `_camelCase` for private fields. Avoid public mutable fields; use properties. |
| Public fields | Microsoft page does not focus on public field naming. | Says public member variables use PascalCase. | Not a direct conflict, but public fields are usually avoided in modern backend code. | Avoid public mutable fields. Use PascalCase properties. |
| Interface naming | Not emphasized in the Microsoft page opened. | Prefix interface names with `I`. | No conflict. | Use `IUserRepository`, `IFileStorageService`, etc. |
| Enum naming | Not emphasized in the Microsoft page opened. | PascalCase; singular unless flags; no `Enum`, `Flag`, or `Flags` suffix; no prefixes on values. | No conflict. | Follow C# Corner here. |
| `var` usage | Use `var` only when type is obvious; use it for LINQ projections where it improves readability. | Not covered. | No conflict. | Follow Microsoft. |
| Namespace style | Use file-scoped namespaces for most single-namespace files; put `using` directives outside namespace. | Namespace names PascalCase. | No conflict. | Use PascalCase file-scoped namespaces and `using` outside namespace. |
| Braces and indentation | Four spaces, no tabs, Allman braces. | Not covered in detail. | No conflict. | Follow Microsoft. |
| Line length | Microsoft docs recommend short lines for documentation readability. | Not covered. | No direct conflict, but 65 characters is too strict for backend code. | Prefer readable lines; use about 120 characters as a practical team limit. |
| WebForms control prefixes | Not covered. | Lists old control prefixes like `btn`, `ddl`, `txt`. | Conflicts with the article's own “do not use abbreviations” advice and is not relevant to this backend. | Do not use WebForms-style prefixes in .NET 8 backend code. |
| Comments | Single-line comments for brief explanations; XML comments for public members; comment on separate line. | Shows XML comments in interface example but does not give detailed style. | No conflict. | Follow Microsoft. |

## 12. Final team convention checklist

Before committing backend C# code, check:

- [ ] Entity IDs that map to `uniqueidentifier` use `Guid`, not `int`/`long`.
- [ ] Audit ID keeps the database audit ID type.
- [ ] Classes, methods, properties, DTOs, and enums use PascalCase.
- [ ] Parameters and local variables use camelCase.
- [ ] Private fields use `_camelCase`.
- [ ] Interfaces start with `I`.
- [ ] Async methods end with `Async`.
- [ ] `CancellationToken cancellationToken` is the last parameter.
- [ ] SQL GUID parameters use `SqlDbType.UniqueIdentifier`.
- [ ] Built-in types use C# keywords: `string`, `int`, `long`, `bool`.
- [ ] No Hungarian notation: avoid `strName`, `intCount`, `guidUserId`.
- [ ] No WebForms-style prefixes in backend code.
- [ ] Use file-scoped namespaces for new files.
- [ ] Put `using` directives outside namespaces.
- [ ] Use four spaces, no tabs.
- [ ] Use Allman braces.
- [ ] Use one statement per line and one declaration per line.
- [ ] Catch only exceptions that can be handled meaningfully.
- [ ] Use async I/O for database, Cloudinary, file, and HTTP operations.
- [ ] Use XML comments for public interfaces or non-obvious service contracts.

## 13. Source links

- Microsoft Learn — Common C# code conventions: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
- C# Corner — C# Naming Conventions: https://www.c-sharpcorner.com/UploadFile/8a67c0/C-Sharp-coding-standards-and-naming-conventions/
