FROM mcr.microsoft.com/dotnet/runtime:8.0 AS build
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SpaceWarDiscordApp.csproj", "."]
RUN dotnet restore "SpaceWarDiscordApp.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "SpaceWarDiscordApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SpaceWarDiscordApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SpaceWarDiscordApp.dll"]