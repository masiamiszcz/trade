FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TradingPlatform.Api/TradingPlatform.Api.csproj", "TradingPlatform.Api/"]
COPY ["TradingPlatform.Core/TradingPlatform.Core.csproj", "TradingPlatform.Core/"]
COPY ["TradingPlatform.Data/TradingPlatform.Data.csproj", "TradingPlatform.Data/"]
RUN dotnet restore "TradingPlatform.Api/TradingPlatform.Api.csproj"
COPY . .
WORKDIR "/src/TradingPlatform.Api"
RUN dotnet build "TradingPlatform.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TradingPlatform.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TradingPlatform.Api.dll"]