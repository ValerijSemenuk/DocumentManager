FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["DocumentManager.API/DocumentManager.API.csproj", "DocumentManager.API/"]
COPY ["DocumentManager.Core/DocumentManager.Core.csproj", "DocumentManager.Core/"]
COPY ["DocumentManager.Infrastructure/DocumentManager.Infrastructure.csproj", "DocumentManager.Infrastructure/"]
RUN dotnet restore "DocumentManager.API/DocumentManager.API.csproj"

COPY . .
WORKDIR "/src/DocumentManager.API"
RUN dotnet build "DocumentManager.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocumentManager.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocumentManager.API.dll"]