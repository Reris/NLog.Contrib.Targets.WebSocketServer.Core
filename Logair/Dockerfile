FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Logair/Logair.csproj", "Logair/"]
COPY ["NLog.Contrib.LogListener/NLog.Contrib.LogListener.csproj", "NLog.Contrib.LogListener/"]
COPY ["NLog.Contrib.Targets.WebSocketServer.Core/NLog.Contrib.Targets.WebSocketServer.Core.csproj", "NLog.Contrib.Targets.WebSocketServer.Core/"]
RUN dotnet restore "Logair/Logair.csproj"
COPY . .
WORKDIR "/src/Logair"
RUN dotnet build "Logair.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Logair.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=3s --retries=3 CMD curl --silent --fail http://localhost:80/healthz || exit 1
ENTRYPOINT ["dotnet", "Logair.dll"]
