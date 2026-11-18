# TOTVSHub - Release Oficial v2.5.0

## 🛡️ Verificação de Integridade (SHA-256)
```text
Instalador: TOTVSHub_Setup_v2.5.0.exe
SHA-256:    100E7F6287A86E85F9E32FBD73D2BF3D67A9313C30F3E8B5ECBA64591B55859E

Portátil:   TOTVSHub.exe
SHA-256:    2FB8E9760DD763E3244BF2AE44493B8DBFFCCA0BA73D21FB76D6C98B3D126AAC
```

Para verificar no PowerShell:
```powershell
Get-FileHash .\TOTVSHub_Setup_v2.5.0.exe -Algorithm SHA256
```

---

## 🚀 Novidades desta Versão (v2.5.0)
- **System Tray (Bandeja do Windows) Totalmente Reformulada**:
  - Menu em Dark Theme nativo sem bordas ou margens brancas.
  - Ambientes ativos exibidos diretamente no miolo principal sem submenus ou setas.
  - 1 clique direto para abrir Protheus WebApp ou encerrar o AppServer.
  - Clique único com botão esquerdo restaura instantaneamente a janela em primeiro plano (compatível com Windows 11).
- **Automação do Navegador Padrão do Usuário**:
  - Detecção inteligente do navegador configurado no Windows (Edge, Chrome, Firefox) via Registro.
  - Delay de inicialização de 3 segundos garantindo que o AppServer suba antes da página carregar.
- **Controle e Encerramento Robusto de Processos**:
  - Elevação com `requireAdministrator` no manifesto da aplicação.
  - Desligamento em camadas com `ServiceController`, `Process.Kill()`, fallback para `taskkill /F /T`, `net stop` e varredura da árvore de processos.
- **Refinamento Visual e Geometria Alinhada**:
  - Badges de Status (RUNNING) e Usuário com largura e formato cápsula idênticos (84px). Truncamento inteligente com reticências (`...`) para nomes longos.
  - Exibição de Nome Completo + PID ao passar o mouse (ToolTip) sobre as cápsulas.
  - Altura uniforme entre cards com e sem anotações/comentários.
  - Símbolo `:` para associação de chave e seção nas regras de configurações.
  - Concordância gramatical perfeita (singular/plural) no contador de ambientes ativos.
- **Pipeline de Build e Versionamento Dinâmico**:
  - `pipeline.bat` integrado que sincroniza versão entre manifesto, UI, instalador Inno Setup e notas de release.
  - Proteção anti-reversa via Obfuscar e assinatura digital com carimbo de tempo DigiCert.

---

## 💼 Sugestão de Copy para Publicação no LinkedIn

```text
🚀 Lançamento Oficial do TOTVSHub v2.5.0: Produtividade e Segurança para Administradores TOTVS Protheus!

Quem gerencia múltiplos ambientes AppServer conhece a rotina: portas concorrentes, chaves espalhadas no appserver.ini e alternância manual entre consoles e serviços do Windows.

O TOTVSHub foi atualizado com melhorias de usabilidade e performance:

🔹 Acesso rápido na bandeja do sistema: veja ambientes ativos no miolo e abra o WebApp em 1 clique;
🔹 Restauração imediata em primeiro plano e integração ao navegador padrão do usuário com delay automático;
🔹 Encerramento e reinício infalíveis de consoles e serviços Protheus via controle elevado de processos;
🔹 Cards com alturas uniformes, badges alinhados e PID revelado discretamente no tooltip;
🔹 Editor visual de appserver.ini com layout Masonry e gravação atômica anti-corrupção;
🔹 Instalador oficial assinado digitalmente e binário portátil protegido contra engenharia reversa.

📦 Baixe a versão oficial (com integridade SHA-256 verificável):
https://github.com/vinicius-domingues/totvshub/releases/tag/v2.5.0

#TOTVS #Protheus #ERP #Dev #DotNet #SoftwareEngineering #SysAdmin #OpenSource #Tech
```
