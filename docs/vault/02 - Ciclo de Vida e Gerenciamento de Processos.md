# ⚙️ TOTVSHub — Ciclo de Vida e Gerenciamento de Processos

O controle de servidores AppServer do TOTVS Protheus é uma das funções mais críticas do TOTVSHub, exigindo lidar tanto com processos em primeiro plano (console) quanto serviços nativos do Windows (`ServiceController`).

---

## 🔍 Descoberta e Associação de Processos

1. **Correspondência por Caminho Canônico**: O scanner mapeia o executável do AppServer (`appserver.exe` ou nome customizado no `.ini`) comparando o caminho absoluto normalizado (`Path.GetFullPath`) com a lista de processos ativos no Windows.
2. **Descoberta do Usuário e PID**:
   - Através de `NativeWin32.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION)` e `OpenProcessToken`, o sistema inspeciona o token de segurança de cada processo ativo para obter o SID do proprietário e convertê-lo em nome de usuário legível (`Dominio\Usuario`).
   - Evita falhas de segurança e não trava a interface mesmo para processos do sistema `NT AUTHORITY\SYSTEM`.

---

## 🛑 Encerramento Robusto em Camadas (Tiered Shutdown)

Encerrar um AppServer Protheus pode falhar caso haja conexões presas ou permissões elevadas. Por isso, o método `PararPorPidOuServico` implementa uma estratégia de 4 camadas:

### 1ª Camada: Parada Graciosa via `ServiceController`
- Caso o ambiente esteja registrado e rodando como serviço Windows:
  - Invoca `sc.Stop()`.
  - Aguarda até 5 segundos via `sc.WaitForStatus(ServiceControllerStatus.Stopped)`.

### 2ª Camada: Encerramento de Processo Gerenciado
- Se o PID estiver disponível:
  - Tenta `Process.GetProcessById(pid).Kill()`.
  - Aguarda até 3 segundos via `p.WaitForExit(3000)`.

### 3ª Camada: Fallback Forçado via `taskkill.exe /F /T`
- Caso o processo ainda esteja ativo (por exemplo, travado em I/O ou thread morta):
  - Executa `taskkill.exe /F /T /PID <pid>`.
  - O parâmetro `/T` mata toda a árvore de processos filhos (como instâncias subsidiárias de DBAccess ou threads zumbis).
  - Executa também `net stop <nome_servico>` caso aplicável.

### 4ª Camada: Varredura de Sobreviventes pelo Caminho do Executável
- Por fim, executa uma varredura de segurança procurando qualquer instância de `appserver.exe` cujo módulo principal aponte para o diretório daquele ambiente específico, eliminando qualquer processo remanescente.

---

## 🛡️ Elevação Administrativa (`requireAdministrator`)

Em ambientes corporativos, servidores AppServer frequentemente rodam sob contas de serviço com privilégios de Administrador.
Para que o TOTVSHub possa interagir, parar e reiniciar esses processos sem erros de acesso negado (`UnauthorizedAccessException`), o arquivo `assets/app.manifest` define:
```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```
Isso garante permissão irrestrita para `taskkill /F`, manipulação de serviços e abertura de portas locais.
