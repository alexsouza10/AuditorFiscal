@echo off
setlocal

if not exist "%~dp0confiar-neste-certificado.cer" (
    echo.
    echo ERRO: nao encontrei o arquivo "confiar-neste-certificado.cer" nesta pasta.
    echo.
    echo Isso costuma acontecer quando este .cmd e executado direto de dentro do
    echo arquivo .zip, sem extrair primeiro ^(o Windows roda so este arquivo isolado
    echo numa pasta temporaria, sem os arquivos vizinhos^).
    echo.
    echo Corrija assim: feche esta janela, clique com o botao direito no arquivo
    echo .zip que voce baixou, escolha "Extrair Tudo..." e rode o
    echo "Instalar e Abrir.cmd" de dentro da pasta que foi extraida.
    echo.
    pause
    exit /b 1
)

if not exist "%~dp0AuditorFiscal.exe" (
    echo.
    echo ERRO: nao encontrei o arquivo "AuditorFiscal.exe" nesta pasta. Extraia o
    echo .zip inteiro para uma pasta antes de rodar este instalador.
    echo.
    pause
    exit /b 1
)

echo Instalando certificado de confianca (necessario so na primeira vez nesta maquina)...
echo Se aparecer uma caixa de seguranca do Windows perguntando se confia no certificado
echo "APURA Fiscal (autoassinado)", clique em SIM/YES para instalar automaticamente.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Import-Certificate -FilePath '%~dp0confiar-neste-certificado.cer' -CertStoreLocation Cert:\CurrentUser\Root -ErrorAction Stop | Out-Null; exit 0 } catch { Write-Host ('Motivo: ' + $_.Exception.Message); exit 1 }"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Nao foi possivel instalar o certificado automaticamente ^(motivo acima -- o mais
    echo comum e ter clicado em "Nao"/"Cancelar" na caixa de seguranca do Windows^).
    echo Sem problema - o app ainda abre normalmente assim mesmo: se o Windows avisar
    echo "Editor desconhecido", clique em "Mais informacoes" e depois em "Executar
    echo assim mesmo".
    echo.
    pause
)

echo Abrindo o APURA Fiscal...
start "" "%~dp0AuditorFiscal.exe"
