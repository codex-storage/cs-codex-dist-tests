# Variables
ARG IMAGE=mcr.microsoft.com/dotnet/sdk:7.0
ARG APP_HOME=/app

# Create
FROM ${IMAGE}
ARG APP_HOME

WORKDIR ${APP_HOME}
COPY ./Tools/TestNetRewarder ./Tools/TestNetRewarder
COPY ./Framework ./Framework
COPY ./ProjectPlugins ./ProjectPlugins
CMD ["dotnet", "run", "--project", "Tools/TestNetRewarder"]
