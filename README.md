# Inventory Management System

[![Continuous Integration](https://github.com/renatossjxd/inventory-management-system/actions/workflows/ci.yml/badge.svg)](https://github.com/renatossjxd/inventory-management-system/actions/workflows/ci.yml)

API REST profesional para administrar productos y movimientos de inventario. El proyecto demuestra un flujo completo con ASP.NET Core, arquitectura limpia, SQL Server, Entity Framework Core, JWT, Swagger, pruebas y contenedores.

## Funcionalidad del primer incremento

- Registro e inicio de sesión con JWT.
- Roles `Admin` y `Operator` con autorización por endpoint.
- Creación segura del administrador inicial y alta posterior de usuarios solo por administradores.
- Contraseñas protegidas con PBKDF2, salt aleatorio y comparación en tiempo constante.
- Crear, consultar y actualizar productos.
- Registrar entradas y salidas de inventario con trazabilidad del usuario.
- Regla de negocio que impide stock negativo.
- Consulta de productos con stock bajo.
- Carga y reemplazo de imágenes JPEG, PNG y WebP en Azure Blob Storage.
- Validación del contenido real y límite de 5 MB para las imágenes.
- Catálogo de categorías y proveedores relacionado con los productos.
- Órdenes de compra con recepción transaccional y actualización automática de stock.
- Migración inicial de Entity Framework Core.
- Swagger UI en `/swagger` y health check en `/health`.

## Arquitectura

```text
src/
├── InventoryManagement.Domain          Entidades y reglas de negocio
├── InventoryManagement.Application     Contratos y modelos de aplicación
├── InventoryManagement.Infrastructure  EF Core, SQL Server y seguridad
└── InventoryManagement.Api             HTTP, JWT y Swagger
tests/
└── InventoryManagement.UnitTests       Pruebas de reglas del dominio
```

Las dependencias apuntan hacia el dominio. La API compone las capas mediante inyección de dependencias.

## Ejecutar con Docker

Requisitos: Docker Desktop.

1. Copia `.env.example` como `.env`.
2. Reemplaza ambos valores por secretos fuertes. No subas `.env` a Git.
3. Ejecuta:

```bash
docker compose up --build
```

Abre `http://localhost:8080/swagger`. Registra un usuario, copia el token y autorízate con el botón **Authorize**. Docker también inicia Azurite, el emulador local compatible con Azure Blob Storage, en `http://localhost:10000`.

## Ejecutar con .NET

Requisitos: .NET 10 SDK y SQL Server accesible.

```bash
dotnet tool restore
dotnet restore
dotnet ef database update --project src/InventoryManagement.Infrastructure --startup-project src/InventoryManagement.Api
dotnet run --project src/InventoryManagement.Api
```

Para desarrollo se pueden sustituir secretos sin escribirlos en Git:

```bash
dotnet user-secrets set "ConnectionStrings:InventoryDb" "<connection-string>" --project src/InventoryManagement.Api
dotnet user-secrets set "Jwt:Key" "<32-or-more-random-characters>" --project src/InventoryManagement.Api
```

## Endpoints principales

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/auth/register` | Crea el Admin inicial; después requiere Admin y permite asignar un rol |
| POST | `/api/auth/login` | Obtiene un token JWT |
| GET | `/api/auth/users` | Lista usuarios; requiere rol Admin |
| GET | `/api/products?page=1&pageSize=20&search=mouse` | Lista productos paginados; permite buscar y filtrar por categoría, proveedor y stock bajo |
| POST | `/api/products` | Crea un producto; requiere rol Admin |
| PUT | `/api/products/{id}` | Actualiza un producto; requiere rol Admin |
| POST | `/api/products/{id}/stock-movements` | Registra entrada o salida |
| GET | `/api/products/{id}/stock-movements` | Consulta el historial |
| POST | `/api/products/{id}/image` | Sube o reemplaza la imagen; requiere rol Admin |
| GET | `/api/categories` | Lista categorías |
| POST/PUT/DELETE | `/api/categories` | Administra categorías; requiere rol Admin |
| GET | `/api/suppliers` | Lista proveedores |
| POST/PUT/DELETE | `/api/suppliers` | Administra proveedores; requiere rol Admin |
| GET/POST | `/api/purchase-orders` | Consulta o crea órdenes de compra |
| POST | `/api/purchase-orders/{id}/receive` | Recibe mercadería y aumenta el stock |
| POST | `/api/purchase-orders/{id}/cancel` | Cancela una orden pendiente; requiere Admin |
| GET | `/api/dashboard` | Resume valor y unidades de inventario, stock bajo, órdenes y actividad reciente |
| GET | `/api/reports/inventory.csv` | Descarga el inventario filtrado en CSV; requiere rol Admin |

La respuesta de `GET /api/products` incluye `items`, `page`, `pageSize`, `totalCount` y `totalPages`. El tamaño máximo permitido es de 100 elementos por página.

## Preparación para Azure

- **Azure App Service:** la API escucha el puerto indicado por ASP.NET y cuenta con `/health`.
- **Azure SQL:** solo hay que definir `ConnectionStrings__InventoryDb` en App Service; el proveedor ya es SQL Server.
- **Azure Blob Storage:** las imágenes se gestionan mediante `IFileStorage` y el SDK oficial. En Azure se configura `BlobStorage__ConnectionString`, `BlobStorage__ContainerName` y, opcionalmente, `BlobStorage__PublicBaseUrl`.
- **Secretos:** configurar `Jwt__Key` y la cadena de conexión en App Service/Key Vault; nunca guardarlos en `appsettings.json` de producción.
- **Migraciones:** para producción se recomienda ejecutarlas desde CI/CD antes del despliegue y mantener `Database__MigrateOnStartup=false`.
- **Infraestructura:** `infra/main.bicep` crea App Service, Azure SQL y Blob Storage de forma reproducible.
- **Despliegue continuo:** `.github/workflows/deploy-azure.yml` usa OIDC, aplica migraciones, publica la API y comprueba `/health`. Consulta [la guía de despliegue](docs/azure-deployment.md).

## Calidad y comandos útiles

```bash
dotnet build InventoryManagement.slnx
dotnet test InventoryManagement.slnx
dotnet list InventoryManagement.slnx package --vulnerable --include-transitive
```

GitHub Actions ejecuta estas validaciones automáticamente en cada cambio enviado a `main` y en cada pull request. Los resultados de las pruebas quedan disponibles como artefacto descargable durante siete días.

La validación de integración inicia la API, SQL Server y Azurite en contenedores y comprueba el recorrido completo de autenticación, catálogo, movimientos, dashboard y carga de imágenes. Con el entorno local iniciado, también puede ejecutarse con:

```bash
bash tests/integration/api-smoke-test.sh
```

## Próximos incrementos

1. Interfaz web administrativa.
2. Notificaciones de stock bajo.
3. Auditoría avanzada de operaciones.
