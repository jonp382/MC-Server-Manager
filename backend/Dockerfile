# build with the SDK, run with the smaller aspnet runtime image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# copy csproj first so code changes don't force a nuget restore (layer cache)
COPY MinecraftServerManager.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# java is needed to spawn detached minecraft servers, and curl for the healthcheck
RUN apt-get update \
    && apt-get install -y --no-install-recommends openjdk-17-jre-headless curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out .

# bind to all interfaces inside the container; compose overrides the port
ENV ASPNETCORE_URLS=http://0.0.0.0:5029
EXPOSE 5029

ENTRYPOINT ["dotnet", "MinecraftServerManager.dll"]
