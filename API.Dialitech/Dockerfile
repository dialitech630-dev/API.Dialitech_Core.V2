FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["API.Dialitech/API.Dialitech.csproj", "API.Dialitech/"]
COPY ["API.Dialitech.Domain/API.Dialitech.Domain.csproj", "API.Dialitech.Domain/"]
COPY ["API.Dialitech.Application/API.Dialitech.Application.csproj", "API.Dialitech.Application/"]
COPY ["API.Dialitech.Infrastructure/API.Dialitech.Infrastructure.csproj", "API.Dialitech.Infrastructure/"]
RUN dotnet restore "API.Dialitech/API.Dialitech.csproj"

COPY . .
WORKDIR "/src/API.Dialitech"
RUN dotnet build -c $BUILD_CONFIGURATION --no-restore -o /app/build

FROM build AS publish
RUN dotnet publish -c $BUILD_CONFIGURATION --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "API.Dialitech.dll"]
