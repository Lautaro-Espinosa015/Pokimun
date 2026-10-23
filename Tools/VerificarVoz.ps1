param(
    [ValidateSet('recursos', 'pruebas', 'linux', 'windows', 'android', 'plataformas')]
    [string]$Accion = 'recursos'
)

$ErrorActionPreference = 'Stop'
$taskProjectRoot = Split-Path -Parent $PSScriptRoot
$taskManifest = Get-Content -LiteralPath (Join-Path $taskProjectRoot 'Tools/Voz/Vosk-Dependencias.json') -Raw | ConvertFrom-Json
foreach ($taskEntry in $taskManifest.files) {
    $taskFile = Join-Path $taskProjectRoot $taskEntry.path
    if (-not (Test-Path -LiteralPath $taskFile -PathType Leaf)) {
        throw "Falta el recurso de voz: $($taskEntry.path)"
    }
    $taskHash = (Get-FileHash -LiteralPath $taskFile -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($taskHash -ne $taskEntry.sha256) {
        throw "El recurso de voz difiere de la instalación verificada: $($taskEntry.path)"
    }
}
Write-Output "Recursos Vosk verificados: $($taskManifest.files.Count) archivos."

if ($Accion -ne 'recursos') {
    $taskResults = Join-Path $taskProjectRoot 'Temp/ValidacionVoz'
    New-Item -ItemType Directory -Path $taskResults -Force | Out-Null
    $taskRequest = Join-Path $taskResults 'solicitud.txt'
    if (Test-Path -LiteralPath $taskRequest) {
        throw 'Ya hay una solicitud de validación pendiente. Espera a que Unity la procese.'
    }
    Set-Content -LiteralPath $taskRequest -Value $Accion -Encoding UTF8
    Write-Output "Solicitud '$Accion' guardada. Abre este proyecto en Unity, fuera de Play Mode."
    Write-Output "Los resultados se guardan en $taskResults. También puedes usar el menú Pokimun > Voz."
}
