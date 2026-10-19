
Sirve para decirle a Git qué archivos o carpetas debe ignorar y no subir al repositorio (son de Obsidian).

Realmente es un archivo que indica a Git qué archivos o carpetas debe ignorar

Es decir, esos elementos no se incluirán normalmente en los commits ni se subirán al repositorio.es un archivo que indica a Git qué archivos o carpetas debe ignorar. 

Es decir, esos elementos no se incluirán normalmente en los commits ni se subirán al repositorio

## Ejemplo

```gitignore
.obsidian/
.env
.venv/
__pycache__/
*.pyc