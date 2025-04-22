FROM mcr.microsoft.com/dotnet/sdk:8.0

COPY --chmod=0755 docker/docker-entrypoint.sh /

RUN apt-get update
RUN apt-get install docker -y

ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["dotnet", "test"]

