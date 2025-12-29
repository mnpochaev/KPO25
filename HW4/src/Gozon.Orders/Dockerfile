FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Gozon.Orders/Gozon.Orders.csproj", "src/Gozon.Orders/"]
COPY ["src/Gozon.Contracts/Gozon.Contracts.csproj", "src/Gozon.Contracts/"]
RUN dotnet restore "src/Gozon.Orders/Gozon.Orders.csproj"
COPY . .
WORKDIR "/src/src/Gozon.Orders"
RUN dotnet build "./Gozon.Orders.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Gozon.Orders.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gozon.Orders.dll"]
