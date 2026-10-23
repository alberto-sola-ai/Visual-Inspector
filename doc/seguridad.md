# Cómo protege esta aplicación tus datos

Última actualización: 23/10/2026 · Versión: 0.12.1

## La puerta de entrada
Para entrar necesitas tu usuario y tu contraseña, como la llave de casa.
Si alguien se equivoca 3 veces seguidas, la puerta se cierra 15 minutos. El administrador puede
abrirla antes. Nadie puede crearse una cuenta por su cuenta: las cuentas las crea el administrador.

## Cada uno, solo lo suyo
Al principio nadie puede hacer nada, salvo el administrador. Es como un colegio
donde el director reparte las llaves de cada clase: solo puedes entrar en las
clases cuya llave te ha dado. Y la cerradura está en el servidor, no en la pantalla: aunque
alguien escondiera o mostrara un botón, sin la llave no puede hacer nada.

## La primera vez, cambia la cerradura
Cuando te dan una contraseña temporal (al crear tu cuenta o si la olvidas), lo primero que te
pide la aplicación es cambiarla. Hasta que no lo hagas, no puedes hacer nada más.

## Las contraseñas, bien escondidas
Tu contraseña se guarda convertida en un "revoltijo" que no se puede deshacer
(Argon2id). Ni siquiera quien gestiona la aplicación puede leerla.

## Si te vas, la puerta se cierra sola
Si dejas la aplicación 30 minutos sin usar, te pide entrar otra vez. Y si el administrador te
desactiva o cambia tus permisos, tu sesión se cierra en menos de un minuto.

## Un segundo candado (desactivado)
Existe la opción de un segundo candado (verificación en dos pasos con una aplicación del móvil),
pero viene apagada. Solo el administrador puede encenderla, en Administración → Parámetros
(`MfaActivo`).

## Mensajes por un tubo secreto
Los mensajes viajan dentro de la casa de WYS, y es la propia red de la empresa la que los vigila
y protege por el camino, como un guarda en el pasillo. Por eso la aplicación no pone sus propios
candados de cifrado (chocarían con los de la empresa). La base de datos no se puede alcanzar
desde fuera: solo habla con la aplicación, dentro de Docker.

## El navegador también ayuda
La aplicación le dice al navegador que no cargue nada de fuera, que no deje meterla dentro de
otra web y que no "adivine" el tipo de los archivos. Así es más difícil engañar a quien la usa.
Tampoco dice qué programa de servidor usa: es una pista que no hace falta dar.

## Pruebas automáticas
Unas pruebas automáticas comprueban que todo lo de esta página se cumple de verdad (80 de 80
correctas). El resumen está en [pruebas-seguridad.md](pruebas-seguridad.md).

## Los archivos, revisados en la puerta y sin guardar
Cuando cargas el patrón y las muestras, la aplicación mira qué son de verdad (no se fía del nombre
ni de la extensión), no deja pasar archivos de más de 50 MB ni imágenes gigantes (más de unos
12.000 × 12.000 píxeles), corta cualquier proceso que tarde más de 2 minutos, y solo
quien tiene permiso para hacer autocontroles puede cargarlos. Los archivos no se guardan: se
revisan en memoria y se olvidan al salir de la pantalla. Solo se apunta su "huella" (SHA-256),
como la huella dactilar de un documento, para poder demostrar después cuál se usó. De cada
diferencia se guarda solo un recorte pequeño, como foto de prueba para el informe.

## Un registro que no se puede retocar
Cuando el operario registra un autocontrol, queda guardado tal cual: quién lo validó, cuándo, qué
decidió en cada diferencia y con qué comentario. Después ya no se puede cambiar, ni siquiera desde
la pantalla: el servidor lo rechaza. Como una hoja firmada del cuaderno de fabricación.
Lo mismo pasa al finalizar una orden: queda apuntado quién y cuándo, y ya no admite autocontroles
nuevos. Solo el administrador puede reabrirla, y eso también queda apuntado en la orden y en el
diario de seguridad. El informe en PDF solo se descarga con permiso para ver autocontroles, y el servidor lo
vuelve a comprobar en cada descarga.

Si un autocontrol está mal, solo el administrador puede tacharlo (anularlo, con el motivo escrito)
o arrancar la hoja (eliminarlo), aunque la orden ya esté cerrada. Las dos cosas se apuntan
en el diario de seguridad: quién, cuándo y qué autocontrol era.

## Nada de secretos a la vista
Las claves y contraseñas internas no están escritas en el programa: se guardan
aparte, en la "caja fuerte" del servidor (archivo `.env`, que nunca se sube a GitHub).
Las claves con las que la aplicación sella las sesiones se guardan en su propio cajón
(volumen `claves-app`), que tampoco se sube a GitHub ni viaja dentro de la aplicación.

## Un diario de lo importante
La aplicación apunta en un diario (Log-Wys-Seg) quién entra, quién falla, quién se bloquea y
quién cambia usuarios, permisos o parámetros, con la hora y desde qué equipo. Nunca apunta
contraseñas. Los errores se apuntan en otro diario (err-log).
Solo el administrador puede leer los dos diarios desde la aplicación (Administración →
Registros), sin poder cambiarlos. Como guardan datos personales, cada vez que alguien los
consulta queda apuntado quién y qué diario miró.

## Si algo falla
Verás un mensaje sencillo con un código. El código ayuda a IT a encontrar el
problema, sin enseñar detalles que podrían usar personas malintencionadas.

## Inteligencia artificial
Esta aplicación todavía no usa inteligencia artificial. Los interruptores de IA existen en
Administración → Parámetros, pero están apagados y bloqueados hasta que la aplicación la use.

| Opción | ¿Disponible en la API? | Dónde se comprueba |
|---|---|---|
| Máximo de tokens por petición | No aplica (sin IA) | — |
| Tokens usados en cada respuesta | No aplica (sin IA) | — |

## Revisar las piezas
El administrador puede ver qué piezas usa la aplicación y si alguna necesita
actualizarse (Administración → Versiones y actualizaciones), como revisar que los frenos de la
bici estén bien.

## Dónde se guardan los datos
- **Servidor:** pendiente de definir por IT (de momento, el equipo de desarrollo con Docker).
- **Base de datos:** MariaDB, dentro de Docker, en el volumen `datos-mariadb`.
- **Diarios:** volumen `registros-app`. Los de seguridad se guardan 90 días; los de errores, 30.
  El diario de seguridad guarda el identificador del usuario y la dirección de su equipo, que son
  datos personales: por eso solo se guardan ahí y solo 90 días.
- **Cifrado del disco:** se recomienda cifrar el disco del servidor donde se instale.
