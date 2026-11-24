# Registro de cambios

## 0.23.2 — 2026-11-24
Correcciones de una revisión independiente del código de la planificación (decisión de Alberto, 24/11/2026:
importantes y menores).

- **Solo se cierran las órdenes que el operario vio.**
  - Al pulsar Continuar, la pantalla envía qué órdenes avisó que se finalizarían.
  - Si el servidor fuera a cerrar otras, porque la planificación cambió o alguien abrió otra orden en la línea
    mientras la ventana estaba abierta, no crea ni cierra nada y pide revisarlo.
  - Lo mismo si la planificación ha movido la orden a otra línea.
- **Una lectura de la hoja sin ninguna orden válida cuenta como fallo.** Suele ser que producción la está
  recargando. Se sigue con la copia anterior en lugar de dar por terminadas todas las órdenes abiertas.
- **Finalizar a la vez que otra persona** ya no rompe la pantalla: sale un aviso.
- Si falla la lectura de los ajustes, la planificación ya no aparece como activa estando apagada.
- La línea elegida a mano para una orden fuera de planificación ya no se arrastra a otra orden.
- La ventana de confirmación vuelve a calcular qué RPI será y qué se cerraría justo al abrirse.
- Las órdenes abiertas que **siguen en la hoja** como «siguientes» ya no ofrecen Finalizar («Sigue en la
  planificación»).
- Crear el autocontrol desbloquea siempre el botón, aunque algo falle.
- Prueba nueva (no se cierra una orden no vista): 169 de 169 correctas.
- **Guía de usuario actualizada**: órdenes pendientes de cerrar y una pregunta frecuente.

## 0.23.1 — 2026-11-24
- **Las diferencias validadas como correctas entran en la trazabilidad** (decisión de Alberto, 24/11/2026). Ya se
  guardaban todas en la base de datos (recorte, quién y cuándo), pero solo se enseñaban en la ficha de cada
  autocontrol:
  - **Informe de fabricación (pantalla y PDF):**
    - caja nueva «validadas como correctas» en el resumen (en el PDF, cinco cajas);
    - apartado nuevo «Diferencias validadas como correctas», con control, número, motivo, muestras, tamaño, quién y
      cuándo la validó y su recorte (en el PDF, algo más pequeño que el de las incidencias).
  - **Trazabilidad de la orden:** cifras nuevas «Diferencias revisadas» y «Validadas como correctas» (de los
    autocontroles registrados, como las incidencias) y columna «Correctas» en la tabla de autocontroles.
  - La exportación a Excel/CSV y la lista de trazabilidad no cambian.
- Prueba nueva: el PDF se genera con las correctas y sus recortes; 168 de 168 correctas.
- Documentación al día: `descripcion.md` (estaba en la 0.2.0), `pruebas-seguridad.md` (estaba en la 0.12.5) y
  `README.md`.
- **Guía de usuario actualizada**: «El informe de fabricación» y «Trazabilidad».

## Documentación — 2026-11-24
- **`arquitectura.md`** al día (0.23.0):
  - Piezas y capas nuevas, carpetas `wys-api-sheet/` y `patrones/`.
  - Apartado nuevo «Integraciones externas», con el flujo de la planificación y de la carpeta de patrones.
  - Decisiones del 24/11/2026 y próximos pasos: puesta en producción y repositorio privado.
- **`guia-master.md`** al día hasta la 0.23.0:
  - Cifras actuales: 24 commits, 167 pruebas y 12 migraciones.
  - Mejoras y cronología, ejemplos de prompts y malentendidos de esta etapa.
  - Bibliotecas nuevas, Docker con los secretos prestados y la carpeta de red.
  - El incidente de la clave contado tal cual, lecciones nuevas, próximos pasos y glosario.
  - Se corrige que el repositorio es **público** (pasará a privado).
- **`CLAUDE.md`** del proyecto: las dos integraciones externas y la regla de no subir datos internos mientras el
  repositorio sea público.
- **Carpeta de la clave** renombrada a `wys-api-sheet` (minúsculas), como espera Linux.

## 0.23.0 — 2026-11-24
- **Patrón desde la carpeta de patrones, filtrado por especialidad** (decisiones de Alberto, 24/11/2026): al cargar
  los archivos del autocontrol, el hueco **Patrón (PDF)** enseña los PDF de la carpeta de red de patrones que
  llevan en el nombre la especialidad de la orden sin la E (E2201829 → `L2201829R002 ….pdf`), con la versión más
  alta primero.
  - Un clic lo carga y lo revisa igual que desde el PC. **Elegir archivo** sigue debajo.
  - El navegador no permite filtrar su explorador por nombre: por eso la lista la prepara el servidor.
- **La especialidad se guarda en la orden** (columna nueva `OrdenesFabricacion.Especialidad`, migración
  `AnadeEspecialidadOrden`). Se toma de la planificación al crear un autocontrol, nunca de la pantalla. Las
  órdenes fuera de planificación no la tienen y no se filtra.
- **Sin errores para el operario**: si no hay especialidad, la carpeta no existe, está vacía, no contesta en 10
  segundos o no deja leer, se elige desde el PC como siempre. El motivo va a `log/err-log`.
- Seguridad:
  - Solo se puede abrir un nombre de la lista encontrada para esa orden: ni otros archivos ni salir de la
    carpeta.
  - La carpeta llega al contenedor en solo lectura.
  - La ruta real no está en el repositorio, sino en el `.env` (`PATRONES_CARPETA_SERVIDOR`).
- **Parámetro nuevo `CarpetaPatrones`** (Administración → Parámetros) para cambiar la carpeta cuando haga falta:
  - Si tiene valor, manda sobre la configuración de la instalación; vacío, se usa la del `.env`.
  - Es la ruta tal como la ve la aplicación: en Docker, una carpeta del contenedor.
  - Se valida que sea una ruta completa y el cambio queda en `Log-Wys-Seg`.
  - En Docker, una ruta de Windows (`\\servidor\…` o `C:\…`) se rechaza con un mensaje que explica por qué y qué
    escribir. Antes el ejemplo del mensaje sugería justo una ruta que no vale.
- **Descripciones de los parámetros al día**: al arrancar, las de los parámetros que ya existían se actualizan con
  las del código. El administrador no las edita y el valor no se toca.
- Pieza nueva `BuscadorPatrones`. La carga de archivos se ha reorganizado para que el PC y la carpeta compartan los
  mismos pasos.
- Comprobado el 24/11/2026:
  - La carpeta de red tiene 2.121 PDF sin subcarpetas, y las 7 especialidades en curso encuentran su patrón.
  - Desde Docker no deja entrar como invitado: en el servidor, IT debe montarla con un usuario de solo lectura
    (ver `doc/patrones.md`).
- Pruebas: 18 nuevas (código de búsqueda, filtro, sin especialidad, sin carpeta, no abrir fuera de la lista,
  carpeta del parámetro, validación de la ruta y carpeta real): 167 de 167 correctas.
- Documentación: `doc/patrones.md` (nuevo), `seguridad.md`, `puesta-en-marcha.md`, `.env.example` y
  `docker-compose.yml`.
- **Guía de usuario actualizada**: Paso 2 «Cargar el patrón y las muestras».

## 0.22.3 — 2026-11-24
Decisiones de Alberto, 24/11/2026.

- **Autocontrol fuera de planificación**: si la orden escrita a mano no está en la planificación ni abierta en la
  aplicación, ya no se bloquea.
  - Aparece el campo **Línea** con el aviso «No está en la planificación ni abierta: elige la línea…».
  - La ventana de confirmación lo indica: «elegida a mano: autocontrol fuera de planificación».
  - El servidor solo acepta la línea elegida en este caso: si la orden está en la hoja o abierta, sigue mandando
    su línea.
  - Sustituye al aviso «Esa orden no está en la planificación… ni abierta» de la 0.22.0.
- **Fondo amarillo en las órdenes que ya tienen autocontroles**, tanto la en curso como las abiertas por cerrar.
  - El borde izquierdo se mantiene (verde la en curso, azul las abiertas) para distinguirlas.
  - La en curso sin autocontroles sigue en verde.
  - La elegida va siempre en rojo, también al pasar el ratón.
- **Especialidad** (columna nueva `ESPECIALIDAD` de la hoja, por ejemplo E2201359), solo informativa: sale en cada
  orden (en curso, abiertas por cerrar si están en la hoja y siguientes), justo debajo del número de orden, y en
  la ventana de confirmación. El número va ahora con «OF:» delante en todas las tarjetas (y «OF» en la ventana). Se lee
  por el título de la columna, se corta a 30 caracteres y, si la columna falta, no se muestra. Prueba ampliada.
- **Leyenda de colores** arriba a la derecha de la planificación, en vertical y en pequeño, con una muestra de cada
  caso: en curso, en curso ya
  con autocontroles, abierta pendiente de cerrar, siguientes, elegida y barra de tubos fabricados. Usa los mismos
  colores que las tarjetas, también de noche.
- **Barra de progreso en azul WYS** (azul brillante, que se aclara solo en el modo nocturno) en lugar de verde,
  para que destaque sobre la tarjeta verde.
- Pruebas: actualizada la de cómo se decide la línea; 149 de 149 correctas.
- **Guía de usuario actualizada**: paso 4 de «Crear el autocontrol» y dos preguntas frecuentes.

## 0.22.2 — 2026-11-24
Mejoras visuales de **Nuevo Autocontrol** (decisiones de Alberto, 24/11/2026).

- **Barra de progreso** en la orden en curso: una barra verde fina con los tubos fabricados sobre la cantidad, y el
  porcentaje junto a las cifras (por ejemplo «Fabricados 148.608 de 189.000 (79 %)»).
- **Franja para crear el autocontrol en una sola fila**: orden, línea (solo sin conexión con la planificación),
  observaciones y botón **Crear autocontrol**.
  - Se queda fija abajo al bajar por la pantalla, con un filete azul que la separa (sin sombras, según la
    identidad WYS).
  - El RPI que será y el aviso de cierre de la línea van debajo, dentro de la franja.
  - El desplegable de línea dice «Línea 1»… como el resto de la pantalla.
- **La pantalla se pone al día sola cada minuto** (planificación y órdenes abiertas), igual que la lista de
  Autocontroles.
  - No pregunta a Google: usa la copia del servidor.
  - Se salta la vuelta si está abierta la ventana de confirmación o se está creando el autocontrol.
- **Guía de usuario actualizada**: Paso 1 (barra, franja y refresco).

## 0.22.1 — 2026-11-24
Decisiones de Alberto, 24/11/2026.

- **Ventana de confirmación al pulsar «Crear autocontrol»**: «¿Seguro que quieres crear este autocontrol?» con la
  orden, la descripción, la línea (y de dónde sale), qué RPI será, la fecha y hora, el último control y las
  observaciones, y los botones **Continuar** y **Cancelar**.
  - Si se va a finalizar otra orden de la línea, la ventana lo dice en rojo y **Continuar** lo acepta. Sustituye
    a los antiguos botones «Sí, finalizar y crear el autocontrol» / «No, volver».
  - El aviso amarillo bajo la orden se mantiene.
- **Con conexión con la planificación, el campo «Línea» no se muestra**: la línea se ve en la columna de la orden
  y en la ventana de confirmación.
- **El desplegable de línea solo sale si no hay conexión con la planificación**: es decir, si la última lectura
  falló o tiene más de 2 horas. Antes bastaba con haberla leído alguna vez.
  - Sin conexión, la línea se elige a mano y se admiten órdenes nuevas, porque una orden cargada después de la
    última lectura no estaría en la copia.
  - Las columnas de la planificación siguen visibles con el aviso «No hay conexión con la planificación…».
  - El servidor aplica la misma regla.
- El aviso de cierre dice «la línea 3» en lugar de «la línea L3».
- Pruebas ampliadas: sin conexión (lectura fallida o vieja), la línea se elige a mano.
- **Guía de usuario actualizada**: pasos 4 y 6 de «Crear el autocontrol», «Finalizar una orden» y una pregunta
  frecuente. Actualizados también `arquitectura.md` y `planificacion-google.md`.

## 0.22.0 — 2026-11-24
- **La línea la pone la planificación** (decisión de Alberto, 24/11/2026): toda orden nueva viene con su línea en la
  hoja, así que en **Nuevo Autocontrol** la línea pasa a ser solo informativa:
  - si la orden está en la hoja, su línea de la hoja;
  - si no, la de la orden que sigue abierta en la aplicación.
- **La orden escrita a mano** tiene que estar en la planificación (también las del desplegable **Siguientes**) o
  seguir abierta. Si no, sale el aviso «Esa orden no está en la planificación de producción ni abierta…» y no se
  puede crear el autocontrol.
- El servidor lo vuelve a comprobar al crear: decide él la línea e ignora la que mande la pantalla.
- **Si la planificación no está disponible** (apagada, sin clave o sin haberla podido leer nunca), todo sigue como
  antes: la línea se elige a mano y se pueden crear órdenes nuevas, para no parar la producción. Si se leyó alguna
  vez pero la última lectura falló, se usa la última copia.
- En la cabecera de la planificación se indica cada cuánto se actualiza sola («Se actualiza sola cada 15
  minutos», según el parámetro `MinutosLecturaPlanificacion`).
- Pruebas: 2 nuevas (cómo se decide la línea y cuándo se elige a mano); 149 de 149 correctas.
- **Guía de usuario actualizada**: pasos 3 y 4 de «Crear el autocontrol» y dos preguntas frecuentes.

## 0.21.3 — 2026-11-24
- **Planificación de producción por línea, más clara** (decisiones de Alberto, 24/11/2026). Cada columna enseña:
  - la orden **en curso** (en verde, con fabricados, cantidad y pendientes);
  - las órdenes de esa línea que **siguen abiertas** en la aplicación y no son la de curso (borde azul), con su
    botón **Finalizar orden**;
  - un desplegable **Siguientes (n)** con el resto de órdenes de la hoja, a modo de consulta, desde el que también
    se puede elegir una para crear su autocontrol.
- Sustituye al aviso amarillo «Órdenes abiertas que ya no están en la planificación». **Finalizar orden** sigue
  saliendo solo si la hoja se leyó bien hace poco y la orden tiene algún autocontrol registrado.
- En la orden en curso y en las abiertas se indica **cuántos autocontroles lleva**, cuánto hace del último y cuál
  toca. Si la orden en curso está finalizada en la aplicación, se avisa de que hay que pedir que la reabran.
- La **orden elegida se marca en rojo** WYS. El amarillo ya avisa de «toca otro control». También cambia en la
  lista de órdenes abiertas que sale cuando no hay planificación.
- Los títulos de columna dicen **«Línea 1», «Línea 2»…** en lugar de «Línea L1».
- Por dentro:
  - Pieza nueva `OrganizadorPlanificacion`, que reparte las órdenes por línea y se prueba sin pantalla.
  - `ListarResumenOrdenesAsync` sustituye a `ListarOrdenesFueraDePlanificacionAsync`. Busca los códigos con un
    `IN (…)` normal; comprobado con el conector de MariaDB.
  - 3 pruebas nuevas: 147 de 147 correctas.
- **Guía de usuario actualizada**: Paso 1 y apartado «Órdenes abiertas pendientes de cerrar».

## 0.21.2 — 2026-11-24
- **Planificación de producción más compacta** (decisión de Alberto, 24/11/2026): todas las líneas en una sola fila
  (de L1 a L7, una columna por línea). La orden en curso va **en verde** (el verde de «correcto» de Bootstrap, que
  admite la identidad WYS, también en modo nocturno), sin la etiqueta «En curso». Ya no se muestra la prioridad.
  Las pendientes llevan solo la orden y la descripción; el detalle (fabricados, cantidad, pendientes y último
  control) queda solo en la orden en curso. Para el lector de pantalla, la orden en curso sigue diciéndolo.
- **Guía de usuario actualizada**: Paso 1 de «Cómo hago un autocontrol».

## 0.21.1 — 2026-11-24
- **Seguridad: MySqlConnector 2.6.1** (decisión de Alberto, 24/11/2026). Pomelo 9 traía la 2.4.0, con un fallo
  conocido (GHSA-473q-m89c-ghf8, gravedad moderada): con `SslMode` VerifyCA o VerifyFull y sin `SslCa`, el
  certificado no se comprobaba y la contraseña de la base de datos podía viajar sin protección. **No afectaba** a
  esta configuración: la conexión no usa esos modos y MariaDB solo es accesible dentro de la red interna de Docker.
  Aun así, se fuerza la versión corregida en `VisualInspector.csproj` (como ya se hace con
  `System.Security.Cryptography.Pkcs`) y se anota en `generar-versiones.ps1`. `dotnet list package --vulnerable`
  ya no da ningún aviso. Comprobado: compila, 144 de 144 pruebas correctas y arranca en Docker conectando con MariaDB.

## 0.21.0 — 2026-11-24
- **Planificación de producción desde Google Sheets** en **Nuevo Autocontrol** (decisiones de Alberto, 24/11/2026):
  encima del campo de la orden sale, por línea, la orden **En curso** (prioridad 1, la cargada en la máquina) y las
  pendientes por prioridad, con su descripción. Un clic rellena la orden y la línea. La orden se puede seguir
  escribiendo a mano (**O escribe la orden**). En la orden en curso se ven, solo a título informativo, los tubos
  fabricados, la cantidad y los pendientes. Si la orden ya tiene autocontroles, también cuánto hace del último y
  cuál toca.
- La hoja se lee con una **cuenta de servicio de Google de solo lectura** (clave en `wys-api-sheet/`, excluida de
  Git, de Docker y de la publicación). Un trabajo en segundo plano la lee cada 15 minutos (parámetro nuevo
  `MinutosLecturaPlanificacion`), y el botón **Actualizar ahora** la lee al momento (como mucho una vez por minuto).
  Se apaga con el parámetro nuevo `UsarPlanificacion`. Si Google falla, se muestra la última copia con un aviso. Sin
  clave, la pantalla funciona como antes.
- La línea «LINEA 1» se traduce a L1. Las columnas se localizan por su título, y se muestra la hora de carga de
  la hoja («Datos de producción del…»). Las filas que no encajan se descartan y se cuentan en `log/err-log`. El
  estado sale de la prioridad, no de la columna ESTADO.
- **Órdenes abiertas que ya no están en la planificación**: se listan con un botón **Finalizar orden** (con
  confirmación), que hace lo mismo que finalizarla desde su ficha. Solo salen si la hoja se leyó bien hace menos de
  2 horas, y nunca se cierran solas.
- Al crear un autocontrol, si la orden no tiene descripción, se copia la de la planificación.
- Las «órdenes de fabricación abiertas» solo salen ahora cuando no hay planificación.
- Biblioteca nueva: **Google.Apis.Sheets.v4 1.78.0.4330** (Apache 2.0), anotada en `generar-versiones.ps1`;
  `recursos/versiones.json` regenerado.
- Documentación: `doc/planificacion-google.md` (nuevo: columnas, funcionamiento, cómo crear y cambiar la cuenta),
  `seguridad.md` («La planificación, solo para leer») y `puesta-en-marcha.md` (copiar la clave al servidor).
  `docker-compose.yml` monta `wys-api-sheet/` en solo lectura, y `.env.example` lo explica.
- **Guía de usuario actualizada**: Paso 1 de «Cómo hago un autocontrol», apartado nuevo «Órdenes abiertas que ya no
  están en la planificación» y tres preguntas frecuentes.
- Pruebas: 19 nuevas de la planificación (144 de 144 correctas) (conversión de filas, líneas, órdenes, hora de carga y antigüedad). Una
  de ellas lee la hoja real si está la clave: 47 filas, 45 órdenes y ninguna descartada, con una orden en curso
  en cada línea L1 a L7.

## Documentación — 2026-10-30
- **Capítulos 03 a 09 del máster** (`doc/master/`): preparación del entorno, primer prototipo, integración de IA,
  base de datos, testing, mejoras y conclusiones. Estaban vacíos; cada uno explica la necesidad que plantea su título,
  el concepto y cómo se aplicó en el proyecto, con enfoque de presentación para el máster: sin fechas ni versiones,
  con cifras globales comprobadas el 30/10/2026 y diciendo también lo que no se ha probado.
- **`guia-master.md` actualizada**: periodo hasta la 0.20.2, cifras actuales (21 commits, 11 migraciones, 125
  pruebas, 46 archivos de C# y 57 pantallas), mejoras 0.13 a 0.20, próximos pasos de hoy, glosario con «RPI» y
  enlaces a los nuevos capítulos. Se corrige que el repositorio de GitHub es **público**, no privado.
- **`guia-usuario.md`**: se concretan el bloqueo de la cuenta (3 fallos, 15 minutos) y el cierre de sesión por
  inactividad (30 minutos), datos comprobados en el código. Llegará a la aplicación al reconstruirla.

## 0.20.2 — 2026-10-30
- **Botones de exportación con icono y junto a Buscar** (decisión de Alberto, 30/10/2026): en lugar de dos botones
  de texto debajo del filtro, ahora hay dos botones solo con icono (hoja de cálculo para Excel y las letras CSV
  para CSV), pegados al botón **Buscar** de Trazabilidad y al botón **Ver** de Impacto productivo, con su ayuda
  al pasar el ratón.
- **Solo se activan si hay algo que exportar**: en Trazabilidad, si la lista tiene órdenes; en Impacto, si el cuadro
  tiene incidencias. Si no, se ven apagados. En la ficha de una orden, el botón de Excel siempre está activo.
- Mientras se prepara el archivo, el botón pulsado muestra una ruedecita. El aviso de error (por ejemplo
  «demasiadas filas») pasa a salir flotando justo debajo de los botones, con su cruz para cerrarlo, para no
  descuadrar la fila de filtros. Azul de la identidad WYS, también de noche.

## 0.20.1 — 2026-10-30
- **Icono de la aplicación que no se actualizaba** (el logo antiguo seguía saliendo en la lista de pestañas o de
  favoritos del navegador): los navegadores guardan los iconos mucho tiempo. Ahora las direcciones de los iconos de
  la pestaña y del móvil llevan el número de versión (`?v=…`), así que se vuelven a pedir con cada actualización, y
  `/favicon.ico`, que los navegadores piden por su cuenta, devuelve el icono nuevo sin exigir sesión (antes
  redirigía a la pantalla de entrada).

## 0.20.0 — 2026-10-30
- **Exportación a Excel y CSV** de Trazabilidad e Impacto productivo (botones «Descargar Excel» y «Descargar CSV»):
  - Trazabilidad, lista de órdenes cerradas: Excel y CSV, con los filtros aplicados (9 columnas).
  - Trazabilidad, ficha de una orden: Excel de varias hojas (resumen, incidencias por motivo, impacto,
    producción a revisar y autocontroles).
  - Impacto productivo: Excel con resumen, por línea, por litografía, por defecto, órdenes (todas, no solo 15) y
    **detalle de incidencias** (una fila por incidencia, dato nuevo que no estaba en pantalla); el CSV es ese detalle.
- El CSV va pensado para el Excel en español: UTF-8 con marca, `;` como separador, coma decimal y fechas
  `dd/MM/aaaa HH:mm`. En el Excel, números y fechas son valores reales (se pueden sumar y ordenar).
- **Seguridad**: permiso de ver autocontroles, filtros revalidados, tope de 5.000 órdenes y 20.000 incidencias por
  archivo, **protección contra fórmulas** (los textos que empiezan por =, +, - o @ llevan una comilla delante) y
  cada exportación apuntada en el diario (`ExportacionDatos`).
- La descarga la hace un pequeño script propio (`descargar-archivo.js`) a partir de un botón de la pantalla y no de
  un enlace directo, para que los avisos (por ejemplo «demasiadas filas») salgan junto a los botones.
- Biblioteca nueva: **MiniExcel 1.46.0** (Apache 2.0, gratuita también para uso comercial), anotada en
  `generar-versiones.ps1` y en el panel de versiones.
- 18 pruebas nuevas (125 de 125 correctas): escritor de CSV y de Excel, protección contra fórmulas, permisos,
  topes y diario.

## 0.19.20 — 2026-10-30
- **Histórico de OFs**: la ilustración se hace **más pequeña** (de 14 x 10,5 a 9,5 x 7,1 rem), **más discreta**
  (intensidad del 65 % en lugar del 92 %) y se coloca **junto al botón Buscar** en lugar de en el borde derecho de
  la pantalla.

## 0.19.19 — 2026-10-30
- **Conmutador diurno/nocturno, modo diurno**: la píldora usa ahora los azules de la identidad WYS (degradado de
  Pantone 7685 a 287 y 654, con borde gris azulado claro) en lugar de los azules celestes de antes, y el sol es
  un **oro pálido** menos intenso, con el símbolo en ámbar suave (`--wys-conmutador-sol`). El modo nocturno no
  cambia. Colores en `recursos/tema.css`.

## 0.19.18 — 2026-10-30
- **Histórico de OFs**: nueva ilustración decorativa (carpeta de órdenes con una lupa sobre un tubo y un reloj de
  historial; `recursos/app/historico.png`) a la derecha de la cabecera, con la misma inclinación de 6 grados que la
  de la lista de autocontroles. La imagen original tenía fondo blanco opaco: se ha pasado a **transparente** (solo el
  fondo exterior; los folios blancos se conservan) para que se integre también de noche, donde lleva un halo claro
  suave. Reducida a 480 px (160 KB). La cabecera pasa a dos zonas (título y búsqueda, e ilustración); se oculta por
  debajo de 1150 px.

## 0.19.17 — 2026-10-30
- La pantalla de la opción «Histórico» pasa a titularse **«Histórico de OFs»** (encabezado y pestaña del
  navegador), y las rutas de migas «Órdenes ›» de la ficha de la orden y del informe también dicen ahora
  «Histórico de OFs ›». Guía de usuario actualizada.

## 0.19.16 — 2026-10-30
- La opción del menú «Órdenes de Fabricación» pasa a llamarse **«Histórico»**. La dirección (`/ordenes`) y los
  permisos no cambian, y la pantalla a la que lleva mantiene su título «Órdenes de fabricación». Guía de
  usuario actualizada.

## 0.19.15 — 2026-10-30
- La lupa de la foto del escáner se hace **más pequeña** (de 8,3 a 6,8 rem) y se **desplaza hacia la izquierda**
  (la lente, de unos 5 rem, queda sobre la parte izquierda del sobre). El mango ya no se acerca a la tabla.

## 0.19.14 — 2026-10-30
- Lista de Autocontroles: se añade una **lupa transparente** (`recursos/app/lupa.png`, PNG con fondo y cristal
  transparentes, 29 KB) sobre el sobre de la foto del escáner, como si se estuviera inspeccionando la muestra.
  La foto y la lupa forman una escena que va inclinada 6 grados y cuyo tamaño pasa a 24 x 8,5 rem (la lupa,
  de 8,3 rem, con la lente de unos 6 rem sobre el sobre). De noche la lupa lleva un halo claro para que el aro
  azul no se pierda. Sigue siendo solo decorativa y se oculta por debajo de 1150 px.

## 0.19.13 — 2026-10-30
- El botón de la lista de Autocontroles pasa a llamarse **«Nuevo Autocontrol»** (antes «Nuevo Autocontrol RPI»).
  La pantalla a la que lleva mantiene su título «Nuevo Autocontrol RPI». Guía de usuario actualizada.

## 0.19.12 — 2026-10-30
- El botón «Nuevo Autocontrol RPI» de la lista tiene las esquinas **muy redondeadas** (forma de píldora) y
  algo más de relleno a los lados. Solo ese botón: el resto de botones de la aplicación no cambian.

## 0.19.11 — 2026-10-30
- **«RPI» en lugar de «Control».** Los autocontroles se llaman ahora **RPI 1 (inicial), RPI 2, RPI 3…** (antes
  «Control 1 (inicial)», «Control 2»…) en todas partes: lista de autocontroles, ficha, orden, informe en
  pantalla y PDF, trazabilidad, avisos y diario de seguridad (las entradas antiguas del diario conservan el
  texto de entonces). Es solo el nombre: el número y los datos guardados no cambian. Los títulos de columna
  «Control» se mantienen.
- La foto del escáner de la lista se **inclina más** (6 grados) y se **acerca al botón** «Nuevo Autocontrol
  RPI» (queda a la derecha del hueco, con medio rem de margen).
- Guía de usuario, arquitectura y descripción actualizadas.

## 0.19.10 — 2026-10-30
- La foto del escáner de la lista de Autocontroles se hace más **apaisada** (23 x 8,1 rem; antes 20 x 8,5) y
  se **inclina 3 grados** (`transform: rotate(-3deg)`), como una foto dejada sobre la mesa. Se vuelve a
  recortar el original en panorámico (720 x 253 px) en lugar de estirarlo, para no deformarla.

## 0.19.9 — 2026-10-30
- Lista de Autocontroles: la foto del escáner se sustituye por una **más ilustrativa** (sobre en el cristal del
  escáner y, al lado, la hoja del patrón). Recortada en apaisado y reducida a 28 KB (`recursos/app/escaner.jpg`).
  Como la imagen ocupa casi todo el ancho, el difuminado de los lados se reduce (14 %) para no borrar el
  escáner ni la hoja. Mismo tamaño (20 x 8,5 rem). Ya no sale ninguna marca real.

## 0.19.8 — 2026-10-30
- La foto del escáner de la lista de Autocontroles sube a un tamaño intermedio (20 x 8,5 rem y 80 % de
  intensidad): la 0.19.7 la había dejado demasiado pequeña. La marca del sobre sigue sin poder leerse.

## 0.19.7 — 2026-10-30
- La foto del escáner de la lista de Autocontroles se reduce a la **mitad** (14 x 6,25 rem), se centra en el
  hueco entre la búsqueda y el botón y baja de intensidad (70 %) para que no resalte. El archivo
  `escaner.jpg` pasa a 440 px de ancho (11 KB): la marca del sobre queda solo representativa, no legible.

## 0.19.6 — 2026-10-30
- Lista de Autocontroles: nueva **foto del escáner** (`recursos/app/escaner.jpg`, recortada y reducida a 50 KB)
  entre la búsqueda y el botón «Nuevo Autocontrol RPI». Va difuminada por los lados (y algo por arriba y
  abajo) con una máscara de transparencia, así se funde con el fondo de día y de noche. La cabecera pasa a
  tres zonas: título y búsqueda, foto y botón. En pantallas de menos de 1150 px la foto se oculta. Es
  solo decorativa (sin texto alternativo ni enlace).

## 0.19.5 — 2026-10-30
- Los títulos de columna de todas las tablas llevan ahora un **separador vertical visible** entre columna y
  columna (solo en la cabecera). El color está en `recursos/tema.css` (`--wys-tabla-separador`), uno para el
  modo diurno y otro para el nocturno.

## 0.19.4 — 2026-10-30
- Lista de Autocontroles: la columna «Tiempo sin control (hh:mm)» pasa a llamarse **Tiempo AC** (con una ayuda
  al pasar el ratón) y el tiempo se escribe como **00 h : 00 min** (por ejemplo `03 h : 50 min`, `26 h : 05 min`).

## 0.19.3 — 2026-10-30
- Lista de Autocontroles: nuevo orden de columnas: **Línea, Orden de fabricación (con el icono de alerta
  delante), Tiempo sin control, Control, Fecha y hora, Estado, Resultado y Realizado por**. El icono ocupa
  un hueco fijo para que los números de orden queden alineados. «Fecha y hora» se mantiene (no estaba en la
  lista pedida; se puede quitar si no hace falta).

## 0.19.2 — 2026-10-30
- Lista de Autocontroles: el **icono de alerta es más grande** (1,9 rem) y hay una columna nueva, **Tiempo
  sin control (hh:mm)**, con el tiempo desde el último autocontrol de cada orden (`03:50`, `26:05`; en rojo
  desde las 4 horas). Solo sale en la fila del último autocontrol de una orden **en fabricación con algún
  autocontrol registrado**; no en órdenes finalizadas ni sin ninguno registrado (decisión de Alberto,
  30/10/2026). El icono de alerta sigue la misma regla para no contradecirse.
- 8 pruebas nuevas (107 de 107 correctas). Guía de usuario actualizada.

## 0.19.1 — 2026-10-30
- La lista de Autocontroles se actualiza sola **cada minuto** (antes cada 10), para que el icono de alerta
  de las 4 horas salga casi a la hora exacta. Es la constante `MinutosRefresco_wys` de
  `ListadoAutocontroles.razor`. Cada refresco repite la última búsqueda; la guía de usuario lo recoge.

## 0.19.0 — 2026-10-30
- **Icono de alerta en la lista de Autocontroles.** La fila del último autocontrol de cada orden que sigue
  en fabricación muestra el icono corporativo de alerta si lleva **4 horas o más** sin un autocontrol nuevo
  (al pasar el ratón: «Hace 5 h 10 min sin un nuevo autocontrol»). No sale en órdenes finalizadas ni en
  autocontroles anulados. La lista **se actualiza sola cada 10 minutos**, repitiendo la última búsqueda.
- **Una sola orden en fabricación por línea, con aceptación expresa.** Al pulsar «Crear autocontrol» de una
  orden nueva en una línea que ya tiene otra orden en fabricación, la pantalla pide confirmar («Sí, finalizar
  y crear el autocontrol» / «No, volver»). El servidor también lo exige (`CrearAsync(…, aceptaCierre)`).
  La regla se aplica ahora a **todas** las órdenes en fabricación de la línea, tengan o no autocontroles
  registrados (antes solo a las que tenían alguno registrado, y podía haber dos abiertas).
- **Se borran los autocontroles sin registrar** de la orden que se finaliza, tanto en el cierre automático
  como en «Finalizar orden» (con su aviso del número que se borrará): ya no se podrían registrar. Los
  registrados no se tocan. Una orden que se queda sin autocontroles queda vacía y finalizada. Cada borrado se
  apunta en el diario de seguridad (`EliminacionAutocontrol`).
- Efecto a tener en cuenta: el cuadro de trazabilidad «Autocontroles completados» ya no contará como
  incompletos los que se borran al cerrar la orden.
- 13 pruebas nuevas (`PruebasCierreDeLinea.cs`); 99 de 99 correctas. Guía de usuario, arquitectura y
  seguridad actualizadas.

## 0.18.6 — 2026-10-30
- Pantalla de entrada: el logo nuevo lleva fondo blanco, así que va sobre una tarjeta blanca redondeada
  del ancho del formulario (antes se veía como un rectángulo de esquinas vivas sobre la foto). Es igual
  de día y de noche, por lo que se quita la regla que solo lo hacía de noche.

## 0.18.5 — 2026-10-30
- **Nuevo logo de Visual Inspector** (ojo con lupa que compara dos muestras, en azul y rojo WYS). Se
  sustituye en todos los sitios donde se usaba, en `recursos/app/`: el logo completo de la pantalla de
  entrada (`visual-inspector-logo.png`), el símbolo de la cabecera (`visual-inspector-simbolo.png`, ahora
  rectangular, sin deformar), el icono de la pestaña (`favicon-32.png`) y el icono de apple-touch
  (`icono-180.png`). Recortados sin márgenes y con fondo blanco, así que de noche siguen sobre la placa
  blanca. El logo de WYS no se toca.

## 0.18.4 — 2026-10-26
- Autocontrol, comparación: al cambiar la tolerancia **con validaciones hechas** sale un aviso («Has
  validado N diferencias (M incidencias)… ¿Cambiar de todas formas?») con «Sí, cambiar» y «No, seguir
  con…». La tolerancia no cambia hasta confirmar. Sin validaciones, el cambio sigue siendo inmediato.
- Un cuadro azul junto al resultado indica **con qué tolerancia se ha comparado** y hacia dónde afinar
  (demasiadas diferencias: más amplia; muy pocas: más estricta).
- No se conservan las validaciones al cambiar de nivel: cada comparación numera las diferencias de
  nuevo y una validación podría quedar pegada a otra diferencia distinta.
- Guía de usuario actualizada.

## 0.18.3 — 2026-10-26
- El icono de ayuda cambia de «?» en un círculo azul a un dibujo vectorial: círculo gris claro con borde
  rojo oscuro e interrogante rojo con contorno (rojo y rojo oscuro de WYS), como la imagen de referencia.
  Con el panel abierto lleva un aro azul; al pasar el ratón se ilumina un poco.

## 0.18.2 — 2026-10-26
- El botón «Ayuda» de la barra pasa a ser un **icono de interrogante** (círculo con «?») junto al
  conmutador de modo diurno y nocturno. Abre y cierra el panel flotante; en las pantallas no
  interactivas lleva a `/ayuda`. Ya no hay un texto «Ayuda» entre las secciones.

## 0.18.1 — 2026-10-26
- La **Ayuda** pasa a ser un **panel flotante**: «Ayuda» en la barra lo abre encima de la pantalla
  actual, sin fondo que la bloquee. Se mueve arrastrando su cabecera, se cambia de tamaño con la
  esquina inferior derecha y se cierra con la ✕. Sigue abierto al cambiar de pantalla y recuerda la
  búsqueda; la posición se recuerda mientras la pestaña siga abierta.
- Dentro, el buscador y los apartados como secciones plegables (al buscar se abren los que coinciden).
  El enlace «Pantalla completa» lleva a `/ayuda`, que se mantiene.
- En las pantallas que no son interactivas («Mis datos», «Cambiar contraseña») el botón «Ayuda»
  sigue llevando a `/ayuda`.
- Piezas nuevas: `EstadoAyuda` (por sesión), `AyudaFlotante.razor` y `wwwroot/js/ayuda-flotante.js`
  (script propio; la CSP no cambia).

## 0.18.0 — 2026-10-26
- Nueva pantalla **Ayuda** (`/ayuda`, opción «Ayuda» en la barra): muestra la guía de usuario con un
  índice a la izquierda y un buscador. Al escribir se enseñan solo los apartados que contienen todas las
  palabras (sin distinguir mayúsculas ni acentos); primero los que las llevan en el título.
- La guía es un único archivo, [guia-usuario.md](guia-usuario.md): se edita ahí y la pantalla se
  actualiza sola, sin recompilar. Para que llegue a la aplicación, el `.csproj` y el `.dockerignore`
  tienen una excepción solo para ese archivo (el resto de `doc/` sigue sin publicarse).
- La puede ver cualquiera con la sesión iniciada, aunque no tenga permisos; sin sesión redirige a la entrada.
- Biblioteca nueva: **Markdig 1.5.0** (BSD-2-Clause), con el HTML en bruto desactivado. Añadida a
  `generar-versiones.ps1` y regenerado `recursos/versiones.json`.
- Servicio nuevo `ServicioAyuda` (una sola instancia, relee el archivo si cambia).

## Documentación — 2026-10-26
- Nueva [guía de usuario](guia-usuario.md) en lenguaje de operario: cómo hacer un autocontrol paso a
  paso, tolerancias, incidencias, órdenes, informe, trazabilidad, impacto y preguntas frecuentes.
  Pensada también como base de una futura ayuda dentro de la aplicación (sin tocar código de momento).

## 0.17.0 — 2026-10-26
- Nueva opción **Administración → Limpiar datos de prueba** (`/administracion/limpieza`) para dejar
  la base de datos limpia al pasar de las pruebas a lo real. Solo administrador.
  - Se buscan órdenes por fecha de creación (desde / hasta) y se marcan una o varias.
  - Borrar una orden borra también todos sus autocontroles, diferencias y validaciones.
  - Tres pasos: elegir órdenes, escribir la contraseña del administrador conectado (se pide siempre,
    sea quien sea el que creó la orden) y un último aviso «¿Está seguro?» con lo que se va a borrar.
  - El servidor (`ServicioLimpieza`) vuelve a comprobar permiso y contraseña; una contraseña
    incorrecta avisa con «Contraseña incorrecta» y deja reintentar, sin bloquear la cuenta; tras
    3 fallos seguidos hay una pausa de 38 segundos (constantes `FallosAntesDePausa_wys` y `SegundosPausa_wys`). El borrado es una sola operación: todo o nada.
  - Queda apuntado en el diario de seguridad (acción nueva `LimpiezaDatos`): quién, qué órdenes y cuántos
    autocontroles y diferencias; nunca la contraseña. Máximo 200 órdenes de una vez.
  - Es una excepción a «los autocontroles no se borran», pensada para el paso de pruebas a producción.
    Conviene hacer antes una copia de la base de datos.

## 0.16.7 — 2026-10-26
- Conmutador de modo **sin texto** y aún más estrecho (unos 58 px): solo la píldora con el sol (día)
  o la luna (noche), como en la imagen de referencia. Sigue teniendo la etiqueta «Cambiar entre
  modo diurno y nocturno» al pasar el ratón y para los lectores de pantalla.

## 0.16.6 — 2026-10-26
- Conmutador de modo más estrecho (de 164 a 120 px) y sin la palabra «Modo»: solo «Diurno» o
  «Nocturno». El texto de ayuda al pasar el ratón sigue diciendo «Cambiar entre modo diurno y nocturno».

## 0.16.5 — 2026-10-26
- El botón de modo diurno / nocturno pasa a ser un **conmutador** (píldora con una bola que se
  desliza): de día, sol a la izquierda sobre azul claro y «Modo diurno» a la derecha; de noche,
  luna a la derecha sobre azul noche y «Modo nocturno» a la izquierda. Ahora el texto dice en qué
  modo se está, no a cuál se pasa. Los colores están en `recursos/tema.css` (`--wys-conmutador-*`).

## 0.16.4 — 2026-10-26
- Nuevo menú **Calidad** en la barra, con **Trazabilidad** e **Impacto productivo** dentro (antes
  eran dos secciones sueltas). Es un desplegable como el de Administración, que se abre hacia la
  derecha; el botón se ve en negrita cuando se está en una de sus pantallas. Las direcciones y los
  permisos no cambian (`/trazabilidad`, `/impacto`, «Autocontroles.Ver»).

## 0.16.3 — 2026-10-26
- La línea descriptiva del pie empieza ahora con «AutoControl Retenido Para Inspección (RPI)» en
  lugar de «Autocontrol visual de impresión».

## 0.16.2 — 2026-10-26
- La descripción de la aplicación («Autocontrol visual de impresión: compara el patrón aprobado
  (PDF) con las dos muestras escaneadas y resalta las diferencias») vuelve como una línea pequeña
  justo encima del pie, en todas las pantallas con la sesión iniciada. Va en el flujo normal de la
  página, no flota, así que no tapa nada.

## 0.16.1 — 2026-10-26
- **Se quita la sección «Inicio».** Solo repetía lo que ya está en Autocontroles. Ya no aparece en
  la barra; al entrar (o al pulsar el logo) se abre directamente el listado de autocontroles. La
  dirección raíz sigue existiendo y redirige a Autocontroles, así que los enlaces antiguos y «Volver
  al inicio» siguen funcionando. Se borran los estilos de la antigua portada.

## 0.16.0 — 2026-10-26
- **Impacto productivo.** Cuando el operario marca «Parada de línea» o «Retención de producto» en
  una incidencia, apunta los **minutos de parada** y los **tubos retenidos**, aproximados
  (obligatorios en ese caso; entre 1 y 10.080 minutos y entre 1 y 9.999.999 tubos).
- **Se pueden corregir después.** En la ficha del autocontrol, botón «Corregir» junto a esas cifras
  (permiso «Autocontroles.Realizar», también con la orden finalizada). Es lo único de un registro
  que se puede cambiar: la detección y la decisión no. Quién y cuándo queda en `ModificadoPor` y el
  diario de seguridad guarda el valor de antes y el de después (`CorreccionImpacto`).
- **Pantalla nueva «Impacto»** (menú de la barra, `/impacto`): totales del periodo (minutos de
  parada, tubos retenidos, incidencias, con parada, con retención, órdenes con incidencias), con
  filtro por fechas (por defecto, los últimos 30 días) y por línea, y rankings de más a menos
  impacto por **orden, línea, diseño (litografía) y tipo de defecto**. Se calcula en el momento
  con lo registrado: cada autocontrol que se registra la actualiza sola.
- El cuadro de trazabilidad de cada orden y su listado muestran también los minutos de parada y
  los tubos retenidos; el informe de fabricación (pantalla y PDF) los incluye en cada incidencia.
- Migración `ImpactoProductivoIncidencia`: dos columnas vacías nuevas en las diferencias.
- Fuera de esta fase, por decisión de Alberto: análisis por cliente (otra fase) y posición de impresión.

## 0.15.0 — 2026-10-26
- **Impacto de cada incidencia.** Al marcar una diferencia como «Incidencia», el operario indica
  con casillas qué se hizo en producción: sin intervención, ajuste en marcha, parada de línea,
  retención de producto y/o aviso a Calidad. Se puede marcar varias (no es una escala de gravedad);
  «Sin intervención» va sola. Es obligatorio marcar al menos una. Sigue siendo el operario quien
  decide: no hay criticidad automática, ni flujo de autorización de Calidad, ni zonas críticas, ni
  producto regulado (decisiones de Alberto, 26/10/2026). «Aviso a Calidad» es solo informativo.
- Se guarda en cada diferencia (columna `Actuaciones`, migración `ActuacionesIncidencia`). Las
  incidencias anteriores quedan «Sin clasificar». El servidor vuelve a comprobarlo al registrar.
- Se ve en el registro del autocontrol, en el informe de fabricación (pantalla y PDF) y en el
  cuadro de trazabilidad: cuántas incidencias tuvo cada actuación y, si hubo retención de producto,
  la **producción a revisar** (desde el último autocontrol sin incidencias hasta el control en el
  que se retuvo).

## 0.14.1 — 2026-10-26
- Los títulos de columna de todas las tablas (listados, parámetros, usuarios, versiones…) llevan
  ahora un fondo amarillo clarito con texto oscuro; de noche, un amarillo oscuro apagado con texto
  claro. Los colores están en `recursos/tema.css` (`--wys-tabla-cabecera`).

## 0.14.0 — 2026-10-26
- **Trazabilidad.** Opción nueva en la barra, «Trazabilidad»: listado de órdenes cerradas y, al
  elegir una, su cuadro de mando «Calidad de impresión» con autocontroles completados, muestras
  escaneadas, incidencias detectadas, correcciones verificadas y dos barras (eficacia de corrección
  e integridad de autocontroles), más incidencias por motivo y la lista de autocontroles.
- Una incidencia se da por corregida si en el siguiente autocontrol registrado ya no se repite ese
  motivo. Las cifras se calculan con lo ya guardado: sin tablas ni migraciones nuevas.
- **Cierre automático de órdenes.** Al crear un autocontrol de una orden en una línea, la orden
  anterior en fabricación de esa línea se finaliza sola. La pantalla avisa antes de crearlo y el
  cierre queda apuntado en el diario de seguridad.
- Una orden ya se puede finalizar con autocontroles sin registrar (no se sabe cuántos se harán);
  basta con tener al menos uno registrado. Los pendientes de una orden finalizada ya no se pueden
  registrar salvo que el administrador la reabra.
- Sin permiso propio de momento: la trazabilidad usa «Autocontroles.Ver».

## 0.13.4 — 2026-10-26
- El cambio de modo diurno / nocturno pasa de estar dentro del menú con el nombre a ser un botón
  propio en la barra superior, a la derecha, junto a Administración. También se ve en la pantalla
  de entrada (antes no había forma de cambiarlo sin haber iniciado sesión).

## 0.13.3 — 2026-10-26
- Modo nocturno: el logo, los menús y algunos textos no se veían. Causa: `recursos/tema.css` no
  llevaba número de versión y el navegador seguía usando una copia antigua (sin los colores
  nocturnos) junto al resto de estilos nuevos, así que la cabecera quedaba blanca con logo y menús
  blancos, y los textos azules sobre fondo oscuro. Ahora se pide como `tema.css?v=<versión>`, y se
  descarga de nuevo con cada actualización de la aplicación.

## 0.13.2 — 2026-10-26
- Modo nocturno: el fondo con la ilustración (patrón y muestra) es claro y el velo oscuro que lo
  cubría era demasiado suave, así que había texto claro sobre zonas claras. Ahora el velo es casi
  opaco (`--wys-velo-opacidad` en `recursos/tema.css`) y el fondo queda oscuro de forma uniforme:
  texto claro sobre fondo oscuro en todas las pantallas. El modo diurno no cambia.
- Revisadas con capturas reales en modo nocturno: entrada, inicio, Administración, usuarios y su
  ficha, parámetros, registros, versiones, órdenes y nuevo autocontrol.

## 0.13.1 — 2026-10-26
- Modo nocturno: corregidos textos y botones que se leían mal.
  - Botones con solo borde (por ejemplo «Correcta», «Incidencia», «Descargar»): ahora usan colores
    claros de noche.
  - Textos de apoyo en gris y avisos en rojo o verde: más claros y con contraste suficiente.
  - Logos de la aplicación (pantalla de entrada y cabecera): llevan letras azul oscuro, así que de
    noche se ponen sobre una placa blanca; el dibujo no se toca.
  - Aviso de «conexión perdida»: usaba fondo blanco fijo con texto claro; ahora sigue el modo.

## 0.13.0 — 2026-10-26
- **Modo diurno y nocturno.** En el menú con el nombre de la persona (arriba a la derecha) hay una
  opción nueva, «Modo nocturno» (o «Modo diurno» si ya está activo). La elección se recuerda en el
  navegador de cada persona (`localStorage`); no se guarda nada en el servidor.
- De noche la cabecera pasa a azul oscuro con el logo negativo de WYS (sin modificarlo), y los
  fondos, textos y azules de lectura se aclaran u oscurecen para mantener el contraste. Los colores
  del modo nocturno están en `recursos/tema.css` (bloque `data-bs-theme="dark"`).
- Decisión de diseño: el manual de identidad WYS solo define el modo diurno; el nocturno es una
  variante añadida a petición. El modo diurno queda exactamente igual que antes.
- La pantalla de entrada también sigue la elección guardada (no tiene menú para cambiarla).
- Script nuevo `wwwroot/js/modo-visual.js`, cargado en la cabecera de `App.razor` para evitar el
  parpadeo en blanco. No afecta a la seguridad (CSP sin cambios).

## 0.12.5 — 2026-10-26
- Publicación detrás de **Nginx Proxy Manager** (producción, con dominio y certificado): con
  `USAR_CABECERAS_PROXY=true` y `PROXY_CONFIABLE` en el `.env`, la aplicación sabe que el usuario
  entra por https (las cookies se marcan «solo https») y el diario de seguridad guarda la IP real
  de cada persona. Solo se aceptan las cabeceras del proxy indicado; las de cualquier otro equipo
  se ignoran. Apagado por defecto (en el PC de desarrollo todo sigue igual).
- La cookie que protege los formularios se configura de forma explícita: no la lee JavaScript,
  no viaja a otras webs y solo viaja por https cuando se entra por https.
- Pruebas de seguridad: 86 de 86 correctas.

## 0.12.4 — 2026-10-26
- Corrección: en producción, al entrar con Chrome la sesión se abría pero salía el aviso «La
  página había caducado». Chrome enviaba el formulario de entrada dos veces y el segundo envío
  llegaba con la sesión ya abierta y el token anterior. Ahora, si llega un formulario de entrada
  caducado de alguien que ya tiene la sesión abierta, se le lleva a la aplicación sin aviso (no
  se procesa nada del formulario). Y si alguien con la sesión abierta abre la pantalla de
  entrada, se le lleva a la aplicación.

## 0.12.3 — 2026-10-23
- Corrección: en Chrome, entrar podía dar **error 400**. Al restaurar pestañas o volver atrás,
  Chrome recupera la página de entrada de su memoria con un token antiguo que ya no casa con su
  cookie (Firefox vuelve a pedir la página y no le pasaba). Ahora, si el formulario de entrar
  llega con el token caducado, se vuelve a la entrada con un token nuevo y el aviso «La página
  había caducado»; y si es el de cerrar sesión, la sesión se cierra igualmente (antes podía
  quedarse abierta). La protección contra formularios de otras webs sigue igual y el rechazo
  se apunta en el diario de seguridad (`TokenFormularioNoValido`).
- Pruebas de seguridad: 81 de 81 correctas.

## Documentación — 2026-10-23
- Nueva guía [guia-master.md](guia-master.md) para el Máster de IA: el MVP, Claude como copiloto
  en VS Code, prompting, Obsidian, lenguajes y librerías, visión artificial, Docker, seguridad y
  testing, decisiones y lecciones aprendidas.

## 0.12.2 — 2026-10-23
- Pantalla de entrada: nota «(TFM) > app developed with AI» arriba a la derecha, y el título
  «Entrar» pasa a «¡Bienvenido!» (el botón sigue diciendo «Entrar»).
- Nuevo Autocontrol RPI: la lista de sugerencias se titula «Órdenes de fabricación abiertas».

## 0.12.1 — 2026-10-23
- Nuevo Autocontrol RPI: arriba salen las **órdenes en fabricación** (activas, sin finalizar y
  con algún autocontrol; las 10 más recientes) con su línea, su litografía, cuánto hace del
  último control y qué control toca. En amarillo si ya han pasado las 4 horas recomendadas.
  Un clic rellena la orden y la línea; también se puede seguir escribiendo una orden nueva.
- Pruebas de seguridad: 80 de 80 correctas.

## 0.12.0 — 2026-10-23
- **Línea de fabricación** (L1 … L7) en la orden, para los futuros KPI de eficiencia
  operativa. En Nuevo Autocontrol RPI se elige en un desplegable, es obligatoria y, si la orden
  ya tiene autocontroles, viene elegida la de su último control. La orden se queda con la
  línea elegida.
- La lista de líneas es un parámetro (`Lineas` = L1|L2|…|L7) que el administrador puede ampliar
  en Administración → Parámetros (solo letras y cifras, sin repetir). El servidor solo acepta
  líneas de la lista.
- La línea se ve en los listados de Autocontroles y de Órdenes de Fabricación, en las fichas de
  la orden y del autocontrol y en el informe de fabricación (pantalla y PDF).
- Base de datos: columna `Linea` en `OrdenesFabricacion` (migración AnadeLineaOrden).
- Pruebas de seguridad: 78 de 78 correctas.

## 0.11.8 — 2026-10-23
- Nuevo Autocontrol RPI: el campo de la orden viene relleno con el inicio habitual de las órdenes
  («10000») y el operario escribe a continuación el resto. Se puede cambiar como cualquier texto
  si una orden empieza distinto; el servidor sigue exigiendo las 10 cifras.
- El inicio habitual se puede cambiar en Administración → Parámetros (`PrefijoOrden`): solo
  cifras, como mucho 9. Vacío = el campo sale en blanco.
- Buscadores de Autocontroles y de Órdenes de Fabricación: basta con escribir las últimas cifras
  de la orden. La explicación del buscador de autocontroles pasa debajo del campo.
- En los buscadores de Órdenes de Fabricación y de Autocontroles, el campo de la orden también
  viene relleno con el inicio habitual; al abrir cada pantalla se sigue viendo la lista completa.
- Pruebas de seguridad: 69 de 69 correctas.

## 0.11.7 — 2026-10-23
- «Nuevo autocontrol» pasa a llamarse «Nuevo Autocontrol RPI» (caja de Inicio, botón del
  listado de autocontroles y título de la pantalla).
- En el menú de la cabecera, «Órdenes» pasa a llamarse «Órdenes de Fabricación».

## 0.11.6 — 2026-10-23
- Cabecera: Inicio, Autocontroles y Órdenes van junto al logo de Visual Inspector; a la derecha
  quedan Administración (solo el administrador) y el menú de la persona conectada.
- La fecha, la hora y el turno pasan al pie, abajo a la izquierda (se ven también en la pantalla
  de entrada, con el día de la semana siempre).

## 0.11.5 — 2026-10-23
- Se quitan las **carpetas por defecto** (pantalla de Administración, «Elegir de la carpeta de
  muestras», explorador «Examinar…» y su conexión en Docker): el navegador no deja abrir
  «Elegir archivo» en una carpeta indicada por la aplicación, así que no tenían sentido
  (decisión de Alberto, 23/10/2026). Se mantiene «Elegir archivo» con la carpeta recordada
  de la 0.11.3.
- Base de datos: se borran los parámetros `CarpetaPatrones` y `CarpetaMuestras`
  (migración QuitaCarpetasPorDefecto).
- Pruebas de seguridad: 65 de 65 correctas.

## 0.11.4 — 2026-10-23
- Se quita «Buscar en la carpeta de patrones» del autocontrol: el patrón se elige con «Elegir
  archivo», que recuerda la carpeta de patrones. Las muestras mantienen «Elegir de la carpeta
  de muestras».

## 0.11.3 — 2026-10-23
- «Elegir archivo» en el autocontrol recuerda la carpeta: el del patrón abre el explorador de
  Windows en la última carpeta de patrones usada, y los de las muestras en la última de
  muestras. La primera vez se abre en «Mis documentos». Funciona en Edge y Chrome cuando la
  aplicación se usa por https (o en el propio equipo); si no, sale el explorador de siempre.
  Script propio `wwwroot/js/elegir-archivo.js`; la CSP no cambia.
- El autocontrol ya no enseña la ruta de la carpeta del servidor al operario.

## 0.11.2 — 2026-10-23
- La opción «Registros» del menú de Administración (y su caja en la portada) pasa a llamarse
  «Registros Err./Seg.».

## 0.11.1 — 2026-10-23
- Carpetas por defecto: botón «Examinar…» que abre un explorador de carpetas dentro de la
  aplicación (entrar, subir y «Elegir esta carpeta») y enseña cuántos PDF e imágenes hay en
  cada una. Solo el administrador, solo carpetas (nunca abre archivos) y solo dentro de las
  carpetas permitidas (`Carpetas:RaicesExaminables`; en Docker, `/carpetas`).
- Pruebas de seguridad: 90 de 90 correctas.

## 0.11.0 — 2026-10-23
- Menú desplegable «Administración» en la cabecera (solo el administrador): Usuarios y permisos,
  Parámetros, Carpetas por defecto, Registros y Versiones y actualizaciones. La portada de
  Administración tiene las mismas cinco opciones.
- **Carpetas por defecto**: el administrador indica la ruta de la carpeta de patrones y la de
  muestras (parámetros `CarpetaPatrones` y `CarpetaMuestras`, que se pueden cambiar cuando
  haga falta) y ve si la aplicación las puede leer. En el autocontrol, cada hueco ofrece
  «Buscar en la carpeta de patrones» (por litografía o nombre) o «Elegir de la carpeta de
  muestras» (las últimas escaneadas primero), además de cargar desde el PC. Solo lectura y sin
  poder salir de la carpeta; los archivos siguen sin guardarse.
- **Registros**: el administrador elige el diario (errores o seguridad) y el día, y lee los
  apuntes del más reciente al más antiguo, con búsqueda de texto. Cada consulta se apunta en
  el diario de seguridad (`ConsultaRegistro`).
- Docker: ejemplo en `docker-compose.yml` para conectar las carpetas en solo lectura;
  `docker-compose.override.yml` local (fuera de GitHub) para probar con las carpetas de prueba.
- Cabecera: el reloj oculta el día de la semana en pantallas de menos de 1400 px para que todo
  quepa a 1280 px; recuadros de carga del autocontrol con fondo blanco.
- Pruebas de seguridad: 82 de 82 correctas (20 nuevas para carpetas y registros).

## 0.10.8 — 2026-10-23
- Inicio: «Nuevo autocontrol» y «Autocontroles realizados» van uno debajo de otro, y la
  presentación de la aplicación pasa a la esquina inferior derecha, discreta.
- Menú de la persona conectada: un botón con su nombre en la cabecera despliega «Mis datos»,
  «Cambiar contraseña» y «Cerrar sesión». Se cierra al pulsar fuera, al elegir una opción o con
  Escape (script propio `wwwroot/js/menu-usuario.js`; la CSP no cambia).

## 0.10.7 — 2026-10-23
- Pruebas automáticas de las normas SEGURIDAD WYS 2026 en `test/VisualInspector.Pruebas`
  (carpeta local, fuera de GitHub y de Docker). Resumen en [pruebas-seguridad.md](pruebas-seguridad.md):
  62 de 62 correctas.
- Corregidos los 2 avisos que encontraron las pruebas: el cifrado responde "no" ante una
  contraseña vacía (antes daba un error interno) y el servidor ya no envía la cabecera
  `Server: Kestrel`.
- `VisualInspector.csproj` excluye la carpeta `test/` para que la aplicación no compile las pruebas.

## 0.10.6 — 2026-10-23
- Inicio: se quita el aviso «En desarrollo · Próximamente…», que ya no era cierto. La caja
  «Nuevo autocontrol» dice ahora «cuando lo consideres (se recomienda cada 4 horas)».

## 0.10.5 — 2026-10-23
- La versión de la aplicación se ve abajo a la derecha, en el pie, solo con la sesión iniciada.
  Se lee sola de `VisualInspector.csproj`, así que no hay que escribirla a mano.

## 0.10.4 — 2026-10-23
- Fondo para todas las pantallas con la sesión iniciada: ilustración de patrón y muestra
  (`recursos/app/fondo-interior.jpg`), fija y con un velo blanco para que textos, tablas y
  recortes se lean igual. La pantalla de entrada mantiene su foto de la fábrica.
- Cabecera ajustada para que, con el reloj, todo quepa en una línea en pantallas de 1280 px: nada
  se parte en dos líneas y el nombre de la persona se corta con «…» si es muy largo.

## 0.10.3 — 2026-10-23
- Pantalla de entrada con una foto de fondo de la fábrica (máquina de impresión y tubos),
  difuminada y con un velo blanco en el centro; el formulario va en una tarjeta blanca y el logo
  de la aplicación, sin fondo, encima.
- Reloj en la barra superior de todas las pantallas: fecha, hora peninsular (Europe/Madrid) que
  se actualiza cada segundo y turno (mañana de 6 a 14 h, tarde de 14 a 22 h, noche de 22 a 6 h).
  Script propio (`wwwroot/js/reloj-turno.js`); la política de seguridad (CSP) no cambia.

## 0.10.2 — 2026-10-23
- Logo propio de la aplicación (carpeta `recursos/app/`): el símbolo va en la cabecera de todas
  las pantallas, junto al nombre y al lado del logo de WYS, que se mantiene igual. En la
  pantalla de entrada se muestra el logo completo, grande, encima del formulario.
- El icono de la pestaña del navegador pasa a ser el símbolo de la aplicación (sustituye al
  cuadrado azul provisional). El informe PDF sigue solo con el logo de WYS.

## 0.10.1 — 2026-10-22
- Botón «ojo» en los campos de contraseña de la pantalla de entrada y de «Cambiar contraseña»:
  al pulsarlo se ve lo escrito y al volver a pulsarlo se oculta. Al enviar el formulario se
  oculta siempre. Script propio (`wwwroot/js/mostrar-password.js`): la política de seguridad
  del navegador (CSP) no cambia.

## 0.10.0 — 2026-10-22
- Los autocontroles ya no van por horas fijas: el operario los hace cuando lo considera. Cada uno
  lleva un número dentro de su orden («Control 1 (inicial)», «Control 2»…) y el tiempo real
  que pasó desde el primero («+3 h 50 min»), en pantallas, listados e informe.
- La recomendación de un autocontrol cada 4 horas es solo un aviso, al crear un autocontrol y
  en la ficha de la orden: «Último autocontrol hace…». Si ya han pasado 4 horas, sale destacado.
- El administrador puede anular o eliminar autocontroles también con la orden finalizada, sin
  reabrirla: el informe se actualiza solo.
- Base de datos: la columna `HorasDesdeInicio` pasa a `NumeroControl` y los controles ya
  hechos se convierten (0 h → 1, 4 h → 2…) (migración RenombraHorasANumeroControl).

## 0.9.0 — 2026-10-22
- El administrador puede **anular** un autocontrol (con motivo obligatorio): sigue en la orden
  marcado «Anulado», con el motivo, quién y cuándo, pero no cuenta en las cifras de la orden, al
  finalizarla ni en el informe. En el informe (pantalla y PDF) aparece en gris con su motivo.
  Un autocontrol anulado ya no se puede registrar.
- El administrador puede **eliminar** un autocontrol erróneo: se borra con todas sus diferencias
  y desaparece de la orden y del informe.
- Las dos acciones están en la ficha del autocontrol, piden confirmación y solo se pueden hacer
  con la orden en fabricación (si está finalizada, primero hay que reabrirla). Las dos se apuntan
  en el diario de seguridad (`AnulacionAutocontrol`, `EliminacionAutocontrol`).
- El tipo del siguiente autocontrol es ahora el último de la orden + 4 h (antes se contaban, y
  tras eliminar uno del medio el siguiente habría chocado con uno existente).
- Base de datos: `EstaAnulado`, `MotivoAnulacion`, `AnuladoPor` y `FechaAnulacion` en
  `Autocontroles` (migración AnadeAnulacionAutocontrol).

## 0.8.0 — 2026-10-22
- El administrador puede reabrir una orden finalizada (botón «Reabrir orden» en la ficha, con
  confirmación) para añadirle más autocontroles. Los nuevos siguen la secuencia de la orden
  (+12 h, +16 h…) y el informe vuelve a ser provisional hasta que se finalice otra vez.
- Quién y cuándo la reabrió queda en la trazabilidad de la orden y en el diario de seguridad
  (evento `ReaperturaOrden`). Sin motivo ni historial aparte (decisión de Alberto, 22/10/2026).
- Si se intenta crear un autocontrol en una orden finalizada, el aviso indica que hay que pedir
  al administrador que la reabra.

## 0.7.0 — 2026-10-22
- Pantalla «Órdenes» (menú): listado con litografía, autocontroles registrados y pendientes, los
  que tienen incidencias, primer y último control y estado. Búsqueda por orden o litografía.
- Ficha de la orden con sus autocontroles en orden de tiempo y botón «Finalizar orden» (con
  confirmación), para quien puede hacer autocontroles. No se puede finalizar si la orden no tiene
  autocontroles o queda alguno sin registrar. Una orden finalizada ya no admite autocontroles nuevos.
- Informe de fabricación en pantalla y en PDF con la marca WYS (logo, Lato, colores y pie
  corporativo): datos de la orden, resumen, autocontroles e incidencias con su comentario, quién
  las validó y su recorte. Antes de finalizar la orden se puede ver, marcado como PROVISIONAL.
- El PDF se descarga desde una dirección propia que vuelve a comprobar el permiso.
- Biblioteca nueva: PDFsharp-MigraDoc 6.2.4 (MIT). Se queda en esa versión: desde la 6.5.0 la
  licencia es AGPL. Se fuerza System.Security.Cryptography.Pkcs 10.0.13 porque la que trae tiene
  fallos de seguridad conocidos.
- Base de datos: estado, fecha de fin y quién finalizó en `OrdenesFabricacion`
  (migración AnadeFinalizacionOrden).

## 0.6.0 — 2026-10-22
- Formulario de validación: cada diferencia se marca como Correcta o Incidencia (comentario
  obligatorio en las incidencias); «Marcar pendientes como correctas» valida un grupo entero de un
  clic, también las diferencias plegadas. Hay que validar todas para poder registrar.
- Botón «Registrar autocontrol»: guarda en una sola operación el patrón (archivo, litografía y
  versión), las huellas SHA-256 de los tres archivos, la tolerancia, las similitudes, el resultado
  (sin diferencias / diferencias correctas / con incidencias) y cada diferencia con su recorte y su
  validación. Después ya no se puede cambiar.
- Si la litografía del patrón no coincide con la de la orden, se avisa y hay que confirmar que el
  patrón es el correcto (queda registrado). La orden toma la litografía de su primer autocontrol.
- No se deja registrar si alguna muestra no se ha podido comparar.
- Ficha de solo lectura de los autocontroles registrados y columna «Resultado» en el listado.
- Base de datos: tabla `Diferencias` y columnas nuevas en `Autocontroles` y `OrdenesFabricacion`
  (migración AnadeRegistroAutocontrol).

## 0.5.0 — 2026-10-22
- Comparación de las dos muestras con el patrón (botón «Comparar con el patrón»): ajuste fino de
  la colocación por zonas, tono igualado tinta por tinta (no se analiza el color) y búsqueda de
  diferencias de contenido con un margen de desajuste.
- Tres motivos: mancha o suciedad, falta elemento o tinta, y desplazamiento o texto doble. Los
  pliegues, los bordes de la pieza y las guías de montaje del patrón no se tienen en cuenta.
- Cada diferencia indica si sale en las dos muestras (posible fallo de plancha) o en una, y trae
  un recorte en color patrón | muestra(s). Se ordenan por evidencia y, de cada motivo, se enseñan
  abiertas solo las 5 más evidentes.
- Tolerancia en tres niveles (Estricta, Normal, Amplia) que el operario cambia en cada
  autocontrol; la inicial la fija el administrador (parámetro nuevo `ToleranciaPorDefecto`).
- Parámetros de tipo lista (desplegable en Administración → Parámetros).
- Los mismos archivos dan siempre el mismo resultado (semilla fija en los pasos al azar).

## 0.4.0 — 2026-10-22
- La litografía (L + 7 cifras) y la versión (R + cifras) se leen del principio del nombre del
  patrón. Si el nombre no sigue el formato, se avisa y se escriben a mano.
- Botón «Localizar muestras en el patrón»: recorta cada pieza del fondo del escáner y la encuentra
  en la página del patrón con cualquier giro, aunque el tubo se haya abierto por otro punto
  (dos trozos). Muestra el giro, los trozos y el patrón con la muestra superpuesta, en color.
- Las muestras pueden ser también TIFF.
- Visión artificial con OpenCvSharp 4.13 (Apache 2.0). La imagen de Docker instala las bibliotecas
  del sistema que necesita y solo lleva lo de Linux.
- En Windows (equipo de desarrollo) OpenCV trabaja con un solo hilo: con varios se cerraba de golpe.
- Panel de versiones: cada pieza (bibliotecas, .NET y MariaDB) explica para qué se usa en el
  proyecto y cuál es su licencia.

## 0.3.0 — 2026-10-22
- Ficha del autocontrol: carga desde el PC del patrón (PDF) y de las dos muestras (JPG o PNG a
  300 ppp, en cualquier orientación), con su vista en pantalla, tamaño en milímetros y huella SHA-256.
- El PDF se convierte en imagen a 300 ppp (biblioteca PDFtoImage). Se propone la versión del
  patrón si el nombre del archivo la incluye (por ejemplo R002).
- Los archivos no se guardan: se revisan en memoria y se descartan al salir de la pantalla.
- Carpetas locales `muestras-prueba/` y `patrones/` para pruebas, excluidas de Git y de Docker.

## Sin versión — 2026-10-21
- Puerto de acceso desde fuera de Docker cambiado del 8080 al 8082 (`docker-compose.yml`). Dentro
  del contenedor la aplicación sigue en el 8080. Ahora se abre <http://localhost:8082>.

## 0.2.1 — 2026-10-21
- Nuevo autocontrol: quitado el número de ejemplo del campo de la orden (parecía una orden ya
  escrita). Debajo del campo se explica el formato: exactamente 10 cifras, solo números.

## 0.2.0 — 2026-10-21
- Añadidas las órdenes de fabricación (10 cifras, por ejemplo 1000063961) y los autocontroles.
- Pantallas: listado de autocontroles con búsqueda por orden, nuevo autocontrol y ficha.
- El tipo de control se calcula solo (Inicial, +4 h, +8 h…) y la fecha y hora las pone el
  servidor. Una orden no puede tener dos controles del mismo tipo.
- Inicio con accesos directos a "Nuevo autocontrol" y "Autocontroles realizados".

## 0.1.1 — 2026-10-21
- Corregido: los botones de las pantallas interactivas (por ejemplo "Guardar permisos") no hacían
  nada. La imagen de Docker no incluía el programa de Blazor (`_framework/blazor.web.js`) porque el
  `Dockerfile` compilaba con `--no-restore`. Ahora vuelve a resolver los paquetes con todo el código.
- Las claves que firman las sesiones se guardan en el volumen `claves-app`: reiniciar o actualizar
  la aplicación ya no cierra las sesiones ni da errores de formulario caducado.

## 0.1.0 — 2026-10-21
- Adaptado el proyecto a las normas WYS (skill wys-code, ámbito Interno WYS).
- Creado el esqueleto de la aplicación: C# · .NET 10 · Blazor · MariaDB · Docker.
- Entrada con usuario y contraseña, bloqueo tras 3 intentos y cambio obligatorio de la
  contraseña temporal.
- Permisos por funcionalidad con denegación por defecto (`Autocontroles.Ver`,
  `Autocontroles.Realizar`).
- Administración: usuarios, permisos, parámetros y versiones.
- Registros en `log/` (err-log y Log-Wys-Seg) e identidad visual WYS.
- Documentación técnica en `doc/`; las notas del Máster pasan de `docu/` a `doc/master/`.
- Excepción temporal: capa de datos con Entity Framework 9 + Pomelo 9 hasta que exista Pomelo 10.
