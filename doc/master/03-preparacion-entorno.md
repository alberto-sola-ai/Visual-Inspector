# 03 - Preparación del entorno

## La necesidad

Desarrollar con una IA como copiloto exige un entorno más cuidado que el habitual. La IA no recuerda nada de una conversación a otra, así que las reglas del trabajo tienen que estar escritas. La aplicación debe poder ejecutarse igual en cualquier equipo, y debe nacer ya alineada con las normas de desarrollo de la empresa, no adaptarse a ellas al final.

## El concepto

Un entorno de desarrollo reproducible reúne tres cosas: las herramientas con las que se trabaja, las reglas que se aplican siempre y un empaquetado que permite arrancar el resultado con un solo paso.

| Pieza | Función en el proyecto |
|---|---|
| Visual Studio Code con Claude Code | Editor con un agente de IA que lee, escribe, ejecuta y comprueba |
| C#, .NET y Blazor | Plataforma de la aplicación, estándar de la empresa |
| OpenCV | Visión por computador para comparar imágenes |
| Docker | Empaqueta la aplicación y la base de datos para que funcionen igual en cualquier sitio |
| Git y GitHub | Control de versiones |
| Obsidian | Bóveda de notas donde viven el código, la documentación y el máster |
| Reglas escritas | Memoria permanente de la IA: los archivos `CLAUDE.md` y las skills |

## Cómo se aplicó

La primera decisión fue de plataforma. La idea inicial del máster era usar Python, pero al aplicar las normas de la empresa se optó por su estándar, C# con .NET, para que el equipo de IT pueda mantener la aplicación. La visión artificial se conservó con OpenCV, que tiene versión para C#.

La segunda fue convertir las normas de la empresa en conocimiento que la IA carga sola. Se hizo con dos mecanismos: un `CLAUDE.md` en la bóveda y otro en el proyecto, que se leen al empezar cada conversación, y unas skills, paquetes de instrucciones que la IA aplica cuando la tarea lo requiere. Una skill recoge cómo se nombran las cosas, otra la identidad visual de la empresa, y una skill orquestadora decide cuáles hacen falta en cada petición y obliga a enseñar una lista de comprobación antes de escribir código.

La tercera fue empaquetar todo con Docker, norma de la empresa para cualquier aplicación. La aplicación y la base de datos viven en contenedores separados, la base de datos no es accesible desde fuera y los datos sobreviven a las actualizaciones. Las contraseñas no están en el código ni en GitHub, sino en un archivo de configuración que se queda en el equipo. Para producción se prevé publicarla detrás de un proxy inverso que aporte el dominio y el certificado.

## Resultado

La aplicación completa, con su base de datos, arranca con un único comando en cualquier equipo con Docker, y al hacerlo prepara sola las tablas y los usuarios iniciales. Todo el código sigue las mismas reglas de nombres, seguridad e imagen sin que haya que repetirlas. Las skills, además, se guardaron en un repositorio propio y privado para reutilizarlas en otros proyectos de la empresa.

## Limitaciones y aprendizaje

Los problemas que aparecieron al principio fueron de entorno y no de lógica: botones que no respondían por un detalle del empaquetado, sesiones que se cerraban al actualizar y un fallo de acceso en un navegador concreto. Cada uno se resolvió y se trasladó a las reglas para que no se repitiera. La lección es que el entorno es parte del producto: si no se puede arrancar de forma sencilla y repetible, tampoco se puede probar con confianza.

Siguiente: [[04-primer-prototipo]]. El resumen de todo el proyecto está en [[guia-master]].
