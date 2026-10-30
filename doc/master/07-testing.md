# 07 - Testing

## La necesidad

Cuando una IA escribe buena parte del código, probar deja de ser un trámite y pasa a ser la forma de saber qué se tiene. Puede producir algo que compila y parece razonable y que, sin embargo, no hace lo que se pidió. En esta aplicación un fallo tiene además dos consecuencias serias: un registro de calidad equivocado o una brecha de seguridad en una herramienta interna.

## El concepto

Una prueba automática es un pequeño programa que comprueba una expectativa concreta y responde «bien» o «mal». Se repiten cada vez que se cambia algo, y por eso permiten modificar el código con la confianza de que no se rompe lo que ya funcionaba. Como complemento, las pruebas manuales y visuales cubren lo que un programa no puede ver.

## Cómo se aplicó

Las pruebas viven en un proyecto aparte, con xUnit, y se quedan en el equipo: no se publican ni van dentro de la aplicación. Son 125 y todas pasan. Se organizan en cuatro familias.

| Familia | Qué comprueba |
|---|---|
| Seguridad | Que se cumplen las normas internas: contraseñas cifradas de forma irreversible, permisos denegados por defecto y vigilados en el servidor, rechazo de archivos disfrazados o de textos con trucos de inyección, y configuración sin secretos |
| Aplicación en marcha | Desde fuera, con la aplicación en su contenedor: sin sesión todo manda a la entrada, un formulario falso de otra web se rechaza y la base de datos no es accesible |
| Reglas de negocio | Las normas propias del producto: la línea de fabricación solo admite valores de la lista, el cierre de una orden exige aceptación, las alertas de tiempo salen cuando corresponde y la exportación respeta permisos y límites |
| Datos | Las conversiones de la base de datos, probadas antes en una tabla temporal y en los dos sentidos |

A estas se suma la verificación visual: capturas automáticas con un navegador sin ventana, a varios anchos de pantalla, y maquetas con los estilos reales de la aplicación, de día y de noche. Una IA no puede pulsar los botones como una persona, y estas comprobaciones sirvieron para detectar problemas de diseño que el código no delata, como una cabecera que no cabía o un logo con un recuadro alrededor.

También se inspeccionaron por dentro los archivos generados: abrir un libro de Excel de la exportación permitió confirmar que las fechas y los números se guardan como tales y que un texto que empieza por `=` no se ejecuta como fórmula, algo relevante porque los comentarios de las incidencias los escriben los operarios.

## Resultado

Las pruebas de seguridad encontraron dos fallos reales, que se corrigieron: comprobar una contraseña vacía producía un error interno y el servidor anunciaba qué programa usaba, una pista innecesaria. El resultado es una red de seguridad que permitió seguir cambiando el producto durante todo el desarrollo sin miedo a romperlo.

## Limitaciones y aprendizaje

Una memoria honesta debe decir también lo que no se ha probado. Falta una prueba completa con un operario y archivos de producción, de principio a fin, y medir el rendimiento de la comparación en planta. El comparador de imágenes se ha comprobado con muestras reales, pero no tiene pruebas automáticas propias, y su tolerancia se ajustó con muy pocas muestras. La limpieza de datos, la función más delicada, no se ha ejecutado contra una base de datos real, y la descarga de archivos exportados depende del navegador y no se ha probado con una sesión real.

El aprendizaje es doble: probar cuanto antes sale más barato, y cuando algo no se puede comprobar conviene decirlo en lugar de darlo por bueno. Varios problemas solo aparecieron con datos reales o dentro del contenedor, y ninguna prueba automática sustituye a probar la aplicación en su entorno.

Siguiente: [[08-mejoras]]. Las pruebas de seguridad se explican en sencillo en [[pruebas-seguridad]].
