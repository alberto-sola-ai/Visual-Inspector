# Arquitectura de Visual Inspector

Última actualización: 22/10/2026 · Versión 0.6.0

## Resumen

Visual Inspector es una aplicación web interna de WYS, construida según las normas de
desarrollo de la empresa (skill **wys-code**):

| Pieza | Tecnología |
|---|---|
| Pantallas | Blazor (renderizado en el servidor), pensadas para ordenador |
| Lógica | C# · .NET 10, en la carpeta `Servicios/` |
| Datos | MariaDB 11.4 (LTS) con Entity Framework y el conector Pomelo |
| Usuarios | ASP.NET Core Identity, contraseñas con Argon2id |
| Registros | Serilog, ficheros diarios en `log/` |
| Despliegue | Docker: la aplicación y MariaDB definidas en `docker-compose.yml` |
| Ámbito | Interno WYS: la red de la empresa gestiona el cifrado (sin TLS en la aplicación) |

## Capas

Cada capa solo habla con la de debajo. Las pantallas nunca acceden a la base de datos
directamente: siempre pasan por un servicio, que es quien comprueba los permisos.

```
 Navegador (pantalla de ordenador)
        │  HTTP 8080 (cifrado gestionado por la red de WYS)
        ▼
 ┌─────────────────────────────────────────────────────────────┐
 │ Components/  Pantallas Blazor                               │
 │   Pages/              Inicio, Autocontroles, Órdenes,       │
 │                       Administración (4 secciones)          │
 │   Account/            Entrar, Mi cuenta, verificación 2 pasos│
 │   Layout/, Compartido/ Cabecera WYS, menú, avisos de error  │
 ├─────────────────────────────────────────────────────────────┤
 │ Seguridad/   Permisos, cifrado Argon2id, registro Log-Wys-Seg,│
 │              cabeceras, comprobación de permisos             │
 ├─────────────────────────────────────────────────────────────┤
 │ Servicios/   Lógica de negocio (usuarios, parámetros,        │
 │              versiones, autocontroles; próximamente visión)  │
 ├─────────────────────────────────────────────────────────────┤
 │ Datos/       AppDbContext, entidades, trazabilidad,          │
 │              migraciones, inicialización                     │
 └─────────────────────────────────────────────────────────────┘
        │  red interna de Docker (MariaDB no se publica fuera)
        ▼
 MariaDB 11.4 (volumen datos-mariadb)
```

## Carpetas del proyecto

| Carpeta | Contenido |
|---|---|
| `Components/` | Pantallas y piezas de pantalla Blazor |
| `Datos/` | Base de datos: contexto, entidades, migraciones (`Datos/Migraciones/`) e inicialización |
| `Seguridad/` | Permisos, cifrado de contraseñas, registro de seguridad, cabeceras |
| `Servicios/` | Lógica de negocio |
| `recursos/` | Marca WYS (logo, fuentes, iconos, `tema.css`) e informe `versiones.json` |
| `wwwroot/` | Bootstrap y estilos propios (`app.css`) |
| `doc/` | Documentación técnica; `doc/master/` contiene las notas del Máster |
| `log/` | Registros (se crea sola; no se sube a GitHub) |

## Cómo se protege cada acción

1. Todas las páginas exigen haber entrado, salvo la de entrada (política por defecto).
2. Cada página exige su permiso (`[Authorize(Policy = Permisos.AutocontrolesVer_wys)]`).
3. Cada servicio vuelve a comprobar el permiso en el servidor (`ServicioAutorizacion`), aunque
   la página ya lo haya hecho. Ocultar un botón es solo comodidad.
4. Mientras una persona tenga pendiente cambiar su contraseña temporal, no tiene ningún permiso.
5. Cada acción de seguridad se apunta en `log/Log-Wys-Seg-AAAAMMDD.log`.

Detalle completo en [seguridad.md](seguridad.md).

## Modelo de datos

### Ya existe

| Tabla | Contenido |
|---|---|
| `AspNetUsers` | Usuarios (con `NombreCompleto`, `Activo`, `DebeCambiarPassword`, `UltimoAcceso` y trazabilidad) |
| `AspNetRoles`, `AspNetUserRoles` | Roles `Administrador` y `Usuario` |
| `AspNetUserClaims` | Permisos concedidos (tipo `permiso`) |
| `Parametros` | Ajustes de la aplicación (`MfaActivo`, `IaActiva`…) |
| `OrdenesFabricacion` | Código de la orden (10 cifras, único), descripción, litografía (la del primer autocontrol registrado), línea de fabricación (la del último autocontrol; parámetro `Lineas`), estado (en fabricación / finalizada), fecha de fin y quién la finalizó |
| `Autocontroles` | Orden, fecha y hora (las pone el servidor), `NumeroControl` (1 = inicial, 2, 3…; el operario decide cuándo, se recomienda cada 4 horas), estado (pendiente de archivos / registrado) y observaciones. Al registrar: archivo, litografía y versión del patrón, huellas SHA-256 de los tres archivos, tolerancia, similitudes (%), resultado (sin diferencias / diferencias correctas / con incidencias), si se confirmó una litografía distinta, quién y cuándo registró. Anulación: si está anulado, motivo, quién y cuándo. Índice único orden + número (el siguiente es el último + 1) |
| `Diferencias` | Una fila por diferencia registrada: número, motivo (mancha / falta / desplazamiento), en qué muestra(s), zona y superficie en mm, evidencia, recorte JPEG en color (~30 KB, `mediumblob`), validación (correcta / incidencia), comentario (obligatorio en incidencias) y trazabilidad (quién validó y cuándo) |

Todas las tablas propias guardan quién y cuándo creó y modificó cada registro (`CreadoPor`,
`FechaCreacion`, `ModificadoPor`, `FechaModificacion`), en hora de Madrid. Juego de caracteres
`utf8mb4` con ordenación española (`utf8mb4_uca1400_spanish_ai_ci`).

### Objetivo final del flujo (decisión de Alberto, 22/10/2026)

El resultado de cada autocontrol es un **formulario de validación**: la aplicación propone las
posibles diferencias y la persona valida cada una como **correcta** o como **incidencia a
corregir**, con un comentario. Todo queda registrado. Al **finalizar la fabricación** se saca un
**informe de fabricación** (en pantalla y en PDF con la marca WYS) con todo lo ocurrido en esa
orden: autocontroles, diferencias, validaciones e incidencias.

```
AUTOCONTROL (Control 1 (inicial), Control 2… cuando lo decide el operario; se recomienda cada 4 h)
  Cargar patrón + 2 muestras ─► comparar ─► lista de posibles diferencias
                                                │
                     FORMULARIO DE VALIDACIÓN (diferencias agrupadas por motivo)
                     recorte en color patrón/muestra · en qué muestra(s) aparece
                     [ Correcta ]  [ Incidencia a corregir ] + comentario
                                                │
                                                ▼
                          Registro en base de datos (quién, cuándo, qué)

FIN DE LA ORDEN ─► finalizar la orden ─► INFORME DE FABRICACIÓN (pantalla + PDF WYS)
```

- **Litografía y versión**: se leen del nombre del archivo del patrón, que siempre empieza con
  el mismo formato (confirmado por Alberto, 22/10/2026): **L + 7 cifras** (litografía) seguido de
  **R + cifras** (versión). Ejemplo: `L2201989R002 S_CONT_NEO SS SOLVANT 30 ML.pdf` → litografía
  `L2201989`, versión `R002`. Si el nombre no sigue el formato, la aplicación avisa y deja
  escribirlas a mano.
- **Orden de fabricación**: la escribe el usuario (10 cifras).
- **Cliente**: pendiente de decidir de dónde se obtiene (a mano o desde el ERP). Mientras tanto
  los informes se agrupan por orden y litografía.
- **Incidencias**: solo se registran; la corrección se gestiona fuera de la aplicación.
- La comparación muestra 1 ↔ muestra 2 indica si una diferencia aparece **en los dos tubos**
  (posible fallo de plancha, se repetirá) o **en uno solo** (mancha puntual).

### Finalización e informe de fabricación (0.7.0)

- Finaliza la orden quien puede hacer autocontroles (`Autocontroles.Realizar`), y solo si tiene
  al menos un autocontrol registrado (desde la 0.14.0 puede haber otros sin registrar: no se sabe
  cuántos se harán en una orden). Después no admite autocontroles nuevos ni registrar los que
  quedaron pendientes.
- **Cierre automático (0.14.0):** una línea fabrica una orden a la vez. Al crear un autocontrol de
  una orden en la línea L1, las demás órdenes en fabricación de L1 que ya tienen algún autocontrol
  registrado se finalizan solas (`FinalizadaPor` = «nombre (cierre automático)»). La pantalla de
  «Nuevo autocontrol» avisa antes. Cada cierre se apunta en `Log-Wys-Seg` (`CierreAutomaticoOrden`).
- Solo el administrador (`Administracion`) puede **reabrirla** (0.8.0): vuelve a «En
  fabricación», se borran la fecha de fin y quién la finalizó, y el informe vuelve a ser
  provisional. Quién y cuándo la reabrió queda en `ModificadoPor`/`FechaModificacion` y en
  `Log-Wys-Seg`. No se guarda motivo ni historial de reaperturas.
- El administrador puede **anular** un autocontrol (0.9.0): queda en la orden con su motivo,
  pero no cuenta en las cifras, al finalizar ni en el informe (donde sale en gris con el
  motivo). O puede **eliminarlo**: se borra con sus diferencias. Desde la 0.10.0 las dos
  acciones se pueden hacer también con la orden finalizada, sin reabrirla, y el informe se
  actualiza solo. Las dos se apuntan en `Log-Wys-Seg`.
- El informe se puede ver antes de finalizar, marcado como **PROVISIONAL**.
- Pantalla: `/ordenes/{id}/informe`. PDF: `/ordenes/{id}/informe.pdf`, que exige
  `Autocontroles.Ver` y vuelve a comprobarlo en el servicio.
- El PDF lo genera `Servicios/Informes/GeneradorInformePdf.cs` con PDFsharp-MigraDoc 6.2.4 (MIT),
  con el logo y las fuentes Lato de `recursos/`. Comprobado en Windows y en Linux (Docker).

### Trazabilidad: cuadro de mando de las órdenes cerradas (0.14.0)

- Menú **Trazabilidad** (`/trazabilidad`): listado de órdenes finalizadas con búsqueda por orden y
  litografía. Al elegir una se abre su cuadro de mando (`/trazabilidad/{id}`). Exige
  `Autocontroles.Ver` (de momento no hay permiso propio, decisión de Alberto, 26/10/2026).
- Todo se calcula en `Servicios/ServicioTrazabilidad.cs` con lo que ya se guarda; no hay tablas
  nuevas. Los autocontroles anulados no cuentan.

| Cifra | Cómo se calcula |
|---|---|
| Autocontroles completados (6/7) | registrados entre los iniciados (los iniciados que quedaron sin registrar cuentan como no completados) |
| Muestras escaneadas (12/14) | muestras con huella guardada entre 2 por cada autocontrol iniciado |
| Incidencias detectadas | diferencias que el operario marcó como incidencia |
| Correcciones verificadas | incidencia que, en el siguiente autocontrol registrado de la orden, ya no se repite con el mismo motivo (decisión de Alberto, 26/10/2026); las del último control no se pueden comprobar |
| Eficacia de corrección | correcciones verificadas entre incidencias |
| Integridad de autocontroles | registrados con patrón, versión, litografía, tres huellas, quién y cuándo, entre registrados (no vuelve a abrir los archivos: no se guardan) |

### Detección, decisión e impacto (0.15.0)

Cada diferencia se mide en tres cosas independientes:

| Dimensión | Quién | Dónde se guarda |
|---|---|---|
| Detección: qué encontró el sistema | La aplicación | `Motivo`, medidas, evidencia, muestra(s) |
| Decisión: cómo la clasificó el operario | El operario | `Validacion` (Correcta / Incidencia) y `Comentario` |
| Impacto: qué se hizo en producción | El operario, solo en incidencias | `Actuaciones` (varias a la vez, como texto: «ParadaLinea, RetencionProducto») |

- Actuaciones: sin intervención, ajuste en marcha, parada de línea, retención de producto y aviso
  a Calidad. No es una escala de gravedad: se pueden combinar. «Sin intervención» no se combina
  con las demás. Es obligatorio elegir al menos una en cada incidencia (comprobado en el servidor).
- Decisión de Alberto (26/10/2026): el operario es autónomo (hay turnos sin responsable de
  Calidad), así que **no hay** criticidad automática, flujo de autorización de Calidad, zonas
  críticas sobre el patrón ni producto regulado. «Aviso a Calidad» solo deja constancia.
- Las incidencias anteriores a la 0.15.0 tienen la columna vacía y se muestran «Sin clasificar».
- **Cifras de impacto (0.16.0):** con «Parada de línea» el operario apunta los minutos
  (`MinutosParada`) y con «Retención de producto» los tubos (`TubosRetenidos`), aproximados. Se
  pueden corregir después (`ServicioAutocontroles.CorregirImpactoAsync`): es la única excepción a
  «un autocontrol registrado no se cambia». Queda quién y cuándo en `ModificadoPor` y, en
  `Log-Wys-Seg`, el valor anterior y el nuevo (`CorreccionImpacto`).
- **Cuadro de impacto productivo** (`/impacto`, `Servicios/ServicioImpacto.cs`, permiso
  `Autocontroles.Ver`): suma minutos y tubos de las incidencias de autocontroles registrados y no
  anulados, por fecha del control y, si se elige, por línea. Rankings por orden, línea, litografía y
  defecto, ordenados por minutos, luego tubos y luego número de incidencias. No hay una «puntuación
  única»: minutos y tubos se muestran por separado para no mezclarlos con pesos inventados.
  Pendiente para otra fase: análisis por cliente. No se tiene en cuenta la posición de impresión.
- El cuadro de trazabilidad cuenta las incidencias por actuación y calcula la **producción a
  revisar** si hubo retención: desde el último autocontrol registrado sin incidencias (o el primero
  de la orden) hasta el control con retención; si varios controles seguidos retienen sin uno limpio
  en medio, forman un solo tramo.

**Los archivos completos no se guardan** (decisión de Alberto, 22/10/2026): el patrón y las
muestras se cargan desde el PC, se procesan en memoria y se descartan al terminar. Solo se guarda
el registro, la huella SHA-256 de cada archivo (para demostrar después cuál se usó) y un recorte
pequeño de cada diferencia como evidencia para el informe.

Si el patrón de un autocontrol tiene una litografía distinta de la de su orden, la aplicación
avisa antes de comparar (posible patrón equivocado).

## Motor de visión (previsto)

Irá en `Servicios/Vision/`, separado de las pantallas, para poder probarlo por su cuenta.

### Condiciones de partida (ejemplos reales del 22/10/2026)

| Condición | Consecuencia |
|---|---|
| Los patrones vienen de **varios proveedores**; el PDF es una hoja de aprobación (BAT) con cajetín, cotas, logos y guías de montaje alrededor y encima del diseño | No se puede recortar el tubo con medidas fijas. Solo cuenta la imagen del tubo |
| Patrón y muestras a **300 ppp** | El tubo aparece a la misma escala en los dos: la alineación solo busca posición y giro, no tamaño |
| La muestra se escanea en **cualquier orientación** y ocupa una parte pequeña del escaneo | Hay que localizar la pieza en el escaneo y probar cualquier giro |
| El tubo se corta para abrirlo por **cualquier punto** del contorno | El diseño puede llegar partido en dos trozos que hay que alinear por separado |
| Pliegues, bordes rotos, brillo del aluminio, polvo en el cristal del escáner | No deben salir como defecto |
| **Se mantienen los colores, pero no se analiza el color** | Patrón, muestras, vistas y recortes siempre en color. Se buscan diferencias de contenido (manchas, textos, elementos que faltan o sobran, desplazamientos), no de tono |

### Flujo

```
 Patrón PDF ─► página entera a 300 ppp (en color)
                                   │
 Muestra ─► recortar la pieza del fondo del escáner (en color)
                                   │
                                   ▼
        Localizar la muestra DENTRO de la página del patrón:
        puntos característicos + giro libre (el cajetín, cotas y logos no encajan y se descartan).
        Si el corte deja el diseño en dos trozos, se localiza cada trozo por separado.
                                   │
                                   ▼
        Ajuste fino de la alineación ─► diferencias de forma
        (se ignoran bordes, pliegues y las líneas finas de guía del patrón que no se imprimen)
                                   │
                                   ▼
        Mapa de diferencias + similitud (%) ─► se muestra en pantalla y se registra el resultado
```

| Pieza | Tecnología | Estado |
|---|---|---|
| PDF a imagen | PDFtoImage 5.4 (PDFium, MIT; renderiza a 300 ppp en color) | Comprobado en Docker (Linux) y Windows |
| Localizar, alinear y comparar | OpenCvSharp 4.13 (OpenCV, Apache 2.0). Localización: puntos SIFT a 150 ppp + RANSAC con giro libre y escala ≈ 1; segundo trozo con las coincidencias sobrantes | Comprobado en Docker (Linux, 16 hilos) y Windows (1 hilo: con varios se cierra de golpe en el equipo de desarrollo). La imagen de .NET 10 es Ubuntu 24.04 y necesita unas bibliotecas del sistema (ver `Dockerfile`) |

**Limitación conocida (0.4.0):** las zonas del tubo **sin imprimir y en el borde de la pieza** (por
ejemplo la franja blanca inferior con la marca negra) tienen el mismo blanco que el fondo del
escáner y pueden quedar fuera del recorte. Una mancha en esa zona sí se detecta, porque se aparta
del color del fondo.

### Cómo se compara (0.5.0)

1. **Ajuste fino**: tras la localización, en zonas de ~1,4 cm se mide cuánto está corrida la muestra
   (como mucho 1 mm) y se endereza suavemente (curvatura y estiramientos de la pieza aplanada).
2. **Tono por tinta**: los colores del patrón se agrupan en 8 "tintas" y se corrige la luminosidad
   de la muestra tinta por tinta (mediana). Así un negro o un rojo algo distintos no cuentan: no se
   analiza el color.
3. **Diferencias con margen**: "de más" si la muestra es más oscura que todo lo que hay alrededor
   en el patrón; "de menos" si el patrón es más oscuro que todo lo que hay alrededor en la muestra
   (o la muestra más clara). El margen depende de la tolerancia.
4. **Descartes**: guías de montaje y marcos del patrón (líneas finas, rectas y de más de 12 mm),
   pliegues (menos de 0,5 mm de grueso y más de 4 mm de largo) y todo lo que toque el borde de la
   pieza (franja de 2,5 mm).
5. **Motivo**: desplazamiento o texto doble si lo de más está pegado a un texto del patrón (o hay
   de más y de menos juntos); mancha si es de más; falta si es de menos.
6. **Las dos muestras**: si una diferencia sale en el mismo sitio (±1 mm) en las dos, es una sola,
   "en las dos muestras" (posible fallo de plancha).
7. **Evidencia** = superficie × contraste (×1,5 si sale en las dos), para ordenarlas.

| Tolerancia | Superficie mínima | Margen de desajuste | Contraste mínimo |
|---|---|---|---|
| Estricta | 0,3 mm² | 2 px (0,17 mm) | 22 |
| Normal | 1 mm² | 4 px (0,34 mm) | 24 |
| Amplia | 3 mm² | 7 px (0,6 mm) | 40 |

Con los ejemplos reales (22/10/2026), la tolerancia Normal encuentra las manchas de las dos
muestras y el texto doble de la muestra 2, con 3 avisos dudosos (bordes entre colores impresos con
la trama más marcada). Ajustada solo con dos muestras de un cliente: conviene revisarla con más
ejemplos. Los mismos archivos dan siempre el mismo resultado (semilla fija).

Los porcentajes de similitud son solo indicadores técnicos: no son criterios de aceptación ni de
rechazo.

## Decisiones y excepciones

| Decisión | Motivo |
|---|---|
| C# / Blazor en lugar de Python | Normas WYS (decisión de Alberto, 21/10/2026). La visión artificial se hace con OpenCvSharp |
| Entity Framework 9 + Pomelo 9 sobre .NET 10 | **Excepción temporal**: Pomelo aún no tiene versión para Entity Framework 10. Se actualizará cuando salga |
| Sin TLS en la aplicación | Ámbito Interno WYS: lo gestiona la red de la empresa. En producción, Nginx Proxy Manager pone el dominio y el certificado; la aplicación solo se fía de sus cabeceras (Seguridad/ConfiguracionProxy.cs) |
| No se guardan los archivos completos | Decisión de Alberto (22/10/2026): se procesan en memoria; queda el registro, la huella SHA-256 de cada archivo y un recorte pequeño de cada diferencia |
| Validación humana de cada diferencia | Decisión de Alberto (22/10/2026): la aplicación propone, la persona decide (correcta / incidencia). Las incidencias solo se registran |
| Informe de fabricación en pantalla y PDF WYS | Decisión de Alberto (22/10/2026). PDF con PDFsharp-MigraDoc 6.2.4, la última con licencia MIT: no actualizar (desde la 6.5.0 es AGPL) |
| Finalizar la orden | Decisión de Alberto (22/10/2026): la finaliza quien hace autocontroles, nunca con autocontroles sin registrar; antes, el informe es provisional |
| Reabrir la orden | Decisión de Alberto (22/10/2026): solo el administrador, sin motivo ni tabla de historial de momento |
| Anular o eliminar autocontroles | Decisión de Alberto (22/10/2026): solo el administrador. Anular deja rastro con motivo; eliminar lo borra de la orden y del informe, y solo queda apuntado en el diario de seguridad |
| Colores mantenidos, color no analizado | Decisión de Alberto (22/10/2026): todo se ve y se guarda en color; no se mide el tono. Por dentro, la localización de puntos puede usar una copia en gris que no se muestra |
| Diferencias agrupadas por motivo | Decisión de Alberto (22/10/2026): si hay muchas, se agrupan (manchas, faltan, sobran, desplazamientos, pliegues o bordes) y se pueden validar por grupo o una a una |
| Sin carpetas por defecto | Probado en la 0.11.0 y quitado en la 0.11.5 (decisión de Alberto, 23/10/2026): el navegador no deja abrir «Elegir archivo» en una carpeta indicada por la aplicación. En su lugar, cada botón de «Elegir archivo» recuerda su carpeta (una para patrones y otra para muestras; la primera vez, «Mis documentos»), en Edge y Chrome con https o en el propio equipo (`wwwroot/js/elegir-archivo.js`) |
| Sin borrado de registros | Propuesto por Alberto y descartado (23/10/2026): borrar el diario de seguridad permitiría borrar el rastro; los diarios se borran solos (errores a los 30 días, seguridad a los 90) |
| Línea de fabricación en la orden | Decisión de Alberto (23/10/2026): obligatoria al crear cada autocontrol, viene elegida la del último y la orden se queda con ella. Lista de líneas en el parámetro `Lineas`. Para los futuros KPI de eficiencia operativa |
| Registros desde la aplicación | Decisión de Alberto (23/10/2026): el administrador elige el diario (errores o seguridad) y el día (`Servicios/ServicioRegistros.cs`). Solo lectura; como mucho los 1.000 apuntes más recientes; cada consulta se apunta en `Log-Wys-Seg` |

## Próximos pasos

En el orden del principio de desarrollo del MVP (cada paso, solo cuando el anterior funcione):

1. ~~Probar el esqueleto con Docker Desktop.~~ Hecho (0.1.1).
2. ~~Tablas `OrdenesFabricacion` y `Autocontroles` y pantalla para iniciar un autocontrol.~~ Hecho (0.2.0).
3. ~~Carga desde el PC del patrón PDF y de las dos muestras, cálculo de su huella, conversión
   del PDF a imagen y visualización de los tres (sin guardar archivos).~~ Hecho (0.3.0).
4. ~~Lectura de la litografía y la versión del nombre del patrón; recorte de la pieza en cada
   muestra y localización dentro del patrón (giro y corte libres).~~ Hecho (0.4.0).
5. ~~Comparaciones y lista de posibles diferencias, con su recorte.~~ Hecho (0.5.0).
6. ~~Formulario de validación (correcta / incidencia + comentario) y registro del autocontrol.~~ Hecho (0.6.0).
7. ~~Finalizar la orden e informe de fabricación (pantalla + PDF con marca WYS).~~ Hecho (0.7.0).
