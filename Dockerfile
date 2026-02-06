# Use the .NET 10.0 runtime image for production
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

# Use the .NET 10.0 SDK image to build/publish the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "./SpaceWarDiscordApp.csproj"
RUN dotnet publish "./SpaceWarDiscordApp.csproj" -c Release -o /app/publish

# Prepare final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SpaceWarDiscordApp.dll"]