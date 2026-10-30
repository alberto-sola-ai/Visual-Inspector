# Genera recursos/versiones.json para la pantalla Administración → Versiones y actualizaciones.
# Se ejecuta ANTES de publicar cada versión (necesita internet para consultar nuget.org):
#     powershell -ExecutionPolicy Bypass -File generar-versiones.ps1
# Así la aplicación muestra qué piezas usa y si conviene actualizarlas, sin salir a internet.

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

# Para qué se usa cada pieza en Visual Inspector y su licencia (todas gratuitas, también para
# uso comercial). Al añadir una biblioteca nueva al proyecto, se añade aquí su línea.
$descripciones = @{
    'Pomelo.EntityFrameworkCore.MySql'                  = 'Conecta la aplicación con la base de datos MariaDB, donde se guardan usuarios, autocontroles y resultados. Licencia MIT.'
    'Microsoft.AspNetCore.Identity.EntityFrameworkCore' = 'Guarda en la base de datos los usuarios, sus roles y sus permisos. Licencia MIT.'
    'Microsoft.EntityFrameworkCore.Relational'          = 'Motor de acceso a la base de datos (Entity Framework): lee y guarda los datos sin escribir SQL a mano, lo que evita inyecciones. Licencia MIT.'
    'Microsoft.EntityFrameworkCore.Design'              = 'Herramienta para crear y cambiar las tablas (migraciones). Solo se usa al programar, no en el servidor. Licencia MIT.'
    'Konscious.Security.Cryptography.Argon2'            = 'Cifra las contraseñas con Argon2id, el método que pide la norma de seguridad WYS: nadie puede leerlas. Licencia MIT.'
    'Serilog.AspNetCore'                                = 'Escribe los registros de la carpeta log: errores (err-log) y seguridad (Log-Wys-Seg). Licencia Apache 2.0.'
    'Serilog.Expressions'                               = 'Separa los registros de seguridad de los de errores, cada uno en su fichero. Licencia Apache 2.0.'
    'PDFtoImage'                                        = 'Convierte el patrón PDF en imagen a 300 ppp y en color para compararlo con las muestras. Usa por dentro PDFium (el lector de PDF de Chrome) y SkiaSharp. Licencia MIT.'
    'OpenCvSharp4'                                      = 'Visión artificial (OpenCV): lee las muestras escaneadas, recorta la pieza del fondo del escáner y localiza cada muestra dentro del patrón con cualquier giro. Licencia Apache 2.0.'
    'OpenCvSharp4.runtime.win'                          = 'Parte nativa de OpenCV para Windows. Solo se usa en el equipo de desarrollo. Licencia Apache 2.0.'
    'OpenCvSharp4.official.runtime.linux-x64'           = 'Parte nativa de OpenCV para Linux: la que usa el servidor (Docker). Licencia Apache 2.0.'
    'Markdig'                                           = 'Convierte la guía de usuario (un archivo de texto en Markdown) en la pantalla Ayuda, sin permitir HTML en bruto. Licencia BSD-2-Clause.'
    'MiniExcel'                                         = 'Escribe los archivos de Excel (.xlsx) de las exportaciones de Trazabilidad e Impacto productivo. Licencia Apache 2.0.'
    'PDFsharp-MigraDoc'                               = 'Genera el informe de fabricación en PDF con la imagen de WYS. Licencia MIT. No actualizar: desde la versión 6.5.0 la licencia pasa a AGPL.'
    'System.Security.Cryptography.Pkcs'                 = 'Firma y cifrado de documentos que usa PDFsharp por dentro. Se fija una versión corregida porque la que trae PDFsharp tiene fallos de seguridad conocidos. Licencia MIT.'
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
    nombre = '.NET'; descripcion = 'La plataforma sobre la que funciona toda la aplicación (pantallas Blazor, servicios y seguridad). Licencia MIT.'
    instalada = $runtime; ultima = $ultimaNet; recomendacion = $recomendacionNet
}

# MariaDB (la versión la fija docker-compose.yml)
$imagen = (Select-String -Path docker-compose.yml -Pattern 'image:\s*mariadb:(\S+)').Matches[0].Groups[1].Value
$componentes += [ordered]@{
    nombre = 'MariaDB Server'; descripcion = 'La base de datos donde se guardan usuarios, parámetros, órdenes y autocontroles. Rama de soporte largo (LTS). Licencia GPL v2, gratuita; funciona como servicio aparte.'
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
