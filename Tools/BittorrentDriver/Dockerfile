# Variables
ARG IMAGE=ubuntu:24.10
ARG APP_HOME=/app


# Build
FROM ${IMAGE} AS builder
ARG APP_HOME
RUN apt-get update
RUN apt-get install dotnet-sdk-8.0 -y

WORKDIR ${APP_HOME}
COPY ./Tools/BittorrentDriver ./Tools/BittorrentDriver
COPY ./Framework ./Framework
RUN dotnet restore Tools/BittorrentDriver
RUN dotnet publish Tools/BittorrentDriver -c Release -o out

# Create
FROM ${IMAGE}
ARG DEBIAN_FRONTEND=noninteractive
ARG APP_HOME
ENV APP_HOME=${APP_HOME}

# Set up npm and bittorrent-tracker
RUN apt-get update
RUN apt-get install npm aspnetcore-runtime-8.0 -y
RUN npm install -g bittorrent-tracker
# Set up transmission
RUN apt-get install transmission-cli transmission-common transmission-daemon -y

WORKDIR ${APP_HOME}
COPY --from=builder ${APP_HOME}/out .
CMD dotnet ${APP_HOME}/BittorrentDriver.dll
