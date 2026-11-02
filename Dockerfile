# =======================================================
# 1. SDK env setup
# =======================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base-env
WORKDIR /app

COPY *.sln Directory.Build.props ./
COPY src/Domain/Domain.csproj ./src/Domain/
COPY src/Application/Application.csproj ./src/Application/
COPY src/Infrastructure/Infrastructure.csproj ./src/Infrastructure/
COPY src/Api/Api.csproj ./src/Api/
COPY src/ConsoleUi/ConsoleUi.csproj ./src/ConsoleUi/
COPY tests/Tests/Tests.csproj ./tests/Tests/

RUN dotnet restore
COPY . .

# =======================================================
# 2. Unit and build tests
# =======================================================
FROM base-env AS test-env
ARG BUILD_CONFIGURATION=Release

RUN dotnet test tests/Tests/Tests.csproj --configuration $BUILD_CONFIGURATION --logger:"console;verbosity=normal" || [ "$BUILD_CONFIGURATION" = "Debug" ]

# =======================================================
# 3. Build and publish
# =======================================================
FROM test-env AS publish-env
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish src/Api/Api.csproj -c $BUILD_CONFIGURATION -o out

# =======================================================
# 4. Runtime image
# =======================================================
FROM  mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish-env /app/out .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ARG BUILD_CONFIGURATION=Release
ENV ASPNETCORE_ENVIRONMENT=${BUILD_CONFIGURATION}

ENTRYPOINT ["dotnet", "Api.dll"]

