# Project Regression Testing

## Purpose

This project-wide regression foundation provides an automated safety net
for selected high-risk MangaManagementSystem flows.

It is designed to detect regressions in important application and API
contracts without requiring production databases, external services,
email delivery, Cloudinary, OAuth, reCAPTCHA, or AI services.

This suite is not intended to represent complete functional coverage of
the entire MangaManagementSystem.

## Baseline

Before this regression foundation was added, the current main baseline
did not contain a registered automated test project or project-wide
regression script.

The regression project is:

    tests/MangaManagementSystem.ProjectRegressionTests/

It is registered in:

    MangaManagementSystem.slnx

## Test technology

The regression project uses:

- .NET 8
- xUnit
- Microsoft.NET.Test.Sdk
- Moq
- coverlet.collector
- Cobertura coverage output

Production abstractions are mocked where appropriate so the current
suite remains deterministic and does not modify a real database.

## Current regression scope

### Authentication and current-account authorization

File:

    Auth/AuthenticatedActorResolverTests.cs

Tests:

- Invalid authenticated identity is rejected.
- Missing database user is rejected.
- Disabled account is rejected.
- A token claiming Admin does not override a current Mangaka database role.
- A stale token role does not override a current Admin database role.

Current test count: 5.

### Profile password reset

File:

    Profile/ProfilePasswordControllerTests.cs

Tests:

- Invalid actor cannot reset a password.
- Empty OTP is rejected before OTP verification.
- Password shorter than the minimum length is rejected.
- Invalid OTP does not reset the password.
- Valid OTP resets the password.
- Inactive account cannot request a profile password OTP.

Current test count: 6.

### Series workspace authorization

File:

    Series/SeriesWorkspaceEntryControllerTests.cs

Tests:

- Empty series slug is rejected before actor resolution.
- Invalid identity is rejected.
- Missing current user is rejected.
- Inactive account is forbidden.
- Active actor ID is forwarded to the workspace-entry query.

Current test count: 5.

### Admin account workflows

File:

    Admin/AdminUserCommandHandlersTests.cs

Tests:

- Administrator cannot disable their own account.
- Administrator cannot reject their own account.
- Disable forwards the target user and reason correctly.
- Reject forwards the reason and reloads the updated user.
- Activate forwards the correct actor and target IDs.

Current test count: 5.

### Notifications

File:

    Notifications/NotificationHandlersTests.cs

Tests:

- Empty recipient ID is rejected.
- Empty notification ID is rejected.
- Mark-as-read forwards recipient, notification, and read timestamp.
- Mark-all-as-read returns the repository update count.
- Unread-count query returns the repository count.

Current test count: 5.

### Assistant completed work

File:

    Assistant/AssistantCompletedWorkHandlerTests.cs

Tests:

- Empty actor ID is rejected before repository access.
- Empty completed-work data produces a zero summary.
- Task type, region count, and estimated compensation are aggregated.
- Updated/completed timestamps determine recent-item ordering.

Current test count: 4.

## Current automated result

Latest verified regression execution:

- Total tests: 30
- Passed: 30
- Failed: 0
- Skipped: 0
- Test result: PASS
- Full solution build: PASS
- Build errors: 0

The latest full build reported 39 warnings while compiling the Web
project. These warnings do not prevent the current solution from
building successfully. This regression change does not modify Web
production source to address those warnings.

## Code coverage

Coverage is generated with:

    XPlat Code Coverage

Output format:

    coverage.cobertura.xml

Current coverage snapshot:

| Package | Line coverage | Branch coverage |
| --- | ---: | ---: |
| MangaManagementSystem.API | 4.11% | 4.51% |
| MangaManagementSystem.Application | 2.58% | 0.85% |
| MangaManagementSystem.Domain | 2.32% | 0.00% |
| MangaManagementSystem.Infrastructure | 0.00% | 0.00% |

Overall instrumented coverage:

- Line coverage: 1.89%
- Lines covered: 416 / 21,912
- Branch coverage: 1.49%
- Branches covered: 76 / 5,100

These percentages must not be interpreted as full-system coverage.

The current suite intentionally prioritizes selected high-risk
authorization, validation, command/query, and aggregation contracts.

Infrastructure currently has no direct automated integration coverage
in this suite because repository/database behavior is not exercised
against a relational test database.

The Web project is compiled by the regression script as part of the
full solution build, but Web UI behavior is not currently measured by
this test project's Cobertura coverage output.

## Running the project regression suite

From the MangaManagementSystem solution directory:

    .\scripts\run-project-regression.ps1

Default configuration:

    Release

The script performs:

1. `dotnet restore`
2. Full solution `dotnet build`
3. Full registered `dotnet test`
4. XPlat Code Coverage collection
5. Verification that a Cobertura coverage file was generated

The command fails when restore, build, tests, or coverage generation
fails.

## Coverage output location

By default, regression results are written outside the Git worktree:

    %TEMP%\SWP391\MangaManagementSystem\ProjectRegression\<timestamp>\

This prevents generated test and coverage artifacts from being added
to source control accidentally.

A custom result directory can also be supplied through the script's
`ResultsDirectory` parameter.

## Safety and isolation

Current tests:

- Do not connect to the production database.
- Do not send email.
- Do not call Cloudinary.
- Do not call Google OAuth.
- Do not call Google reCAPTCHA.
- Do not call external AI services.
- Do not depend on test execution order.
- Do not use artificial delays.
- Exercise production classes rather than placeholder assertions.

## Known limitations

The current project-wide regression foundation does not claim complete
coverage of MangaManagementSystem.

Important areas still requiring future automated coverage include:

- Infrastructure repository behavior against a relational test database.
- Web/Blazor component behavior.
- End-to-end authentication flows.
- Database transactions and persistence rollback behavior.
- Publication scheduling.
- Editorial Board workflows.
- Chapter review and annotation workflows.
- Mangaka chapter/page workflows.
- File upload and Cloudinary integration boundaries.
- Ranking behavior not already covered by separate ranking regression work.
- External-service failure and retry paths.

Future tests should be added incrementally according to regression risk
and confirmed business rules.

## Terminology

Use the following descriptions for this work:

- Project-wide regression foundation.
- Priority high-risk regression suite.
- Initial module-based automated coverage.

Do not describe the current suite as:

- Full coverage.
- 100% project coverage.
- All project functions tested.

Those claims are not supported by the current coverage measurement.