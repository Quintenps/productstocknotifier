﻿FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ProductNotifier.csproj", "./"]
RUN dotnet restore "ProductNotifier.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "ProductNotifier.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductNotifier.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductNotifier.dll"]
