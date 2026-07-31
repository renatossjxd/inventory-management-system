FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore InventoryManagement.slnx

FROM build AS test
RUN dotnet test InventoryManagement.slnx -c Release --no-restore

FROM build AS publish
RUN dotnet publish src/InventoryManagement.Api/InventoryManagement.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "InventoryManagement.Api.dll"]
