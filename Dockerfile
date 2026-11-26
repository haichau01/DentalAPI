# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes.
COPY Directory.Build.props .editorconfig ./
COPY src/DentalAPI.Api/DentalAPI.Api.csproj src/DentalAPI.Api/
COPY src/DentalAPI.Application/DentalAPI.Application.csproj src/DentalAPI.Application/
COPY src/DentalAPI.Domain/DentalAPI.Domain.csproj src/DentalAPI.Domain/
COPY src/DentalAPI.Infrastructure/DentalAPI.Infrastructure.csproj src/DentalAPI.Infrastructure/
RUN dotnet restore src/DentalAPI.Api/DentalAPI.Api.csproj

COPY src/ src/
RUN dotnet publish src/DentalAPI.Api/DentalAPI.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "DentalAPI.Api.dll"]
