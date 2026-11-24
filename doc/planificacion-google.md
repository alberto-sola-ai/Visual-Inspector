# Planificación de producción desde Google Sheets

Visual Inspector lee la hoja de planificación de producción (Google Sheets) para que el operario
elija la orden con un clic en **Nuevo Autocontrol**. La orden se puede seguir escribiendo a mano.
Desde la versión 0.21.0.

## Qué se lee

- **Documento:** el identificador que va en la dirección de la hoja, tras `/d/`
  (`appsettings.json` → `Planificacion:IdHoja`).
- **Pestaña:** `tubosFabricados` (`Planificacion:Pestana`).
- **Columnas usadas**, localizadas por su título (da igual en qué orden estén):

| Columna de la hoja | Uso en la aplicación |
|---|---|
| `LINEA` | «LINEA 1» se convierte en L1. Si esa línea no está en el parámetro `Lineas`, la fila se descarta |
| `OF` | Orden de 10 cifras. Si viniera sin el inicio habitual, se completa con el parámetro `PrefijoOrden` |
| `PRIORIDAD` | 1 = en curso (cargada en la línea); el resto, pendientes. Hay dos columnas iguales: se usa la primera |
| `DESCRIPCION` | Se muestra y, si la orden no tiene descripción en la aplicación, se copia al crear el autocontrol |
| `Fabricados`, `CANTIDAD`, `Pendiente de fabricar` | Solo informativas |
| `ESPECIALIDAD` | Código de especialidad (por ejemplo E2201359), solo informativo: se muestra en cada orden y en la ventana de confirmación. Columna añadida el 24/11/2026; si falta, no se muestra |
| `ESTADO` | No se usa: el estado sale de la prioridad |

- La celda «Carga: dd/mm/aaaa hh:mm:ss» que hay encima de los títulos se muestra como «Datos de
  producción del…».
- Las filas que no encajan (línea desconocida, orden que no tiene 10 cifras, sin prioridad u orden
  repetida) se descartan y se cuenta cuántas en `log/err-log`.

## Cómo funciona

- Un trabajo en segundo plano lee la hoja al arrancar y después cada `MinutosLecturaPlanificacion`
  minutos (Administración → Parámetros; 15 por defecto, entre 5 y 120). El botón **Actualizar
  ahora** de la pantalla vuelve a leerla, como mucho una vez por minuto.
- La copia se guarda en memoria, no en la base de datos. Si Google falla, se sigue mostrando la
  última copia con un aviso.
- Las órdenes abiertas en la aplicación que no están en la hoja se proponen para finalizar, solo si
  la última lectura fue bien y tiene menos de 2 horas. Nunca se finalizan solas.
- **Línea (0.22.0):** con conexión (última lectura correcta y de menos de 2 horas), la línea la pone el
  servidor: la de la hoja o, si la orden no está en ella, la de la orden abierta. Si la orden escrita a mano no
  está en ninguno de los dos sitios (0.22.3), es un autocontrol fuera de planificación: el operario elige la
  línea y el servidor solo acepta esa línea en este caso. Sin conexión, la línea se elige en un desplegable y se admiten órdenes
  nuevas, para no parar la producción.
- Se apaga con el parámetro `UsarPlanificacion` = No. Sin clave o sin identificador de la hoja, la
  función tampoco se activa, y la pantalla funciona como antes.
- Cada lectura correcta deja en la consola (`docker compose logs app`) la línea «Hoja de
  planificación leída: N órdenes». Los fallos van a `log/err-log`.

## Crear la cuenta de servicio (una sola vez)

El correo de la cuenta actual está en el `.env` del equipo de Alberto (bloque «Planificación de
producción») y en Google Cloud; no se escribe aquí porque el repositorio es público.

1. En https://console.cloud.google.com, elegir o crear el proyecto.
2. **APIs y servicios → Biblioteca**, buscar «Google Sheets API» y pulsar **Habilitar**.
3. **IAM y administración → Cuentas de servicio → Crear cuenta de servicio**, sin roles.
4. En la cuenta, pestaña **Claves → Agregar clave → Crear clave nueva → JSON**. Se descarga el
   archivo de la clave: es una contraseña, no se envía por correo ni se sube a GitHub.
5. En la hoja, **Compartir** con el correo de la cuenta, permiso **Lector** y sin notificar.

Posibles bloqueos, que resuelve el administrador de Google Workspace: la organización puede
prohibir crear claves (`iam.disableServiceAccountKeyCreation`) o compartir fuera del dominio.

## Dónde va la clave

- **Debe haber un único archivo `.json`** en la carpeta `wys-api-sheet/`, escrita así, en minúsculas: en un
  servidor Linux las mayúsculas cuentan y `docker-compose.yml` monta `./wys-api-sheet` (ajustable con
  `Planificacion:CarpetaCredenciales`). Si hay varios, no se usa ninguno.
- La carpeta está excluida de Git (`.gitignore`), de la imagen de Docker (`.dockerignore`) y de la
  publicación (`VisualInspector.csproj`).
- **En el equipo de desarrollo:** `wys-api-sheet/` dentro del proyecto. Ojo: el proyecto está en
  OneDrive, así que la clave se sincroniza con la nube; es mejor guardarla fuera y copiarla.
- **En el servidor:** `wys-api-sheet/` junto a `docker-compose.yml`. Se monta en el contenedor en
  solo lectura. El contenedor usa el usuario `app` (número 1654), que tiene que poder leerla:

  ```bash
  sudo chown 1654 wys-api-sheet/*.json
  sudo chmod 400 wys-api-sheet/*.json
  ```

## Cambiar o revocar la clave

Si la clave se pierde o se filtra: en Google Cloud, en la cuenta de servicio → **Claves**, borrar
la clave antigua y crear una nueva; sustituir el archivo en `wys-api-sheet/` y reiniciar
(`docker compose restart app`). Para cortar el acceso del todo, basta con dejar de compartir la
hoja con la cuenta.
