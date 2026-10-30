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

Si fallas la contraseña varias veces, la cuenta se bloquea. Pide a un administrador que la desbloquee.
Si estás un rato sin usar la aplicación, la sesión se cierra sola y tendrás que entrar de nuevo.

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
2. Elige la orden. Arriba salen las **órdenes de fabricación abiertas**: pulsa una y se rellenan
   solas la orden y la línea. Si el último control de una orden fue hace 4 horas o más, sale
   destacada en amarillo porque ya toca otro. Lo mismo ves en la lista de **Autocontroles**: una columna
   **Tiempo AC** (tiempo desde el último autocontrol) dice cuánto hace del último autocontrol de cada
   orden en fabricación (por ejemplo `03 h : 50 min`; en rojo desde las 4 horas) y, delante del número de la orden, aparece un
   **icono de alerta** (un triángulo de aviso) cuando ya han pasado 4 horas o más. Solo sale en el último
   autocontrol de órdenes en fabricación que tienen algún autocontrol registrado: no en órdenes
   finalizadas ni en las que aún no tienen ninguno registrado. La lista se actualiza sola cada minuto.
   Las columnas van en este orden: Línea, Orden de fabricación (con el icono delante), Tiempo AC,
   Control, Fecha y hora, Estado, Resultado y Realizado por.
3. Si es una orden nueva, escríbela: son **exactamente 10 cifras, solo números**. El campo ya trae
   escrito el inicio habitual de las órdenes; añade el resto.
4. Elige la **línea** (L1, L2…). Es obligatoria.
5. Si quieres, escribe observaciones.
6. Pulsa **Crear autocontrol**.

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
tiene otra orden en fabricación, la anterior se finaliza**. Al pulsar **Crear autocontrol** la
aplicación te dice qué orden se finalizará (y cuántos autocontroles sin registrar se borrarán) y te
pide que lo aceptes con **Sí, finalizar y crear el autocontrol**. Si pulsas **No, volver**, no se
hace nada. Si la línea tenía más de una orden abierta, se finalizan todas.

### Reabrir una orden

Si una orden finalizada necesita más autocontroles, solo un administrador puede reabrirla
(**Reabrir orden**). Se pide al administrador.

## El informe de fabricación

En la ficha de la orden, el botón de informe abre el **informe de fabricación**: datos de la
orden, resumen, autocontroles e incidencias con su comentario, quién las validó y su recorte.
Se puede descargar en **PDF**.

Mientras la orden no está finalizada, el informe sale marcado como **PROVISIONAL**.

## Trazabilidad

En **Calidad → Trazabilidad** ves las órdenes cerradas. Al elegir una, aparece su cuadro de
**Calidad de impresión**: autocontroles completados, muestras escaneadas, incidencias detectadas,
correcciones verificadas, y dos barras (eficacia de corrección e integridad de autocontroles).

Una incidencia se da por **corregida** si en el siguiente autocontrol registrado ya no se repite
ese motivo.

Si en una incidencia se retuvo producto, la trazabilidad indica la **producción a revisar**:
desde el último autocontrol sin incidencias hasta el control en el que se retuvo.

## Impacto productivo

En **Calidad → Impacto productivo** ves cuánto han costado las incidencias: minutos de parada,
tubos retenidos, número de incidencias y órdenes afectadas. Puedes filtrar por fechas (por defecto,
los últimos 30 días) y por línea. Hay rankings por orden, línea, diseño (litografía) y tipo de
defecto, de más a menos impacto. Se actualiza solo con cada autocontrol que se registra.

## Preguntas frecuentes

**No encuentro mi orden en la lista de órdenes abiertas.**
Solo salen las órdenes que siguen en fabricación y ya tienen algún autocontrol. Si es nueva,
escríbela a mano. Si estaba finalizada, pide a un administrador que la reabra.

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
