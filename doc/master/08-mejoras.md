# 08 - Mejoras

## La necesidad

El MVP demostró que la idea funciona, pero una aplicación solo se descubre del todo cuando alguien la usa. Cada vez que se probaba una pantalla aparecía algo que faltaba, que sobraba o que se entendía de otra manera. Las mejoras no estaban en el plan inicial: salieron del uso real, y hacía falta un método para incorporarlas sin romper lo que ya funcionaba.

## El concepto

El desarrollo iterativo avanza por ciclos cortos de cambio y prueba, en los que el usuario orienta el resultado. En este proyecto cada ciclo siguió el mismo camino: una petición en lenguaje de fábrica, la opinión de la IA, una lista de comprobación de normas, la confirmación de la persona, el cambio, las pruebas, la documentación y una nueva versión. El registro de cambios acumula más de ochenta entradas y el repositorio tiene más de veinte entregas, todas con su explicación.

## Las mejoras, por categorías

| Categoría | Qué se mejoró |
|---|---|
| Flujo de trabajo | Reabrir, anular y eliminar autocontroles con rastro; controles numerados que decide el operario en lugar de horas fijas; línea de fabricación; órdenes abiertas a un clic; cierre de la orden anterior de una línea con aceptación expresa |
| Calidad del dato | Registrar qué se hizo con cada incidencia, con minutos de parada y tubos retenidos que se pueden corregir después |
| Análisis | Trazabilidad de cada orden cerrada, cuadro de impacto productivo con rankings por línea, diseño y defecto, y exportación a Excel y CSV con un detalle de incidencias |
| Operación diaria | Aviso visual de las órdenes que llevan demasiado tiempo sin autocontrol, aviso antes de perder validaciones al cambiar de tolerancia y limpieza de datos de prueba para pasar a producción |
| Experiencia de uso | Modo diurno y nocturno, menús, reloj con turno, histórico de órdenes, identidad visual y logo propios, e iconos y botones pulidos tras varias vueltas |
| Ayuda al usuario | Guía de usuario escrita para los operarios y panel de ayuda flotante con buscador, sin necesidad de IA |
| Seguridad y puesta en marcha | Pruebas de seguridad, lector de registros para el administrador, correcciones de acceso y preparación para publicar tras un proxy inverso |

Varias mejoras cambiaron de forma por el camino, y eso también es parte del trabajo: el cierre automático de órdenes empezó solo avisando y acabó exigiendo una aceptación expresa también en el servidor, y los autocontroles pasaron de tener horas fijas a numerarse según decide el operario.

## Lo que se probó y se descartó

No todo lo probado se quedó. Las carpetas por defecto para patrones y muestras se quitaron al comprobar que el navegador no permite abrir el selector de archivos en una carpeta que indique la aplicación. El borrado de registros se descartó porque permitiría borrar el rastro. Una criticidad automática de las incidencias, un flujo de autorización de Calidad y el análisis por cliente se dejaron fuera a propósito. Y el asistente de ayuda con IA se sustituyó por una solución más sencilla, como se cuenta en [[05-integracion-ia]].

## Resultado

La aplicación pasó de un prototipo que comparaba dos muestras a una herramienta con gestión de usuarios y permisos, un flujo completo del autocontrol, información de calidad exportable y ayuda integrada.

## Limitaciones y trabajo futuro

Lo más importante que queda es operativo: copias de seguridad de los datos, una prueba completa con operarios y datos de producción y la revisión de la tolerancia con más proveedores. Después vienen los filtros de la lista de autocontroles, el cliente en la orden para analizar el impacto por cliente, pruebas automáticas del comparador, un servidor definitivo en la empresa y actualizar la capa de acceso a datos cuando haya una versión compatible. La conclusión de este recorrido está en [[09-conclusiones]].
