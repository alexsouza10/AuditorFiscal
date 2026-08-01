<#
.SYNOPSIS
    Assina digitalmente o executável publicado com um certificado autoassinado,
    criando o certificado na primeira execução e reaproveitando nas seguintes.

.DESCRIPTION
    Sem assinatura nenhuma, o Windows SmartScreen bloqueia o .exe com "Editor
    desconhecido" em qualquer máquina que não seja a de quem publicou. Assinar com
    um certificado autoassinado não elimina o aviso sozinho — quem for rodar o app
    ainda precisa confiar nesse certificado uma vez (por isso o .cer é exportado
    junto, para distribuir ao lado do .exe) — mas já troca "Editor desconhecido"
    por "APURA Fiscal" e é o primeiro passo caso um certificado pago (que resolve
    para qualquer pessoa, sem esse passo manual) seja comprado no futuro.

    O certificado fica guardado no repositório de certificados do Windows
    (Cert:\CurrentUser\My) da máquina que publica — nunca em disco em texto puro,
    e nunca commitado no git. Só o .cer (chave pública, sem risco algum de vazar
    nada sensível) é copiado para a pasta de publicação, para ir dentro do .zip.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$CaminhoExecutavel,

    [Parameter(Mandatory)]
    [string]$PastaPublicacao,

    [string]$AssuntoCertificado = "CN=APURA Fiscal (autoassinado)"
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
        -FriendlyName "APURA Fiscal - assinatura de código"
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

Write-Host "Assinatura aplicada (status de confiança: $($resultado.Status) — esperado para certificado autoassinado; some ao instalar o .cer)."

$caminhoCer = Join-Path $PastaPublicacao "confiar-neste-certificado.cer"
Export-Certificate -Cert $certificado -FilePath $caminhoCer | Out-Null

$caminhoInstalador = Join-Path $PSScriptRoot "Instalar e Abrir.cmd"
Copy-Item -Path $caminhoInstalador -Destination $PastaPublicacao -Force

$leiaMe = Join-Path $PastaPublicacao "LEIA-ME - evitar aviso do Windows.txt"
Set-Content -Path $leiaMe -Encoding UTF8 -Value @"
IMPORTANTE: extraia o .zip inteiro para uma pasta antes de rodar qualquer coisa
(botão direito no .zip -> "Extrair Tudo..."). Rodar os arquivos direto de dentro do
.zip, sem extrair, faz o Windows executá-los isolados, sem os arquivos vizinhos, e
o instalador abaixo não vai encontrar o certificado.

O Windows pode avisar "Editor desconhecido" ou que o SmartScreen impediu o início de
um aplicativo não reconhecido ao abrir o AuditorFiscal.exe pela primeira vez nesta
máquina. Isso é esperado (o app usa um certificado autoassinado, não um pago) — três
formas de contornar, da mais fácil para a mais manual:

CAMINHO RÁPIDO (recomendado)
Dê duplo-clique em "Instalar e Abrir.cmd". Ele instala o certificado de confiança
automaticamente (o Windows vai pedir UMA confirmação de segurança — é normal, é o
Windows garantindo que você mesmo autorizou) e já abre o app em seguida. Só precisa
fazer isso uma vez; as próximas aberturas (e futuras versões assinadas com o mesmo
certificado) não pedem mais nada.

SE O WINDOWS AVISAR MESMO ASSIM (SmartScreen)
No aviso, clique em "Mais informações" e depois em "Executar assim mesmo". Não requer
instalar nada — funciona na hora, mas pode voltar a perguntar em versões futuras.

CAMINHO MANUAL (se preferir não rodar o .cmd)
1. Dê duplo-clique em "confiar-neste-certificado.cer".
2. Clique em "Instalar Certificado...".
3. Escolha "Usuário Atual" e avance (não precisa de administrador).
4. Selecione "Colocar todos os certificados no repositório a seguir" e escolha
   "Autoridades de Certificação Raiz Confiáveis".
5. Conclua. Pode ser necessário reiniciar o AuditorFiscal.exe depois.
"@

Write-Host "Certificado exportado: $caminhoCer"
Write-Host "Assinatura aplicada com sucesso (thumbprint: $($certificado.Thumbprint))."
