# Consulte https://aka.ms/customizecontainer para personalizar el contenedor de depuración.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/Ticket/Ticket.API/Ticket.API.csproj", "Services/Ticket/Ticket.API/"]
RUN dotnet restore "./Services/Ticket/Ticket.API/Ticket.API.csproj"
COPY . .
WORKDIR "/src/Services/Ticket/Ticket.API"
RUN dotnet build "./Ticket.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ticket.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ticket.API.dll"]
