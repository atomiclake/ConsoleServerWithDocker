FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /CLI

COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /CLI
COPY --from=build-env /CLI/out .
ENTRYPOINT [ "dotnet", "ConsoleServerWithDocker.dll" ]

EXPOSE 8080