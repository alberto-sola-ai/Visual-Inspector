# Arquitectura de Visual Inspector

Última actualización: 22/10/2026 · Versión 0.5.0

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
 │   Pages/              Inicio, Administración                │
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
| `OrdenesFabricacion` | Código de la orden (10 cifras, único), descripción y si está activa |
| `Autocontroles` | Orden, fecha y hora (las pone el servidor), `HorasDesdeInicio` (0 = Inicial, 4, 8…), estado y observaciones. Índice único orden + horas: una orden no puede tener dos controles del mismo tipo |

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
AUTOCONTROL (Inicial, +4 h, +8 h…)
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

### Previsto para el MVP

| Tabla | Contenido |
|---|---|
| `OrdenesFabricacion` (ampliación) | Litografía (la del primer patrón cargado), estado (en fabricación / finalizada), fecha de fin y quién la finalizó |
| `Autocontroles` (ampliación) | Nombre del archivo del patrón, litografía, versión, huella SHA-256 del patrón y de cada muestra, resultado global (sin diferencias / diferencias correctas / con incidencias), quién y cuándo validó |
| `ResultadosComparacion` | Una fila por comparación (patrón ↔ muestra 1, patrón ↔ muestra 2, muestra 1 ↔ muestra 2): similitud y número de diferencias |
| `Diferencias` | Motivo (mancha, falta, sobra, desplazamiento, pliegue o borde) para agruparlas, zona en mm sobre el patrón, tamaño, en qué muestra(s) aparece, **recorte pequeño** (patrón y muestra lado a lado, unos 20-50 KB), validación (pendiente / correcta / incidencia), comentario, quién y cuándo |

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
| Sin TLS en la aplicación | Ámbito Interno WYS: lo gestiona la red de la empresa |
| No se guardan los archivos completos | Decisión de Alberto (22/10/2026): se procesan en memoria; queda el registro, la huella SHA-256 de cada archivo y un recorte pequeño de cada diferencia |
| Validación humana de cada diferencia | Decisión de Alberto (22/10/2026): la aplicación propone, la persona decide (correcta / incidencia). Las incidencias solo se registran |
| Informe de fabricación en pantalla y PDF WYS | Decisión de Alberto (22/10/2026) |
| Colores mantenidos, color no analizado | Decisión de Alberto (22/10/2026): todo se ve y se guarda en color; no se mide el tono. Por dentro, la localización de puntos puede usar una copia en gris que no se muestra |
| Diferencias agrupadas por motivo | Decisión de Alberto (22/10/2026): si hay muchas, se agrupan (manchas, faltan, sobran, desplazamientos, pliegues o bordes) y se pueden validar por grupo o una a una |

## Próximos pasos

En el orden del principio de desarrollo del MVP (cada paso, solo cuando el anterior funcione):

1. ~~Probar el esqueleto con Docker Desktop.~~ Hecho (0.1.1).
2. ~~Tablas `OrdenesFabricacion` y `Autocontroles` y pantalla para iniciar un autocontrol.~~ Hecho (0.2.0).
3. ~~Carga desde el PC del patrón PDF y de las dos muestras, cálculo de su huella, conversión
   del PDF a imagen y visualización de los tres (sin guardar archivos).~~ Hecho (0.3.0).
4. ~~Lectura de la litografía y la versión del nombre del patrón; recorte de la pieza en cada
   muestra y localización dentro del patrón (giro y corte libres).~~ Hecho (0.4.0).
5. ~~Comparaciones y lista de posibles diferencias, con su recorte.~~ Hecho (0.5.0).
6. Formulario de validación (correcta / incidencia + comentario) y registro del autocontrol.
7. Finalizar la orden e informe de fabricación (pantalla + PDF con marca WYS).
