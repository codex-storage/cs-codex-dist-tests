FROM mcr.microsoft.com/dotnet/sdk:7.0

COPY --chmod=0755 docker/docker-entrypoint.sh /

ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["dotnet", "test"]

