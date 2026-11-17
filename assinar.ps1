param(
    [string]$PfxPath,
    [string]$Password,
    [string]$TargetExe = "TOTVSHub.exe"
)

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "   TOTVSHub - Assinador Digital Authenticode (Windows)  " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

if ($TargetExe -eq "TOTVSHub.exe" -and (Test-Path "Dist_Protected\TOTVSHub.exe")) {
    $TargetExe = "Dist_Protected\TOTVSHub.exe"
}

if (-not (Test-Path $TargetExe)) {
    if (Test-Path "Dist_Protected\TOTVSHub.exe") {
        $TargetExe = "Dist_Protected\TOTVSHub.exe"
    } else {
        Write-Host "[ERRO] Arquivo executavel $TargetExe nao encontrado. Compile primeiro!" -ForegroundColor Red
        exit 1
    }
}

$cert = $null

if ($PfxPath -and (Test-Path $PfxPath)) {
    Write-Host "[INFO] Carregando certificado PFX: $PfxPath" -ForegroundColor Yellow
    $securePass = $null
    if ($Password) {
        $securePass = ConvertTo-SecureString $Password -AsPlainText -Force
    }
    $cert = Get-PfxCertificate -FilePath $PfxPath
} else {
    Write-Host "[INFO] Procurando certificado 'TOTVS Hub Code Signing' no repositorio do Windows..." -ForegroundColor Yellow
    $cert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -like "*TOTVS Hub*" } | Select-Object -First 1

    if (-not $cert) {
        Write-Host "[CRIANDO] Gerando certificado autoassinado de desenvolvedor para testes..." -ForegroundColor Green
        $cert = New-SelfSignedCertificate -Subject "CN=TOTVS Hub Developer, O=TOTVS Hub Project" -Type CodeSigningCert -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(5)
    }
}

if (-not $cert) {
    Write-Host "[ERRO] Nao foi possivel obter um certificado de Code Signing." -ForegroundColor Red
    exit 1
}

Write-Host "[INFO] Certificado Selecionado: $($cert.Subject)" -ForegroundColor Gray
Write-Host "[INFO] Thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
Write-Host "[ASSINANDO] Aplicando assinatura digital em: $TargetExe" -ForegroundColor Yellow

$timestampServer = "http://timestamp.digicert.com"
$result = Set-AuthenticodeSignature -FilePath $TargetExe -Certificate $cert -TimestampServer $timestampServer -HashAlgorithm SHA256

Write-Host ""
if ($result.Status -eq "Valid") {
    Write-Host "[SUCESSO] Arquivo assinado digitalmente com sucesso!" -ForegroundColor Green
    Write-Host "  Status: $($result.Status)" -ForegroundColor Green
    Write-Host "  Signer: $($result.SignerCertificate.Subject)" -ForegroundColor Green
    Write-Host "  Hash:   $($result.SignerCertificate.Thumbprint)" -ForegroundColor Green
} else {
    Write-Host "[STATUS] Assinatura aplicada: $($result.Status) - $($result.StatusMessage)" -ForegroundColor Yellow
    Write-Host "Nota: Se for certificado autoassinado, o status 'UnknownError' no PowerShell eh normal ate que a raiz seja confiada no Windows." -ForegroundColor Gray
}

Write-Host ""
Write-Host "Verifique nas Propriedades do arquivo $TargetExe a nova aba 'Assinaturas Digitais'!" -ForegroundColor Cyan
