# Variables
ARG BUILDER=mcr.microsoft.com/dotnet/sdk:8.0
ARG IMAGE=${BUILDER}
ARG APP_HOME=/app


# Build
FROM ${IMAGE} AS builder
ARG APP_HOME

WORKDIR ${APP_HOME}
COPY ./Tools/KeyMaker ./Tools/KeyMaker
COPY ./Framework ./Framework
COPY ./ProjectPlugins ./ProjectPlugins
RUN dotnet restore Tools/KeyMaker
RUN dotnet publish Tools/KeyMaker -c Release -o out


# Create
FROM ${IMAGE}
ARG APP_HOME
ENV APP_HOME=${APP_HOME}

WORKDIR ${APP_HOME}
COPY --from=builder ${APP_HOME}/out .
CMD dotnet ${APP_HOME}/KeyMaker.dll
