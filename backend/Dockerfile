# Stage 1: Build e Publish (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto primeiro para cache de restore
COPY ["backend/DeSanti.slnx", "backend/"]
COPY ["backend/DeSanti.Domain/DeSanti.Domain.csproj", "backend/DeSanti.Domain/"]
COPY ["backend/DeSanti.Application/DeSanti.Application.csproj", "backend/DeSanti.Application/"]
COPY ["backend/DeSanti.Infrastructure/DeSanti.Infrastructure.csproj", "backend/DeSanti.Infrastructure/"]
COPY ["backend/DeSanti.Api/DeSanti.API.csproj", "backend/DeSanti.Api/"]

RUN dotnet restore "backend/DeSanti.Api/DeSanti.API.csproj"

# Copia o backend inteiro e gera publish de produção
COPY backend/ backend/
WORKDIR "/src/backend/DeSanti.Api"
RUN dotnet publish "DeSanti.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime leve ASP.NET Core 10
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "DeSanti.API.dll"]
