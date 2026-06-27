FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/MangaManagementSystem.Web/*.csproj src/MangaManagementSystem.Web/
COPY src/MangaManagementSystem.Application/*.csproj src/MangaManagementSystem.Application/
COPY src/MangaManagementSystem.Domain/*.csproj src/MangaManagementSystem.Domain/
COPY src/MangaManagementSystem.Infrastructure/*.csproj src/MangaManagementSystem.Infrastructure/

RUN dotnet restore src/MangaManagementSystem.Web/MangaManagementSystem.Web.csproj

COPY . .

RUN dotnet publish src/MangaManagementSystem.Web/MangaManagementSystem.Web.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MangaManagementSystem.Web.dll"]