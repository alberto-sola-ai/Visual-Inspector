# Puesta en marcha

## Requisitos

- **Docker Desktop** (en el equipo) o Docker (en el servidor). Es lo único imprescindible.
- Para programar: SDK de **.NET 10** y la herramienta de migraciones
  (`dotnet tool install --global dotnet-ef`).

## Arrancar con Docker (la forma normal)

1. Copia `.env.example` como `.env` y rellena las contraseñas:
   - `MARIADB_PASSWORD` y `MARIADB_ROOT_PASSWORD`: contraseñas de la base de datos.
   - `PASSWORD_ADMIN` y `PASSWORD_PRUEBA`: contraseñas iniciales (mínimo 8 caracteres, con
     mayúscula, minúscula y número). Si las dejas vacías, la aplicación inventa una y la muestra
     una sola vez en la consola.
   - `CREAR_USUARIO_PRUEBA`: `true` para pruebas; `false` en producción.
2. En la carpeta del proyecto:
   ```bash
   docker compose up --build
   ```
3. Abre <http://localhost:8082>.

Al arrancar, la aplicación crea sola las tablas (migraciones), los roles, los parámetros y los
usuarios iniciales.

`.env` nunca se sube a GitHub (está en `.gitignore`).

## Primera entrada

1. Entra con el usuario `admin` y la contraseña de `PASSWORD_ADMIN` (o la que salió en la
   consola: `docker compose logs app`).
2. La aplicación te pedirá cambiarla en ese momento.
3. En **Administración → Usuarios** crea los usuarios reales y, en la ficha de cada uno,
   marca sus permisos. Los usuarios nuevos no pueden hacer nada hasta que les des permisos.

El usuario `prueba` existe solo si `CREAR_USUARIO_PRUEBA=true` y nace sin permisos.

## Arrancar sin Docker (solo para programar)

Hace falta un servidor MariaDB 11.4 accesible. La contraseña de la conexión se guarda con
user-secrets (nunca en `appsettings.json`):

```bash
dotnet user-secrets set "ConnectionStrings:Principal" "Server=...;Port=3306;Database=visualinspector;User=visualinspector;Password=..."
dotnet user-secrets set "UsuariosIniciales:PasswordAdmin" "..."
dotnet run
```

Antes de dar por buena una versión, pruébala siempre con `docker compose up --build`.

## Desplegar en un servidor

1. Copia el proyecto al servidor.
2. Crea el `.env` a partir de `.env.example` con los datos de ese servidor
   (`ASPNETCORE_ENVIRONMENT=Production`, `CREAR_USUARIO_PRUEBA=false`).
3. Arranca: `docker compose up -d --build`.
4. Para actualizar a una versión nueva, repite el paso 3: los datos y los registros se conservan
   en sus volúmenes (`datos-mariadb`, `registros-app` y `claves-app`, que guarda las claves de
   las sesiones para que nadie tenga que volver a entrar tras actualizar).

Los datos de los servidores de la empresa están pendientes de definir (skill wys-code,
`references/infraestructura.md`).

## Publicar detrás de Nginx Proxy Manager (producción)

En producción, **Nginx Proxy Manager** está delante de la aplicación: recibe las visitas por el
dominio con su certificado (https) y se las pasa a la aplicación por http. La aplicación no
lleva certificados (ámbito Interno WYS).

1. En Nginx Proxy Manager, el «Proxy Host» del dominio apunta a la aplicación (puerto 8082 del
   servidor, o 8080 del contenedor si comparten red de Docker), con el certificado SSL y
   «Websockets Support» activado (Blazor lo necesita).
2. En el `.env` del servidor:
   ```
   USAR_CABECERAS_PROXY=true
   PROXY_CONFIABLE=<IP de Nginx Proxy Manager tal como la ve la aplicación, o su red, p. ej. 172.18.0.0/16>
   ```
3. `docker compose up -d` para aplicar el cambio.

Con eso la aplicación sabe que el usuario entra por https (las cookies se marcan «solo https») y
el diario de seguridad guarda la IP real de cada usuario. Solo se aceptan esas cabeceras si
llegan de `PROXY_CONFIABLE`: las de cualquier otro equipo se ignoran.

**Cómo saber la IP de Nginx Proxy Manager:** activa primero `USAR_CABECERAS_PROXY=true` con
`PROXY_CONFIABLE` vacío, entra en la aplicación y mira en Administración → Registros Err./Seg. →
Seguridad la IP de tu entrada (`InicioSesion`): esa es la del proxy. Ponla en `PROXY_CONFIABLE`
y reinicia. Desde ese momento, las entradas siguientes mostrarán la IP real de cada equipo.

## Registros (carpeta `log/`)

| Fichero | Contenido | Se guarda |
|---|---|---|
| `log/err-log-AAAAMMDD.log` | Errores y avisos, con el código `ERR-…` que ve el usuario | 30 días |
| `log/Log-Wys-Seg-AAAAMMDD.log` | Seguridad: entradas, fallos, bloqueos, cambios de usuarios, permisos y parámetros | 90 días |

- Sin Docker, están en la carpeta `log/` del proyecto.
- Con Docker, están en el volumen `registros-app`. Para descargarlos al equipo:
  ```bash
  docker compose cp app:/app/log ./log-servidor
  ```
- El administrador también los puede leer desde la aplicación: **Administración → Registros**
  (cada consulta queda apuntada en el diario de seguridad).


## Informe de versiones

La pantalla **Administración → Versiones y actualizaciones** lee `recursos/versiones.json`.
Regenéralo **antes de publicar cada versión** (necesita internet):

```powershell
powershell -ExecutionPolicy Bypass -File generar-versiones.ps1
```

## Cambios en la base de datos

Siempre con migraciones, nunca tocando la base de datos a mano:

```bash
dotnet ef migrations add NombreDelCambio -o Datos/Migraciones
```

La aplicación aplica las migraciones pendientes al arrancar.
