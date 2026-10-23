# Pruebas de seguridad

Última ejecución: 23/10/2026 · Versión: 0.12.1 · Resultado: **80 de 80 correctas**

## Qué es esto

Son pruebas automáticas: pequeños programas que comprueban, uno a uno, que las normas de
seguridad de WYS (SEGURIDAD WYS 2026) se cumplen de verdad en Visual Inspector. Es como pasar
la ITV: cada prueba revisa una pieza concreta y dice "bien" o "mal".

Están en la carpeta `test/`, que se queda en el equipo: no se sube a GitHub ni va dentro de la
aplicación.

## Qué se comprueba

| Norma | Qué se comprueba, en sencillo | Resultado |
|---|---|---|
| Contraseñas (1.3) | Se guardan "revueltas" con Argon2id y nunca se pueden leer. La correcta entra, la incorrecta (o vacía) no, y la misma contraseña nunca se guarda igual dos veces | Bien |
| Nadie puede nada por defecto (1.1) | Un usuario nuevo no puede hacer nada. Cada permiso abre solo lo suyo. Administración es solo para el rol Administrador | Bien |
| Contraseña temporal | Quien aún no ha cambiado su contraseña temporal no puede hacer nada, ni siendo administrador | Bien |
| El servidor vigila (1.2) | Aunque alguien se salte la pantalla, el servidor vuelve a mirar el permiso, para la operación y lo apunta en el diario de seguridad | Bien |
| No fiarse de lo que llega | Un archivo disfrazado (un programa con nombre de PDF), uno de más de 50 MB o un texto con trucos (inyección) se rechazan. La orden de fabricación siempre se comprueba completa en el servidor (10 cifras) y el inicio habitual de las órdenes solo admite cifras. La línea de fabricación es obligatoria y solo vale una de la lista. Las órdenes en fabricación que se sugieren solo las ve quien puede hacer autocontroles, y nunca salen las finalizadas ni las desactivadas | Bien |
| Registros (logs) | Solo el administrador los lee, del más reciente al más antiguo, y cada consulta queda apuntada en el diario de seguridad | Bien |
| Sin secretos (3.1) | La configuración no lleva contraseñas, y `.env`, `log/`, las claves y los archivos de clientes no se suben a GitHub | Bien |
| Sin pistas (4.1) | El servidor no dice qué programa usa | Bien |
| Cabeceras del navegador (4.2) | El navegador solo carga lo de la propia aplicación y no deja meterla dentro de otra web | Bien |
| Sin registro público | No hay pantalla para crearse una cuenta ni para recuperar la contraseña por correo | Bien |
| IA y segundo candado (6.1) | La IA y la verificación en dos pasos empiezan apagadas | Bien |
| Aplicación en marcha | Sin entrar, todo manda a la pantalla de entrada (también el PDF y las pantallas de Administración). Un formulario falso desde otra web se rechaza. Un usuario que no existe recibe un mensaje genérico. Los errores solo enseñan un código. La base de datos no se alcanza desde fuera | Bien |
| Bloqueo tras 3 fallos | No se prueba de forma automática, para no bloquear cuentas reales. **Ya está comprobado a mano** | Bien |

## Avisos encontrados y corregidos (0.10.7)

La primera vez que se pasaron las pruebas salieron 2 avisos. Ninguno dejaba entrar a nadie ni
exponía datos, y los dos ya están corregidos:

1. **Contraseña vacía.** Al comprobar una contraseña vacía, el cifrado daba un error interno en
   lugar de un simple "no". Desde la pantalla de entrada no podía pasar (el formulario pide
   escribirla), pero ahora el cifrado responde "no" en cualquier caso.
2. **El servidor decía qué programa usa.** Cada respuesta llevaba la cabecera `Server: Kestrel`,
   una pista innecesaria (norma 4.1). Ya no se envía.

## Cómo repetir las pruebas

Con la aplicación en marcha en Docker (para las pruebas "en marcha"; si no lo está, esas se
saltan solas):

```
dotnet test test/VisualInspector.Pruebas
```
