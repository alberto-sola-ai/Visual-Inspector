# Arquitectura de Visual Inspector

Última actualización: 21/10/2026 · Versión 0.2.0

## Resumen

Visual Inspector es una aplicación web interna de WYS, construida según las normas de
desarrollo de la empresa (skill **wys-code**):

| Pieza | Tecnología |
|---|---|
| Pantallas | Blazor (renderizado en el servidor), pensadas para ordenador |
| Lógica | C# · .NET 10, en la carpeta `Servicios/` |
| Datos | MariaDB 11.4 (LTS) con Entity Framework y el conector Pomelo |
| Usuarios | ASP.NET Core Identity, contraseñas con Argon2id |
| Registros | Serilog, ficheros diarios en `log/` |
| Despliegue | Docker: la aplicación y MariaDB definidas en `docker-compose.yml` |
| Ámbito | Interno WYS: la red de la empresa gestiona el cifrado (sin TLS en la aplicación) |

## Capas

Cada capa solo habla con la de debajo. Las pantallas nunca acceden a la base de datos
directamente: siempre pasan por un servicio, que es quien comprueba los permisos.

```
 Navegador (pantalla de ordenador)
        │  HTTP 8080 (cifrado gestionado por la red de WYS)
        ▼
 ┌─────────────────────────────────────────────────────────────┐
 │ Components/  Pantallas Blazor                               │
 │   Pages/              Inicio, Administración                │
 │   Account/            Entrar, Mi cuenta, verificación 2 pasos│
 │   Layout/, Compartido/ Cabecera WYS, menú, avisos de error  │
 ├─────────────────────────────────────────────────────────────┤
 │ Seguridad/   Permisos, cifrado Argon2id, registro Log-Wys-Seg,│
 │              cabeceras, comprobación de permisos             │
 ├─────────────────────────────────────────────────────────────┤
 │ Servicios/   Lógica de negocio (usuarios, parámetros,        │
 │              versiones, autocontroles; próximamente visión)  │
 ├─────────────────────────────────────────────────────────────┤
 │ Datos/       AppDbContext, entidades, trazabilidad,          │
 │              migraciones, inicialización                     │
 └─────────────────────────────────────────────────────────────┘
        │  red interna de Docker (MariaDB no se publica fuera)
        ▼
 MariaDB 11.4 (volumen datos-mariadb)
```

## Carpetas del proyecto

| Carpeta | Contenido |
|---|---|
| `Components/` | Pantallas y piezas de pantalla Blazor |
| `Datos/` | Base de datos: contexto, entidades, migraciones (`Datos/Migraciones/`) e inicialización |
| `Seguridad/` | Permisos, cifrado de contraseñas, registro de seguridad, cabeceras |
| `Servicios/` | Lógica de negocio |
| `recursos/` | Marca WYS (logo, fuentes, iconos, `tema.css`) e informe `versiones.json` |
| `wwwroot/` | Bootstrap y estilos propios (`app.css`) |
| `doc/` | Documentación técnica; `doc/master/` contiene las notas del Máster |
| `log/` | Registros (se crea sola; no se sube a GitHub) |

## Cómo se protege cada acción

1. Todas las páginas exigen haber entrado, salvo la de entrada (política por defecto).
2. Cada página exige su permiso (`[Authorize(Policy = Permisos.AutocontrolesVer_wys)]`).
3. Cada servicio vuelve a comprobar el permiso en el servidor (`ServicioAutorizacion`), aunque
   la página ya lo haya hecho. Ocultar un botón es solo comodidad.
4. Mientras una persona tenga pendiente cambiar su contraseña temporal, no tiene ningún permiso.
5. Cada acción de seguridad se apunta en `log/Log-Wys-Seg-AAAAMMDD.log`.

Detalle completo en [seguridad.md](seguridad.md).

## Modelo de datos

### Ya existe

| Tabla | Contenido |
|---|---|
| `AspNetUsers` | Usuarios (con `NombreCompleto`, `Activo`, `DebeCambiarPassword`, `UltimoAcceso` y trazabilidad) |
| `AspNetRoles`, `AspNetUserRoles` | Roles `Administrador` y `Usuario` |
| `AspNetUserClaims` | Permisos concedidos (tipo `permiso`) |
| `Parametros` | Ajustes de la aplicación (`MfaActivo`, `IaActiva`…) |
| `OrdenesFabricacion` | Código de la orden (10 cifras, único), descripción y si está activa |
| `Autocontroles` | Orden, fecha y hora (las pone el servidor), `HorasDesdeInicio` (0 = Inicial, 4, 8…), estado y observaciones. Índice único orden + horas: una orden no puede tener dos controles del mismo tipo |

Todas las tablas propias guardan quién y cuándo creó y modificó cada registro (`CreadoPor`,
`FechaCreacion`, `ModificadoPor`, `FechaModificacion`), en hora de Madrid. Juego de caracteres
`utf8mb4` con ordenación española (`utf8mb4_uca1400_spanish_ai_ci`).

### Previsto para el MVP

Según [master/02-definicion-mvp.md](master/02-definicion-mvp.md):

| Tabla | Contenido |
|---|---|
| `Autocontroles` (ampliación) | Patrón usado y su versión, resultado global |
| `ArchivosAutocontrol` | Patrón PDF y las dos muestras: ruta en el volumen de archivos, nombre original, tamaño y huella SHA-256 (para demostrar que no se han cambiado) |
| `ResultadosComparacion` | Una fila por comparación (patrón ↔ muestra 1, patrón ↔ muestra 2, muestra 1 ↔ muestra 2): similitud, número de diferencias e imagen del mapa de diferencias |

Los archivos (PDF e imágenes) **no** se guardan dentro de la base de datos, sino en un volumen
de Docker propio (`archivos-app`, montado en `/app/archivos`). La base de datos guarda dónde
están y su huella.

## Motor de visión (previsto)

Irá en `Servicios/Vision/`, separado de las pantallas, para poder probarlo por su cuenta:

```
 Patrón PDF ──► ConversorPdf ──► imagen ─┐
                                         ├─► PreparadorImagenes ─► Alineador ─► Comparador ─► mapa de diferencias
 Muestra 1 ─────────────────────────────┤      (tamaño, color,     (cada muestra   (diferencias,     + similitud (%)
 Muestra 2 ─────────────────────────────┘       márgenes)           con el patrón)   zonas resaltadas)
```

| Pieza | Tecnología candidata | Pendiente de decidir |
|---|---|---|
| PDF a imagen | Biblioteca basada en PDFium (por ejemplo PDFtoImage) | Licencia y funcionamiento dentro de Docker (Linux) |
| Alinear y comparar | OpenCvSharp (OpenCV para C#): puntos característicos y homografía, diferencia absoluta y contornos | Paquete nativo para Linux en la imagen Docker |

Los porcentajes de similitud son solo indicadores técnicos: no son criterios de aceptación ni de
rechazo.

## Decisiones y excepciones

| Decisión | Motivo |
|---|---|
| C# / Blazor en lugar de Python | Normas WYS (decisión de Alberto, 21/10/2026). La visión artificial se hace con OpenCvSharp |
| Entity Framework 9 + Pomelo 9 sobre .NET 10 | **Excepción temporal**: Pomelo aún no tiene versión para Entity Framework 10. Se actualizará cuando salga |
| Sin TLS en la aplicación | Ámbito Interno WYS: lo gestiona la red de la empresa |
| Archivos fuera de la base de datos | Las imágenes escaneadas pesan mucho; la base de datos guarda la ruta y la huella |

## Próximos pasos

En el orden del principio de desarrollo del MVP (cada paso, solo cuando el anterior funcione):

1. ~~Probar el esqueleto con Docker Desktop.~~ Hecho (0.1.1).
2. ~~Tablas `OrdenesFabricacion` y `Autocontroles` y pantalla para iniciar un autocontrol.~~ Hecho (0.2.0).
3. Carga del patrón PDF y de las dos muestras, con su huella, y visualización de los tres.
4. Conversión del PDF a imagen.
5. Preparación y alineación de las muestras.
6. Comparaciones y mapa de diferencias.
7. Registro del resultado del autocontrol.
