# Carpeta de patrones

Desde la versión 0.23.0, al cargar los archivos de un autocontrol, el hueco **Patrón (PDF)** enseña
los PDF de la carpeta de patrones cuyo nombre lleva la **especialidad** de la orden sin la E. Por
ejemplo, la especialidad `E2201829` encuentra `L2201829R002 ECONAZOLE 1% VIATRIS CR 30 G.pdf`. Un clic
carga el patrón. **Elegir archivo** desde el PC sigue estando siempre debajo.

## Cómo funciona

- La especialidad sale de la planificación de producción (`doc/planificacion-google.md`). Se guarda en la
  orden (`OrdenesFabricacion.Especialidad`) al crear un autocontrol de una orden que está en la hoja.
  Nunca se toma de lo que mande la pantalla.
- Las órdenes fuera de planificación (escritas a mano y que no están en la hoja) no tienen especialidad:
  el patrón se elige desde el PC, como antes.
- Se buscan los PDF de la carpeta, sin subcarpetas, cuyo nombre contiene las cifras de la especialidad.
  Salen como mucho 20, con la versión más alta primero (R002 antes que R001).
- **Nunca da error al operario.** Si la carpeta no existe, está vacía, no contesta en 10 segundos o no
  deja leer, se dice «No se ha podido leer la carpeta de patrones» y se elige desde el PC. El motivo
  queda en `log/err-log`.
- Solo se puede abrir un archivo de la lista encontrada para esa orden: nadie puede pedir otro nombre ni
  salirse de la carpeta. La aplicación solo lee: nunca escribe ni borra en la carpeta.

## Configuración

| Dónde | Qué |
|---|---|
| **Administración → Parámetros → `CarpetaPatrones`** | Si tiene valor, **manda sobre todo lo demás**. Se puede cambiar cuando haga falta, sin reiniciar. Vacío: se usa la de la instalación (filas siguientes) |
| `appsettings.json` → `Patrones:Carpeta` | Vacío por defecto (sin carpeta) |
| `docker-compose.yml` | `Patrones__Carpeta=/app/patrones` y el volumen `${PATRONES_CARPETA_SERVIDOR:-./patrones}:/app/patrones:ro` |
| `.env` → `PATRONES_CARPETA_SERVIDOR` | Carpeta **del servidor** donde está montada la carpeta de red. Vacío: la carpeta local `patrones/` |

**Ojo con el parámetro:** es la ruta **tal como la ve la aplicación**. En Docker, la aplicación solo ve
las carpetas del contenedor, así que vale `/app/patrones` o una subcarpeta (`/app/patrones/2027`). No vale
una ruta de red de Windows (`\\servidor\…`): si la carpeta de red cambia de sitio, IT la vuelve a montar
en el servidor (apartado siguiente) y el parámetro puede quedarse vacío. Una ruta de red solo funciona
si la aplicación corre en Windows. Se valida que sea una ruta completa, y cada cambio queda apuntado en
el diario de seguridad (`Log-Wys-Seg`), como cualquier parámetro.

La ruta real de la carpeta de red no se escribe en el repositorio (es público). Está en el `.env` de
cada servidor y la conoce IT.

## Montar la carpeta de red en el servidor (lo hace IT)

La aplicación corre en Linux dentro de Docker y no tiene usuario de Windows. Aunque desde los PC de la
red la carpeta se abra sin pedir contraseña, desde Linux el servidor de ficheros puede pedir usuario.
Comprobado el 24/11/2026: desde Docker Desktop no deja entrar como invitado. Lo recomendable es
montarla en el servidor con un **usuario de solo lectura**:

1. Crear la carpeta: `sudo mkdir -p /mnt/patrones`.
2. Guardar el usuario en un archivo que solo lea root, por ejemplo `/root/.patrones-credenciales`, con
   `username=…`, `password=…` y `domain=…`, y protegerlo con `sudo chmod 600 /root/.patrones-credenciales`.
3. Añadir una línea a `/etc/fstab`. Los espacios de la ruta se escriben `\040`. `nofail` hace que, si la
   red falla, el servidor arranque igual. `uid=1654` es el usuario `app` del contenedor:

   ```
   //SERVIDOR/RECURSO/Gestión\040documental/Pruebas\040Preliminares  /mnt/patrones  cifs  ro,credentials=/root/.patrones-credenciales,iconv=utf8,uid=1654,nofail,_netdev  0  0
   ```

4. `sudo mount -a` y comprobar con `ls /mnt/patrones | head`.
5. En el `.env`: `PATRONES_CARPETA_SERVIDOR=/mnt/patrones`, y `docker compose up -d`.

Si el servidor de Docker es Windows (Docker Desktop), no se puede montar una ruta de red directamente:
hay que usar un volumen `cifs` con usuario, o copiar los patrones a una carpeta local.

## En el equipo de desarrollo

Con `dotnet run`, se puede poner la ruta de red en `Patrones:Carpeta` con
`dotnet user-secrets set "Patrones:Carpeta" "\\SERVIDOR\…"`: Windows entra con el usuario de la sesión.
Con Docker Desktop, sin montar nada, se usa la carpeta local `patrones/` (excluida de Git y de Docker),
donde se pueden copiar patrones de prueba.
