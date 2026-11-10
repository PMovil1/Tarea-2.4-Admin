# Bitacora API - UTH

API REST para la gestion de registros de bitacora de campo, desarrollada con ASP.NET Core y Entity Framework Core sobre Azure SQL Database, aplicando principios de Arquitectura Limpia (Clean Architecture).

---

## Tabla de Contenidos

- [Descripcion General](#descripcion-general)
- [Arquitectura de la Solucion](#arquitectura-de-la-solucion)
- [Estructura de Directorios](#estructura-de-directorios)
- [Entidades de Dominio](#entidades-de-dominio)
- [Modelos de Transferencia de Datos (DTOs)](#modelos-de-transferencia-de-datos-dtos)
- [Contrato de Persistencia (Repositorio)](#contrato-de-persistencia-repositorio)
- [Controladores y Endpoints de la API](#controladores-y-endpoints-de-la-api)
- [Manejo Global de Errores](#manejo-global-de-errores)
- [Base de Datos y Migraciones](#base-de-datos-y-migraciones)
- [Requisitos y Tecnologias](#requisitos-y-tecnologias)
- [Guia de Instalacion y Ejecucion](#guia-de-instalacion-y-ejecucion)
- [Pruebas con Postman](#pruebas-con-postman)

---

## Descripcion General

El proyecto **Bitacora API - UTH** es un servicio web RESTful que permite gestionar anotaciones y evidencias fotograficas tomadas durante actividades de campo. Cada registro contiene un identificador, titulo, descripcion, estampa de tiempo UTC y una fotografia opcional almacenada en formato Base64, con validaciones estrictas de formato y limite de tamano.

El diseno desacopla completamente el dominio del negocio, la logica de aplicacion, el acceso a datos y la capa de exposicion HTTP.

---

## Arquitectura de la Solucion

La solucion sigue una separacion de responsabilidades en cuatro proyectos interconectados:

1. **Bitacora.Domain**: El nucleo del sistema. No tiene dependencias de librerias externas ni de otros proyectos de la solucion. Define las entidades puras de la logica de negocio.
2. **Bitacora.Application**: Contiene los contratos de interfaces, los modelos de transferencia de datos (DTOs) y las reglas de validacion semanticas. Depende unicamente de `Bitacora.Domain`.
3. **Bitacora.Infrastructure**: Encargada del acceso a datos, implementacion del patron repositorio y mapeo objeto-relacional mediante Entity Framework Core sobre Azure SQL Database. Depende de `Bitacora.Domain` y `Bitacora.Application`.
4. **Bitacora.Api**: Capa de presentacion HTTP. Contiene los controladores REST, configuracion de inyeccion de dependencias, middlewares y documentacion interactiva OpenAPI/Swagger. Depende de `Bitacora.Application` y `Bitacora.Infrastructure`.

---

## Estructura de Directorios

```text
Tarea2.2_ApiRest_BitacoraUTH/
|-- Bitacora.slnx                                 # Definicion de solucion .NET
|-- Bitacora API - UTH.postman_collection .json   # Coleccion de pruebas en Postman
|-- .gitignore                                    # Exclusiones de Git
|-- README.md                                     # Documentacion del proyecto
`-- src/
    |-- Bitacora.Domain/                          # Capa de Dominio
    |   |-- Bitacora.Domain.csproj
    |   `-- Entities/
    |       `-- Registro.cs                       # Entidad de negocio
    |
    |-- Bitacora.Application/                     # Capa de Aplicacion
    |   |-- Bitacora.Application.csproj
    |   |-- DTOs/
    |   |   |-- CrearRegistroRequest.cs           # DTO para crear un registro
    |   |   |-- ActualizarRegistroRequest.cs      # DTO para actualizar un registro
    |   |   `-- RegistroResponse.cs               # DTO de respuesta estructurada
    |   `-- Interfaces/
    |       `-- IRegistroRepository.cs            # Contrato del repositorio
    |
    |-- Bitacora.Infrastructure/                  # Capa de Infraestructura
    |   |-- Bitacora.Infrastructure.csproj
    |   |-- Data/
    |   |   |-- ApplicationDbContext.cs           # Contexto de base de datos EF Core
    |   |   `-- Migrations/                       # Migraciones versionadas de EF Core
    |   |       |-- 20261110182619_InitialAzureSql.cs
    |   |       |-- 20261110182619_InitialAzureSql.Designer.cs
    |   |       `-- ApplicationDbContextModelSnapshot.cs
    |   `-- Repositories/
    |       `-- RegistroRepository.cs             # Implementacion del repositorio
    |
    `-- Bitacora.Api/                             # Capa de Presentacion API
        |-- Bitacora.Api.csproj
        |-- Bitacora.Api.http                     # Peticiones de prueba para clientes HTTP
        |-- Program.cs                            # Inicializacion y pipeline de la aplicacion
        |-- appsettings.json                      # Configuracion base
        |-- appsettings.Development.json          # Configuracion de desarrollo
        |-- Controllers/
        |   |-- RegistrosController.cs            # Controlador CRUD de registros
        |   `-- HealthController.cs               # Estado de la API y Azure SQL
        `-- Properties/
            `-- launchSettings.json               # Perfiles de ejecucion local
```

---

## Entidades de Dominio

### Registro (`Bitacora.Domain.Entities.Registro`)

Representa una entrada individual dentro de la bitacora de campo.

| Propiedad | Tipo de Dato | Requerido | Descripcion |
|---|---|---|---|
| `Id` | `int` | Si (Autoincremental) | Clave primaria unica generada por el motor de base de datos. |
| `Titulo` | `string` | Si | Nombre o encabezado descriptivo del registro (maximo 100 caracteres). |
| `Descripcion` | `string` | Si | Explicacion o detalle de la observacion de campo (maximo 500 caracteres). |
| `FotoBase64` | `string?` | No (Opcional) | Cadena de texto con la imagen codificada en formato Base64. |
| `FechaCreacion` | `DateTimeOffset` | Si | Fecha y hora UTC del momento en que se genera el registro. Inicializada por defecto con `DateTimeOffset.UtcNow`. |

---

## Modelos de Transferencia de Datos (DTOs)

Los DTOs definen los formatos de entrada y salida de informacion para desacoplar el contrato de la API del esquema de la base de datos y aplicar validaciones automaticas.

### 1. CrearRegistroRequest (`Bitacora.Application.DTOs.CrearRegistroRequest`)

Utilizado en solicitudes `POST /api/registros`. Implementa la interfaz `IValidatableObject` para validaciones declarativas y logicas.

- **Campos**:
  - `Titulo` (`string`, requerido): Minimo 3 caracteres, maximo 100 caracteres.
  - `Descripcion` (`string`, requerido): Maximo 500 caracteres.
  - `FotoBase64` (`string?`, opcional): Imagen codificada en Base64.
- **Validaciones personalizadas**:
  - Remueve de forma segura el prefijo del esquema Data URL si esta presente (por ejemplo, `data:image/jpeg;base64,`).
  - Valida que la cadena sea una secuencia Base64 valida mediante decodificacion en bloque.
  - Verifica que el tamano de los bytes resultantes no sobrepase el limite maximo permitido de **2 MB** (2,097,152 bytes).

### 2. ActualizarRegistroRequest (`Bitacora.Application.DTOs.ActualizarRegistroRequest`)

Utilizado en solicitudes `PUT /api/registros/{id}`.

- **Campos y validaciones**:
  - `Titulo` (`string`, requerido): Entre 3 y 100 caracteres.
  - `Descripcion` (`string`, requerido): Maximo 500 caracteres.
  - `FotoBase64` (`string?`, opcional): Imagen codificada en Base64 con soporte de prefijo Data URL, comprobacion de formato y tamano maximo de 2 MB.

### 3. RegistroResponse (`Bitacora.Application.DTOs.RegistroResponse`)

Objeto de respuesta inmutable (`record`) devuelto por la API en consultas y creaciones.

| Propiedad | Tipo | Descripcion |
|---|---|---|
| `Id` | `int` | Identificador del registro. |
| `Titulo` | `string` | Titulo asignado al registro. |
| `Descripcion` | `string` | Descripcion de la actividad. |
| `TieneFoto` | `bool` | Indica `true` si el registro cuenta con una fotografia asociada, o `false` si es nulo o vacio. |
| `FotoBase64` | `string?` | Cadena en Base64 de la fotografia (o `null`). |
| `FechaCreacion` | `DateTimeOffset` | Estampa de tiempo UTC de creacion. |

---

## Contrato de Persistencia (Repositorio)

### Interfaz IRegistroRepository (`Bitacora.Application.Interfaces.IRegistroRepository`)

Define el patron Repositorio para aislar el acceso a la base de datos de los controladores:

```csharp
public interface IRegistroRepository
{
    Task<IEnumerable<Registro>> ObtenerTodosAsync(CancellationToken cancellationToken);
    Task<Registro?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);
    Task CrearAsync(Registro registro, CancellationToken cancellationToken);
    Task ActualizarAsync(Registro registro, CancellationToken cancellationToken);
    Task EliminarAsync(Registro registro, CancellationToken cancellationToken);
}
```

### Implementacion RegistroRepository (`Bitacora.Infrastructure.Repositories.RegistroRepository`)

- Utiliza `ApplicationDbContext` inyectado por el contenedor de dependencias con ciclo de vida `Scoped`.
- Optimiza consultas de lectura masiva mediante `.AsNoTracking()` para evitar sobrecarga en el seguimiento de entidades de EF Core.
- Aplica operaciones asincronas completas respetando el paso de `CancellationToken`.

---

## Controladores y Endpoints de la API

El controlador principal es `RegistrosController` (`Bitacora.Api.Controllers.RegistrosController`), configurado bajo la ruta base `/api/registros`.

### Tabla Resumen de Endpoints

| Metodo HTTP | Ruta | Descripcion | Codigo Exito | Codigos de Error Comunes |
|---|---|---|---|---|
| `GET` | `/api/registros` | Listar todos los registros de la bitacora | `200 OK` | `500 Internal Server Error` |
| `GET` | `/api/registros/{id}` | Consultar un registro por identificador | `200 OK` | `404 Not Found`, `500 Internal Server Error` |
| `POST` | `/api/registros` | Crear un nuevo registro de bitacora | `201 Created` | `400 Bad Request`, `500 Internal Server Error` |
| `PUT` | `/api/registros/{id}` | Actualizar por completo un registro existente | `204 No Content` | `400 Bad Request`, `404 Not Found`, `500 Internal Server Error` |
| `DELETE` | `/api/registros/{id}` | Eliminar un registro por identificador | `204 No Content` | `404 Not Found`, `500 Internal Server Error` |

### Detalle de Peticiones y Respuestas

#### 1. Listar Registros
- **Solicitud**: `GET /api/registros`
- **Respuesta (200 OK)**:
```json
[
  {
    "id": 1,
    "titulo": "Inspeccion de Terreno",
    "descripcion": "Revision preliminar de campo en el area norte.",
    "tieneFoto": true,
    "fotoBase64": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
    "fechaCreacion": "2026-11-04T02:30:00+00:00"
  }
]
```

#### 2. Consultar Registro por ID
- **Solicitud**: `GET /api/registros/1`
- **Respuesta (200 OK)**: Objeto `RegistroResponse` individual.
- **Respuesta (404 Not Found)**: Devuelta cuando el registro no existe en la base de datos.

#### 3. Crear Registro
- **Solicitud**: `POST /api/registros`
- **Cuerpo (JSON)**:
```json
{
  "titulo": "Muestreo de suelo",
  "descripcion": "Recoleccion de muestras a profundidad de 30 cm.",
  "fotoBase64": null
}
```
- **Respuesta (201 Created)**: Retorna el objeto `RegistroResponse` creado e incluye la cabecera HTTP `Location` con la URL directa del recurso (por ejemplo, `/api/registros/2`).

#### 4. Actualizar Registro
- **Solicitud**: `PUT /api/registros/1`
- **Cuerpo (JSON)**:
```json
{
  "titulo": "Inspeccion de Terreno - Modificada",
  "descripcion": "Revision de campo complementada con toma de medidas.",
  "fotoBase64": null
}
```
- **Respuesta (204 No Content)**: Indica actualizacion satisfactoria sin contenido en el cuerpo.

#### 5. Eliminar Registro
- **Solicitud**: `DELETE /api/registros/1`
- **Respuesta (204 No Content)**: Indica eliminacion satisfactoria.

---

## Manejo Global de Errores

En `Program.cs` se encuentra configurado un middleware centralizado mediante `app.UseExceptionHandler()` que intercepta cualquier excepcion no controlada durante la ejecucion de las peticiones.

- Transforma las fallas imprevistas en una respuesta con formato estandarizado **RFC 7807** (`application/problem+json`).
- Asigna el codigo de estado HTTP `500 Internal Server Error`.
- Devuelve un mensaje claro sin exponer detalles sensibles o trazas de pila al cliente final:
```json
{
  "status": 500,
  "title": "Ocurrio un error inesperado en el servidor.",
  "detail": "Por favor, intentelo mas tarde o contacte al administrador."
}
```

---

## Base de Datos y Migraciones

- **Motor**: Azure SQL Database.
- **Conexion**. En desarrollo, crea `appsettings.Local.json` en la raiz del repositorio con el formato de abajo. Git excluye este archivo. Para Cloud Run, configura `ConnectionStrings__DefaultConnection` y `JwtSettings__Secret` mediante Secret Manager.
- **Acceso**. Autoriza en Azure SQL la IP desde donde aplicaras la migracion y configura el acceso de red del entorno donde se ejecutara la API. Configura tambien `JwtSettings__Secret` para iniciar la API en ese entorno.
- **Mapeo**: Realizado a traves de `ApplicationDbContext` aplicando las siguientes restricciones en `OnModelCreating`:
  - `Id`: Clave primaria de la tabla.
  - `Titulo`: Requerido, longitud maxima de 100 caracteres.
  - `Descripcion`: Requerido, longitud maxima de 500 caracteres.
  - `FotoBase64`: Campo de texto de longitud flexible.
  - `FechaCreacion`: Requerido.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<cadena de Azure SQL>"
  },
  "JwtSettings": {
    "Secret": "<clave privada de al menos 32 bytes>"
  }
}
```

### Gestion de Migraciones con EF Core CLI

Para aplicar las migraciones existentes o crear nuevas:

```bash
# Crear las tablas Usuarios y Registros en Azure SQL
dotnet ef database update --project src/Bitacora.Infrastructure --startup-project src/Bitacora.Api

# Crear una nueva migracion
dotnet ef migrations add NombreMigracion --project src/Bitacora.Infrastructure --startup-project src/Bitacora.Api
```

---

## Requisitos y Tecnologias

- **SDK de .NET**: Version 10.0 (o superior).
- **Lenguaje**: C# 13 / C# 14.
- **Entity Framework Core**: Version 10.0.14 (`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`).
- **Documentacion de API**: `Microsoft.AspNetCore.OpenApi` v10.0.12 y `Swashbuckle.AspNetCore.SwaggerUI` v10.3.1.
- **Gestor de Base de Datos**: Azure SQL Database.

---

## Guia de Instalacion y Ejecucion

### 1. Clonar el repositorio

```bash
git clone https://github.com/tu-usuario/Tarea2.2_ApiRest_BitacoraUTH.git
cd Tarea2.2_ApiRest_BitacoraUTH
```

### 2. Restaurar dependencias

```bash
dotnet restore
```

### 3. Compilar la solucion

```bash
dotnet build
```

### 4. Ejecutar el proyecto API

Configura la conexion y aplica la migracion segun la seccion [Base de Datos y Migraciones](#base-de-datos-y-migraciones).

```bash
dotnet run --project src/Bitacora.Api
```

### 5. Acceso a la interfaz interactiva Swagger UI

Una vez iniciado el servidor en entorno de desarrollo (`Development`), la documentacion OpenAPI y la consola de pruebas se encuentran disponibles en:

- HTTP: `http://localhost:5033/swagger`
- HTTPS: `https://localhost:7190/swagger`

---

## Pruebas con Postman

El repositorio incluye el archivo `Bitacora API - UTH.postman_collection .json` en la raiz del proyecto con una suite de pruebas preconfiguradas.

### Casos de prueba incluidos:
- **Listar Registros**: Verifica respuesta `200 OK` y formato de coleccion.
- **Consultar Registro por ID**: Prueba de consulta satisfactoria con `200 OK` y prueba de recurso inexistente con `404 Not Found`.
- **Crear Registro (Exito)**: Peticion con cuerpo valido verificando estado `201 Created`.
- **Crear Registro con Validaciones Fallidas**:
  - Titulo menor a 3 caracteres (`400 Bad Request`).
  - Titulo mayor a 100 caracteres (`400 Bad Request`).
  - Descripcion mayor a 500 caracteres (`400 Bad Request`).
  - Fotografia Base64 con formato corrupto o invalido (`400 Bad Request`).
  - Fotografia Base64 que excede el limite de 2 MB (`400 Bad Request`).
- **Actualizar Registro**: Casos para actualizacion satisfactoria (`204 No Content`) y recurso inexistente (`404 Not Found`).
- **Eliminar Registro**: Casos para eliminacion correcta (`204 No Content`) y validacion de inexistencia (`404 Not Found`).

Para utilizarlas, importe la coleccion en Postman y configure la variable de entorno `baseUrl` con el valor `http://localhost:5033` o `https://localhost:7190`.





*********************************************************************************************************

*********************************************************************************************************

### Funcionamiento (Conceptos)

<img width="1024" height="572" alt="image" src="https://github.com/user-attachments/assets/9b90d1ec-3d89-4c7e-85f7-b1941cd59aa7" />




### 🔄 Diagrama de Secuencia de Gestión Interna

    sequenceDiagram
    autonumber
    actor Usuario as 👤 Usuario
    participant UI as 📱 AddRecordActivity
    participant VM as ⚡ RegistroViewModel
    participant Repo as 🛡️ RegistroRepository
    participant Retrofit as 🌐 RetrofitClient
    participant API as 💻 API .NET 10
    participant DB as 🗄️ Azure SQL

    Usuario->>UI: Toca "+ Capturar en Campo"
    UI->>UI: Procesa Foto (Base64) + Dictado por Voz
    UI->>VM: createRecord(newRecord)
    VM->>VM: isLoadingLiveData.setValue(true)
    VM->>Repo: createRecord(record)
    Repo->>Repo: Validar red (NetworkCapabilities)
    
    alt Conexión Establecida
        Repo->>Retrofit: POST /api/registros (JSON)
        Retrofit->>API: Petición HTTP REST (Puerto 5033)
        API->>DB: Entity Framework Core inserta registro
        DB-->>API: Confirmación de almacenamiento
        API-->>Retrofit: HTTP 201 Created (JSON Response)
        Retrofit-->>Repo: onSuccess(Record)
        Repo-->>VM: Notifica Éxito
        VM->>VM: operationSuccessLiveData.setValue(true)
        VM-->>UI: Notifica cierre de pantalla
        UI->>Usuario: Refresca lista en HomeActivity
    else Sin Conexión
        Repo-->>VM: onError("Sin conexión a internet")
        VM-->>UI: Muestra Toast de error al usuario
    end

### 🎨 Representación Gráfica del Flujo de Datos
    
<img width="1912" height="582" alt="image" src="https://github.com/user-attachments/assets/d05b4a68-7855-4662-a53b-c548e76b91fb" />

