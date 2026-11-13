
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY OrderService/OrderService.csproj OrderService/
RUN dotnet restore OrderService/OrderService.csproj

COPY OrderService/ OrderService/

WORKDIR /src/OrderService

RUN dotnet publish OrderService.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderService.dll"]
