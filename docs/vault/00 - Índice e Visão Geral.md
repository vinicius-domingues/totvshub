# 🚀 TOTVSHub — Índice e Visão Geral

**TOTVSHub** é uma aplicação desktop nativa Windows (.NET Framework 4.8 / WPF / Win32) desenvolvida para administradores, desenvolvedores e consultores do ecossistema **TOTVS Protheus**.

O sistema resolve a complexidade operacional de gerenciar dezenas de servidores **AppServer**, centralizando o controle de instâncias locais, portas de comunicação, serviços Windows, consoles em primeiro plano e edição segura de arquivos `appserver.ini`.

---

## 🗺️ Mapa do Conhecimento (Obsidian Vault)

- [[01 - Arquitetura e Engenharia do Sistema]]: Estrutura de pastas, partial classes, modelo de threads e interoperabilidade Win32 P/Invoke.
- [[02 - Ciclo de Vida e Gerenciamento de Processos]]: Detecção de consoles vs serviços Windows, PID discovery, desligamento multinível com fallback (`taskkill /F /T`, `net stop`, process tree sweep) e elevação `requireAdministrator`.
- [[03 - Scanner, Configurações e Gravação Atômica]]: Varredura de pastas de instalação, dedução inteligente de portas (WebApp, REST, TCP), regras personalizadas (`[Seção] : Chave`) e gravação física segura com substituição atômica (`.tmp` + `.bak`).
- [[04 - Interface Gráfica, Cards e System Tray]]: Design System em Tailwind Slate Dark Theme, layout dinâmico Masonry de 2 e 3 colunas, badges em formato cápsula perfeitamente alinhados, tooltip revelando usuário + PID, e menu da bandeja sem margens brancas.
- [[05 - Pipeline de Build, Ofuscação e Assinatura]]: Pipeline automatizado `pipeline.bat`, compilação pura sem dependências externas pesadas, ofuscação anti-engenharia reversa (Obfuscar), assinatura digital Authenticode com timestamp DigiCert e instalador Inno Setup.
- [[06 - Integração com Navegadores e Registry]]: Detecção automática do navegador padrão do Windows (Edge, Chrome, Firefox) via chaves de registro `UserChoice` e delay inteligente de 3 segundos para inicialização dos serviços Web.
- [[07 - Versionamento Semântico e Release Notes]]: Padrão SemVer (MAJOR.MINOR.PATCH), sincronização de versão em todos os artefatos e histórico evolutivo do projeto.

---

## ⚙️ Stack Tecnológica

| Componente | Tecnologia | Finalidade |
| :--- | :--- | :--- |
| **Linguagem & Runtime** | C# 7.3 / .NET Framework 4.8 | Compatibilidade nativa Windows 10/11 sem runtime extra |
| **Camada de Apresentação** | WPF (Windows Presentation Foundation) | Renderização acelerada por hardware, animações e layout fluido |
| **Controle de Sistema** | Win32 API (P/Invoke `user32.dll`, `kernel32.dll`, `advapi32.dll`) | Manipulação de janelas, hooks de DPI multimonitor, sinalização única e controle de processos |
| **Bandeja do Sistema** | WinForms `NotifyIcon` + `DarkTrayRenderer` customizado | Execução discreta em segundo plano e acesso imediato |
| **Proteção Anti-Reversa** | Obfuscar | Renomeação de símbolos, ocultação de strings e achatamento de fluxo |
| **Assinatura Digital** | Authenticode + DigiCert RFC 3161 | Validação de autenticidade, integridade de binários e segurança do instalador |
| **Instalador Oficial** | Inno Setup 7 | Assistente moderno multilíngue (PT-BR) com auto-elevação e criação de atalhos |
