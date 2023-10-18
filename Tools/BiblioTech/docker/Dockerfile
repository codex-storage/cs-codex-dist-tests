FROM mcr.microsoft.com/dotnet/sdk:7.0

WORKDIR app
COPY ./Tools/BiblioTech ./Tools/BiblioTech
COPY ./Framework/ArgsUniform ./Framework/ArgsUniform
CMD ["dotnet", "run", "--project", "Tools/BiblioTech"]
