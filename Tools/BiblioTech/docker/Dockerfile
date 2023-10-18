FROM mcr.microsoft.com/dotnet/sdk:7.0

WORKDIR app
COPY ./Tools/BiblioTech ./Tools/BiblioTech
COPY ./Framework ./Framework
COPY ./ProjectPlugins ./ProjectPlugins
CMD ["dotnet", "run", "--project", "Tools/BiblioTech"]
