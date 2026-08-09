<#
.SYNOPSIS
    Assina digitalmente o executável publicado com um certificado autoassinado,
    criando o certificado na primeira execução e reaproveitando nas seguintes.

.DESCRIPTION
    Sem assinatura nenhuma, o Windows SmartScreen bloqueia o .exe com "Editor
    desconhecido" em qualquer máquina que não seja a de quem publicou. Assinar com um
    certificado autoassinado já troca "Editor desconhecido" por "Gerenciador de AFT" no
    aviso, e é o primeiro passo caso um certificado pago (que elimina o aviso de vez,
    sem nenhuma etapa manual) seja comprado no futuro.

    Confiar de fato no certificado em cada máquina nova é responsabilidade do próprio
    Gerenciador de AFT.exe na primeira execução (ver Program.cs,
    ConfiarNoProprioCertificadoSeNecessario) — por isso este script não precisa
    exportar .cer nem gerar instruções à parte, só assinar.

    O certificado fica guardado no repositório de certificados do Windows
    (Cert:\CurrentUser\My) da máquina que publica — nunca em disco em texto puro, e
    nunca commitado no git.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$CaminhoExecutavel,

    [Parameter(Mandatory)]
    [string]$PastaPublicacao,

    [string]$AssuntoCertificado = "CN=Gerenciador de AFT (autoassinado)"
)

$ErrorActionPreference = "Stop"

function Obter-OuCriarCertificado {
    $existente = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
        Where-Object { $_.Subject -eq $AssuntoCertificado } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if ($existente -and $existente.NotAfter -gt (Get-Date).AddDays(30)) {
        Write-Host "Reaproveitando certificado existente (válido até $($existente.NotAfter.ToString('dd/MM/yyyy')))."
        return $existente
    }

    Write-Host "Criando novo certificado de assinatura de código autoassinado..."
    return New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $AssuntoCertificado `
        -CertStoreLocation Cert:\CurrentUser\My `
        -KeyUsage DigitalSignature `
        -KeyExportPolicy Exportable `
        -NotAfter (Get-Date).AddYears(5) `
        -FriendlyName "Gerenciador de AFT - assinatura de código"
}

$certificado = Obter-OuCriarCertificado

Write-Host "Assinando $CaminhoExecutavel..."
try {
    $resultado = Set-AuthenticodeSignature -FilePath $CaminhoExecutavel -Certificate $certificado `
        -TimestampServer "http://timestamp.digicert.com" -HashAlgorithm SHA256
} catch {
    Write-Warning "Timestamp server indisponível, assinando sem carimbo de tempo (a assinatura ainda vale, só não sobrevive à expiração do certificado)."
    $resultado = Set-AuthenticodeSignature -FilePath $CaminhoExecutavel -Certificate $certificado -HashAlgorithm SHA256
}

# "UnknownError" com uma cadeia terminando num certificado raiz não confiável é o
# resultado ESPERADO para um certificado autoassinado nesta própria máquina (ela
# também não confia nele automaticamente) — a assinatura foi aplicada normalmente,
# só a validação de confiança que falha. Só é falha de verdade se não assinou nada.
if ($resultado.Status -eq "NotSigned" -or -not $resultado.SignerCertificate) {
    throw "Falha ao assinar: $($resultado.StatusMessage)"
}

Write-Host "Assinatura aplicada (status de confiança: $($resultado.Status) — esperado para certificado autoassinado; o próprio .exe cuida de se autoconfiar na primeira execução)."
Write-Host "Assinatura aplicada com sucesso (thumbprint: $($certificado.Thumbprint))."
