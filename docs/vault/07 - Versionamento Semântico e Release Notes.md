# 🏷️ TOTVSHub — Versionamento Semântico e Release Notes

O ciclo de versões do TOTVSHub segue a especificação internacional **Semantic Versioning 2.0.0 (SemVer)**:
`MAJOR.MINOR.PATCH`

---

## 📐 Regras de Incremento SemVer

- **MAJOR (X.0.0)**: Mudanças incompatíveis com versões anteriores (ex: reescrita completa que altere o formato das configurações persistidas sem migração).
- **MINOR (2.X.0)**: Adição de novas funcionalidades compatíveis com versões anteriores (novos layouts, novos instaladores, suporte a novos navegadores, integração à bandeja, pipeline automatizado).
- **PATCH (2.4.X)**: Correções de bugs retrocompatíveis sem adição de funcionalidades substanciais.

### Por que o projeto evoluiu de 2.4.0 para 2.5.0?
Desde o lançamento da versão `v2.4.0`, o projeto recebeu um conjunto significativo de novas funcionalidades e melhorias estruturais:
1. **Redesenho do System Tray (Bandeja)**: Menu escuro sem margens brancas, exibição de ambientes ativos diretamente no miolo sem submenus e ativação imediata no clique único.
2. **Navegador Padrão com Delay de 3s**: Mapeamento do `UserChoice` no registro e abertura suave dos serviços Web.
3. **Parada e Reinício Robustos de Processos**: Estratégia em camadas (`ServiceController`, `p.Kill`, `taskkill /F /T`, `net stop`) e privilégios `requireAdministrator`.
4. **Refinamento de Design e Alinhamento**: Badges de status e usuário perfeitamente simétricos (84px), cards com altura uniforme mesmo sem anotações, e ToolTip exibindo nome completo e PID no mouse hover.
5. **Automação do Pipeline de Release**: Scripts dinâmicos que sincronizam automaticamente versões sem necessidade de edição manual repetida.

Conforme a norma SemVer, o acúmulo de novas funcionalidades retrocompatíveis exige o incremento da versão **MINOR**, justificando formalmente a versão **v2.5.0**.

---

## 🔄 Sincronização da Versão nos Artefatos do Projeto

A versão é centralizada e sincronizada entre:
- `Theme/UiTheme.cs`: `AppVersion = "v2.5.0"`
- `assets/app.manifest`: `version="2.5.0.0"`
- `scripts/installer.iss`: `#define MyAppVersion "2.5.0"`
- `scripts/gerar_instalador.bat`: Extrai a versão via `MyAppVersion`
- `scripts/gerar_release.ps1`: Extrai a versão via `MyAppVersion` e monta `Output\TOTVSHub_Setup_v2.5.0.exe` e `RELEASE_NOTES.md`
- `pipeline.bat`: Valida e gera os artefatos com base na versão dinâmica
