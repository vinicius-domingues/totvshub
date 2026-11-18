# 🏛️ TOTVSHub — Arquitetura e Engenharia do Sistema

O TOTVSHub é projetado em uma arquitetura orientada a componentes modulares baseada em **Partial Classes** da janela principal (`MainWindow`), separando responsabilidades de forma coesa sem acoplamento monolítico.

---

## 📂 Organização dos Arquivos e Módulos

```
d:\Projects\totvshub\
├── assets\                   # Recursos estáticos oficiais
│   ├── app.manifest          # Manifesto de aplicação (UAC requireAdministrator, DPI PerMonitorV2)
│   ├── totvs_white.ico       # Ícone oficial empacotado para o executável
│   └── totvs_white.svg       # Símbolo oficial vetorizado da TOTVS
├── Native\
│   └── NativeWin32.cs        # P/Invoke Win32 (ShowWindow, SetForegroundWindow, OpenProcess, etc.)
├── Theme\
│   └── UiTheme.cs            # Design System (Cores Slate, Glifos vetoriais, Versão global v2.5.0)
├── Views\                    # Visões modulares (Partial Classes de MainWindow)
│   ├── MainWindow.Core.cs     # Inicialização, ciclo de vida, persistência e janelas modais
│   ├── MainWindow.Home.cs     # Grid de ambientes, cards dinâmicos, badges e monitoramento
│   ├── MainWindow.IniEditor.cs# Editor visual de .ini, layout Masonry de seções e gravação atômica
│   ├── MainWindow.Logs.cs     # Visualizador de console e logs em tempo real
│   ├── MainWindow.Scanner.cs  # Descoberta de pastas e detecção de chaves/portas no .ini
│   ├── MainWindow.Settings.cs # Painel de configurações, tempos de poll e regras de mapeamento
│   └── MainWindow.Tray.cs     # System Tray (NotifyIcon), dark menu e restauração de janela
├── scripts\                  # Scripts de automação do ciclo de desenvolvimento
│   ├── compilar.bat          # Compilação C# via csc.exe do .NET Framework 4.8
│   ├── ofuscar.bat           # Execução do Obfuscar sobre o binário compilado
│   ├── obfuscar.xml          # Configuração de proteção anti-reversa
│   ├── assinar.bat           # Wrapper para assinatura digital Authenticode
│   ├── assinar.ps1           # Script PowerShell de assinatura com timestamp DigiCert
│   ├── installer.iss         # Script de compilação do instalador Inno Setup
│   ├── gerar_instalador.bat  # Wrapper para geração e assinatura do instalador
│   ├── gerar_release.bat     # Wrapper de geração de release
│   └── gerar_release.ps1     # Geração automática de RELEASE_NOTES.md e cálculo de SHA-256
├── pipeline.bat              # Script mestre que executa todo o fluxo de release em sequência
├── Program.cs                # Entry point com Single Instance Mutex e tratamento de exceções
└── TOTVSHub.exe              # Executável gerado
```

---

## 🧵 Modelo de Threading e Despacho de Eventos

1. **WPF UI Thread**: Gerencia toda a árvore visual, animações e renderização dos controles WPF.
2. **WinForms Message Loop (System Tray)**: O componente `System.Windows.Forms.NotifyIcon` opera através do loop de mensagens do Windows. Quando interações ocorrem no ícone da bandeja, o método `RestaurarJanelaPrincipal()` utiliza `Dispatcher.CheckAccess()` e `Dispatcher.BeginInvoke()` para cruzar a barreira de threads de forma totalmente segura.
3. **Background Scanning**: O monitoramento do estado dos servidores e atualização de status ocorre periodicamente através de timers assíncronos que não bloqueiam a digitação ou renderização da interface.

---

## 🪟 Instância Única (Single Instance)

O TOTVSHub implementa prevenção rigorosa contra múltiplas instâncias através de:
- **`System.Threading.Mutex` com nome global**: Garante que apenas um executável rode por sessão do Windows.
- **`EventWaitHandle` de sinalização (`BringToFrontSignal`)**: Quando uma segunda instância é iniciada, ela sinaliza o evento e encerra imediatamente. A instância ativa captura o sinal via `ThreadPool.RegisterWaitForSingleObject` e restaura a janela automaticamente para o primeiro plano.
