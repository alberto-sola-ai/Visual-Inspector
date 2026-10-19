
# 02 - Definición del MVP

## ¿Qué es el MVP de Visual Inspector?

El MVP (*Minimum Viable Product* o Producto Mínimo Viable) será la primera versión funcional de **Visual Inspector**.

Su objetivo será demostrar que es posible automatizar parcialmente el proceso actual de autocontrol visual de impresión mediante la comparación de:

* un patrón gráfico oficial de referencia;
* una primera muestra real escaneada;
* una segunda muestra real escaneada.

El concepto principal que deberá validar el MVP será:

> **Comparar automáticamente las dos muestras obtenidas durante el autocontrol de producción contra el patrón gráfico aprobado por el cliente, facilitando la detección de desviaciones visuales y dejando registro del resultado obtenido.**

---

# Contexto del autocontrol

## Patrón de referencia

El patrón de referencia estará disponible siempre en formato:

```text
PDF
```

Este PDF corresponde al documento gráfico aprobado y firmado por el cliente.

El documento dispone de su correspondiente versión y constituye la referencia contra la que debe comprobarse la impresión realizada durante la fabricación.

Por tanto, Visual Inspector no generará ni modificará el patrón gráfico.

El patrón será siempre un documento externo previamente aprobado.

---

# Obtención de las muestras

Durante el proceso de impresión, la plancha utilizada imprime **dos tubos simultáneamente**.

Por este motivo, durante el autocontrol se toman dos tubos procedentes de la misma impresión.

Actualmente, el procedimiento consiste en:

```text
Máquina de impresión
        ↓
Extracción de dos tubos
        ↓
Corte de los tubos
        ↓
Apertura para obtener superficie plana
        ↓
Escaneado
        ↓
Comparación visual
```

Los dos tubos se cortan para poder disponer de la superficie impresa en formato plano.

Posteriormente se escanean y se generan dos archivos de imagen.

Visual Inspector utilizará estos dos archivos como muestras reales de producción.

---

# ¿Por qué se analizan dos tubos?

La utilización de dos muestras procedentes de la misma plancha es importante porque permite disponer de mayor información sobre el origen de una posible desviación.

Por ejemplo, una diferencia visual podría estar relacionada con:

* un posible defecto de la propia plancha;
* una desviación de impresión;
* una mancha puntual;
* una anomalía presente únicamente en uno de los tubos;
* una desviación reproducida en ambos tubos.

Por este motivo, Visual Inspector no realizará solamente:

```text
Patrón ↔ Muestra 1
```

sino que analizará:

```text
                 PATRÓN PDF
                 /        \
                /          \
               ▼            ▼

          MUESTRA 1      MUESTRA 2
               \            /
                \          /
                 ▼        ▼

             COMPARACIÓN
             ENTRE MUESTRAS
```

Esto dará lugar a tres posibles comparaciones:

```text
Patrón   ↔ Muestra 1

Patrón   ↔ Muestra 2

Muestra 1 ↔ Muestra 2
```

La comparación entre las dos muestras podrá ayudar posteriormente a interpretar mejor las diferencias encontradas.

En el MVP, la aplicación mostrará estas diferencias al usuario, pero no determinará automáticamente su causa.

---

# Momento de realización del autocontrol

El autocontrol de impresión se realiza durante una orden de fabricación.

La prueba se realizará:

1. **Al inicio de la orden de fabricación.**
2. **Cada cuatro horas de fabricación.**

Por tanto, una misma orden de fabricación podrá generar varios controles Visual Inspector.

Por ejemplo:

```text
ORDEN DE FABRICACIÓN
        │
        ├── Control inicial
        │
        ├── Control +4 horas
        │
        ├── Control +8 horas
        │
        ├── Control +12 horas
        │
        └── ...
```

Cada control deberá quedar asociado a la correspondiente orden de fabricación.

---

# Registro del autocontrol

El resultado del autocontrol no será únicamente una visualización temporal.

La prueba formará parte del **registro de fabricación**.

Esto permitirá conservar evidencias de los controles realizados durante la producción.

Posteriormente, esta información podrá ser utilizada como parte de la información necesaria para la generación del correspondiente control de calidad y certificado.

Por este motivo, el MVP deberá contemplar un registro básico de cada inspección realizada.

---

# Objetivo del MVP

El usuario deberá poder:

1. Identificar la orden de fabricación que se está controlando.
2. Cargar el patrón gráfico aprobado en formato PDF.
3. Identificar la versión del patrón utilizado.
4. Cargar la primera muestra escaneada.
5. Cargar la segunda muestra escaneada.
6. Visualizar patrón y muestras.
7. Convertir el PDF a una imagen procesable.
8. Preparar las tres imágenes.
9. Alinear cada muestra con el patrón.
10. Comparar cada muestra contra el patrón.
11. Comparar las dos muestras entre sí.
12. Detectar y resaltar diferencias visuales.
13. Mostrar el resultado al usuario.
14. Registrar el autocontrol realizado.

---

# Entradas del sistema

Cada autocontrol tendrá como mínimo las siguientes entradas:

```text
Orden de fabricación
Fecha y hora del control
Tipo de control
Patrón PDF
Versión del patrón
Muestra 1
Muestra 2
```

El tipo de control podrá ser inicialmente:

```text
INICIAL

4 HORAS
```

Posteriormente podrá evolucionar para identificar automáticamente:

```text
Inicial
+4 h
+8 h
+12 h
+16 h
...
```

---

# Archivos utilizados

## Patrón

Siempre será:

```text
1 archivo PDF
```

Correspondiente al patrón aprobado por el cliente.

---

## Muestras

Siempre serán:

```text
2 archivos de imagen
```

procedentes del escaneado realizado en el momento del autocontrol.

Inicialmente podrán admitirse formatos como:

```text
PNG
JPG
JPEG
```

---

# Flujo básico del MVP

El funcionamiento será:

```text
           ORDEN DE FABRICACIÓN
                    │
                    ▼
             PATRÓN PDF
            aprobado cliente
                    │
                    ▼
           Conversión a imagen
                    │
                    ▼
               PATRÓN
              /      \
             /        \
            ▼          ▼
       MUESTRA 1    MUESTRA 2
            │          │
            ▼          ▼
        Normalizar imágenes
            │          │
            ▼          ▼
          Alinear con patrón
            │          │
            ▼          ▼
           Comparar resultados
                │
                ▼
       Comparar muestras entre sí
                │
                ▼
         Detectar diferencias
                │
                ▼
         Visualizar resultados
                │
                ▼
        Registrar autocontrol
```

---

# Procesamiento

## 1. Conversión del patrón

El PDF será convertido a una imagen adecuada para su procesamiento.

De esta forma, patrón y muestras podrán ser tratados mediante las mismas herramientas de visión por computador.

---

## 2. Preparación de imágenes

Los archivos podrán presentar diferencias debido al proceso de escaneado:

* resolución;
* dimensiones;
* orientación;
* márgenes;
* posición;
* pequeñas rotaciones.

Las imágenes deberán prepararse antes de realizar la comparación.

---

## 3. Alineación

Cada una de las dos muestras deberá alinearse de forma independiente respecto al patrón.

Tendremos:

```text
Patrón
   │
   ├── Alineación Muestra 1
   │
   └── Alineación Muestra 2
```

Esta etapa será fundamental.

Una diferencia de posición producida durante el escaneado no debe confundirse con una diferencia real de impresión.

---

# 4. Comparación patrón-muestra

Se realizarán dos análisis independientes:

### Comparación A

```text
Patrón ↔ Muestra 1
```

### Comparación B

```text
Patrón ↔ Muestra 2
```

Cada comparación deberá generar un mapa visual de las diferencias encontradas.

---

# 5. Comparación entre muestras

Visual Inspector realizará también:

```text
Muestra 1 ↔ Muestra 2
```

Esta comparación permitirá visualizar si determinadas diferencias aparecen:

* en ambas muestras;
* únicamente en la muestra 1;
* únicamente en la muestra 2.

Esta información podrá resultar especialmente útil durante la revisión del autocontrol.

---

# 6. Visualización

La interfaz deberá permitir visualizar de forma sencilla:

```text
PATRÓN

[ imagen ]


MUESTRA 1

[ imagen ]


MUESTRA 2

[ imagen ]
```

y posteriormente los resultados:

```text
PATRÓN vs MUESTRA 1

[ diferencias ]


PATRÓN vs MUESTRA 2

[ diferencias ]


MUESTRA 1 vs MUESTRA 2

[ diferencias ]
```

Las zonas donde existan diferencias deberán aparecer resaltadas visualmente.

---

# Indicadores

Como apoyo al usuario podrán calcularse indicadores sencillos.

Por ejemplo:

```text
Muestra 1
Similitud con patrón: 97,2 %

Muestra 2
Similitud con patrón: 96,8 %

Similitud entre muestras: 98,4 %
```

Estos valores serán únicamente indicadores técnicos del algoritmo.

Durante el MVP no constituirán por sí solos criterios oficiales de aceptación o rechazo del producto.

---

# Resultado del autocontrol

Visual Inspector deberá generar un resultado asociado al control realizado.

Por ejemplo:

```text
ORDEN DE FABRICACIÓN: OF-XXXXXX

PATRÓN
Versión: XX

CONTROL
Tipo: Inicial
Fecha: XX/XX/XXXX
Hora: XX:XX


MUESTRA 1
Similitud: 97,2 %
Diferencias detectadas: 2


MUESTRA 2
Similitud: 96,8 %
Diferencias detectadas: 3


COMPARACIÓN ENTRE MUESTRAS
Similitud: 98,4 %


RESULTADO
Revisión visual requerida
```

---

# Registro de fabricación

Cada análisis realizado deberá generar un registro básico.

Inicialmente deberá conservar como mínimo:

```text
Orden de fabricación

Fecha

Hora

Tipo de control

Identificación del patrón

Versión del patrón

Muestra 1

Muestra 2

Resultado comparación muestra 1

Resultado comparación muestra 2

Resultado comparación entre muestras
```

El objetivo será disponer de trazabilidad sobre los autocontroles realizados durante la fabricación.

---

# Criterios de éxito del MVP

Consideraremos que la primera versión de Visual Inspector funciona cuando pueda realizarse de principio a fin el siguiente proceso:

```text
1. Introducir una orden de fabricación.

2. Cargar el patrón PDF.

3. Indicar la versión del patrón.

4. Cargar las dos muestras escaneadas.

5. Visualizar los tres documentos.

6. Convertir el patrón PDF.

7. Preparar las imágenes.

8. Alinear automáticamente las muestras.

9. Comparar Muestra 1 contra patrón.

10. Comparar Muestra 2 contra patrón.

11. Comparar ambas muestras entre sí.

12. Detectar diferencias.

13. Mostrar gráficamente esas diferencias.

14. Registrar el autocontrol.
```

Si conseguimos ejecutar correctamente este flujo, tendremos validado el núcleo funcional de Visual Inspector.

---

# Qué NO hará inicialmente el MVP

Para mantener un alcance asumible durante la primera fase, Visual Inspector no realizará inicialmente:

* liberación automática del producto;
* aprobación o rechazo automático;
* modificación del patrón firmado;
* validación metrológica;
* criterios oficiales de tolerancia;
* integración directa con SAP;
* integración con MES;
* integración con las máquinas de producción;
* integración con los sistemas industriales de visión artificial existentes;
* análisis avanzado de color;
* clasificación automática de la causa del defecto;
* generación automática del certificado de calidad;
* inteligencia artificial generativa para interpretar el defecto.

Estas capacidades podrán estudiarse en fases posteriores.

---

# Evolución posterior

Una vez validado el núcleo del sistema podremos incorporar nuevas funcionalidades.

## Tolerancias

Definir qué desviaciones pueden considerarse aceptables.

## Análisis de color

Incorporar métricas específicas para comparar diferencias cromáticas.

## Clasificación de diferencias

Distinguir automáticamente entre:

```text
desplazamiento

mancha

elemento faltante

deformación

posible defecto repetitivo

diferencia cromática
```

## Interpretación mediante IA

Utilizar inteligencia artificial para analizar conjuntamente:

```text
Patrón

Muestra 1

Muestra 2

Mapa de diferencias

Histórico
```

y proporcionar al usuario una explicación asistida de las anomalías encontradas.

## Integración con registro de fabricación

Integrar Visual Inspector con los sistemas corporativos para evitar introducir manualmente datos como:

```text
orden de fabricación

referencia

cliente

producto

versión

fecha
```

## Certificado de calidad

Utilizar los controles realizados durante la fabricación como fuente de datos para generar o complementar posteriormente la documentación de calidad correspondiente.

---

# Principio de desarrollo

Visual Inspector se desarrollará de forma incremental.

El orden previsto será:

```text
Cargar patrón PDF
        ↓
Cargar dos muestras
        ↓
Visualizar los tres archivos
        ↓
Convertir PDF
        ↓
Preparar imágenes
        ↓
Alinear muestra 1
        ↓
Alinear muestra 2
        ↓
Comparar patrón/muestra 1
        ↓
Comparar patrón/muestra 2
        ↓
Comparar muestra 1/muestra 2
        ↓
Mostrar diferencias
        ↓
Registrar autocontrol
        ↓
Mejorar precisión
        ↓
Añadir inteligencia
```

Cada nueva fase se añadirá únicamente cuando la anterior pueda ser entendida, probada y validada.
