# 🎨 TOTVSHub — Interface Gráfica, Cards e System Tray

A experiência visual do TOTVSHub combina a fluidez do **WPF** com o acabamento refinado de paletas escuras modernas (Tailwind Slate-900).

---

## 🃏 Grid de Cards e Layout Masonry

- **2 Colunas Dinâmicas na Tela Inicial**: Os cards de ambientes são distribuídos de forma equilibrada entre colunas, preenchendo o espaço vertical de maneira inteligente.
- **Alternador de Visualização**: Botão na barra de ferramentas permite alternar entre **Modo Cards** e **Modo Lista corrida**.
- **Cards com Altura Uniforme**:
  - Cards que não possuem anotações/comentários mantêm a mesma altura de cards que possuem (`Visibility = Visibility.Hidden` com dimensões idênticas reservadas).
  - Isso evita desníveis visuais ou deslocamento repentino de elementos ao adicionar notas.

---

## 💊 Badges em Cápsula (Status e Usuário)

- **Geometria Perfeitamente Alinhada**:
  - `statusBadge` (`● RUNNING`, `● STOPPED`, etc.): Largura fixa de 84px, altura de 22px, `CornerRadius(10)`.
  - `userBadge` (`👤 usuario`): Largura idêntica de 84px, altura de 22px, `CornerRadius(10)`.
  - Ambos alinhados à esquerda abaixo dos ícones de ação.
- **Truncamento com Reticências (`...`)**:
  - Nomes de usuário extensos recebem `TextTrimming.CharacterEllipsis` e `TextWrapping.NoWrap`, nunca estourando o card.
- **Tooltip Completo no Mouse Hover**:
  - Ao posicionar o cursor sobre o badge de usuário ou o badge de status, um ToolTip detalhado é exibido com:
    `Usuário: <nome_completo> • PID: <pid>`
  - Isso removeu a necessidade de opções redundantes na tela de Configurações, mantendo a tela inicial limpa e as informações essenciais a apenas um toque do mouse.

---

## 📥 System Tray (Bandeja do Windows) Totalmente Escura

O menu da bandeja (`_trayMenu`) utiliza um renderizador customizado (`DarkTrayRenderer : ToolStripProfessionalRenderer`) com melhorias de alto nível:

1. **Eliminação de Bordas e Margens Brancas**:
   - `ShowImageMargin = false` e `ShowCheckMargin = false`.
   - `BackColor = Color.FromArgb(15, 23, 42)` (`#0f172a`).
   - Sobrescrita de `OnRenderSeparator`, `OnRenderImageMargin` e `OnRenderItemText` para pintar o fundo uniformemente sem artefatos brancos nativos do WinForms.
2. **Ambientes Ativos no Miolo (Sem Submenus)**:
   - Os ambientes em execução aparecem diretamente na lista principal do menu:
     - `🟢 NomeAmbiente (:Porta)` (1 clique abre o Protheus WebApp no navegador).
     - `   🌐 REST (:Porta/rest)` (se configurado, abre o endpoint REST).
     - `   ⏹️ Parar NomeAmbiente` (1 clique encerra o AppServer).
   - Se nenhum ambiente estiver rodando: exibe `⚪ Nenhum ambiente ativo no momento`.
   - Removeu o antigo submenu aninhado com seta e removeu "Todos os Ambientes".
3. **Clique do Botão Esquerdo**:
   - Clique com o botão esquerdo do mouse restaura e traz a janela imediatamente para o primeiro plano (contornando os bloqueios de ativação de foreground do Windows 11 via `NativeWin32.ShowWindow(SW_RESTORE)` e `NativeWin32.SetForegroundWindow`).
   - Clique com o botão direito abre o menu escuro com opções rápidas.
