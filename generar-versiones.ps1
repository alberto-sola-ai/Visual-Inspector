# Genera recursos/versiones.json para la pantalla Administración → Versiones y actualizaciones.
# Se ejecuta ANTES de publicar cada versión (necesita internet para consultar nuget.org):
#     powershell -ExecutionPolicy Bypass -File generar-versiones.ps1
# Así la aplicación muestra qué piezas usa y si conviene actualizarlas, sin salir a internet.

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

# Explicación sencilla de cada pieza, para que el administrador sepa para qué sirve
$descripciones = @{
    'Pomelo.EntityFrameworkCore.MySql'                  = 'Conexión con la base de datos MariaDB.'
    'Microsoft.AspNetCore.Identity.EntityFrameworkCore' = 'Guarda los usuarios, roles y permisos en la base de datos.'
    'Microsoft.EntityFrameworkCore.Relational'          = 'Motor de acceso a la base de datos (Entity Framework).'
    'Microsoft.EntityFrameworkCore.Design'              = 'Herramienta para crear las tablas (migraciones). Solo se usa al programar.'
    'Konscious.Security.Cryptography.Argon2'            = 'Cifrado de contraseñas (Argon2id).'
    'Serilog.AspNetCore'                                = 'Escribe los registros de errores y de seguridad en la carpeta log.'
    'Serilog.Expressions'                               = 'Separa los registros de seguridad de los de errores.'
}

# Paquetes que no se pueden actualizar todavía, y por qué
$notas = @{
    'Microsoft.AspNetCore.Identity.EntityFrameworkCore' = ' Se mantiene en la versión 9 hasta que exista Pomelo 10.'
    'Microsoft.EntityFrameworkCore.Relational'          = ' Se mantiene en la versión 9 hasta que exista Pomelo 10.'
    'Microsoft.EntityFrameworkCore.Design'              = ' Se mantiene en la versión 9 hasta que exista Pomelo 10.'
}

function Get-Paquetes([string]$opcion) {
    $json = dotnet list VisualInspector.csproj package $opcion --format json | Out-String | ConvertFrom-Json
    $resultado = @{}
    foreach ($proyecto in $json.projects) {
        foreach ($marco in $proyecto.frameworks) {
            foreach ($paquete in $marco.topLevelPackages) {
                # Los paquetes internos que añade .NET por su cuenta no se muestran
                if ($paquete.id -like 'Microsoft.AspNetCore.App.Internal*') { continue }
                $resultado[$paquete.id] = $paquete
            }
        }
    }
    return $resultado
}

Write-Host 'Consultando paquetes instalados, desactualizados y con fallos de seguridad...'
$instalados = Get-Paquetes ''
$desactualizados = Get-Paquetes '--outdated'
$vulnerables = Get-Paquetes '--vulnerable'

$componentes = @()

# .NET (la plataforma)
$runtime = (dotnet --list-runtimes | Select-String 'Microsoft.AspNetCore.App 10\.' | Select-Object -Last 1).ToString().Split(' ')[1]
$ultimaNet = '-'
try {
    $versionesNet = Invoke-RestMethod 'https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json'
    $ultimaNet = $versionesNet.'latest-runtime'
} catch { Write-Host 'No se pudo consultar la última versión de .NET.' }
$recomendacionNet = if ($ultimaNet -ne '-' -and [version]$ultimaNet -gt [version]$runtime) { 'Recomendada' } else { 'Al día' }
$componentes += [ordered]@{
    nombre = '.NET'; descripcion = 'La plataforma sobre la que funciona la aplicación.'
    instalada = $runtime; ultima = $ultimaNet; recomendacion = $recomendacionNet
}

# MariaDB (la versión la fija docker-compose.yml)
$imagen = (Select-String -Path docker-compose.yml -Pattern 'image:\s*mariadb:(\S+)').Matches[0].Groups[1].Value
$componentes += [ordered]@{
    nombre = 'MariaDB Server'; descripcion = 'La base de datos. Se usa la rama de soporte largo (LTS).'
    instalada = $imagen; ultima = "$imagen (rama LTS)"; recomendacion = 'Al día'
}

# Paquetes de la aplicación
foreach ($id in ($instalados.Keys | Sort-Object)) {
    $paquete = $instalados[$id]
    $instalada = $paquete.resolvedVersion
    $ultima = if ($desactualizados.ContainsKey($id)) { $desactualizados[$id].latestVersion } else { $instalada }
    $recomendacion = if ($vulnerables.ContainsKey($id)) { 'Necesaria' }
                     elseif ($ultima -ne $instalada) { 'Recomendada' }
                     else { 'Al día' }
    $descripcion = if ($descripciones.ContainsKey($id)) { $descripciones[$id] } else { 'Componente de la aplicación.' }
    if ($notas.ContainsKey($id)) { $descripcion += $notas[$id] }
    $componentes += [ordered]@{
        nombre = $id; descripcion = $descripcion
        instalada = $instalada; ultima = $ultima; recomendacion = $recomendacion
    }
}

$informe = [ordered]@{
    generado = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss')
    componentes = $componentes
}

$informe | ConvertTo-Json -Depth 5 | Set-Content -Path recursos/versiones.json -Encoding utf8
Write-Host "Informe guardado en recursos/versiones.json ($($componentes.Count) componentes)."
