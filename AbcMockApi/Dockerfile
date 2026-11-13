
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY AbcMockApi/AbcMockApi.csproj AbcMockApi/
RUN dotnet restore AbcMockApi/AbcMockApi.csproj

COPY AbcMockApi/ AbcMockApi/

WORKDIR /src/AbcMockApi

RUN dotnet publish AbcMockApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "AbcMockApi.dll"]
