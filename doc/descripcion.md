# Descripción de Visual Inspector

## Qué hace

Ayuda en el autocontrol visual de la impresión de tubos de aluminio. Durante cada orden de
fabricación se toman dos tubos de la misma plancha, se abren, se escanean y se comparan con el
patrón gráfico aprobado y firmado por el cliente (PDF). Visual Inspector hace esa comparación
de forma automática, resalta dónde hay diferencias, la persona valida cada una y todo queda
registrado, con un informe de fabricación al cerrar la orden.

La orden se elige con un clic desde la **planificación de producción** (una hoja de Google
Sheets que mantiene la fábrica), y el patrón se busca en la **carpeta de patrones** de la red por
la especialidad del producto. Si alguna de las dos no está disponible, se trabaja a mano como
siempre.

Es una herramienta de **asistencia**: no aprueba ni rechaza producto. La decisión la toma
siempre la persona que revisa.

La idea completa y el alcance del MVP están en [master/01-idea-y-objetivo.md](master/01-idea-y-objetivo.md)
y [master/02-definicion-mvp.md](master/02-definicion-mvp.md).

## Quién la usa

- Personal de producción y calidad que hace el autocontrol de impresión (al inicio de la orden
  y después cuando lo considera; se recomienda cada 4 horas).
- Administradores de la aplicación, que gestionan usuarios, permisos y parámetros.

## Qué datos maneja

- Usuarios de la aplicación (usuario, nombre completo, rol, permisos, último acceso).
- Parámetros de la aplicación.
- Órdenes de fabricación (código de 10 cifras, línea, descripción y especialidad) y sus
  autocontroles (fecha y hora, número: RPI 1 (inicial), RPI 2…, estado, observaciones, resultado
  y diferencias validadas, con lo que se hizo en producción).
- Del patrón y las muestras solo se guarda su huella SHA-256 y un recorte de cada diferencia:
  los archivos no se guardan.
- Lee, sin guardarla, la planificación de producción (órdenes por línea, prioridad, cantidades)
  y los nombres de los PDF de la carpeta de patrones. Las dos, solo en lectura.

No contiene datos personales más allá del nombre de los usuarios.

## Roles y permisos

Al principio nadie puede hacer nada, salvo el administrador (denegación por defecto).

| Rol | Qué puede hacer |
|---|---|
| Administrador | Todo, incluida la sección de Administración |
| Usuario | Solo lo que el administrador le conceda |

| Permiso | Para qué sirve |
|---|---|
| `Autocontroles.Ver` | Ver la aplicación y consultar los autocontroles realizados |
| `Autocontroles.Realizar` | Hacer un autocontrol nuevo (cargar patrón y muestras y comparar) |

## Estado actual (versión 0.23.0)

El MVP está completo: autocontroles con carga del patrón y las muestras, localización,
comparación, validación de cada diferencia, finalización de la orden e informe de fabricación en
PDF. Por encima hay trazabilidad e impacto productivo exportables a Excel y CSV, guía de usuario
con ayuda dentro de la aplicación, modo diurno y nocturno, y la integración con la planificación
de producción y con la carpeta de patrones.

Pendiente para producción: copias de seguridad, prueba completa con operarios, puesta en marcha
de las integraciones en el servidor y pasar el repositorio a privado. El detalle de cada versión
está en [cambios.md](cambios.md) y la estructura en [arquitectura.md](arquitectura.md).
