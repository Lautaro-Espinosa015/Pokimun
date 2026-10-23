param()
$ErrorActionPreference = 'Stop'
$taskProjectRoot = Split-Path -Parent $PSScriptRoot
$taskFolder = Join-Path $taskProjectRoot 'Temp/Android'
New-Item -ItemType Directory -Path $taskFolder -Force | Out-Null
$taskRequest = Join-Path $taskFolder 'solicitud.txt'
if (Test-Path -LiteralPath $taskRequest) { throw 'Ya hay una compilación Android pendiente.' }
Set-Content -LiteralPath (Join-Path $taskFolder 'resultado.txt') -Value 'PENDIENTE: esperando a que Unity procese la solicitud.' -Encoding UTF8
Set-Content -LiteralPath $taskRequest -Value 'apk' -Encoding UTF8
Write-Output 'Solicitud creada. Unity debe estar abierto, fuera de Play Mode y con las escenas guardadas.'
Write-Output 'Unity activará Android. Resultado: Temp/Android/resultado.txt; APK: Builds/Android/Pokimun-prueba.apk.'
