# Guía de usuario de Visual Inspector

Guía para quien hace los autocontroles en planta. Explica, con palabras sencillas, qué hace la
aplicación y cómo usarla. Si algo de esta guía no coincide con lo que ves en pantalla, avisa a IT.

## Qué es Visual Inspector

Es una ayuda para el autocontrol visual de la impresión de los tubos (AutoControl Retenido Para
Inspección, RPI). Compara el **patrón** aprobado (un PDF) con las **dos muestras** escaneadas de
cada control y te enseña dónde hay diferencias.

La aplicación **no aprueba ni rechaza** el producto: solo señala diferencias. La decisión es
siempre tuya, y la tomas marcando cada diferencia como correcta o como incidencia.

## Qué puedo hacer yo

Depende de los permisos que te haya dado el administrador. Si no tienes ninguno, no verás nada.

| Permiso | Qué permite |
|---|---|
| Ver autocontroles | Consultar autocontroles, órdenes, informes, trazabilidad e impacto productivo |
| Realizar autocontroles | Todo lo anterior, y además crear autocontroles, validarlos, finalizar órdenes y corregir cifras de impacto |
| Administración | Solo administradores: usuarios, parámetros, anular o eliminar autocontroles, reabrir órdenes |

Si pulsas algo y la aplicación dice que no tienes permiso, pídeselo a un administrador.

## Cómo entro y cómo cambio mi contraseña

1. Entra con tu usuario y tu contraseña. El botón del ojo permite ver lo que escribes.
2. La primera vez te pedirá cambiar la contraseña temporal. Es obligatorio.
3. Para cambiarla más adelante, pulsa tu nombre (arriba a la derecha) y elige **Cambiar contraseña**.
   Ahí también está **Mis datos** y **Cerrar sesión**.

Si fallas la contraseña 3 veces seguidas, la cuenta se bloquea durante 15 minutos. Si no quieres esperar, pide a un
administrador que la desbloquee.
Si dejas la aplicación 30 minutos sin usarla, la sesión se cierra sola y tendrás que entrar de nuevo.

## Cómo está organizada la aplicación

En la barra de arriba tienes:

- **Autocontroles**: la lista de todos los autocontroles hechos, con buscador. Aquí está el botón
  «Nuevo Autocontrol».
- **Histórico**: la lista de órdenes de fabricación y su ficha.
- **Calidad**: un menú con **Trazabilidad** e **Impacto productivo**.
- **Administración**: solo si eres administrador.
- Un conmutador con un sol y una luna para cambiar entre **modo diurno y nocturno**. Se recuerda
  en tu navegador.

Abajo se ven la fecha, la hora y el turno (mañana de 6 a 14 h, tarde de 14 a 22 h y noche de
22 a 6 h) y la versión de la aplicación.

## Cómo hago un autocontrol, paso a paso

Un autocontrol son tres pasos: crearlo, cargar los archivos y validar las diferencias.

### Paso 1. Crear el autocontrol

1. Entra en **Autocontroles** y pulsa **Nuevo Autocontrol**.
2. Elige la orden. Arriba sale la **Planificación de producción**, con una columna por línea
   (Línea 1, Línea 2… todas en la misma fila). En cada línea ves:
   - **En verde**, la orden que está cargada ahora en la máquina, con una barra que se va llenando
     según los tubos **fabricados** sobre la **cantidad** de la orden, y los **pendientes** (solo
     informativos).
   - Debajo, con el borde azul, las órdenes de esa línea que **siguen abiertas** en Visual
     Inspector y hay que cerrar (ver «Órdenes abiertas pendientes de cerrar»).
   - Al pie, **Siguientes (n)**: despliégalo para ver las órdenes que vienen detrás, en el orden en
     que se fabricarán. También puedes elegir una de ellas.
   - En las órdenes en verde y en las abiertas pone **cuántos autocontroles lleva**, cuánto hace del
     último y cuál toca ahora (por ejemplo **RPI 3**).
   - Cada orden muestra su número (**OF:** 1000064262), debajo su **especialidad** (por ejemplo
     E2201359, solo como información) y su descripción.
   - Arriba a la derecha hay una pequeña **leyenda** que explica cada color.
   - Las órdenes que **ya tienen autocontroles** salen con el fondo **amarillo**. La de la máquina
     conserva el borde verde a la izquierda, y las abiertas, el azul.
   - Pulsa una orden y se rellenan solas la orden y la línea. La orden elegida se marca **en rojo**.
   - Arriba se indica de cuándo son los datos de producción y cuándo se leyeron. La aplicación los
     lee sola cada 15 minutos (lo pone junto a la hora de lectura) y la pantalla se pone al día sola
     cada minuto; si quieres los últimos, pulsa **Actualizar ahora**.
   - Si sale el aviso «No hay conexión con la planificación», ves la última que se leyó: comprueba
     que la orden es la correcta y elige la línea a mano.

   Si la planificación no está disponible, en su lugar salen las **órdenes de fabricación
   abiertas**: pulsa una y se rellenan solas la orden y la línea. Si el último control de una orden fue hace 4 horas o más, sale
   destacada en amarillo porque ya toca otro. Lo mismo ves en la lista de **Autocontroles**: una columna
   **Tiempo AC** (tiempo desde el último autocontrol) dice cuánto hace del último autocontrol de cada
   orden en fabricación (por ejemplo `03 h : 50 min`; en rojo desde las 4 horas) y, delante del número de la orden, aparece un
   **icono de alerta** (un triángulo de aviso) cuando ya han pasado 4 horas o más. Solo sale en el último
   autocontrol de órdenes en fabricación que tienen algún autocontrol registrado: no en órdenes
   finalizadas ni en las que aún no tienen ninguno registrado. La lista se actualiza sola cada minuto.
   Las columnas van en este orden: Línea, Orden de fabricación (con el icono delante), Tiempo AC,
   Control, Fecha y hora, Estado, Resultado y Realizado por.
3. Debajo de la planificación está la franja para crear el autocontrol, con la orden, las
   observaciones y el botón **Crear autocontrol**. Se queda fija abajo aunque bajes por la pantalla.
   Si prefieres, escribe la orden a mano en **Orden de fabricación**: son **exactamente 10 cifras, solo
   números**. El campo ya trae escrito el inicio habitual de las órdenes; añade el resto.
4. La **línea** normalmente no hay que elegirla: la pone la aplicación (la de la planificación o, si
   la orden ya no está en ella, la de la orden abierta) y se ve en la columna de la orden y en la
   ventana de confirmación. El campo **Línea** solo aparece en dos casos, y entonces es obligatorio:
   - **Autocontrol fuera de planificación:** escribes a mano una orden que no está en la planificación
     (ni en **Siguientes**) ni abierta. Sale el aviso «No está en la planificación ni abierta: elige la
     línea…». Comprueba primero el número; si es correcto, elige la línea y sigue.
   - **Sin conexión con la planificación** (sale el aviso «No hay conexión con la planificación»).
5. Si quieres, escribe observaciones.
6. Pulsa **Crear autocontrol**. Se abre una ventana con los datos del autocontrol (orden, descripción,
   especialidad, línea, qué RPI será, fecha y hora, y las observaciones) para que los revises:
   - **Continuar** crea el autocontrol.
   - **Cancelar** cierra la ventana sin crear nada, para que corrijas lo que haga falta.
   - Si se va a finalizar otra orden de la misma línea, la ventana lo avisa en rojo (ver «Finalizar
     una orden»).

La fecha y la hora las pone la aplicación. El número del autocontrol también: **RPI 1** es el inicial,
y los siguientes son **RPI 2**, **RPI 3**… Puedes hacer uno cuando lo consideres necesario; se
recomienda uno cada 4 horas, pero es solo un aviso.

### Paso 2. Cargar el patrón y las muestras

Tienes tres huecos: **Patrón (PDF)**, **Muestra 1** y **Muestra 2**.

- El patrón es el PDF aprobado.
- Las muestras son los dos tubos abiertos y escaneados, en JPG, PNG o TIFF, a **300 ppp**. Pueden
  estar en cualquier orientación.
- Cada archivo puede pesar como máximo 50 MB.
- Pulsa **Elegir archivo** en cada hueco. El explorador se abre en la última carpeta que usaste.
- **Patrón desde la carpeta de patrones:** si la orden viene de la planificación, en el hueco del
  patrón salen los PDF de la carpeta de patrones que llevan su especialidad sin la E (por ejemplo,
  para E2201829, los que tienen 2201829 en el nombre). La versión más alta sale primero. Pulsa el que
  corresponda y se carga solo. Si no es ninguno, usa **Elegir archivo** debajo.
- Si la orden está fuera de planificación, o sale «No se ha podido leer la carpeta de patrones», elige
  el patrón con **Elegir archivo**, como siempre.

Los archivos **no se guardan** en la aplicación: se usan para comparar y se descartan al salir de
la pantalla. Lo que queda guardado es una huella de cada archivo (para demostrar qué archivos se
usaron) y un recorte de cada diferencia.

Al cargar el patrón, la aplicación lee del nombre del archivo la **litografía** (la letra L y
7 cifras) y la **versión** (la letra R y unas cifras), por ejemplo `L2201989R002`. Si el nombre no
empieza así, te avisará y tendrás que escribirlas a mano.

### Paso 3. Localizar las muestras

Con los tres archivos cargados, pulsa **Localizar muestras en el patrón**. La aplicación busca cada
muestra dentro del patrón, aunque esté girada o el tubo se abriera por otro punto (en dos trozos).
Tarda unos segundos.

Verás cada muestra superpuesta al patrón. Si los textos se ven nítidos y no dobles, la muestra
está bien colocada. Si una muestra no se localiza, saldrá un aviso: revisa que sea la muestra
correcta y que esté bien escaneada.

### Paso 4. Comparar con el patrón

1. Elige la **tolerancia** (ver más abajo).
2. Pulsa **Comparar con el patrón**. Tarda unos segundos.

Verás el resultado: cuántas diferencias hay de cada tipo, a la izquierda el patrón con las
diferencias numeradas y a la derecha la lista de diferencias agrupadas por motivo.

### Paso 5. Validar cada diferencia

Cada diferencia trae un recorte con el patrón y la muestra. Decides tú:

- **Correcta**: la diferencia no es un problema (por ejemplo, un reflejo del escáner).
- **Incidencia**: es un problema real. Te pedirá más datos (ver «Qué hago cuando marco una incidencia»).

Debes validar **todas** las diferencias para poder registrar. Si hay muchas del mismo grupo y son
correctas, el botón **Marcar pendientes como correctas** las marca de una vez, también las que
están plegadas en «Ver más».

### Paso 6. Registrar el autocontrol

Cuando todo está validado, pulsa **Registrar autocontrol**. Se guarda el resultado, cada
diferencia con su recorte y tu decisión, y las huellas de los archivos. **Después ya no se puede
cambiar** (salvo las cifras de impacto, ver más adelante).

El botón no se activa mientras falte algo; al lado te dice qué: diferencias pendientes,
incidencias sin comentario, sin indicar qué se hizo, o sin minutos o tubos.

## Qué significa cada tolerancia

La tolerancia decide cuántas diferencias señala la aplicación:

| Tolerancia | Qué hace |
|---|---|
| Estricta | Avisa también de diferencias pequeñas o suaves. Verás más diferencias |
| Normal | Equilibrio para un autocontrol rápido. Es la que viene por defecto |
| Amplia | Solo señala lo más claro. Verás menos diferencias |

Puedes cambiarla **después de comparar**: pulsa otro nivel y la aplicación vuelve a comparar. A
veces la norma no detecta las diferencias suficientes o detecta demasiadas, y conviene afinar.

Junto al resultado, un cuadro azul dice **con qué tolerancia se ha comparado** y hacia dónde afinar:
si salen demasiadas diferencias, prueba una tolerancia más amplia; si salen muy pocas, una más estricta.

Cuidado: **al cambiar la tolerancia se pierden las validaciones que ya habías hecho** y empiezas a
validar de nuevo. Si ya has validado alguna diferencia, la aplicación te avisa antes de cambiar:
te dice cuántas has validado y te pregunta **«¿Cambiar de todas formas?»**. Si respondes **No, seguir
con…**, no se pierde nada. Si todavía no has validado ninguna, el cambio es inmediato. Aun así, lo
mejor es decidir la tolerancia antes de empezar a validar. Cuando registras el autocontrol queda
guardada la tolerancia que usaste.

## Qué diferencias detecta y cuáles no

Se compara el **contenido**: lo que hay impreso. Los tres motivos son:

- **Mancha o suciedad**: algo que está en la muestra y no en el patrón.
- **Falta elemento o tinta**: algo que está en el patrón y falta en la muestra.
- **Desplazamiento o texto doble**: algo que está corrido o repetido.

La aplicación **no analiza el color**, ni tiene en cuenta los pliegues, los bordes de la pieza ni
las guías de montaje del patrón.

Cada diferencia dice si sale en **las dos muestras** (puede ser un fallo de la plancha) o **solo en
una**. También indica el tamaño de la zona y los milímetros cuadrados distintos.

De cada motivo se enseñan abiertas solo las 5 más evidentes; el resto están en **Ver más**.

El **parecido con el patrón** (en porcentaje) es solo orientativo. No es un criterio para aceptar
o rechazar.

## Qué hago cuando marco una incidencia

Al pulsar **Incidencia** te pide:

1. **Qué se ha hecho**. Marca todo lo que corresponda:
   - Sin intervención (va sola: si la marcas se quitan las demás).
   - Ajuste en marcha.
   - Parada de línea.
   - Retención de producto.
   - Aviso a Calidad (solo informativo).

   Es obligatorio marcar al menos una.
2. Si marcas **Parada de línea**, los **minutos de parada** (entre 1 y 10.080).
3. Si marcas **Retención de producto**, los **tubos retenidos** (entre 1 y 9.999.999).
   Las cifras pueden ser aproximadas.
4. Un **comentario obligatorio** que explique qué pasa.

Si te equivocas y quieres cambiar una incidencia por correcta antes de registrar, pulsa
**Correcta**: se borra lo que habías marcado.

## Qué pasa si la litografía no coincide con la de la orden

La orden se queda con la litografía de su primer autocontrol registrado. Si en un control
posterior cargas un patrón con otra litografía, la aplicación avisa: puede que hayas elegido el
patrón equivocado.

- Si el patrón es correcto, marca **Confirmo que el patrón es el correcto** para poder registrar.
  La confirmación queda guardada.
- Si no lo es, carga el patrón que corresponde.

## Qué hago si me equivoco

- **Antes de registrar**: puedes cambiar tus decisiones, la tolerancia o los archivos.
- **Después de registrar**: el autocontrol no se puede cambiar. Si se hizo mal, pide a un
  administrador que lo **anule** (queda en la orden, marcado y con su motivo, pero no cuenta) o
  que lo **elimine** (desaparece del todo).
- **Los minutos de parada y los tubos retenidos** sí los puedes corregir después, cuando conozcas
  el dato real: abre la ficha del autocontrol y pulsa **Corregir** junto a esas cifras. Necesitas
  el permiso de realizar autocontroles. Queda apuntado quién lo corrigió y cuándo.

## Órdenes de fabricación

En **Histórico** (la pantalla se titula «Histórico de OFs») ves la lista de órdenes con su litografía, línea, cuántos
autocontroles tienen, cuántos con incidencias y si siguen en fabricación o están finalizadas. Se
busca por el número de la orden (basta con las últimas cifras) o por la litografía.

Al abrir una orden ves todos sus autocontroles en orden, con el tiempo que pasó desde el primero
(por ejemplo «+3 h 50 min»).

### Finalizar una orden

Cuando termina la fabricación, pulsa **Finalizar orden** en su ficha y confirma. Para poder
finalizarla debe tener **al menos un autocontrol registrado**. Una orden finalizada ya no admite
autocontroles nuevos. Los autocontroles que estuvieran **sin registrar se borran** al finalizar,
porque ya no se podrían registrar; la ventana de confirmación te dice cuántos son.

Una línea fabrica una orden a la vez: **al crear un autocontrol de una orden nueva en una línea que ya
tiene otra orden en fabricación, la anterior se finaliza**. La pantalla te avisa antes, y al pulsar
**Crear autocontrol** la ventana de confirmación te dice en rojo qué orden se finalizará (y cuántos
autocontroles sin registrar se borrarán). Si pulsas **Continuar**, lo aceptas; si pulsas
**Cancelar**, no se hace nada. Si la línea tenía más de una orden abierta, se finalizan todas.

### Órdenes abiertas pendientes de cerrar

En **Nuevo Autocontrol**, debajo de la orden en verde de cada línea, salen con el borde azul las
órdenes de esa línea que **siguen abiertas** en Visual Inspector (tienen algún autocontrol y no
están finalizadas) pero no son la que está en curso. Lo normal es que ya estén terminadas.

Para cerrar una, pulsa **Finalizar orden** debajo de ella y confirma con **Sí, finalizar**. Pasa lo
mismo que al finalizarla desde su ficha: ya no admite autocontroles nuevos y se borran los que
estuvieran sin registrar. Si la orden sigue fabricándose, no la finalices: déjala como está. También
puedes pulsarla para hacerle otro autocontrol.

La aplicación nunca las cierra sola. El botón **Finalizar orden** solo sale si la orden tiene algún
autocontrol registrado y si la planificación se ha leído bien hace poco, para no cerrar nada por
error. Si la orden sigue en la planificación como una de las **Siguientes** (por ejemplo, se empezó y
se reprogramó), sale «Sigue en la planificación» y no se propone finalizarla.

### Reabrir una orden

Si una orden finalizada necesita más autocontroles, solo un administrador puede reabrirla
(**Reabrir orden**). Se pide al administrador.

## El informe de fabricación

En la ficha de la orden, el botón de informe abre el **informe de fabricación**: datos de la
orden, resumen, autocontroles, incidencias con su comentario, quién las validó y su recorte, y
las **diferencias validadas como correctas**, también con quién y cuándo las dio por buenas y su
recorte. El resumen indica cuántas diferencias se revisaron, cuántas fueron correctas y cuántas
incidencias. Se puede descargar en **PDF**, con los mismos apartados.

Mientras la orden no está finalizada, el informe sale marcado como **PROVISIONAL**.

## Trazabilidad

En **Calidad → Trazabilidad** ves las órdenes cerradas. Al elegir una, aparece su cuadro de
**Calidad de impresión**: autocontroles completados, muestras escaneadas, incidencias detectadas,
correcciones verificadas, diferencias revisadas y validadas como correctas, y dos barras (eficacia
de corrección e integridad de autocontroles). En la tabla de autocontroles se ven también las
correctas y las incidencias de cada uno.

Una incidencia se da por **corregida** si en el siguiente autocontrol registrado ya no se repite
ese motivo.

Si en una incidencia se retuvo producto, la trazabilidad indica la **producción a revisar**:
desde el último autocontrol sin incidencias hasta el control en el que se retuvo.

### Descargar en Excel o CSV

En la lista de órdenes cerradas, junto al botón **Buscar**, hay dos botones con icono: el de la hoja de
cálculo guarda la lista en **Excel** y el que lleva las letras CSV, en **CSV**. Guardan la lista tal como la
tienes buscada (con los filtros de orden y litografía que hayas aplicado). Solo se pueden pulsar si la lista
tiene órdenes; si está vacía, se ven apagados. Dentro de una orden, el botón de **Excel** guarda su cuadro en un
libro de varias hojas: resumen, incidencias por motivo, impacto, producción a revisar y autocontroles.

El CSV está preparado para abrirlo con doble clic en Excel (con acentos y comas decimales bien). Si el
archivo tiene demasiadas filas, la aplicación lo avisa junto a los botones y hay que afinar los filtros. Cada
descarga queda apuntada en el diario de seguridad.

## Impacto productivo

En **Calidad → Impacto productivo** ves cuánto han costado las incidencias: minutos de parada,
tubos retenidos, número de incidencias y órdenes afectadas. Puedes filtrar por fechas (por defecto,
los últimos 30 días) y por línea. Hay rankings por orden, línea, diseño (litografía) y tipo de
defecto, de más a menos impacto. Se actualiza solo con cada autocontrol que se registra.

### Descargar en Excel o CSV

Junto al botón **Ver** hay dos botones con icono, **Excel** (la hoja de cálculo) y **CSV** (las letras CSV), que
guardan lo que ves, con el mismo periodo y la misma línea. Se apagan si no hay incidencias en el periodo.
El Excel trae varias hojas: resumen, por línea, por litografía, por defecto, órdenes (todas, no solo las
15 primeras) y el **detalle de incidencias**, con una fila por incidencia (orden, línea, litografía,
autocontrol, fecha, motivo, qué se hizo, minutos, tubos, comentario y quién la registró). Ese detalle es lo
más útil para hacer tus propias tablas dinámicas. El CSV contiene solo ese detalle de incidencias.

## Preguntas frecuentes

**No encuentro mi orden en la planificación de producción.**
Mira también en **Siguientes** de su línea y pulsa **Actualizar ahora** por si acaba de cambiar.
Si sigue sin salir, escríbela a mano en **Orden de fabricación** y elige la línea: queda como
autocontrol fuera de planificación. Si crees que la planificación está mal, avisa a quien la prepara
en producción.

**Al pulsar Continuar sale «La planificación ha cambiado» o «Ha cambiado la situación de la línea».**
Mientras tenías abierta la ventana de confirmación, cambió algo: la planificación movió la orden a
otra línea, u otra persona abrió una orden en esa línea. No se ha creado ni cerrado nada. Revisa los
datos y pulsa otra vez **Crear autocontrol**.

**No veo dónde elegir la línea.**
Con la planificación disponible no hace falta: la línea la pone la aplicación según la planificación
(o la de la orden abierta) y la ves en la ventana de confirmación. Solo se pide si escribes una orden
que no está en la planificación ni abierta. Si crees que está mal, pulsa
**Cancelar** y avisa a quien prepara la planificación en producción.

**No sale la planificación de producción.**
Puede que no se haya podido leer o que esté desactivada. Escribe la orden a mano, como siempre. Si
pasa a menudo, avisa a IT.

**No encuentro mi orden en la lista de órdenes abiertas.**
Esta lista sale cuando no hay planificación de producción. Solo muestra las órdenes que siguen en
fabricación y ya tienen algún autocontrol. Si es nueva, escríbela a mano. Si estaba finalizada,
pide a un administrador que la reabra.

**Me dice que la orden está finalizada.**
Ya no admite autocontroles nuevos. Pide a un administrador que la reabra.

**La aplicación dice que el nombre del patrón no se puede leer.**
El nombre del archivo debe empezar por la letra L y 7 cifras, y después la letra R y unas cifras
(por ejemplo `L2201989R002`). Si no es así, escribe la litografía y la versión a mano.

**Una muestra no se localiza en el patrón.**
Comprueba que es la muestra de ese patrón y que está bien escaneada (a 300 ppp, completa y sin
cortes). Si el patrón es el correcto y sigue sin localizarse, avisa a IT.

**Salen demasiadas diferencias.**
Prueba con la tolerancia **Amplia**. Recuerda que al cambiarla pierdes las validaciones hechas.

**No sale ninguna diferencia y creo que debería haberlas.**
Prueba con la tolerancia **Estricta**.

**No me deja registrar.**
Mira lo que aparece junto al botón: diferencias pendientes de validar, incidencias sin comentario,
sin indicar qué se hizo o sin minutos o tubos. También hay que confirmar el patrón si la
litografía no coincide con la de la orden, y no se puede registrar si alguna muestra no se pudo
comparar.

**Me he equivocado en los minutos o los tubos.**
Corrígelos desde la ficha del autocontrol con **Corregir**.

**Me he equivocado en otra cosa de un autocontrol ya registrado.**
Pide a un administrador que lo anule o lo elimine, y haz uno nuevo.

**Salió un mensaje de error con un código.**
Apunta el código (empieza por ERR-) y díselo a IT: con él pueden encontrar el problema. No
necesitas copiar nada más.

**La pantalla dice que se ha perdido la conexión.**
La aplicación intenta reconectar sola. Si no lo consigue, recarga la página; lo que no estuviera
registrado puede perderse.

**Quiero ver la aplicación en modo nocturno.**
Pulsa el conmutador del sol y la luna de la barra superior.

## Si no sé qué hacer

1. Mira el aviso que da la propia pantalla: casi siempre explica qué falta.
2. Consulta esta guía.
3. Si sigue sin resolverse, avisa a IT. Si hay un código de error, díselo.
