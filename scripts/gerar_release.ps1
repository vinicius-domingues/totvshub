$ErrorActionPreference = 'Stop'

Write-Host '========================================================' -ForegroundColor Cyan
Write-Host '   TOTVSHub - Gerador de Release e Checksum SHA-256    ' -ForegroundColor Cyan
Write-Host '========================================================' -ForegroundColor Cyan

$rootDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

$issPath = Join-Path $rootDir 'scripts\installer.iss'
$version = '2.5.0'
if (Test-Path $issPath) {
    $match = Select-String -Path $issPath -Pattern '#define MyAppVersion "([^"]+)"'
    if ($match -and $match.Matches.Groups.Count -gt 1) {
        $version = $match.Matches.Groups[1].Value
    }
}

$portableExe = Join-Path $rootDir 'Dist_Protected\TOTVSHub.exe'
if (-not (Test-Path $portableExe)) {
    $portableExe = Join-Path $rootDir 'TOTVSHub.exe'
}

$setupName = "TOTVSHub_Setup_v$version.exe"
$setupExe = Join-Path $rootDir "Output\$setupName"

$lines = @(
"# TOTVSHub - Release Oficial v$version",
'',
'## 🛡️ Verificação de Integridade (SHA-256)',
'```text'
)

if (Test-Path $setupExe) {
    $setupHash = (Get-FileHash -Path $setupExe -Algorithm SHA256).Hash
    Write-Host "[INFO] Instalador: $setupName" -ForegroundColor Gray
    Write-Host "  Hash: $setupHash" -ForegroundColor Green
    $lines += "Instalador: $setupName"
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
"Get-FileHash .\$setupName -Algorithm SHA256",
'```',
'',
'---',
'',
"## 🚀 Novidades desta Versão (v$version)",
'- **System Tray (Bandeja do Windows) Totalmente Reformulada**:',
'  - Menu em Dark Theme nativo sem bordas ou margens brancas.',
'  - Ambientes ativos exibidos diretamente no miolo principal sem submenus ou setas.',
'  - 1 clique direto para abrir Protheus WebApp ou encerrar o AppServer.',
'  - Clique único com botão esquerdo restaura instantaneamente a janela em primeiro plano (compatível com Windows 11).',
'- **Automação do Navegador Padrão do Usuário**:',
'  - Detecção inteligente do navegador configurado no Windows (Edge, Chrome, Firefox) via Registro.',
'  - Delay de inicialização de 3 segundos garantindo que o AppServer suba antes da página carregar.',
'- **Controle e Encerramento Robusto de Processos**:',
'  - Elevação com `requireAdministrator` no manifesto da aplicação.',
'  - Desligamento em camadas com `ServiceController`, `Process.Kill()`, fallback para `taskkill /F /T`, `net stop` e varredura da árvore de processos.',
'- **Refinamento Visual e Geometria Alinhada**:',
'  - Badges de Status (RUNNING) e Usuário com largura e formato cápsula idênticos (84px). Truncamento inteligente com reticências (`...`) para nomes longos.',
'  - Exibição de Nome Completo + PID ao passar o mouse (ToolTip) sobre as cápsulas.',
'  - Altura uniforme entre cards com e sem anotações/comentários.',
'  - Símbolo `:` para associação de chave e seção nas regras de configurações.',
'  - Concordância gramatical perfeita (singular/plural) no contador de ambientes ativos.',
'- **Pipeline de Build e Versionamento Dinâmico**:',
'  - `pipeline.bat` integrado que sincroniza versão entre manifesto, UI, instalador Inno Setup e notas de release.',
'  - Proteção anti-reversa via Obfuscar e assinatura digital com carimbo de tempo DigiCert.',
'',
'---',
'',
'## 💼 Sugestão de Copy para Publicação no LinkedIn',
'',
'```text',
"🚀 Lançamento Oficial do TOTVSHub v${version}: Produtividade e Segurança para Administradores TOTVS Protheus!",
'',
'Quem gerencia múltiplos ambientes AppServer conhece a rotina: portas concorrentes, chaves espalhadas no appserver.ini e alternância manual entre consoles e serviços do Windows.',
'',
'O TOTVSHub foi atualizado com melhorias de usabilidade e performance:',
'',
'🔹 Acesso rápido na bandeja do sistema: veja ambientes ativos no miolo e abra o WebApp em 1 clique;',
'🔹 Restauração imediata em primeiro plano e integração ao navegador padrão do usuário com delay automático;',
'🔹 Encerramento e reinício infalíveis de consoles e serviços Protheus via controle elevado de processos;',
'🔹 Cards com alturas uniformes, badges alinhados e PID revelado discretamente no tooltip;',
'🔹 Editor visual de appserver.ini com layout Masonry e gravação atômica anti-corrupção;',
'🔹 Instalador oficial assinado digitalmente e binário portátil protegido contra engenharia reversa.',
'',
'📦 Baixe a versão oficial (com integridade SHA-256 verificável):',
"https://github.com/vinicius-domingues/totvshub/releases/tag/v$version",
'',
'#TOTVS #Protheus #ERP #Dev #DotNet #SoftwareEngineering #SysAdmin #OpenSource #Tech',
'```'
)

$releasePath = Join-Path $rootDir 'RELEASE_NOTES.md'
[System.IO.File]::WriteAllLines($releasePath, $lines, [System.Text.Encoding]::UTF8)

Write-Host ''
Write-Host '[SUCESSO] RELEASE_NOTES.md atualizado com os dois binarios!' -ForegroundColor Green
