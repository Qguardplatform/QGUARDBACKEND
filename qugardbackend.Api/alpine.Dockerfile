# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
 
# Enable globalization support
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
 
# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
 
COPY qguardbackend.Api/qguardbackend.Api.csproj qguardbackend.Api/
COPY qguardbackend.Core/qguardbackend.Core.csproj qguardbackend.Core/
COPY qguardbackend.Data/qguardbackend.Data.csproj qguardbackend.Data/
 
RUN dotnet restore "qguardbackend.Api/qguardbackend.Api.csproj"
 
COPY . .
WORKDIR "/src/qguardbackend.Api"
RUN dotnet build "qguardbackend.Api.csproj" -c Release -o /app
 
# Publish stage
FROM build AS publish
RUN dotnet publish "qguardbackend.Api.csproj" -c Release -o /app
 
# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app .
 
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "qguardbackend.Api.dll"]