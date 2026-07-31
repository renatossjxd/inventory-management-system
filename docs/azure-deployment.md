# Despliegue en Azure

El repositorio incluye infraestructura como código y despliegue continuo seguro para Azure App Service, Azure SQL Database y Blob Storage. El flujo usa OpenID Connect (OIDC), por lo que GitHub obtiene credenciales temporales y no almacena una contraseña permanente de Azure.

## 1. Crear la infraestructura

Desde Azure Cloud Shell:

```bash
az group create --name rg-inventory-prod --location chilecentral

az deployment group create \
  --resource-group rg-inventory-prod \
  --template-file infra/main.bicep \
  --parameters resourcePrefix=<prefijo-unico> \
               sqlAdministratorLogin=<usuario-sql> \
               sqlAdministratorPassword='<contraseña-segura>' \
               jwtKey='<clave-aleatoria-de-32-o-mas-caracteres>'
```

El comando entrega los nombres de App Service, SQL Server y Storage. Los valores seguros se solicitan al ejecutar el despliegue y no deben guardarse en el repositorio.

## 2. Conectar GitHub mediante OIDC

En Microsoft Entra ID se crea una aplicación, su entidad de servicio y una credencial federada para este repositorio y el entorno `production`. La entidad necesita permisos limitados al grupo de recursos `rg-inventory-prod`.

En el entorno `production` de GitHub configura estos secretos:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_SQL_CONNECTION_STRING`

Y estas variables:

- `AZURE_WEBAPP_NAME`
- `AZURE_RESOURCE_GROUP`
- `AZURE_SQL_SERVER_NAME`

Es recomendable proteger el entorno `production` con aprobación manual.

## 3. Desplegar

El flujo **Deploy to Azure** puede iniciarse manualmente desde GitHub Actions. Después de configurarlo también se ejecuta con cambios de la API enviados a `main`.

Antes de publicar, el flujo:

1. Autentica mediante OIDC.
2. Abre temporalmente el firewall de Azure SQL solo para el agente de GitHub.
3. Aplica las migraciones de Entity Framework.
4. Publica la API en App Service.
5. Comprueba `/health`.
6. Elimina la regla temporal del firewall aunque falle un paso.

Hasta que `AZURE_WEBAPP_NAME` exista, el trabajo de despliegue queda omitido y no afecta la integración continua actual.
