# 04 - Primer prototipo

## La necesidad

Antes de construir una aplicación completa había que comprobar que la idea funcionaba: que un programa podía tomar un patrón en PDF y dos muestras escaneadas, encontrar dónde difieren y dejar el resultado registrado. El prototipo no tenía que ser completo ni bonito. Tenía que demostrar el núcleo, con archivos reales y en poco tiempo.

## El concepto

Un MVP, o producto mínimo viable, es la primera versión que demuestra el valor central de una idea con el menor esfuerzo posible. Aquí se desarrolló de forma incremental, con una regla: cada paso se hace solo cuando el anterior funciona. La definición completa está en [[02-definicion-mvp]].

## El problema técnico

Lo difícil no era comparar dos imágenes, sino colocar una sobre otra. El patrón es una hoja de aprobación de un proveedor, con cajetín, cotas, logos y guías de montaje alrededor del diseño. La muestra es un tubo cortado y abierto en plano, escaneado en cualquier orientación, que ocupa una parte pequeña del escaneo y que puede llegar partido en dos trozos si se abrió por otro punto. Además aparecen pliegues, bordes rotos, brillos del aluminio y polvo del cristal, que no deben contar como defectos.

## Cómo se resolvió

El prototipo se recorrió por etapas, cada una demostrable en pantalla:

1. Registro de órdenes de fabricación y de autocontroles.
2. Carga del patrón y de las dos muestras, con el PDF convertido a imagen a alta resolución.
3. Localización de cada muestra dentro del patrón, con cualquier giro y con el tubo en dos trozos.
4. Comparación, con una lista de posibles diferencias agrupadas por motivo y con tres niveles de tolerancia.
5. Validación de cada diferencia por la persona y registro del autocontrol.
6. Informe de fabricación en pantalla y en PDF.

Cuatro decisiones de diseño marcaron el resultado:

- La persona valida. La aplicación propone y el operario decide si cada diferencia es correcta o una incidencia.
- Los archivos no se guardan. Son diseños de clientes, y bastan una huella de cada archivo y un recorte de cada diferencia.
- Se ve en color, pero no se mide el tono. Importa el contenido impreso, no pequeñas variaciones de color.
- Los mismos archivos dan siempre el mismo resultado, lo que permite repetir y comprobar cada comparación.

Se dejó fuera, a propósito, lo que el MVP declaraba que no haría: liberar o rechazar producto de forma automática, analizar el color, integrarse con otros sistemas de la empresa o interpretar las diferencias con IA generativa.

## Resultado

Con patrones y muestras reales, la comparación encontró las manchas de las dos muestras y un texto duplicado, con muy pocos avisos dudosos. El ciclo completo funcionó de principio a fin: de dos muestras escaneadas a un informe con las decisiones de la persona que las revisó. Con eso la idea quedó demostrada.

## Limitaciones y aprendizaje

La tolerancia se ajustó con muy pocas muestras de un solo cliente, y conviene revisarla con patrones de otros proveedores. Las zonas blancas sin imprimir en el borde de la pieza se confunden con el fondo del escáner y pueden quedar fuera de la comparación. Lo que más ayudó fue avanzar en pasos que se pudieran enseñar y probar cuanto antes con datos reales, porque las dificultades serias, como el giro libre o el tubo en dos trozos, solo aparecieron con material de verdad.

Siguiente: [[05-integracion-ia]].
