# Descripción de Visual Inspector

## Qué hace

Ayuda en el autocontrol visual de la impresión de tubos de aluminio. Durante cada orden de
fabricación se toman dos tubos de la misma plancha, se abren, se escanean y se comparan con el
patrón gráfico aprobado y firmado por el cliente (PDF). Visual Inspector hará esa comparación
de forma automática y resaltará dónde hay diferencias.

Es una herramienta de **asistencia**: no aprueba ni rechaza producto. La decisión la toma
siempre la persona que revisa.

La idea completa y el alcance del MVP están en [master/01-idea-y-objetivo.md](master/01-idea-y-objetivo.md)
y [master/02-definicion-mvp.md](master/02-definicion-mvp.md).

## Quién la usa

- Personal de producción y calidad que hace el autocontrol de impresión (al inicio de la orden
  y cada 4 horas).
- Administradores de la aplicación, que gestionan usuarios, permisos y parámetros.

## Qué datos maneja

- Usuarios de la aplicación (usuario, nombre completo, rol, permisos, último acceso).
- Parámetros de la aplicación.
- Órdenes de fabricación (código de 10 cifras) y sus autocontroles (fecha y hora, tipo:
  Inicial, +4 h, +8 h…, estado y observaciones).
- Próximamente: patrones PDF, imágenes de las muestras y resultados de las comparaciones.

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

## Estado actual (versión 0.2.0)

Está hecho el esqueleto según las normas WYS (entrada con usuario y contraseña, permisos,
Administración, registros en `log/`, identidad visual y Docker) y el primer paso del MVP: crear
y consultar autocontroles de cada orden de fabricación. El siguiente paso es cargar el patrón y
las muestras (ver [arquitectura.md](arquitectura.md)).
