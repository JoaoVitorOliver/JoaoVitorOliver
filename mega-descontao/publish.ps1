# Sobe o site em modo produção na sua máquina: compila a vitrine, publica a API e serve
# tudo num processo só, em http://localhost:8080.
#
# Uso:   .\publish.ps1 -AdminKey "sua-chave-forte"
# Túnel: em outro terminal, cloudflared tunnel --url http://localhost:8080
#
# Se o PowerShell bloquear:  powershell -ExecutionPolicy Bypass -File .\publish.ps1 -AdminKey "..."

param(
    [string]$AdminEmail = $env:MEGA_ADMIN_EMAIL,
    [string]$AdminPassword = $env:MEGA_ADMIN_PASSWORD,
    [string]$AdminKey = $env:MEGA_ADMIN_KEY,
    [int]$Port = 8080
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$publishDir = Join-Path $root 'publish'
$dataDir = Join-Path $root 'data'

if (-not $AdminEmail -or -not $AdminPassword) {
    Write-Host "AVISO: sem -AdminEmail e -AdminPassword, nenhum administrador e criado e voce nao" -ForegroundColor Yellow
    Write-Host "       consegue entrar na tela de curadoria. Em producao o sistema nao inventa" -ForegroundColor Yellow
    Write-Host "       credencial: ou voce define, ou nao existe admin." -ForegroundColor Yellow
    Write-Host "       Rode:  .\publish.ps1 -AdminEmail 'voce@dominio.com' -AdminPassword 'senha-forte'" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "1/4  Compilando a vitrine..." -ForegroundColor Cyan
Push-Location (Join-Path $root 'frontend')
if (-not (Test-Path 'node_modules')) { npm ci }
npm run build
Pop-Location

Write-Host "2/4  Publicando a API..." -ForegroundColor Cyan
# A pasta publish e descartavel e recriada a cada vez — por isso o banco NAO mora nela.
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish (Join-Path $root 'backend\MegaDescontao.Api\MegaDescontao.Api.csproj') -c Release -o $publishDir

Write-Host "3/4  Copiando a vitrine para dentro da API..." -ForegroundColor Cyan
Copy-Item (Join-Path $root 'frontend\dist') (Join-Path $publishDir 'wwwroot') -Recurse

New-Item -ItemType Directory -Force -Path $dataDir | Out-Null

Write-Host "4/4  Subindo em http://localhost:$Port" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Banco:  $dataDir\megadescontao.db  (sobrevive a cada republicacao)" -ForegroundColor DarkGray
Write-Host "  Tunel:  cloudflared tunnel --url http://localhost:$Port" -ForegroundColor DarkGray
Write-Host "  Parar:  Ctrl+C" -ForegroundColor DarkGray
Write-Host ""

$env:ASPNETCORE_ENVIRONMENT = 'Production'
# Escuta so no localhost: quem alcanca a aplicacao e o tunel, nunca a internet direto.
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$env:ConnectionStrings__Default = "Data Source=$dataDir\megadescontao.db"
# Faz a API ler o IP real do visitante no cabecalho encaminhado pelo tunel. Sem isso o
# rate limit trataria todo mundo como um visitante so.
$env:Hosting__BehindProxy = 'true'
# Em Producao o user-secrets NAO e carregado, entao tudo isso precisa vir por variavel.
if ($AdminKey) { $env:Admin__ApiKey = $AdminKey }
if ($AdminEmail) { $env:Admin__Email = $AdminEmail }
if ($AdminPassword) { $env:Admin__Password = $AdminPassword }

Push-Location $publishDir
try {
    dotnet MegaDescontao.Api.dll
}
finally {
    Pop-Location
}
