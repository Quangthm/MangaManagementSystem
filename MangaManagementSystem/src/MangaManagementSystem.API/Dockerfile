FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/MangaManagementSystem.API/*.csproj src/MangaManagementSystem.API/
COPY src/MangaManagementSystem.Application/*.csproj src/MangaManagementSystem.Application/
COPY src/MangaManagementSystem.Domain/*.csproj src/MangaManagementSystem.Domain/
COPY src/MangaManagementSystem.Infrastructure/*.csproj src/MangaManagementSystem.Infrastructure/

RUN dotnet restore src/MangaManagementSystem.API/MangaManagementSystem.API.csproj

COPY . .

RUN dotnet publish src/MangaManagementSystem.API/MangaManagementSystem.API.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MangaManagementSystem.API.dll"]