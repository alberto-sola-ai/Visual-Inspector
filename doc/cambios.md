# Registro de cambios

## 0.10.6 — 2026-10-23
- Inicio: se quita el aviso «En desarrollo · Próximamente…», que ya no era cierto. La caja
  «Nuevo autocontrol» dice ahora «cuando lo consideres (se recomienda cada 4 horas)».

## 0.10.5 — 2026-10-23
- La versión de la aplicación se ve abajo a la derecha, en el pie, solo con la sesión iniciada.
  Se lee sola de `VisualInspector.csproj`, así que no hay que escribirla a mano.

## 0.10.4 — 2026-10-23
- Fondo para todas las pantallas con la sesión iniciada: ilustración de patrón y muestra
  (`recursos/app/fondo-interior.jpg`), fija y con un velo blanco para que textos, tablas y
  recortes se lean igual. La pantalla de entrada mantiene su foto de la fábrica.
- Cabecera ajustada para que, con el reloj, todo quepa en una línea en pantallas de 1280 px: nada
  se parte en dos líneas y el nombre de la persona se corta con «…» si es muy largo.

## 0.10.3 — 2026-10-23
- Pantalla de entrada con una foto de fondo de la fábrica (máquina de impresión y tubos),
  difuminada y con un velo blanco en el centro; el formulario va en una tarjeta blanca y el logo
  de la aplicación, sin fondo, encima.
- Reloj en la barra superior de todas las pantallas: fecha, hora peninsular (Europe/Madrid) que
  se actualiza cada segundo y turno (mañana de 6 a 14 h, tarde de 14 a 22 h, noche de 22 a 6 h).
  Script propio (`wwwroot/js/reloj-turno.js`); la política de seguridad (CSP) no cambia.

## 0.10.2 — 2026-10-23
- Logo propio de la aplicación (carpeta `recursos/app/`): el símbolo va en la cabecera de todas
  las pantallas, junto al nombre y al lado del logo de WYS, que se mantiene igual. En la
  pantalla de entrada se muestra el logo completo, grande, encima del formulario.
- El icono de la pestaña del navegador pasa a ser el símbolo de la aplicación (sustituye al
  cuadrado azul provisional). El informe PDF sigue solo con el logo de WYS.

## 0.10.1 — 2026-10-22
- Botón «ojo» en los campos de contraseña de la pantalla de entrada y de «Cambiar contraseña»:
  al pulsarlo se ve lo escrito y al volver a pulsarlo se oculta. Al enviar el formulario se
  oculta siempre. Script propio (`wwwroot/js/mostrar-password.js`): la política de seguridad
  del navegador (CSP) no cambia.

## 0.10.0 — 2026-10-22
- Los autocontroles ya no van por horas fijas: el operario los hace cuando lo considera. Cada uno
  lleva un número dentro de su orden («Control 1 (inicial)», «Control 2»…) y el tiempo real
  que pasó desde el primero («+3 h 50 min»), en pantallas, listados e informe.
- La recomendación de un autocontrol cada 4 horas es solo un aviso, al crear un autocontrol y
  en la ficha de la orden: «Último autocontrol hace…». Si ya han pasado 4 horas, sale destacado.
- El administrador puede anular o eliminar autocontroles también con la orden finalizada, sin
  reabrirla: el informe se actualiza solo.
- Base de datos: la columna `HorasDesdeInicio` pasa a `NumeroControl` y los controles ya
  hechos se convierten (0 h → 1, 4 h → 2…) (migración RenombraHorasANumeroControl).

## 0.9.0 — 2026-10-22
- El administrador puede **anular** un autocontrol (con motivo obligatorio): sigue en la orden
  marcado «Anulado», con el motivo, quién y cuándo, pero no cuenta en las cifras de la orden, al
  finalizarla ni en el informe. En el informe (pantalla y PDF) aparece en gris con su motivo.
  Un autocontrol anulado ya no se puede registrar.
- El administrador puede **eliminar** un autocontrol erróneo: se borra con todas sus diferencias
  y desaparece de la orden y del informe.
- Las dos acciones están en la ficha del autocontrol, piden confirmación y solo se pueden hacer
  con la orden en fabricación (si está finalizada, primero hay que reabrirla). Las dos se apuntan
  en el diario de seguridad (`AnulacionAutocontrol`, `EliminacionAutocontrol`).
- El tipo del siguiente autocontrol es ahora el último de la orden + 4 h (antes se contaban, y
  tras eliminar uno del medio el siguiente habría chocado con uno existente).
- Base de datos: `EstaAnulado`, `MotivoAnulacion`, `AnuladoPor` y `FechaAnulacion` en
  `Autocontroles` (migración AnadeAnulacionAutocontrol).

## 0.8.0 — 2026-10-22
- El administrador puede reabrir una orden finalizada (botón «Reabrir orden» en la ficha, con
  confirmación) para añadirle más autocontroles. Los nuevos siguen la secuencia de la orden
  (+12 h, +16 h…) y el informe vuelve a ser provisional hasta que se finalice otra vez.
- Quién y cuándo la reabrió queda en la trazabilidad de la orden y en el diario de seguridad
  (evento `ReaperturaOrden`). Sin motivo ni historial aparte (decisión de Alberto, 22/10/2026).
- Si se intenta crear un autocontrol en una orden finalizada, el aviso indica que hay que pedir
  al administrador que la reabra.

## 0.7.0 — 2026-10-22
- Pantalla «Órdenes» (menú): listado con litografía, autocontroles registrados y pendientes, los
  que tienen incidencias, primer y último control y estado. Búsqueda por orden o litografía.
- Ficha de la orden con sus autocontroles en orden de tiempo y botón «Finalizar orden» (con
  confirmación), para quien puede hacer autocontroles. No se puede finalizar si la orden no tiene
  autocontroles o queda alguno sin registrar. Una orden finalizada ya no admite autocontroles nuevos.
- Informe de fabricación en pantalla y en PDF con la marca WYS (logo, Lato, colores y pie
  corporativo): datos de la orden, resumen, autocontroles e incidencias con su comentario, quién
  las validó y su recorte. Antes de finalizar la orden se puede ver, marcado como PROVISIONAL.
- El PDF se descarga desde una dirección propia que vuelve a comprobar el permiso.
- Biblioteca nueva: PDFsharp-MigraDoc 6.2.4 (MIT). Se queda en esa versión: desde la 6.5.0 la
  licencia es AGPL. Se fuerza System.Security.Cryptography.Pkcs 10.0.13 porque la que trae tiene
  fallos de seguridad conocidos.
- Base de datos: estado, fecha de fin y quién finalizó en `OrdenesFabricacion`
  (migración AnadeFinalizacionOrden).

## 0.6.0 — 2026-10-22
- Formulario de validación: cada diferencia se marca como Correcta o Incidencia (comentario
  obligatorio en las incidencias); «Marcar pendientes como correctas» valida un grupo entero de un
  clic, también las diferencias plegadas. Hay que validar todas para poder registrar.
- Botón «Registrar autocontrol»: guarda en una sola operación el patrón (archivo, litografía y
  versión), las huellas SHA-256 de los tres archivos, la tolerancia, las similitudes, el resultado
  (sin diferencias / diferencias correctas / con incidencias) y cada diferencia con su recorte y su
  validación. Después ya no se puede cambiar.
- Si la litografía del patrón no coincide con la de la orden, se avisa y hay que confirmar que el
  patrón es el correcto (queda registrado). La orden toma la litografía de su primer autocontrol.
- No se deja registrar si alguna muestra no se ha podido comparar.
- Ficha de solo lectura de los autocontroles registrados y columna «Resultado» en el listado.
- Base de datos: tabla `Diferencias` y columnas nuevas en `Autocontroles` y `OrdenesFabricacion`
  (migración AnadeRegistroAutocontrol).

## 0.5.0 — 2026-10-22
- Comparación de las dos muestras con el patrón (botón «Comparar con el patrón»): ajuste fino de
  la colocación por zonas, tono igualado tinta por tinta (no se analiza el color) y búsqueda de
  diferencias de contenido con un margen de desajuste.
- Tres motivos: mancha o suciedad, falta elemento o tinta, y desplazamiento o texto doble. Los
  pliegues, los bordes de la pieza y las guías de montaje del patrón no se tienen en cuenta.
- Cada diferencia indica si sale en las dos muestras (posible fallo de plancha) o en una, y trae
  un recorte en color patrón | muestra(s). Se ordenan por evidencia y, de cada motivo, se enseñan
  abiertas solo las 5 más evidentes.
- Tolerancia en tres niveles (Estricta, Normal, Amplia) que el operario cambia en cada
  autocontrol; la inicial la fija el administrador (parámetro nuevo `ToleranciaPorDefecto`).
- Parámetros de tipo lista (desplegable en Administración → Parámetros).
- Los mismos archivos dan siempre el mismo resultado (semilla fija en los pasos al azar).

## 0.4.0 — 2026-10-22
- La litografía (L + 7 cifras) y la versión (R + cifras) se leen del principio del nombre del
  patrón. Si el nombre no sigue el formato, se avisa y se escriben a mano.
- Botón «Localizar muestras en el patrón»: recorta cada pieza del fondo del escáner y la encuentra
  en la página del patrón con cualquier giro, aunque el tubo se haya abierto por otro punto
  (dos trozos). Muestra el giro, los trozos y el patrón con la muestra superpuesta, en color.
- Las muestras pueden ser también TIFF.
- Visión artificial con OpenCvSharp 4.13 (Apache 2.0). La imagen de Docker instala las bibliotecas
  del sistema que necesita y solo lleva lo de Linux.
- En Windows (equipo de desarrollo) OpenCV trabaja con un solo hilo: con varios se cerraba de golpe.
- Panel de versiones: cada pieza (bibliotecas, .NET y MariaDB) explica para qué se usa en el
  proyecto y cuál es su licencia.

## 0.3.0 — 2026-10-22
- Ficha del autocontrol: carga desde el PC del patrón (PDF) y de las dos muestras (JPG o PNG a
  300 ppp, en cualquier orientación), con su vista en pantalla, tamaño en milímetros y huella SHA-256.
- El PDF se convierte en imagen a 300 ppp (biblioteca PDFtoImage). Se propone la versión del
  patrón si el nombre del archivo la incluye (por ejemplo R002).
- Los archivos no se guardan: se revisan en memoria y se descartan al salir de la pantalla.
- Carpetas locales `muestras-prueba/` y `patrones/` para pruebas, excluidas de Git y de Docker.

## Sin versión — 2026-10-21
- Puerto de acceso desde fuera de Docker cambiado del 8080 al 8082 (`docker-compose.yml`). Dentro
  del contenedor la aplicación sigue en el 8080. Ahora se abre <http://localhost:8082>.

## 0.2.1 — 2026-10-21
- Nuevo autocontrol: quitado el número de ejemplo del campo de la orden (parecía una orden ya
  escrita). Debajo del campo se explica el formato: exactamente 10 cifras, solo números.

## 0.2.0 — 2026-10-21
- Añadidas las órdenes de fabricación (10 cifras, por ejemplo 1000063961) y los autocontroles.
- Pantallas: listado de autocontroles con búsqueda por orden, nuevo autocontrol y ficha.
- El tipo de control se calcula solo (Inicial, +4 h, +8 h…) y la fecha y hora las pone el
  servidor. Una orden no puede tener dos controles del mismo tipo.
- Inicio con accesos directos a "Nuevo autocontrol" y "Autocontroles realizados".

## 0.1.1 — 2026-10-21
- Corregido: los botones de las pantallas interactivas (por ejemplo "Guardar permisos") no hacían
  nada. La imagen de Docker no incluía el programa de Blazor (`_framework/blazor.web.js`) porque el
  `Dockerfile` compilaba con `--no-restore`. Ahora vuelve a resolver los paquetes con todo el código.
- Las claves que firman las sesiones se guardan en el volumen `claves-app`: reiniciar o actualizar
  la aplicación ya no cierra las sesiones ni da errores de formulario caducado.

## 0.1.0 — 2026-10-21
- Adaptado el proyecto a las normas WYS (skill wys-code, ámbito Interno WYS).
- Creado el esqueleto de la aplicación: C# · .NET 10 · Blazor · MariaDB · Docker.
- Entrada con usuario y contraseña, bloqueo tras 3 intentos y cambio obligatorio de la
  contraseña temporal.
- Permisos por funcionalidad con denegación por defecto (`Autocontroles.Ver`,
  `Autocontroles.Realizar`).
- Administración: usuarios, permisos, parámetros y versiones.
- Registros en `log/` (err-log y Log-Wys-Seg) e identidad visual WYS.
- Documentación técnica en `doc/`; las notas del Máster pasan de `docu/` a `doc/master/`.
- Excepción temporal: capa de datos con Entity Framework 9 + Pomelo 9 hasta que exista Pomelo 10.
