# Visual Inspector

## Descripción del proyecto

**Visual Inspector** es una aplicación de asistencia al autocontrol visual de producto diseñada para comparar un patrón gráfico de referencia con una muestra real escaneada.

El objetivo es facilitar la detección de desviaciones visuales entre el diseño aprobado y el producto fabricado, reduciendo la dependencia de procesos manuales de comparación.

Actualmente, este tipo de comprobación puede requerir la utilización de herramientas genéricas, como la superposición de capas en PowerPoint, para comparar visualmente el patrón de referencia con una muestra real.

Visual Inspector pretende transformar este proceso en un flujo más estructurado, repetible y automatizado.

## Problema que queremos resolver

Durante el proceso de fabricación de tubos de aluminio personalizados, la impresión es una etapa crítica del producto.

Witte y Solà dispone de sistemas de visión artificial para el control de impresión en varias líneas de producción.

Sin embargo, existen situaciones de autocontrol o validación visual en las que resulta útil comparar una muestra escaneada con el patrón gráfico aprobado.

Cuando esta comparación se realiza manualmente, pueden aparecer varios inconvenientes:

- dependencia de la inspección visual del usuario;
- dificultad para alinear exactamente patrón y muestra;
- falta de criterios homogéneos de comparación;
- tiempo empleado en preparar y superponer las imágenes;
- dificultad para documentar las diferencias detectadas.

## Objetivo principal

Desarrollar una aplicación que permita:

1. Cargar un patrón gráfico de referencia en formato PDF.
2. Cargar una muestra real previamente escaneada.
3. Convertir y normalizar ambas imágenes.
4. Alinear automáticamente la muestra con el patrón.
5. Comparar visualmente ambos elementos.
6. Detectar y resaltar las principales diferencias.
7. Mostrar al usuario un resultado que facilite la revisión del producto.

La aplicación no pretende sustituir los sistemas industriales de visión artificial ni realizar por sí sola la liberación de producto.

Su función será actuar como una **herramienta de asistencia al autocontrol visual**.

## Contexto industrial

Witte y Solà fabrica tubos de aluminio destinados principalmente a sectores como farmacia, cosmética e industria. La compañía dispone de siete líneas de fabricación y una capacidad de hasta 200 millones de tubos anuales.

El proceso productivo incluye múltiples etapas, entre ellas prensado, torneado, barnizado interior, esmaltado, impresión, tapado, encajado y controles posteriores.

La fase de impresión permite realizar decoraciones de hasta cinco tintas y posteriormente el producto pasa por controles específicos de impresión mediante visión artificial en varias líneas.

Visual Inspector se plantea como una herramienta complementaria a estos sistemas existentes.

## Concepto de funcionamiento

El flujo inicial será:

Patrón PDF  
→ Conversión a imagen  
→ Normalización

Muestra escaneada  
→ Normalización

Ambas imágenes  
→ Alineación automática  
→ Comparación  
→ Detección de diferencias  
→ Visualización del resultado

## Alcance inicial

La primera versión se centrará exclusivamente en la comparación visual entre un patrón y una muestra.

Inicialmente se analizarán aspectos como:

- diferencias de posición;
- diferencias geométricas;
- ausencia o aparición de elementos;
- diferencias visuales globales.

Posteriormente podrían incorporarse funcionalidades más avanzadas, como:

- análisis de color;
- clasificación automática de defectos;
- interpretación de diferencias mediante inteligencia artificial;
- definición de tolerancias;
- histórico de inspecciones;
- generación automática de informes.

## Resultado esperado

Visual Inspector deberá permitir que un usuario sin conocimientos técnicos avanzados pueda cargar dos documentos y obtener de forma visual una indicación clara de dónde existen diferencias relevantes entre ellos.

El valor principal del proyecto será convertir un proceso manual de comparación en un proceso digital, reproducible y asistido por visión por computador.

## Objetivo académico

Además de resolver un problema real de negocio, el proyecto servirá para aprender y aplicar progresivamente conceptos de:

- Python;
- desarrollo de interfaces;
- tratamiento de imágenes;
- visión por computador;
- utilización de librerías como OpenCV;
- procesamiento de documentos PDF;
- gestión de archivos;
- testing;
- Git y GitHub;
- documentación técnica;
- integración posterior con modelos de inteligencia artificial.

El desarrollo se realizará de forma incremental, comenzando por un MVP sencillo y añadiendo nuevas capacidades únicamente cuando la base del sistema sea estable.