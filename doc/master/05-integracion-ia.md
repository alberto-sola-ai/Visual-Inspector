# 05 - Integración IA

## La necesidad

La IA interviene en este proyecto de dos maneras distintas que conviene no mezclar. Es la herramienta con la que se ha construido la aplicación, y es también una posible pieza dentro de ella. En los dos casos la necesidad es la misma: aprovechar lo que la IA aporta sin perder el control del negocio, de la calidad del producto ni de la seguridad.

## La IA como copiloto de desarrollo

La aplicación se ha desarrollado con un agente de programación, no con un simple autocompletado. El agente lee los archivos del proyecto, escribe código y documentación, ejecuta comandos, compila, arranca los contenedores, hace capturas de pantalla y corrige lo que falla. El reparto de papeles fue siempre el mismo:

| La persona | La IA |
|---|---|
| Conoce el negocio y decide qué se hace y qué se descarta | Traduce las necesidades en pantallas, servicios y tablas |
| Aporta material real: patrones, muestras, logos | Propone opciones con su recomendación y las construye |
| Prueba en pantalla y da su visto bueno | Compila, prueba y documenta cada cambio |

La regla de fondo es que la IA propone y ejecuta, y la persona decide.

Para que ese reparto funcione hubo que estructurar el trabajo. Las reglas se escribieron una sola vez, en la memoria permanente del proyecto y en las skills de la empresa. Cada cambio sigue un mismo ciclo: la IA presenta una lista de comprobación de normas y un plan, la persona confirma, se escribe y se prueba, se documenta y se cierra con una revisión de seguridad. Para los cambios grandes existe además un modo de planificación en el que la IA solo analiza y propone, y no toca nada hasta que el plan se aprueba.

La comunicación se hizo en lenguaje natural, con contexto de negocio. Funcionó mejor aportar material real, avanzar en pasos pequeños y pedir opinión antes de decidir. Algunas aportaciones de la persona cambiaron el producto, como pedir que el color se viera pero no se midiera, y los malentendidos enseñaron a resolver la ambigüedad con un dibujo en texto o una tabla de opciones antes de programar.

## La IA dentro de la aplicación

La comparación del patrón con la muestra no usa un modelo de IA entrenado, sino visión artificial clásica. Se buscan puntos característicos en las dos imágenes, se calcula el giro y la posición que mejor las hacen coincidir, se corrige la curvatura de la pieza, se igualan los tonos por tintas y se miden las diferencias con un margen que depende de la tolerancia.

Se eligió así porque es explicable (cada diferencia se enseña con su recorte y su motivo), repetible (los mismos archivos dan siempre el mismo resultado) y no necesita datos de entrenamiento, que serían diseños de clientes. El detalle técnico está en [[arquitectura]].

La decisión final es siempre humana. La aplicación señala posibles diferencias y el operario marca cada una como correcta o como incidencia, con un comentario. La herramienta ayuda a mirar, pero no decide si el producto está bien.

## Lo que se decidió no usar

Se descartó la IA generativa para interpretar las diferencias, y la norma de seguridad de la empresa refuerza esa prudencia: las funciones de IA solo las activa el administrador, vienen desactivadas por defecto y sus claves no van nunca en el código. Un asistente de ayuda en lenguaje natural se resolvió también sin IA, porque no había un servidor propio para un modelo ni se quiso depender de una API externa: una guía de usuario y un panel de ayuda con buscador.

## Limitaciones y aprendizaje

La IA se equivoca y a veces entiende otra cosa, por eso existen la lista previa, las preguntas y las pruebas. Tampoco ve la aplicación como un usuario con sesión, y lo que no pudo comprobar lo dijo así. El aprendizaje principal es que la IA acelera mucho el trabajo, pero el conocimiento del negocio sigue siendo humano.

Siguiente: [[06-base-de-datos]].
