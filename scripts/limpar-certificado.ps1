<#
.SYNOPSIS
    Remove desta máquina tudo que faz o certificado de assinatura ser "reconhecido",
    simulando uma máquina nova para testar o fluxo de primeira execução.

.DESCRIPTION
    Remove:
    - O certificado do repositório pessoal (Cert:\CurrentUser\My), de onde
      scripts/assinar-executavel.ps1 o lê/reaproveita a cada "make release".
    - O certificado do repositório de confiança (Cert:\CurrentUser\Root), que é o que
      faz o Windows parar de mostrar "Editor desconhecido".
    - O marcador de "já tentei confiar" (%LOCALAPPDATA%\AuditorFiscal\config), que faz
      o Apura Fiscal.exe pular a tentativa de auto-confiança em aberturas seguintes.

    Depois disso, tanto "make release" (gera um certificado novo) quanto abrir o
    Apura Fiscal.exe direto (tenta se auto-confiar de novo, mostrando a caixa nativa
    do Windows) voltam a se comportar como numa máquina que nunca viu o app.
#>
[CmdletBinding()]
param(
    [string]$AssuntoCertificado = "CN=Apura Fiscal (autoassinado)"
)

$ErrorActionPreference = "Stop"

function Remover-DaLoja {
    param([string]$NomeLoja, [System.Security.Cryptography.X509Certificates.StoreName]$Loja)

    $store = [System.Security.Cryptography.X509Certificates.X509Store]::new(
        $Loja, [System.Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser)
    $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)

    $encontrados = $store.Certificates | Where-Object { $_.Subject -eq $AssuntoCertificado }
    foreach ($cert in $encontrados) {
        $store.Remove($cert)
        Write-Host "Removido de $NomeLoja`: thumbprint $($cert.Thumbprint)"
    }
    if ($encontrados.Count -eq 0) {
        Write-Host "Nada encontrado em $NomeLoja (já estava limpo)."
    }

    $store.Close()
}

Remover-DaLoja -NomeLoja "Cert:\CurrentUser\My" -Loja ([System.Security.Cryptography.X509Certificates.StoreName]::My)
Remover-DaLoja -NomeLoja "Cert:\CurrentUser\Root" -Loja ([System.Security.Cryptography.X509Certificates.StoreName]::Root)

$caminhoMarcador = Join-Path $env:LOCALAPPDATA "AuditorFiscal\config\.confianca-certificado-tentada"
if (Test-Path $caminhoMarcador) {
    Remove-Item $caminhoMarcador -Force
    Write-Host "Marcador de confiança removido: $caminhoMarcador"
} else {
    Write-Host "Marcador de confiança já não existia."
}

Write-Host ""
Write-Host "Pronto. Esta máquina agora se comporta como se nunca tivesse visto o certificado."
Write-Host "Rode 'make release' para gerar um certificado novo, ou abra um Apura Fiscal.exe"
Write-Host "já publicado para ver a caixa de confiança do Windows aparecer de novo."
