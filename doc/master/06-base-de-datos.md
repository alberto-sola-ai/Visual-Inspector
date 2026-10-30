# 06 - Base de datos

## La necesidad

Un autocontrol que no queda registrado sirve de poco en una fábrica. La aplicación tiene que guardar qué se comparó, qué se decidió y quién lo decidió, para poder obtener después el informe de una orden, su trazabilidad o el coste de las incidencias. Y tiene que hacerlo sin conservar los archivos originales, que son diseños de clientes y pesan mucho.

## El concepto

Una base de datos relacional organiza la información en tablas relacionadas entre sí, y garantiza que los datos se guardan de forma coherente. Una capa de acceso traduce los objetos del programa en tablas, de modo que no hace falta escribir consultas a mano y se evitan los errores de seguridad más habituales. Los cambios en la estructura se hacen con migraciones, que son cambios controlados y repetibles.

## Cómo se aplicó

Se eligió MariaDB, en su rama de soporte largo, con Entity Framework como capa de acceso. La base de datos corre en su propio contenedor y solo la aplicación puede alcanzarla. El juego de caracteres y la ordenación son los del español, de modo que acentos, eñes y el símbolo del euro se guardan y se ordenan bien.

El modelo es sencillo y refleja el negocio:

| Tabla | Qué guarda |
|---|---|
| Usuarios, roles y permisos | Quién entra y qué puede hacer; un usuario nuevo no puede nada hasta que se le concede |
| Parámetros | Ajustes que se cambian sin tocar el programa |
| Órdenes de fabricación | Cada orden, su litografía, su línea y si está en fabricación o finalizada |
| Autocontroles | Cada comprobación de una orden, con el patrón usado, la huella de los archivos, la tolerancia y el resultado |
| Diferencias | Cada diferencia detectada, con su recorte, la decisión de la persona y qué se hizo en producción |

Cuatro decisiones de diseño merecen destacarse.

La primera es no guardar los archivos. De cada archivo queda su huella, que demuestra cuál se usó, y de cada diferencia queda un recorte pequeño, lo justo para un registro fiable.

La segunda es la trazabilidad automática. Todas las tablas guardan quién y cuándo creó y modificó cada registro, y esos datos los pone la propia aplicación antes de guardar, así que ninguna pantalla puede olvidarlos.

La tercera es proteger el rastro. Las órdenes con autocontroles no se pueden borrar. Para corregir errores hay dos caminos con distinto rastro: anular, que deja el autocontrol marcado y con su motivo, y eliminar, reservado al administrador y apuntado en el diario de seguridad. Las excepciones controladas se hacen en una sola operación, con aviso y con constancia.

La cuarta es calcular en lugar de duplicar. La trazabilidad de cada orden y el impacto productivo no tienen tablas propias: se calculan en el momento con lo ya guardado, por lo que nunca hay cifras desincronizadas.

## Resultado

La estructura creció con el uso real a lo largo de once migraciones, que la aplicación aplica sola al arrancar y que incluyen una conversión de datos probada antes en una tabla temporal. De ese registro salen el informe de fabricación, la trazabilidad, el cuadro de impacto y las exportaciones, todo con una misma fuente de datos.

## Limitaciones y aprendizaje

Hay una excepción temporal a las normas de la empresa: la aplicación usa una versión anterior de la capa de acceso porque su conector todavía no tiene versión para la última, y se actualizará cuando exista. Y falta lo más importante antes de usar la aplicación con datos reales: las copias de seguridad programadas y un procedimiento de recuperación probado. El aprendizaje es que pensar el modelo desde el negocio, con la trazabilidad y el rastro como requisitos de partida y no como añadidos, evitó muchos problemas posteriores.

Siguiente: [[07-testing]]. La estructura completa está en [[arquitectura]].
