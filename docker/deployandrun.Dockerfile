FROM mcr.microsoft.com/dotnet/sdk:7.0

RUN apt-get update && apt-get install -y screen
WORKDIR /app
COPY --chmod=0755 docker/docker-dnr-entrypoint.sh /
COPY ./Tools ./Tools
COPY ./Tests ./Tests
COPY ./Framework ./Framework
COPY ./ProjectPlugins ./ProjectPlugins

ENTRYPOINT ["/docker-dnr-entrypoint.sh"]
CMD ["/bin/bash", "deploy-and-run.sh"]
