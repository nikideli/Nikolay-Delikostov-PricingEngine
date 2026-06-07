FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first (layer-cache friendly)
COPY ["src/PricingEngine.Api/PricingEngine.Api.csproj",             "src/PricingEngine.Api/"]
COPY ["src/PricingEngine.Application/PricingEngine.Application.csproj",   "src/PricingEngine.Application/"]
COPY ["src/PricingEngine.Domain/PricingEngine.Domain.csproj",             "src/PricingEngine.Domain/"]
COPY ["src/PricingEngine.Infrastructure/PricingEngine.Infrastructure.csproj", "src/PricingEngine.Infrastructure/"]
RUN dotnet restore "src/PricingEngine.Api/PricingEngine.Api.csproj"

# Copy full source and build
COPY . .
RUN dotnet build "src/PricingEngine.Api/PricingEngine.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/PricingEngine.Api/PricingEngine.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PricingEngine.Api.dll"]
