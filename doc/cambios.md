# Registro de cambios

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
