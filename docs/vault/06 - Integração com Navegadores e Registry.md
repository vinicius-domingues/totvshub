# 🌐 TOTVSHub — Integração com Navegadores e Registry

Para proporcionar abertura ágil do Protheus WebApp e dos endpoints REST, o TOTVSHub integra-se diretamente com o subsistema de associações de protocolo do Windows.

---

## 🧭 Descoberta do Navegador Padrão via Registro do Windows

Quando o TOTVSHub é executado com privilégios elevados (`requireAdministrator`), um simples `Process.Start(url)` executado pelo shell padrão do Windows pode falhar ao abrir a sessão do usuário ou tentar abrir no Internet Explorer obsoleto.

Para contornar essa limitação, o método `AbrirNavegadorPadraoDoSistema` consulta a chave de registro do usuário corrente:
```text
HKCU\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice -> ProgId
```

A correspondência do `ProgId` identifica o executável real configurado pelo usuário:
1. **Microsoft Edge**: `AppX...` ou `MSEdgeHTM` ➔ Executa `msedge.exe --single-argument <url>`.
2. **Google Chrome**: `ChromeHTML` ➔ Executa `chrome.exe --single-argument <url>`.
3. **Mozilla Firefox**: `FirefoxURL...` ➔ Executa `firefox.exe <url>`.
4. **Brave / Opera**: Consulta caminhos no `App Paths` do Registro do Windows.
5. **Fallback Seguro**: Caso nenhum seja detectado, executa `ProcessStartInfo` com `UseShellExecute = true`.

---

## ⏱️ Delay de Inicialização de 3 Segundos

Ao iniciar um AppServer (ou clicar para abrir o WebApp em um ambiente recém-iniciado), o serviço Protheus leva alguns instantes para carregar o repositório de objetos (RPO), inicializar as threads de worker e abrir o socket HTTP.

Se o navegador for aberto instantaneamente, o usuário receberia a página de erro `ERR_CONNECTION_REFUSED`.
Para solucionar isso:
- O TOTVSHub introduz um temporizador assíncrono de **3 segundos** (`await Task.Delay(3000)` ou timer configurável nas Configurações).
- Garante que a requisição chegue ao AppServer exatamente quando o listener HTTP estiver pronto para responder.
