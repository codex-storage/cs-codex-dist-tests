#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BittorrentDriver/BittorrentDriver.csproj", "BittorrentDriver/"]
RUN dotnet restore "./BittorrentDriver/BittorrentDriver.csproj"
COPY . .
WORKDIR "/src/BittorrentDriver"
RUN dotnet build "./BittorrentDriver.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BittorrentDriver.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
ARG DEBIAN_FRONTEND=noninteractive

USER root
# Set up npm and bittorrent-tracker
RUN apt-get update
RUN apt-get install npm software-properties-common -y
RUN npm install -g bittorrent-tracker
# Set up transmission
RUN apt-get install transmission-cli transmission-common transmission-daemon -y

USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BittorrentDriver.dll"]