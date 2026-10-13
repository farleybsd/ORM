# Copia o .env da raiz para todas as pastas de capítulo.
# Uso (PowerShell, na pasta do repositório): .\configurar.ps1
if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "Criei o .env a partir do .env.example. Confira a senha do banco e rode de novo."
    exit
}
Get-ChildItem -Directory -Filter "cap*" | ForEach-Object {
    if (Test-Path (Join-Path $_.FullName ".env.example")) {
        Copy-Item ".env" (Join-Path $_.FullName ".env") -Force
        Write-Host "  .env -> $($_.Name)"
    }
}
Write-Host "Pronto."
