# TOTVSHub - Release Oficial v2.5.0

## 🛡️ Verificação de Integridade (SHA-256)

### 🏢 1. Release Corporativa (TOTVSHub Corp)
> **Recomendada para ambientes corporativos (TOTVS / TI corporativa / EPM):**
> Compilado com manifesto `asInvoker` (roda diretamente como usuário comum, sem solicitar elevação UAC e sem acionar bloqueios de Privilege Management corporativo como BeyondTrust/CyberArk/Avecto).

```text
Arquivo: Release_Corp\TOTVSHub.exe
SHA-256: 131B87EF39B14B27E898CF29EB9994740859866271852AAAB5E84722F71EE09A
```

Para verificar no PowerShell:
```powershell
Get-FileHash .\Release_Corp\TOTVSHub.exe -Algorithm SHA256
```

---

### ⚡ 2. Release Total / Administrador (TOTVSHub All)
> **Para máquinas com acesso de Administrador completo:**
> Compilado com manifesto `requireAdministrator` (executa com privilégios de administrador para controle nativo de serviços de sistema do Windows).

```text
Arquivo: Release_All\TOTVSHub.exe
SHA-256: 62DFE8C3AA3BA51B1619A26540D0DCF286103F077CABC04A82E59157B159AFD7
```

Para verificar no PowerShell:
```powershell
Get-FileHash .\Release_All\TOTVSHub.exe -Algorithm SHA256
```

---

## 🚀 Novidades desta Versão (v2.5.0)
- **Modo Corporativo Nativo (`asInvoker`)**: Execução suave em notebooks corporativos protegidos por políticas de segurança de TI e Privilege Management.
- **System Tray (Bandeja do Windows) Totalmente Reformulada**:
  - Menu em Dark Theme nativo sem bordas ou margens brancas.
  - Ambientes ativos exibidos diretamente no miolo principal sem submenus ou setas.
  - 1 clique direto para abrir Protheus WebApp ou encerrar o AppServer.
  - Clique único com botão esquerdo restaura instantaneamente a janela em primeiro plano (compatível com Windows 11).
- **Automação do Navegador Padrão do Usuário**:
  - Detecção inteligente do navegador configurado no Windows (Edge, Chrome, Firefox) via Registro.
  - Delay de inicialização de 3 segundos garantindo que o AppServer suba antes da página carregar.
- **Controle e Encerramento Robusto de Processos**:
  - Desligamento em camadas com `ServiceController`, `Process.Kill()`, fallback para `taskkill /F /T`, `net stop` e varredura da árvore de processos.
- **Refinamento Visual e Geometria Alinhada**:
  - Badges de Status (RUNNING) e Usuário com largura e formato cápsula idênticos (84px). Truncamento inteligente com reticências (`...`) para nomes longos.
  - Exibição de Nome Completo + PID ao passar o mouse (ToolTip) sobre as cápsulas.
  - Altura uniforme entre cards com e sem anotações/comentários.
  - Símbolo `:` para associação de chave e seção nas regras de configurações.
  - Concordância gramatical perfeita (singular/plural) no contador de ambientes ativos.
- **Pipeline de Build e Versionamento Dinâmico**:
  - `pipeline.bat` integrado que sincroniza versão entre manifesto, UI, instalador Inno Setup e notas de release.
  - Assinatura digital Authenticode com carimbo de tempo DigiCert.

---

## 💼 Sugestão de Copy para Publicação no LinkedIn

```text
🚀 Lançamento Oficial do TOTVSHub v2.5.0: Produtividade e Segurança para Administradores TOTVS Protheus!

Quem gerencia múltiplos ambientes AppServer conhece a rotina: portas concorrentes, chaves espalhadas no appserver.ini e alternância manual entre consoles e serviços do Windows.

O TOTVSHub foi atualizado com melhorias de usabilidade e performance:

🔹 Edição Corporativa (asInvoker): Roda tranquilamente sem exigir permissão de admin corporativo nem UAC;
🔹 Acesso rápido na bandeja do sistema: veja ambientes ativos no miolo e abra o WebApp em 1 clique;
🔹 Restauração imediata em primeiro plano e integração ao navegador padrão do usuário com delay automático;
🔹 Encerramento e reinício infalíveis de consoles e serviços Protheus via controle de processos em camadas;
🔹 Cards com alturas uniformes, badges alinhados e PID revelado discretamente no tooltip;
🔹 Editor visual de appserver.ini com layout Masonry e gravação atômica anti-corrupção.

📦 Baixe a versão oficial (com integridade SHA-256 verificável):
https://github.com/vinicius-domingues/totvshub/releases/tag/v2.5.0

#TOTVS #Protheus #ERP #Dev #DotNet #SoftwareEngineering #SysAdmin #OpenSource #Tech
```
