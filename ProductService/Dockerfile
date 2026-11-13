
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ProductService/ProductService.csproj ProductService/
RUN dotnet restore ProductService/ProductService.csproj

COPY ProductService/ ProductService/

WORKDIR /src/ProductService

RUN dotnet publish ProductService.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "ProductService.dll"]
