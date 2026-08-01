@echo off
setlocal

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
