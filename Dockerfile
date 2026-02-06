FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OneClick.Server/OneClick.Server.csproj", "OneClick.Server/"]
COPY ["OneClick.Shared/OneClick.Shared.csproj", "OneClick.Shared/"]
RUN dotnet restore "OneClick.Server/OneClick.Server.csproj"
COPY . .
WORKDIR "/src/OneClick.Server"
RUN dotnet build "OneClick.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OneClick.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OneClick.Server.dll"]
