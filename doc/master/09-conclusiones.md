# 09 - Conclusiones

## La necesidad

Un proyecto de máster no termina con el código: termina cuando se sabe qué se ha conseguido, qué se ha aprendido y qué falta, y se cuenta con honestidad. Este capítulo cierra el recorrido de las notas anteriores, desde la idea de [[01-idea-y-objetivo]], y deja un balance que sirve de base para la memoria del trabajo.

## Qué se ha conseguido

Se partió de una idea y de una definición de MVP y se llegó a una aplicación que cubre el ciclo completo del autocontrol. Un operario carga un patrón en PDF y dos muestras escaneadas, la aplicación localiza cada muestra sobre el patrón con cualquier giro, compara y propone las diferencias con su recorte, la persona valida cada una como correcta o incidencia y todo queda registrado con quién y cuándo. Al cerrar la orden se obtiene un informe en pantalla y en PDF con la marca de la empresa, y por encima hay cuadros de trazabilidad y de impacto productivo que se pueden exportar a Excel. Además, la aplicación se conecta con dos sistemas de la empresa: toma la orden de la planificación de producción, que dice qué se fabrica en cada línea, y busca el patrón en la carpeta de red por la especialidad del producto, siempre en solo lectura y sin depender de ellos para funcionar.

El volumen de trabajo se resume en unas pocas cifras:

| Medida | Valor |
|---|---|
| Código en C# | 53 archivos, unas 8.100 líneas |
| Pantallas | 57, unas 6.300 líneas |
| Migraciones de la base de datos | 12 |
| Pruebas automáticas | 167, todas correctas |
| Entradas en el registro de cambios | casi 100 |
| Integraciones con sistemas de la empresa | 2 (planificación de producción y carpeta de patrones) |
| Skills con las normas de la empresa | 9, en un repositorio propio |

## Qué funcionó

Avanzar en pasos pequeños y probar cada uno evitó construir sobre suposiciones equivocadas ([[04-primer-prototipo]]). Escribir las normas una sola vez dio coherencia a todo el código ([[03-preparacion-entorno]]). Dejar la decisión en la persona, en el producto y en el desarrollo, mantuvo el control. Y documentar a la vez que se programa evitó que la documentación quedara atrás.

## Qué se ha aprendido

La IA acelera mucho, pero no sabe lo que es una litografía: las mejores decisiones vinieron de quien conoce la fábrica. Una instrucción ambigua cuesta más que una larga, y un dibujo en texto o una tabla de opciones ahorra malentendidos. Hay que comprobar lo que se afirma, porque más de una vez algo que se daba por hecho resultó no serlo. El entorno y las pruebas son parte del producto ([[07-testing]]). Y la base de datos manda: pensar la trazabilidad desde el principio evitó inconsistencias ([[06-base-de-datos]]).

Conectar con sistemas reales enseñó dos cosas más. La primera, que los problemas aparecen fuera del código: permisos de una cuenta de Google, una carpeta de red que desde un servidor Linux pide usuario aunque desde un PC no lo parezca, o una mayúscula en el nombre de una carpeta. La segunda, que cada integración necesita su plan B, para que un fallo externo nunca pare la producción. También hubo un error de la propia IA: al revisar que no se subiera ningún secreto, leyó por descuido el archivo de una clave y su contenido quedó en la conversación. No salió de ahí, lo comunicó de inmediato con su riesgo y su solución, y la decisión la tomó la persona. Es un buen recordatorio de que la transparencia forma parte del trabajo con un agente.

## Limitaciones

La comparación es visión artificial clásica, no un modelo entrenado: es explicable y repetible, pero su tolerancia se ajustó con muy pocas muestras de un solo cliente. La aplicación es una ayuda y no decide: los porcentajes de parecido son orientativos y no hay aceptación ni rechazo automático. No se ha hecho aún una prueba completa con un operario y archivos de producción, no hay copias de seguridad programadas y la IA que ayudó a construirla no ve la aplicación como un usuario con sesión, por lo que lo que no pudo ver se dijo así. Las integraciones funcionan en el equipo de desarrollo, pero falta ponerlas en marcha en el servidor definitivo, y el repositorio de código todavía es público.

## Trabajo futuro

Lo inmediato es asegurar los datos con copias de seguridad, probar con operarios reales, poner en marcha las integraciones en el servidor y pasar el repositorio a privado, y revisar la tolerancia con más proveedores. Después, los filtros de las listas, el cliente en la orden, pruebas automáticas del comparador y un servidor definitivo. El detalle está en [[08-mejoras]].

## Valoración para el máster

El proyecto cumple el objetivo académico de aplicar de forma progresiva visión por computador, bases de datos, contenedores, pruebas, seguridad y control de versiones. Pero lo más valioso que deja es una forma de trabajar: dirigir a un agente de IA con contexto de negocio, normas escritas y control humano en cada paso, y exigirle que explique lo que hace y lo que no ha podido comprobar.

También deja una advertencia razonable: una IA produce con rapidez código que parece correcto, y la calidad depende de las pruebas, de la revisión y de que decida alguien con criterio.

El resumen de todo el recorrido, con tablas y glosario, está en [[guia-master]].
