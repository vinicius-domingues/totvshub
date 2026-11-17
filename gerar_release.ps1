$ErrorActionPreference = 'Stop'

Write-Host '========================================================' -ForegroundColor Cyan
Write-Host '   TOTVSHub - Gerador de Release e Checksum SHA-256    ' -ForegroundColor Cyan
Write-Host '========================================================' -ForegroundColor Cyan

$exePath = Join-Path $PSScriptRoot 'TOTVSHub.exe'
$protectedExe = Join-Path $PSScriptRoot 'Dist_Protected\TOTVSHub.exe'

$targetExe = $exePath
$isProtected = $false
if (Test-Path $protectedExe) {
    $targetExe = $protectedExe
    $isProtected = $true
}

if (-not (Test-Path $targetExe)) {
    Write-Host '[ERRO] TOTVSHub.exe nao encontrado. Execute compilar.bat primeiro!' -ForegroundColor Red
    exit 1
}

$hashObj = Get-FileHash -Path $targetExe -Algorithm SHA256
$hashVal = $hashObj.Hash

Write-Host "[INFO] Binario avaliado: $(Split-Path $targetExe -Leaf) (Protegido: $isProtected)" -ForegroundColor Gray
Write-Host '[INFO] Hash SHA-256:' -ForegroundColor Yellow
Write-Host "  $hashVal" -ForegroundColor Green
Write-Host ''

$lines = @(
'# TOTVSHub - Release Oficial v2.4.0',
'',
'## 🛡️ Verificação de Integridade (SHA-256)',
'```text',
'Arquivo: TOTVSHub.exe',
("SHA-256: " + $hashVal),
'```',
'',
'Para verificar no PowerShell:',
'```powershell',
'Get-FileHash .\TOTVSHub.exe -Algorithm SHA256',
'```',
'',
'---',
'',
'## 🚀 Novidades desta Versão (v2.4.0)',
'- **Visualização em Cards Dinâmicos (Masonry)**:',
'  - As seções do `appserver.ini` agora são organizadas em grade de 3 colunas independentes que preenchem espaços vazios de forma inteligente.',
'  - Alternador direto na barra superior para alternar entre Modo Cards e Modo Lista tradicional.',
'- **Alternador de Visualização na Home**:',
'  - Novo botão de organização ao lado de "Ocultos" para alternar os cards de ambientes em 2 colunas dinâmicas ou lista corrida.',
'- **Segurança e Hardening de Código**:',
'  - **Gravação Atômica de Arquivos**: O `appserver.ini` é gravado via swap temporário e cópia de segurança (`.bak`), impossibilitando corrupção em quedas de energia.',
'  - **Sanitização de URLs**: Validação estrita de protocolos `http://` e `https://` antes de qualquer execução externa via shell.',
'  - **Proteção contra Path Traversal**: Validação de caminhos canônicos absolutos ao navegar no explorer e carregar pastas de logs.',
'  - **Application Manifest Integrado**: Nível de privilégio formalizado (`asInvoker`), prevenção de virtualização UAC e suporte nativo a PerMonitorV2 DPI.',
'- **Identidade Visual**:',
'  - Símbolo oficial TOTVS vetorizado com fundo transparente e ícone de sistema com stroke azul escuro de alto contraste.',
'',
'---',
'',
'## 💼 Sugestão de Copy para Publicação no LinkedIn',
'',
'```text',
'🚀 Lançamento Oficial do TOTVSHub v2.4.0: Produtividade e Segurança para Administradores TOTVS Protheus!',
'',
'Quem gerencia múltiplos ambientes AppServer conhece o desafio: portas duplicadas, chaves concorrentes no appserver.ini e alternância manual entre consoles e serviços do Windows.',
'',
'O TOTVSHub foi desenvolvido para simplificar essa rotina com segurança e eficiência:',
'🔹 Monitoramento em tempo real de serviços e consoles Protheus com identificação de PID;',
'🔹 Editor visual de appserver.ini com layout responsivo em Cards (Masonry) ou Lista;',
'🔹 Gravação atômica segura contra corrupção de arquivos de configuração;',
'🔹 Detecção automática de conflitos de portas e chaves monitoradas;',
'🔹 Atalhos web diretos para Protheus WebApp, REST e WebServices;',
'🔹 100% nativo para Windows 10 e 11, sem instaladores pesados ou dependências externas.',
'',
'📦 Baixe a versão oficial e confira a integridade SHA-256 no GitHub:',
'[Link do Repositório ou Release no GitHub]',
'',
'#TOTVS #Protheus #ERP #Dev #DotNet #SoftwareEngineering #SysAdmin #Tech',
'```'
)

$releasePath = Join-Path $PSScriptRoot 'RELEASE_NOTES.md'
[System.IO.File]::WriteAllLines($releasePath, $lines, [System.Text.Encoding]::UTF8)

Write-Host '[SUCESSO] RELEASE_NOTES.md gerado com sucesso!' -ForegroundColor Green
Write-Host 'Abra o arquivo RELEASE_NOTES.md para copiar os hashes e a mensagem do post.' -ForegroundColor Cyan
