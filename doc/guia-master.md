# Guía del proyecto Visual Inspector para el Máster de IA

Documento de apoyo para el Máster de Inteligencia Artificial. Recoge cómo se ha construido
Visual Inspector desde el primer día, con una IA como copiloto de programación, y sirve de base
para pasarlo a un documento formal (memoria del TFM, presentación…).

Periodo descrito: del 19/10/2026 (primer commit) al 30/10/2026 (versión 0.20.2). Cada tema tiene además su capítulo en [[03-preparacion-entorno]], [[04-primer-prototipo]], [[05-integracion-ia]], [[06-base-de-datos]], [[07-testing]], [[08-mejoras]] y [[09-conclusiones]].

---

## 1. Ficha rápida

| Concepto | Valor |
|---|---|
| Nombre | Visual Inspector |
| Qué hace | Ayuda al autocontrol visual de la impresión de tubos de aluminio: compara el patrón gráfico aprobado por el cliente (PDF) con las dos muestras escaneadas, señala las diferencias, el operario las valida y queda todo registrado con un informe de fabricación en PDF |
| Empresa | Witte y Solà, S.A. (WYS) — fabricación de tubos de aluminio |
| Ámbito | Aplicación interna de la red de WYS |
| Tecnología | C# · .NET 10 · Blazor Server · MariaDB 11.4 · Docker · OpenCV |
| Copiloto de IA | Claude Code (Anthropic) dentro de Visual Studio Code |
| Organización del conocimiento | Bóveda de Obsidian «MasterIA» |
| Versiones | De la 0.1.1 a la 0.20.2. 21 commits en GitHub (hasta la 0.19.20; la serie 0.20 aún no está subida). El repositorio es público |
| Tamaño | 46 archivos C# (≈7.000 líneas), 57 pantallas Razor (≈5.700 líneas), 11 migraciones de base de datos |
| Pruebas | 125 pruebas automáticas (seguridad, reglas de negocio y exportación), todas correctas |

---

## 2. El problema y la idea

En la fábrica, durante la impresión se sacan **dos tubos** de la misma plancha, se cortan, se
abren en plano y se escanean. Antes, para compararlos con el diseño aprobado por el cliente se
superponían imágenes a mano (por ejemplo en PowerPoint). Esto tenía inconvenientes:

- dependía del ojo de cada persona;
- era difícil alinear exactamente patrón y muestra;
- no había un criterio común para decidir qué es una diferencia;
- costaba tiempo y quedaba poco documentado.

Visual Inspector convierte ese proceso manual en uno **digital, repetible y registrado**. No
sustituye a los sistemas industriales de visión artificial ni libera producto por sí solo: es
una **herramienta de asistencia** al autocontrol.

La idea original está en [master/01-idea-y-objetivo.md](master/01-idea-y-objetivo.md).

---

## 3. El MVP como proyecto inicial

### 3.1 Qué es el MVP

El MVP (*Minimum Viable Product*, producto mínimo viable) es la primera versión que demuestra
que la idea funciona con lo mínimo imprescindible. Para Visual Inspector se definió así:

> Comparar automáticamente las dos muestras del autocontrol contra el patrón aprobado por el
> cliente, facilitando la detección de desviaciones visuales y dejando registro del resultado.

La definición completa está en [master/02-definicion-mvp.md](master/02-definicion-mvp.md).

### 3.2 Los 7 pasos del MVP

Se avanzó **un paso cada vez**, y no se empezaba el siguiente hasta comprobar que el anterior
funcionaba (con datos reales y en Docker):

| Paso | Contenido | Versión |
|---|---|---|
| 0 | Esqueleto según las normas WYS: usuarios, permisos, administración, registros, Docker | 0.1.1 |
| 1 | Órdenes de fabricación y autocontroles | 0.2.1 |
| 2-3 | Carga del patrón PDF y de las dos muestras, huella SHA-256, vista en color | 0.3.0 |
| 4 | Lectura de litografía y versión del nombre del patrón; localización de cada muestra dentro del patrón | 0.4.0 |
| 5 | Comparación y lista de posibles diferencias con su recorte | 0.5.0 |
| 6 | Formulario de validación (correcta / incidencia + comentario) y registro | 0.6.0 |
| 7 | Finalizar la orden e informe de fabricación (pantalla y PDF con la marca WYS) | 0.7.0 |

### 3.3 Después del MVP: mejoras a partir del uso real

Con el MVP terminado, Alberto fue probando la aplicación y pidiendo ajustes. Los más importantes:

| Versión | Mejora |
|---|---|
| 0.8.0 – 0.10.0 | Reabrir órdenes, anular o eliminar autocontroles erróneos (solo administrador), controles numerados en lugar de horas fijas (el operario decide cuándo; se recomienda cada 4 h) |
| 0.10.1 – 0.10.6 | Botón «ojo» de la contraseña, logo propio, fondos, reloj con turno, versión en el pie |
| 0.10.7 | Pruebas automáticas de seguridad y corrección de los dos avisos que encontraron |
| 0.10.8 – 0.11.5 | Menús desplegables, lector de registros para el administrador, «Elegir archivo» que recuerda la carpeta |
| 0.11.6 – 0.12.2 | Línea de fabricación (para futuros KPI), órdenes abiertas como sugerencia, inicio «10000» ya escrito en el campo de la orden |
| 0.12.3 – 0.12.5 | Correcciones del acceso con Chrome y preparación para publicar detrás de Nginx Proxy Manager |
| 0.13.0 – 0.13.4 | Modo diurno y nocturno |
| 0.14.0 – 0.14.1 | Trazabilidad de las órdenes cerradas y cierre automático por línea |
| 0.15.0 – 0.16.0 | Qué se hizo con cada incidencia, minutos de parada y tubos retenidos, e impacto productivo |
| 0.16.1 – 0.16.7 | Menú Calidad, sin pantalla de inicio y conmutador de modo rediseñado |
| 0.17.0 | Limpieza de datos de prueba para el administrador |
| 0.18.0 – 0.18.6 | Guía de usuario y ayuda dentro de la aplicación, aviso al cambiar de tolerancia y logo nuevo |
| 0.19.0 – 0.19.20 | Una sola orden abierta por línea con aceptación, alertas de las cuatro horas, «RPI» en lugar de «Control» y ajustes de diseño |
| 0.20.0 – 0.20.2 | Exportación a Excel y CSV de trazabilidad e impacto, y el icono de la aplicación sin caché |

El detalle de cada versión está en [cambios.md](cambios.md), y las mejoras explicadas por temas en [[08-mejoras]].

### 3.4 Cronología real

| Fecha | Hito |
|---|---|
| 19/10/2026 | Repositorio en GitHub y documentación inicial (idea y MVP) |
| 21/10/2026 | Esqueleto WYS en C# (0.1.1), órdenes y autocontroles (0.2.1) |
| 22/10/2026 | Visión artificial: carga, localización, comparación, validación e informe (0.4.0 → 0.10.1) |
| 23/10/2026 | Diseño, pruebas de seguridad, administración, línea de fabricación (0.10.2 → 0.12.2) |
| 26/10/2026 | Trazabilidad, impacto productivo, modo nocturno, limpieza de datos y ayuda (0.12.3 → 0.18.4) |
| 30/10/2026 | Una sola orden por línea, alertas, exportación a Excel y CSV y ajustes de diseño (0.18.5 → 0.20.2) |

---

## 4. Claude como copiloto en Visual Studio Code

### 4.1 Qué es Claude Code

**Claude Code** es el asistente de programación de Anthropic. Se usó como **extensión de Visual
Studio Code** (`anthropic.claude-code`), con el modelo **Claude Opus**. No es un simple
autocompletado: es un **agente** que, dentro del proyecto:

- lee y entiende los archivos del proyecto;
- escribe y modifica código, configuración y documentación;
- ejecuta comandos en la terminal (compilar, arrancar Docker, consultar la base de datos, Git…);
- comprueba el resultado (compila, prueba, hace capturas de pantalla) y corrige si algo falla;
- explica lo que hace en lenguaje sencillo y pregunta cuando hay que decidir algo.

### 4.2 Reparto de papeles

| La persona (Alberto) | La IA (Claude) |
|---|---|
| Conoce el negocio: cómo es el autocontrol, qué es una litografía, qué es una línea | Traduce eso en pantallas, servicios y tablas |
| Decide (qué se hace, qué se descarta, qué nombres usar) | Propone opciones con su recomendación y sus pros y contras |
| Aporta material real: patrón PDF, muestras escaneadas, logos, fotos | Prepara ese material (recortes, imágenes optimizadas) y lo prueba |
| Prueba en pantalla y da su visto bueno | Compila, prueba en Docker, hace capturas y pruebas automáticas |
| Sube a GitHub cuando lo decide | Prepara el commit, revisa que no haya contraseñas y lo sube solo cuando se le pide |

La regla de fondo: **la IA propone y ejecuta; la persona decide**.

### 4.3 Herramientas que usa el agente

| Herramienta | Para qué se usó |
|---|---|
| Lectura, búsqueda y edición de archivos | Todo el código y la documentación |
| Terminal (PowerShell y Bash) | `dotnet build`, `dotnet test`, `dotnet ef migrations`, `docker compose`, consultas a MariaDB, `git` |
| Navegador Edge en modo invisible (*headless*) | Capturas de pantalla para revisar el diseño sin intervención humana |
| Proyectos de prueba temporales | Probar piezas sueltas con datos reales (por ejemplo, generar el PDF del informe con recortes reales y verlo como imagen) |
| Preguntas al usuario | Decisiones de negocio (tolerancias, permisos, qué hacer con cada caso) |
| Modo plan | Antes del esqueleto, un plan completo para aprobarlo antes de escribir código |

### 4.4 Memoria del proyecto: cómo «recuerda» la IA

La IA no recuerda nada entre conversaciones por sí sola. Se le dio memoria de tres formas:

1. **`CLAUDE.md` de la bóveda** (raíz de Obsidian): reglas de cómo organizar las notas.
2. **`CLAUDE.md` del proyecto**: normas WYS, ámbito interno, tecnología, excepciones
   (Entity Framework 9 mientras no exista Pomelo 10), que Git se gestiona a mano y que cada
   biblioteca nueva se documenta con su licencia. Se lee al empezar cada conversación.
3. **Memoria automática**: notas cortas que la IA guarda cuando aprende algo sobre cómo trabajar
   con el usuario. En este proyecto guardó tres:
   - responder siempre en español;
   - no poner ejemplos dentro de los campos de un formulario, sino explicar el formato debajo
     (aprendido de un error real: un número de ejemplo parecía una orden ya escrita);
   - las normas WYS dirigen el proyecto y la identidad visual WYS se aplica a todo diseño.

### 4.5 Skills: una orquestadora y sub-skills

Una *skill* es un paquete de instrucciones que la IA carga cuando la tarea lo necesita. Las
normas de desarrollo de WYS se organizaron como **una skill orquestadora** (`wys-code`) y varias
**sub-skills** especializadas (nombres, C#/.NET, base de datos, seguridad, formatos y errores,
Docker e identidad visual).

La orquestadora decide, en cada petición, qué sub-skills hacen falta y obliga a enseñar una
**lista de comprobación** antes de escribir código. Así todo el proyecto sigue las mismas reglas
sin tener que repetirlas en cada conversación.

### 4.6 El ciclo de trabajo de cada cambio

Cada petición seguía siempre el mismo camino, marcado por la skill orquestadora:

```
Petición del usuario
   → Lista de comprobación WYS (qué normas aplican) + plan + preguntas
   → Confirmación del usuario
   → Código (comentado en castellano)
   → Compilar sin errores ni avisos
   → Arrancar en Docker y aplicar migraciones
   → Comprobar: capturas de pantalla, datos reales, pruebas automáticas
   → Anotar en doc/cambios.md (y el resto de doc/ si toca)
   → Verificación final WYS + Revisión de seguridad WYS
   → Subir a GitHub solo cuando el usuario lo pide
```

Ejemplo de lista de comprobación tal como la enseñaba la IA:

```
NORMAS WYS · Lista de comprobación
Modo: Cambio · Ámbito: Interno WYS
[x] wys-code · [x] wys-nombres · [x] wys-dotnet · [x] wys-mariadb (campo nuevo y migración)
[x] wys-seguridad · [x] wys-identidad
[-] wys-docker → no aplica · [-] wys-github → desactivada
Plan: 1. … 2. … 3. …
¿Continuamos?
```

### 4.7 El control humano

- **Git a mano**: la IA no hace commits ni sube nada si no se le pide.
- **Permisos explícitos**: para hacer capturas con la sesión iniciada, la IA pedía permiso para
  crear un usuario temporal, y lo borraba al terminar.
- **Confirmaciones**: antes de borrar o de cambios difíciles de deshacer.
- **Transparencia**: si algo no se podía probar (por ejemplo, un clic en pantalla), lo decía
  como «pendiente: lo pruebas tú».

---

## 5. Prompting con IA

### 5.1 Cómo se habló con la IA

Toda la conversación fue en **castellano y en lenguaje natural**, sin comandos especiales. Los
mensajes eran cortos y del día a día de la fábrica. La IA preguntaba lo que faltaba.

### 5.2 Ejemplos reales de prompts

| Prompt del usuario (literal o casi) | Qué consiguió |
|---|---|
| «A partir de ahora y siempre me hablas en español» | Se guardó en la memoria permanente |
| «Ahora lo que pretendo es…» + envío del patrón PDF y dos muestras escaneadas reales | La comparación se ajustó con datos reales, no inventados |
| «Agrupar por motivo… Mantén los colores. No lo pases a escala de grises. Lo único que digo es que no analices el color» | Un matiz de negocio clave: se ve en color, pero no se mide el tono |
| «Más que sensibilidad lo llamaría Tolerancia, el usuario también puede modificar la tolerancia…» | Cambió el concepto y la pantalla (Estricta / Normal / Amplia) |
| «Los pliegues y bordes no los tengas en cuenta» | La comparación descarta pliegues y bordes de la pieza |
| «El tema es que como ejemplo en el campo pones la misma orden… define debajo del campo qué formato tiene» | Corrección de diseño que la IA guardó como regla para siempre |
| «La norma de cada 4 horas no debe influir… los autocontroles son cuando considera el operario» | Se rediseñó la numeración de controles y se convirtieron los datos existentes |
| «Si existen autocontroles de una OF abierta… deberían salir las OF abiertas como sugerencia» | Lista de órdenes abiertas con un clic |
| «Corrígelo e inclúyelo en el informe» | Corrección de los avisos de seguridad y actualización del informe |
| «¿Cómo lo ves?» | La IA da su opinión razonada antes de tocar nada |

### 5.3 Patrones que funcionaron

1. **Dar contexto de negocio**, no instrucciones técnicas: «el operario», «la orden», «la línea».
2. **Aportar material real**: archivos de muestra, logos, fotos. Así la IA prueba con lo de verdad.
3. **Avanzar en pasos pequeños** y probar cada uno antes del siguiente.
4. **Pedir la lista y el plan antes del código** («enséñame la lista del paso 7»).
5. **Corregir con precisión** cuando algo no es lo que se quería, y explicar por qué.
6. **Preguntar la opinión** («¿cómo lo ves?», «¿se puede hacer?») antes de decidir.
7. **Añadir matices sobre la marcha**: se pueden mandar mensajes mientras la IA trabaja
   («y también en el menú de Autocontroles») y los incorpora.

### 5.4 Malentendidos y cómo se resolvieron

Fueron muy instructivos, porque muestran los límites de comunicarse con una IA:

| Qué se pidió | Qué entendió la IA | Cómo se resolvió |
|---|---|---|
| Carpetas por defecto para patrones y muestras | Carpetas que la aplicación lee desde el servidor | El usuario aclaró que quería que el botón «Elegir archivo» se abriera en esa carpeta. La IA explicó que **el navegador no lo permite por seguridad**, propuso la mejor alternativa (cada botón recuerda su carpeta) y se quitó lo que no tenía sentido |
| Inicio «10000» en la orden | Un inicio fijo que no se podía tocar | «No, lo que quiero es que el 10000 esté por defecto pero sea editable»: se cambió a un campo ya relleno |
| «No veo el botón desactivar» | — | La IA revisó el código y explicó que estaba dentro de la ficha del usuario, no en el listado |
| Menú junto al logo | Ambigüedad «a la derecha… junto al logo» | La IA dibujó un esquema en texto y preguntó antes de mover nada |

Lección: **cuando hay ambigüedad, un dibujo en texto o una tabla de opciones ahorra mucho
trabajo**. La IA lo hacía antes de programar.

### 5.5 Plantilla de prompt recomendada (aprendida del proyecto)

```
Contexto: [qué pasa hoy en la fábrica / quién lo usa]
Quiero: [el resultado que espero ver]
Condiciones: [lo que no se debe cambiar, reglas del negocio]
Material: [archivos, imágenes, ejemplos reales]
Antes de programar: enséñame el plan y pregúntame lo que no esté claro.
```

---

## 6. Integración con Obsidian

### 6.1 La bóveda como «segundo cerebro»

Todo el trabajo vive dentro de una **bóveda de Obsidian** llamada `MasterIA`, sincronizada con
OneDrive:

```
MasterIA/                         ← bóveda de Obsidian
├── CLAUDE.md                     ← reglas para la IA sobre cómo organizar notas
├── AGENTS.md                     ← las mismas reglas para otros asistentes (Codex)
├── notas-diarias/                ← una nota por día
├── proyectos/
│   ├── MASTER IA.md              ← nota del máster
│   ├── ai-project-base/          ← ESTE PROYECTO (código + doc/)
│   └── …                         ← otros cursos y proyectos
└── .obsidian/                    ← configuración de Obsidian (plugins, apariencia)
```

### 6.2 Qué aporta Obsidian

- **El código y la documentación conviven** con las notas del máster: los archivos `.md` de
  `doc/` se leen y enlazan directamente desde Obsidian.
- **Reglas de escritura comunes** (`CLAUDE.md` de la bóveda): Markdown, títulos claros, enlaces
  internos `[[Nota]]`, una carpeta para cada tipo de nota y nada de carpetas innecesarias. La IA
  las respeta al crear notas.
- **Notas del máster dentro del proyecto**: `doc/master/` guarda la idea, la definición del MVP
  y un capítulo por cada tema (preparación del entorno, prototipo, IA, base de datos, testing, mejoras y
  conclusiones), que desarrollan esta guía.
- **Plugin Local REST API** (`obsidian-local-rest-api`): permite que herramientas externas lean
  y escriban notas de la bóveda.
- **Skills de Obsidian para la IA**, para que escriba notas con la sintaxis propia de Obsidian.
- **Separación con Git**: el estado de la ventana de Obsidian (`.obsidian/workspace.json`) no se
  sube a GitHub, porque cambia constantemente.

---

## 7. Lenguajes, tecnologías, librerías y plugins

### 7.1 Por qué C# y no Python

La idea inicial del máster era hacerlo en Python. Al aplicar las normas de desarrollo de WYS se
decidió (21/10/2026) usar el **estándar de la empresa**: C# con .NET y Blazor, para que la
aplicación pueda mantenerla el equipo de IT. La visión artificial se hace igualmente con
**OpenCV**, a través de OpenCvSharp.

### 7.2 Lenguajes usados

| Lenguaje | Dónde |
|---|---|
| C# | Toda la lógica: servicios, seguridad, visión artificial, base de datos, informe PDF |
| Razor (HTML + C#) | Las pantallas de Blazor |
| CSS | Estilos propios sobre el tema corporativo WYS |
| JavaScript | Siete scripts pequeños propios: ojo de la contraseña, reloj con turno, menús desplegables, «Elegir archivo» que recuerda la carpeta, modo diurno y nocturno, panel de ayuda arrastrable y descarga de archivos exportados |
| SQL | Lo genera Entity Framework; a mano solo en conversiones de datos dentro de las migraciones |
| Dockerfile y YAML | Empaquetado y arranque con Docker |
| PowerShell | Script que genera el informe de versiones y licencias (`generar-versiones.ps1`) |
| Markdown | Toda la documentación (`doc/`) y las notas de Obsidian |
| JSON | Configuración (`appsettings.json`) e informe de versiones |

### 7.3 Plataforma

| Pieza | Versión | Papel |
|---|---|---|
| .NET | 10 | Plataforma de la aplicación |
| Blazor Server | .NET 10 | Pantallas interactivas que se ejecutan en el servidor |
| ASP.NET Core Identity | 9 | Usuarios, roles y permisos |
| Entity Framework Core + Pomelo | 9 | Acceso a MariaDB sin escribir SQL a mano. **Excepción temporal**: versión 9 porque Pomelo aún no tiene la 10 |
| MariaDB Server | 11.4 (LTS) | Base de datos |

### 7.4 Librerías (todas gratuitas)

| Librería | Para qué | Licencia |
|---|---|---|
| OpenCvSharp4 (+ partes nativas Windows y Linux) | Visión artificial: recortar la pieza, localizarla en el patrón y comparar | Apache 2.0 |
| PDFtoImage | Convertir el patrón PDF en imagen a 300 ppp, en color (usa PDFium, el lector de PDF de Chrome) | MIT |
| PDFsharp-MigraDoc 6.2.4 | Generar el informe de fabricación en PDF con la marca WYS | MIT |
| Konscious.Security.Cryptography.Argon2 | Cifrar contraseñas con Argon2id | MIT |
| Pomelo.EntityFrameworkCore.MySql | Conectar con MariaDB | MIT |
| Microsoft.EntityFrameworkCore.* / Identity | Base de datos y usuarios | MIT |
| Serilog (+ Expressions) | Diarios de errores y de seguridad en `log/` | Apache 2.0 |
| MiniExcel 1.46.0 | Escribir los archivos de Excel de las exportaciones | Apache 2.0 |
| Markdig | Mostrar la guía de usuario (Markdown) en la pantalla de ayuda, sin HTML en bruto | BSD-2-Clause |
| System.Security.Cryptography.Pkcs 10.0.13 | Forzada a una versión corregida (la que traía PDFsharp tenía fallos de seguridad conocidos) | MIT |
| xUnit + ASP.NET TestHost + EF InMemory | Pruebas automáticas (solo en el equipo, no van en la aplicación) | Apache 2.0 / MIT |

**Criterio de licencias**: solo bibliotecas gratuitas. Un caso real: **PDFsharp desde la versión
6.5.0 cambió a licencia AGPL**, que obligaría a publicar el código. Se fijó la 6.2.4 (MIT) y se
dejó escrito «no actualizar». Todas las bibliotecas aparecen, con su uso y su licencia, en
**Administración → Versiones y actualizaciones**, que también avisa de actualizaciones y fallos
de seguridad conocidos.

### 7.5 Herramientas y extensiones de Visual Studio Code

| Herramienta / extensión | Para qué |
|---|---|
| Claude Code (`anthropic.claude-code`) | El copiloto de IA |
| C# Dev Kit y C# (`ms-dotnettools.*`) | Programar en C# y .NET |
| Container Tools (`ms-azuretools.vscode-containers`) | Ver y manejar los contenedores de Docker |
| Docker Desktop | Ejecutar los contenedores en el PC |
| Git + GitHub | Control de versiones (repositorio privado) |
| Herramienta `dotnet-ef` | Crear las migraciones de la base de datos |
| Microsoft Edge (modo *headless*) | Capturas automáticas de las pantallas para revisarlas |

---

## 8. La visión artificial: cómo compara

Es la parte más «de IA clásica» del proyecto: **visión por computador** con OpenCV.

1. **Patrón a imagen**: el PDF se dibuja a 300 ppp y en color.
2. **Recorte de la pieza**: en cada muestra escaneada se separa la pieza del fondo del escáner.
3. **Localización**: se buscan puntos característicos (**SIFT**) en patrón y muestra, se
   emparejan y se calcula con **RANSAC** el giro y la posición. Funciona con cualquier giro y
   aunque el tubo se haya abierto por otro punto (la muestra llega en dos trozos).
4. **Ajuste fino**: por zonas pequeñas se corrige la curvatura de la pieza aplanada
   (*phase correlation*).
5. **Tono por tinta**: los colores del patrón se agrupan en 8 «tintas» (*k-means*) y se iguala
   la luminosidad de cada una. Así no se analiza el color, solo el contenido.
6. **Diferencias con margen**: se busca lo que sobra (manchas) y lo que falta, con un margen de
   desajuste según la **tolerancia** (Estricta, Normal o Amplia).
7. **Descartes**: guías de montaje del patrón, pliegues y bordes de la pieza.
8. **Motivo y evidencia**: cada diferencia se clasifica (mancha, falta, desplazamiento o texto
   doble), se mira si sale en las dos muestras (posible fallo de plancha) y se ordenan por evidencia.

Con las muestras reales de prueba, la tolerancia Normal encontró las manchas de las dos muestras
y un texto doble, con 3 avisos dudosos. **Los mismos archivos dan siempre el mismo resultado**
(semilla fija). Problemas reales resueltos por el camino: OpenCV se cerraba de golpe en Windows
con varios hilos (se usa uno en Windows) y en Linux faltaban bibliotecas del sistema.

**Decisión importante: la persona valida.** La aplicación propone posibles diferencias y el
operario decide si cada una es correcta o una incidencia, con comentario. La IA no decide sola
sobre la calidad del producto.

---

## 9. Docker y contenedores

### 9.1 Qué es y por qué

Un **contenedor** es como una caja que lleva dentro la aplicación con todo lo que necesita para
funcionar. Así funciona igual en el PC de desarrollo que en el servidor. La norma WYS pide que
toda aplicación se despliegue con Docker.

### 9.2 Cómo está montado

```
docker compose up --build
        │
        ├── Contenedor «app»      → Visual Inspector (.NET 10 sobre Ubuntu 24.04)
        │      puerto 8082 del PC → 8080 dentro
        │      volúmenes: registros-app (log/) · claves-app (sesiones)
        │
        └── Contenedor «mariadb»  → MariaDB 11.4
               SIN puerto hacia fuera: solo lo ve la aplicación
               volumen: datos-mariadb
```

| Elemento | Explicación |
|---|---|
| `Dockerfile` en dos fases | Una imagen grande con el SDK compila la aplicación y otra pequeña, solo con lo necesario, la ejecuta. Instala las bibliotecas de Linux que necesita OpenCV y corre con un usuario sin privilegios |
| `docker-compose.yml` | Define los dos contenedores, sus volúmenes, la zona horaria de Madrid y el chequeo de salud de MariaDB (la app espera a que la base de datos esté lista) |
| `.env` | Las contraseñas (base de datos, usuarios iniciales). **Nunca se sube a GitHub** |
| Volúmenes | Los datos, los registros y las claves de sesión sobreviven a reinicios y actualizaciones |
| `.dockerignore` | Deja fuera de la imagen la documentación, los registros, las pruebas y los archivos de clientes |
| Migraciones al arrancar | La aplicación crea o actualiza sola las tablas al arrancar |

### 9.3 Problemas reales resueltos con Docker

- **Los botones no hacían nada**: el script de Blazor no se copiaba a la imagen por una opción de
  compilación (`--no-restore`). Se corrigió el `Dockerfile` y también la plantilla de la sub-skill
  de Docker, para que no vuelva a pasar en otros proyectos.
- **OpenCV en Linux**: la imagen de .NET 10 es Ubuntu 24.04 y le faltaban bibliotecas del
  sistema; se añadieron en el `Dockerfile`.
- **Probar en Linux lo hecho en Windows**: el informe PDF se generó también dentro de un
  contenedor Linux para confirmar que las fuentes y el logo salían igual.
- **Puerto**: se cambió del 8080 al 8082 en el PC para no chocar con otras aplicaciones.

### 9.4 Comandos habituales

```bash
docker compose up -d --build     # compilar y arrancar en segundo plano
docker compose logs app          # ver qué dice la aplicación
docker compose cp app:/app/log ./log-servidor   # copiar los registros al PC
```

---

## 10. Seguridad y testing

### 10.1 La norma de partida

Se aplicó la norma interna **SEGURIDAD WYS 2026** (sub-skill de seguridad), basada en *Security
by Design*, *Shift Left* y *Zero Trust*. Lo principal que se implementó:

- **Denegación por defecto**: un usuario nuevo no puede hacer nada hasta que el administrador le
  da permisos, y no hay registro público de cuentas.
- **Permisos comprobados en el servidor**, no solo ocultando botones.
- **Contraseñas con Argon2id**, bloqueo tras 3 fallos durante 15 minutos y cambio obligatorio de
  la contraseña temporal.
- **Cabeceras de seguridad** en el navegador (CSP, X-Frame-Options, nosniff). Sin TLS en la
  aplicación, porque es interna y el cifrado lo gestiona la red de WYS.
- **Ningún secreto en el código** ni en GitHub.
- **Diario de seguridad** (`Log-Wys-Seg`, 90 días) y **de errores** (`err-log`, 30 días), sin
  contraseñas. Los errores enseñan al usuario solo un código.
- **Zero Trust con los archivos**: se mira el contenido real (no la extensión), con un máximo de
  50 MB, y los archivos no se guardan: solo su huella SHA-256 y un recorte de cada diferencia.
- **Trazabilidad**: quién y cuándo creó o modificó cada registro.

Al terminar cada cambio, la IA emitía una **Revisión de seguridad WYS** con el nivel de riesgo
(SEGURO, RIESGO MODERADO, INSEGURO o CRÍTICO).

### 10.2 Pruebas automáticas de seguridad

Se creó un proyecto de pruebas (**xUnit**) en la carpeta `test/`, que se queda en el equipo:
no se sube a GitHub ni entra en la aplicación. En la versión 0.12.5 eran **86 de 86 correctas**; hoy, con las pruebas de las reglas de negocio y de la exportación, son **125 pruebas, todas correctas**. Cómo se plantearon, qué encontraron y qué no se ha probado está en [[07-testing]].

| Qué se prueba | Ejemplos |
|---|---|
| Contraseñas | La correcta entra, la incorrecta o vacía no; la misma contraseña nunca se guarda igual |
| Permisos | Sin permisos no se puede nada; «Administración» solo para el rol Administrador |
| Entradas | Un programa con nombre de PDF, un archivo de más de 50 MB o un texto con inyección SQL se rechazan |
| Configuración | Sin contraseñas en los archivos de configuración; `.env`, registros y archivos de clientes fuera de Git |
| Aplicación en marcha (Docker) | Sin entrar, todo manda a la entrada; un formulario falso desde otra web se rechaza; la base de datos no se alcanza desde fuera |
| Funciones nuevas | Línea de fabricación solo de la lista; registros solo para el administrador; órdenes abiertas solo para quien hace autocontroles |

**Las pruebas encontraron dos fallos reales**, que se corrigieron:

1. Comprobar una contraseña vacía producía un error interno en lugar de responder «no».
2. El servidor anunciaba qué programa usaba (cabecera `Server: Kestrel`), una pista innecesaria.

El **bloqueo tras 3 fallos** se comprobó a mano, para no bloquear cuentas reales con las pruebas.

Detalle en [pruebas-seguridad.md](pruebas-seguridad.md) y [seguridad.md](seguridad.md).

### 10.3 Otras pruebas

- **Con datos reales**: patrón y muestras reales de un cliente (que nunca se subieron a GitHub y
  se guardan en carpetas excluidas).
- **Visuales**: capturas automáticas a 1280, 1366 y 1440 px. Sirvieron para detectar, por
  ejemplo, que la cabecera no cabía o que el logo tenía un recuadro gris.
- **Del PDF**: el informe se generó con recortes reales y se convirtió en imagen para revisarlo.
- **De migraciones**: la conversión de datos se probó en una tabla temporal, en los dos sentidos.

---

## 11. Decisiones, cambios de rumbo y lecciones aprendidas

| Decisión | Por qué |
|---|---|
| Python → C# / .NET | Estándar de la empresa, mantenible por IT |
| No guardar los archivos | Privacidad de los diseños de clientes; basta con la huella y los recortes |
| Se ve en color, pero no se analiza el color | Lo que importa es el contenido impreso, no pequeñas variaciones de tono |
| Validación humana de cada diferencia | La IA asiste, la persona decide |
| Controles numerados en lugar de horas fijas | El operario decide cuándo; las 4 horas son solo una recomendación |
| Carpetas por defecto: probadas y **quitadas** | El navegador no deja abrir «Elegir archivo» en una carpeta indicada por la web |
| Borrar registros: **descartado** | Permitiría borrar el rastro; los diarios se borran solos a los 30 y 90 días |
| Eliminar usuarios: **aplazado** | Basta con desactivarlos, que se puede deshacer |
| PDFsharp fijado en 6.2.4 | Las versiones nuevas son AGPL |

**Lecciones para el máster:**

1. **La IA acelera muchísimo**: un MVP con visión artificial, seguridad y Docker en pocos días.
2. **El conocimiento del negocio sigue siendo humano**: las mejores decisiones vinieron del
   usuario (tolerancias, líneas, inicio de la orden).
3. **Las normas escritas (skills y `CLAUDE.md`) dan coherencia**: todo el código sigue el mismo
   estilo, la misma seguridad y la misma imagen.
4. **Hay que probar con datos reales**: varios problemas solo salieron con las muestras reales
   o en Docker.
5. **La IA también se equivoca o malinterpreta**: por eso la lista previa, las preguntas y las
   pruebas son imprescindibles.
6. **Saber cuándo decir «no se puede»** también es valioso: la IA explicó los límites del
   navegador y de la seguridad en lugar de forzar una solución mala.

---

## 12. Próximos pasos

- **Copias de seguridad** de la base de datos con un procedimiento de recuperación probado: es lo primero antes de usar la aplicación con datos reales.
- **Prueba completa con un operario** y archivos de producción, y probar la limpieza de datos contra una copia de la base de datos.
- **Revisar la tolerancia** con más muestras de otros clientes y añadir pruebas automáticas al comparador.
- **Filtros** de la lista de autocontroles (solo con alerta, por línea, por estado de la orden y por resultado) y el cliente en la orden para analizar el impacto por cliente.
- Desplegar en un servidor de WYS (pendiente de que IT defina el servidor).
- Actualizar a Entity Framework 10 cuando salga Pomelo 10, y revisar el aviso de seguridad de `MySqlConnector`.

El detalle está en [[08-mejoras]] y [[09-conclusiones]].

---

## 13. Glosario

| Término | Significado |
|---|---|
| Autocontrol | Comprobación que hace el operario durante la fabricación |
| Patrón | Diseño gráfico aprobado y firmado por el cliente (PDF) |
| Litografía | Código del diseño (L + 7 cifras), por ejemplo L2201989 |
| OF | Orden de fabricación (10 cifras) |
| RPI | AutoControl Retenido Para Inspección. Los autocontroles de una orden se numeran RPI 1 (inicial), RPI 2… |
| Línea | Línea de fabricación de la fábrica (L1 … L7) |
| MVP | Producto mínimo viable |
| Agente de IA | IA que no solo responde, sino que actúa: lee, escribe y ejecuta |
| Skill | Paquete de instrucciones que la IA carga cuando la tarea lo necesita |
| Migración | Cambio controlado y repetible en las tablas de la base de datos |
| Contenedor | «Caja» con la aplicación y todo lo que necesita para funcionar |
| SIFT / RANSAC | Técnicas de visión para encontrar puntos comunes y calcular la posición |
| Argon2id | Método recomendado para guardar contraseñas de forma irreversible |
| CSP | Política que indica al navegador qué puede cargar la página |
| Zero Trust | No fiarse de ninguna entrada: todo se comprueba en el servidor |

---

## 14. Dónde está cada cosa

| Qué | Dónde |
|---|---|
| Idea y MVP | `doc/master/` (notas 01 y 02) |
| Capítulos del máster | `doc/master/` (notas 03 a 09: entorno, prototipo, IA, base de datos, testing, mejoras y conclusiones) |
| Guía de usuario | `doc/guia-usuario.md` (también dentro de la aplicación, con el icono de ayuda) |
| Registro de cambios por versión | `doc/cambios.md` |
| Arquitectura y decisiones | `doc/arquitectura.md` |
| Seguridad explicada en sencillo | `doc/seguridad.md` |
| Pruebas de seguridad | `doc/pruebas-seguridad.md` y carpeta `test/` (solo en el equipo) |
| Puesta en marcha (Docker) | `doc/puesta-en-marcha.md` |
| Instrucciones para la IA | `CLAUDE.md` (proyecto y bóveda) |
| Código | `Components/` (pantallas), `Servicios/` (lógica y visión), `Datos/` (base de datos), `Seguridad/` |
| Docker | `Dockerfile`, `docker-compose.yml`, `.env.example` |

Relacionado: [[MASTER IA]]
