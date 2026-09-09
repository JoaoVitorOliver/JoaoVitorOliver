# Sobe a API e a vitrine juntas, cada uma na sua janela.
# Uso:  .\dev.ps1        (na raiz do projeto)
# Se o PowerShell bloquear a execução:  powershell -ExecutionPolicy Bypass -File .\dev.ps1

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if (-not (Test-Path "$root\frontend\node_modules")) {
    Write-Host "Instalando as dependencias do front (so na primeira vez)..." -ForegroundColor Cyan
    Push-Location "$root\frontend"
    npm install
    Pop-Location
}

Write-Host "Subindo a API em http://localhost:5080 ..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList '-NoExit', '-Command', "Set-Location '$root\backend\MegaDescontao.Api'; dotnet run"

Write-Host "Subindo a vitrine em http://localhost:5173 ..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList '-NoExit', '-Command', "Set-Location '$root\frontend'; npm run dev"

Write-Host ""
Write-Host "Pronto. Abra http://localhost:5173" -ForegroundColor Green
Write-Host "Swagger da API: http://localhost:5080/swagger" -ForegroundColor DarkGray
Write-Host "Para parar, feche as duas janelas que abriram." -ForegroundColor DarkGray
