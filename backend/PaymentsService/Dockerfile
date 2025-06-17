# --------------------------------------------------------------------------------
# PaymentsService Dockerfile for .NET 8.0
# --------------------------------------------------------------------------------

    FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
    WORKDIR /app
    EXPOSE 80
    
    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
    WORKDIR /src
    
    # Копируем только csproj для кеширования restore
    COPY ["PaymentsService/PaymentsService.csproj", "PaymentsService/"]
    COPY ["Shared/Shared.csproj", "Shared/"]
    
    RUN dotnet restore "PaymentsService/PaymentsService.csproj"
    
    # Копируем весь код и собираем
    COPY . .
    WORKDIR "/src/PaymentsService"
    RUN dotnet build "PaymentsService.csproj" -c Release -o /app/build
    
    # Публикуем в папку /app/publish
    FROM build AS publish
    RUN dotnet publish "PaymentsService.csproj" -c Release -o /app/publish
    
    # Собираем финальный образ
    FROM base AS final
    WORKDIR /app
    COPY --from=publish /app/publish .
    ENTRYPOINT ["dotnet", "PaymentsService.dll"]
    