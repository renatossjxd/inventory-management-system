@description('Prefijo globalmente único para los recursos. Usa letras minúsculas y números.')
@minLength(3)
@maxLength(18)
param resourcePrefix string

@description('Región de Azure donde se crearán los recursos.')
param location string = resourceGroup().location

@description('Usuario administrador de Azure SQL.')
param sqlAdministratorLogin string

@secure()
@description('Contraseña del administrador de Azure SQL.')
param sqlAdministratorPassword string

@secure()
@minLength(32)
@description('Clave con al menos 32 caracteres para firmar tokens JWT.')
param jwtKey string

@allowed([
  'B1'
  'S1'
])
param appServiceSku string = 'B1'

var webAppName = '${resourcePrefix}-api'
var sqlServerName = '${resourcePrefix}-sql'
var databaseName = 'InventoryManagement'
var storageAccountName = uniqueString(resourceGroup().id, resourcePrefix)

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${resourcePrefix}-plan'
  location: location
  kind: 'linux'
  sku: {
    name: appServiceSku
  }
  properties: {
    reserved: true
  }
}

resource sqlServer 'Microsoft.Sql/servers@2025-01-01' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    maxSizeBytes: 2147483648
    requestedBackupStorageRedundancy: 'Local'
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: true
    allowSharedKeyAccess: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  parent: storage
  name: 'default'
}

resource imageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = {
  parent: blobService
  name: 'product-images'
  properties: {
    publicAccess: 'Blob'
  }
}

var storageKey = storage.listKeys().keys[0].value
var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};Persist Security Info=False;User ID=${sqlAdministratorLogin};Password=${sqlAdministratorPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
var blobConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storageKey};EndpointSuffix=${environment().suffixes.storage}'

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      ftpsState: 'Disabled'
      http20Enabled: true
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
      healthCheckPath: '/health'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'ConnectionStrings__InventoryDb', value: sqlConnectionString }
        { name: 'Jwt__Issuer', value: 'InventoryManagement.Api' }
        { name: 'Jwt__Audience', value: 'InventoryManagement.Client' }
        { name: 'Jwt__Key', value: jwtKey }
        { name: 'Database__MigrateOnStartup', value: 'false' }
        { name: 'BlobStorage__ConnectionString', value: blobConnectionString }
        { name: 'BlobStorage__ContainerName', value: imageContainer.name }
        { name: 'BlobStorage__PublicBaseUrl', value: 'https://${storage.name}.blob.${environment().suffixes.storage}/${imageContainer.name}' }
      ]
    }
  }
}

output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDatabase.name
output storageAccountName string = storage.name
