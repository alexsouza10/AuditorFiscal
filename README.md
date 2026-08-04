# Auditor Fiscal

Aplicativo desktop para Windows, leve e offline, para um Auditor Fiscal gerenciar suas
Ordens de Serviço (O.S.) do início ao fim: cadastro, acompanhamento em um cronograma
visual, banco de dados pesquisável e indicadores de produção — tudo local, criptografado
e sem depender de internet.

## Sumário

- [O que é o projeto](#o-que-é-o-projeto)
- [Por que ele existe](#por-que-ele-existe)
- [Funcionalidades](#funcionalidades)
- [Stack e arquitetura](#stack-e-arquitetura)
- [Como rodar em desenvolvimento](#como-rodar-em-desenvolvimento)
- [Testes](#testes)
- [Versionamento e publicação (`make`)](#versionamento-e-publicação-make)
- [Backup e restauração](#backup-e-restauração)
- [Segurança](#segurança)
- [Estrutura de pastas](#estrutura-de-pastas)

## O que é o projeto

O Auditor Fiscal é um sistema de gestão de auditorias fiscais construído em torno do
fluxo real de trabalho do SFIT: cada Ordem de Serviço nasce com um número, uma empresa
fiscalizada e passa por uma sequência de datas — Recebimento no SFIT, Abertura, data da
Fiscalização, Prazo NAD, Prazo NCO, Elaboração dos autos e Data final — até ser concluída.

A tela inicial mostra um resumo do dia (quantas auditorias estão em andamento, quantas
essa semana, quantas em aberto) e dá acesso rápido a quatro áreas:

- **Nova Ordem de Serviço** — cadastro completo, com fotos e anexos criptografados.
- **Cronograma (Gantt)** — uma linha do tempo visual, uma barra por O.S., dividida em
  segmentos coloridos que mostram em qual etapa do fluxo SFIT cada uma está.
- **Banco de Dados** — busca e filtros sobre todas as O.S., exportação em PDF/Excel,
  impressão, histórico por empresa.
- **Dashboard** — eficiência de conclusão, produção mensal (recebidas x concluídas),
  distribuição por situação e por tipo de fiscalização.

## Por que ele existe

Um auditor fiscal não conduz uma auditoria por vez: conduz dezenas, cada uma presa a uma
sequência de prazos legais que não podem se confundir entre si (recebimento no SFIT,
abertura, fiscalização, NAD, NCO, elaboração dos autos, data final). Perder de vista em
qual etapa cada O.S. está — ou descobrir tarde demais que um prazo já venceu — não é um
incômodo, é risco de descumprir uma obrigação legal.

Planilha solta e agenda genérica não resolvem isso: nenhuma das duas mostra, de forma
visual, o cronograma de várias auditorias sobrepostas ao mesmo tempo, nem impede que uma
data seja lançada fora de ordem. E os dados que essas auditorias carregam — endereço e
CNPJ da empresa fiscalizada, fotos tiradas em campo — não podem ficar em texto puro num
arquivo qualquer, principalmente em computadores de repartição que várias pessoas usam.

A solução foi um cronograma visual único (o Gantt), que faz as etapas de cada O.S.
saltarem aos olhos e concentra tudo — cadastro, prazos, fotos, anexos — num único lugar
criptografado. Duas restrições guiaram como isso foi construído: (1) precisa rodar em
qualquer computador da repartição, sem instalação nem internet; e (2) nenhum dado sensível
pode ser gravado sem criptografia, mesmo que isso exija mais código de infraestrutura
(chave protegida por DPAPI, banco SQLite cifrado, backups cifrados).

## Funcionalidades

- Cadastro de O.S. com o fluxo completo de datas do SFIT, fotos, anexos e observações.
- Cronograma GANTT com zoom por período (3/6/12 meses), intervalo de datas personalizado
  e busca por número da O.S., CNPJ ou empresa.
- Aba de Histórico por O.S., com todas as alterações registradas (quem mudou o quê e
  quando), acessível tanto no formulário quanto a partir do Gantt e do Banco de Dados.
- Banco de Dados com busca, filtros por situação/empresa/favoritos, exportação em PDF e
  Excel, impressão de relatórios.
- Dashboard com taxa de conclusão, produção mensal e gráficos de distribuição.
- Tema claro/escuro, com o escuro ajustado para não ser preto quase puro.
- Auto Save no formulário — só grava quando algo realmente muda, sem poluir o histórico
  com entradas repetidas.
- Atalhos de teclado (veja a tela de Configurações do próprio app).
- Backup automático e manual, cifrado, com restauração assistida (veja
  [Backup e restauração](#backup-e-restauração) abaixo).

## Stack e arquitetura

- **C# / .NET 8**, interface em **Avalonia UI** (MVVM, `CommunityToolkit.Mvvm`).
- **Entity Framework Core** sobre **SQLite cifrado** (AES-256-GCM via SQLCipher), com a
  chave protegida por **DPAPI** — nada sensível é gravado sem criptografia.
- Clean Architecture em camadas:

  ```
  AuditorFiscal.Domain          entidades e regras de negócio, sem dependências externas
  AuditorFiscal.Application     casos de uso, DTOs, validação (FluentValidation)
  AuditorFiscal.Infrastructure  exportação (PDF/Excel), impressão, backup, autostart, logging
  AuditorFiscal.Persistence     EF Core, migrations, repositórios
  AuditorFiscal.Shared          o único ponto de verdade para caminhos de dados (AppPaths)
  AuditorFiscal.UI              Avalonia, ViewModels, Views, DI (Microsoft.Extensions.Hosting)
  ```
- Repository Pattern + Unit of Work, Dependency Injection, Serilog para logging interno.
- Publicado como **executável único self-contained** (`PublishSingleFile`), sem exigir
  .NET instalado na máquina de quem vai usar.

## Como rodar em desenvolvimento

Pré-requisitos: [.NET SDK 8](https://dotnet.microsoft.com/download) (a versão exata está
fixada em `global.json`).

```bash
dotnet restore
dotnet build
dotnet run --project src/AuditorFiscal.UI
```

Na primeira execução, o app cria seu banco e sua chave de criptografia em
`%LOCALAPPDATA%\AuditorFiscal\` (veja [Backup e restauração](#backup-e-restauração)).

## Testes

```bash
dotnet test
```

Cobre testes unitários (regras de domínio, ex.: `OrdemServico`) e de integração (banco
cifrado real, exportação de PDF/Excel, fluxo completo de criação/edição de O.S.).

## Versionamento e publicação (`make`)

A versão do app vem da última **tag do git**, não é digitada à mão. Um `Makefile` na raiz
cuida de tudo:

| Comando        | O que faz |
|----------------|-----------|
| `make tag`     | Cria a próxima tag (bump de *minor*: `v0.1.0` → `v0.2.0`, e assim por diante) e já publica ela em `origin`. Sem nenhuma tag ainda, a primeira criada é `v0.1.0`. |
| `make release` | Publica o executável único (`win-x64`, self-contained) usando a última tag como versão, e empacota tudo em `dist/Gerenciador-de-AFT-<versão>-win-x64.zip`, pronto para enviar. |
| `make version` | Só mostra qual versão seria usada, sem publicar nada. |
| `make clean`   | Apaga a pasta `dist/`. |

Fluxo típico para lançar uma nova versão:

```bash
make tag       # cria e publica a tag v0.X.0
make release   # gera dist/Gerenciador-de-AFT-v0.X.0-win-x64.zip
```

O arquivo `.zip` gerado é **tudo** que uma pessoa precisa: ela extrai e roda
`Gerenciador de AFT.exe` diretamente — não existe instalador, e a máquina dela não precisa ter
o .NET instalado (o runtime já vai embutido no executável). A versão publicada aparece
no canto inferior direito da tela inicial do próprio app.

Sem nenhuma tag alcançável, o app se identifica como `0.0.0-dev` (build de
desenvolvimento) — é assim que qualquer `dotnet build`/`dotnet run` local aparece.

## Backup e restauração

- Todo backup é salvo cifrado (AES-256-GCM) na pasta:

  ```
  %LOCALAPPDATA%\AuditorFiscal\db\
  ```

  Essa pasta guarda o banco de dados e todos os anexos num único arquivo `.afbkp` por
  backup — é o lugar para copiar/mover se você quiser levar os dados para outra máquina
  ou guardar uma cópia de segurança fora do computador.
- Existem dois tipos de backup:
  - **Automático** (se a opção estiver ligada em Configurações): roda sozinho ao abrir e
    ao fechar o app, sempre gravando por cima do mesmo arquivo (`auditorfiscal-auto.afbkp`).
    Não acumula backups antigos na pasta.
  - **Manual**, pelo botão "Criar backup agora" em Configurações: gera um arquivo novo,
    com data e hora no nome, preservando o histórico de backups anteriores.
- Em Configurações → Backup existe um botão **"Abrir pasta de backups"** que já abre essa
  pasta no Explorer, e o caminho completo fica visível na tela.
- Para restaurar: Configurações → **"Restaurar backup…"** → escolha o arquivo `.afbkp`.
  Antes de qualquer coisa ser agendada, o app pergunta explicitamente:

  > *"Todos os dados atuais serão substituídos pelo conteúdo do backup, sem volta. O
  > aplicativo vai fechar e abrir sozinho para concluir — não é preciso fazer isso
  > manualmente. Continuar?"*

  A restauração **não acontece na hora** — o banco atual está aberto enquanto o app roda,
  então sobrescrevê-lo nesse momento não é seguro. Em vez disso, ela fica agendada e o
  próprio aplicativo se reinicia sozinho para aplicá-la (antes de qualquer tela abrir,
  quando o arquivo do banco está livre para ser trocado) — você não precisa fechar e abrir
  manualmente. Como essa restauração substitui os dados atuais sem criar nenhum backup
  automático do que havia antes, vale criar um backup manual primeiro se quiser poder
  voltar atrás.

## Segurança

- Banco SQLite cifrado (SQLCipher, AES-256-GCM); chave protegida por DPAPI do Windows.
- Fotos, anexos e backups sempre criptografados — nunca gravados em texto puro.
- Hash SHA-256 para checar a integridade dos registros.
- Nenhum arquivo temporário de dados sensíveis é deixado no disco.

Detalhes de arquitetura e segurança adicionais (quando existirem) ficam em `docs/`.

## Estrutura de pastas

```
AuditorFiscal/
├── Makefile                  publicação versionada (make tag / make release)
├── src/
│   ├── AuditorFiscal.UI              Avalonia (Views, ViewModels, DI)
│   ├── AuditorFiscal.Application     casos de uso, DTOs, validadores
│   ├── AuditorFiscal.Domain          entidades e regras de negócio
│   ├── AuditorFiscal.Infrastructure  export, impressão, backup, logging, autostart
│   ├── AuditorFiscal.Persistence     EF Core, migrations, repositórios
│   └── AuditorFiscal.Shared          AppPaths (caminhos de dados do app)
├── tests/
│   ├── UnitTests
│   └── IntegrationTests
└── docs/
```
