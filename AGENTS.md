# Instrucciones para Codex en este proyecto

## Normas WYS

Este proyecto sigue las normas de desarrollo de Witte y Solá.

- Antes de escribir o modificar código, aplica la skill **wys-code** en modo **Cambio**: carga
  las skills WYS que correspondan y muestra la lista de comprobación antes de empezar.
- Comentarios y documentación en castellano, en lenguaje sencillo.
- Anota cada cambio relevante en `doc/cambios.md`.
- Git y GitHub se gestionan a mano: no ejecutes comandos de Git salvo que Alberto lo pida.

## Sobre este proyecto

- **Qué hace:** asiste al autocontrol visual de la impresión de tubos comparando el patrón
  gráfico aprobado (PDF) con las dos muestras escaneadas de cada control.
- **Ámbito:** Interno WYS (la red de la empresa gestiona el cifrado; la aplicación no configura
  TLS ni certificados).
- **Tecnología:** C# / .NET 10 · Blazor Server · MariaDB 11.4 · Docker.
- **Excepciones a las normas WYS:**
  - *Temporal (21/10/2026):* la capa de datos usa Entity Framework 9 + Pomelo 9 sobre .NET 10,
    porque Pomelo aún no tiene versión para Entity Framework 10. Cuando salga Pomelo 10, subir
    a la 10 los paquetes `Pomelo.EntityFrameworkCore.MySql`, `Microsoft.EntityFrameworkCore.*`
    y `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.
- **Documentación:** carpeta `doc/`. Las notas del Máster de IA (Obsidian) están en `doc/master/`.
