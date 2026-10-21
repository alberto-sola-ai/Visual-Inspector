# Adaptación a las normas WYS

- **Fecha del diagnóstico:** 21/10/2026
- **Proyecto:** Visual Inspector
- **Tecnología encontrada:** ninguna todavía. Solo había documentación del Máster (`docu/`,
  con 2 de 9 notas escritas), una carpeta `src/` vacía y un `requeriments.txt` vacío pensado
  para Python.

## Resumen

El proyecto estaba en fase de idea: sin código, sin secretos y sin riesgos de seguridad. Por eso
la adaptación ha consistido en construir desde cero el esqueleto WYS (C# · .NET 10 · Blazor ·
MariaDB · Docker, ámbito Interno WYS) y reorganizar la documentación. Lo siguiente es probarlo
con Docker Desktop y empezar el núcleo del MVP (autocontroles y comparación de imágenes).

## Revisión de seguridad WYS

- **Evaluador de riesgo (antes de la adaptación):** SEGURO. No había código ni datos.
- **Guardarraíles incumplidos:** ninguno aplicable.

## Urgente

| Hallazgo | Riesgo | Propuesta |
|---|---|---|
| Ninguno | — | — |

## Prioridad alta

| Hallazgo | Propuesta | ¿Aprobado? |
|---|---|---|
| Sin aplicación ni sistema de usuarios | Crear el esqueleto WYS con usuarios, permisos, bloqueo y registros | Sí (21/10/2026) |
| `.gitignore` sin reglas para .NET, `log/` ni certificados | Completarlo | Sí |

## Prioridad media

| Hallazgo | Propuesta | ¿Aprobado? |
|---|---|---|
| Documentación en `docu/` con nombres `.md.md` | Mover a `doc/master/` y corregir nombres | Sí |
| Falta `doc/` técnica | Crear descripción, arquitectura, puesta en marcha, seguridad y cambios | Sí |
| Planteado en Python | Construir en C#/.NET según WYS; la visión con OpenCvSharp | Sí |

## Prioridad baja

| Hallazgo | Propuesta | ¿Aprobado? |
|---|---|---|
| `src/` y `requeriments.txt` vacíos | Borrarlos | Sí |

## Lo que ya cumple

- No había contraseñas, claves ni tokens en el repositorio.
- La definición del MVP ya preveía la trazabilidad de cada autocontrol.

## Excepciones aceptadas

- **Entity Framework 9 + Pomelo 9 sobre .NET 10 (temporal).** Pomelo aún no tiene versión para
  Entity Framework 10, y la alternativa que da la norma (.NET 8) deja de tener soporte el
  10/11/2026. Se actualizará cuando salga Pomelo 10.

## Cambios aplicados

| Fecha | Cambio | Copia de seguridad o rama (hecha a mano) |
|---|---|---|
| 21/10/2026 | Esqueleto WYS, reorganización de la documentación | Último commit en GitHub (78c852b), confirmado por Alberto |
