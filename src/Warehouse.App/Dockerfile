FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Warehouse.App/Warehouse.App.csproj", "src/Warehouse.App/"]
COPY ["src/Warehouse.Core/Warehouse.Core.csproj", "src/Warehouse.Core/"]
RUN dotnet restore "src/Warehouse.App/Warehouse.App.csproj"
COPY . .
WORKDIR "/src/src/Warehouse.App"
RUN dotnet build "./Warehouse.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Warehouse.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Warehouse.App.dll"]
