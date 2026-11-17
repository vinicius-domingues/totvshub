$ErrorActionPreference = 'Stop'

Write-Host '========================================================' -ForegroundColor Cyan
Write-Host '   TOTVSHub - Gerador de Release e Checksum SHA-256    ' -ForegroundColor Cyan
Write-Host '========================================================' -ForegroundColor Cyan

$portableExe = Join-Path $PSScriptRoot 'Dist_Protected\TOTVSHub.exe'
if (-not (Test-Path $portableExe)) {
    $portableExe = Join-Path $PSScriptRoot 'TOTVSHub.exe'
}

$setupExe = Join-Path $PSScriptRoot 'Output\TOTVSHub_Setup_v2.4.0.exe'

$lines = @(
'# TOTVSHub - Release Oficial v2.4.0',
'',
'## 🛡️ Verificação de Integridade (SHA-256)',
'```text'
)

if (Test-Path $setupExe) {
    $setupHash = (Get-FileHash -Path $setupExe -Algorithm SHA256).Hash
    Write-Host "[INFO] Instalador: TOTVSHub_Setup_v2.4.0.exe" -ForegroundColor Gray
    Write-Host "  Hash: $setupHash" -ForegroundColor Green
    $lines += "Instalador: TOTVSHub_Setup_v2.4.0.exe"
    $lines += "SHA-256:    $setupHash"
    $lines += ""
}

if (Test-Path $portableExe) {
    $portHash = (Get-FileHash -Path $portableExe -Algorithm SHA256).Hash
    Write-Host "[INFO] Portatil:   TOTVSHub.exe (Protegido)" -ForegroundColor Gray
    Write-Host "  Hash: $portHash" -ForegroundColor Green
    $lines += "Portátil:   TOTVSHub.exe"
    $lines += "SHA-256:    $portHash"
}

$lines += @(
'```',
'',
'Para verificar no PowerShell:',
'```powershell',
'Get-FileHash .\TOTVSHub_Setup_v2.4.0.exe -Algorithm SHA256',
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
'  - **Código Protegido contra Engenharia Reversa**: Ofuscado com achatamento de fluxo e criptografia de strings via Obfuscar.',
'  - **Assinatura Digital Authenticode**: Carimbo de tempo RFC 3161 DigiCert garantindo procedência e integridade.',
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
'Quem gerencia múltiplos ambientes AppServer conhece a rotina: portas concorrentes, chaves espalhadas no appserver.ini e alternância manual entre consoles e serviços do Windows.',
'',
'O TOTVSHub foi desenvolvido para simplificar essa operação com foco em segurança, rapidez e visual limpo:',
'',
'🔹 Monitoramento em tempo real de serviços e consoles Protheus com identificação do PID;',
'🔹 Editor visual de appserver.ini: layout em Cards Dinâmicos (Masonry) ou Lista tradicional;',
'🔹 Alternador de exibição na tela inicial (Grid vs. Lista);',
'🔹 Gravação atômica segura: prevenção física contra corrupção do .ini em quedas de energia;',
'🔹 Sanitização e validação de URLs contra execução maliciosa de comandos;',
'🔹 Instalador oficial com assistente e atalhos, ou executável portátil protegido;',
'🔹 100% nativo para Windows 10 e 11, sem dependências pesadas.',
'',
'📦 Baixe a versão oficial (com integridade SHA-256 verificável):',
'https://github.com/vinicius-domingues/totvshub/releases/tag/v2.4.0',
'',
'#TOTVS #Protheus #ERP #Dev #DotNet #SoftwareEngineering #SysAdmin #OpenSource #Tech',
'```'
)

$releasePath = Join-Path $PSScriptRoot 'RELEASE_NOTES.md'
[System.IO.File]::WriteAllLines($releasePath, $lines, [System.Text.Encoding]::UTF8)

Write-Host ''
Write-Host '[SUCESSO] RELEASE_NOTES.md atualizado com os dois binarios!' -ForegroundColor Green
