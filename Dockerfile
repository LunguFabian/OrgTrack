FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["OrgTrack.Api/OrgTrack.Api.csproj", "OrgTrack.Api/"]
COPY ["OrgTrack.Application/OrgTrack.Application.csproj", "OrgTrack.Application/"]
COPY ["OrgTrack.Domain/OrgTrack.Domain.csproj", "OrgTrack.Domain/"]
COPY ["OrgTrack.Infrastructure/OrgTrack.Infrastructure.csproj", "OrgTrack.Infrastructure/"]

RUN dotnet restore "./OrgTrack.Api/OrgTrack.Api.csproj"

COPY OrgTrack.Api/ OrgTrack.Api/
COPY OrgTrack.Application/ OrgTrack.Application/
COPY OrgTrack.Domain/ OrgTrack.Domain/
COPY OrgTrack.Infrastructure/ OrgTrack.Infrastructure/

WORKDIR "/src/OrgTrack.Api"
RUN dotnet build "./OrgTrack.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OrgTrack.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "OrgTrack.Api.dll"]
