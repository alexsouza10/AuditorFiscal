# CLAUDE.md

## Objetivo

Desenvolver um aplicativo desktop extremamente leve para Windows destinado ao uso de um Auditor Fiscal.

O foco é simplicidade, desempenho, segurança e usabilidade.

## Stack

- C#
- .NET 8 LTS
- Avalonia UI
- MVVM
- Entity Framework Core
- SQLite
- Clean Architecture
- SOLID
- Repository Pattern
- Unit of Work
- Dependency Injection
- FluentValidation
- Serilog

## Requisitos

- Interface minimalista
- Tema Light e Dark
- Executável único (Single File)
- Inicialização automática com o Windows
- Banco local
- Funcionar offline
- Baixo consumo de memória
- Compatível com computadores antigos

## Tela Inicial

Exibir três grandes cartões:

- Nova Ordem de Serviço
- Agenda
- Banco de Dados

Inspirado em interfaces minimalistas como as imagens de referência.

## Cadastro de Ordem de Serviço

Campos:

- Número da OS
- Empresa
- CNPJ
- Endereço
- Cidade
- Responsável
- Data
- Hora
- Situação
- Tipo de Auditoria
- Observações
- Fotos
- Anexos
- Latitude (opcional)
- Longitude (opcional)

## Agenda

Visual semelhante a calendário.

Cada dia deve mostrar pequenas barras indicando auditorias agendadas.

Ao clicar no dia:

- Lista das OS
- Horário
- Status
- Empresa

## Funcionalidades

- Pesquisa rápida
- Histórico por empresa
- Exportar PDF
- Exportar Excel
- Backup automático
- Backup manual
- Restaurar backup
- Auto Save
- Impressão
- Tags
- Dashboard
- Favoritos
- Timeline da auditoria
- Logs internos
- Atalhos de teclado

## Segurança

Implementar segurança desde o início.

- SQLite criptografado
- AES-256 GCM
- DPAPI para proteger a chave
- Fotos criptografadas
- PDFs criptografados
- Backups criptografados
- Hash SHA-256 para integridade dos registros
- Nenhum dado sensível em texto puro
- Nenhum arquivo temporário

## Código

Seguir rigorosamente:

- Clean Architecture
- SOLID
- DRY
- KISS
- YAGNI
- Código desacoplado
- Alta testabilidade
- Comentários apenas quando realmente necessários
- Nomes claros para classes, métodos e variáveis

## Objetivo final

Entregar um aplicativo extremamente simples, rápido, seguro e intuitivo, capaz de ser utilizado diariamente por um Auditor Fiscal em computadores modestos, com armazenamento local criptografado, backups automáticos e interface moderna minimalista.



## Estrutura do projeto

AuditorFiscal/

src/
    AuditorFiscal.UI
    AuditorFiscal.Application
    AuditorFiscal.Domain
    AuditorFiscal.Infrastructure
    AuditorFiscal.Persistence
    AuditorFiscal.Shared

tests/
    UnitTests
    IntegrationTests

docs/





# CLAUDE V2.md

## Objetivo

Desenvolver um aplicativo desktop extremamente leve para Windows destinado ao uso de um Auditor Fiscal. 

O foco é simplicidade, desempenho, segurança, usabilidade e uma excelente visualização temporal das demandas. A interface deve mesclar o estilo minimalista moderno (Dark Mode) com elementos funcionais de sistemas legados de auditoria (Light Mode).

## Stack

- C#
- .NET 8 LTS
- Avalonia UI
- MVVM
- Entity Framework Core
- SQLite
- Clean Architecture
- SOLID
- Repository Pattern
- Unit of Work
- Dependency Injection
- FluentValidation
- Serilog

## Requisitos de Interface e UX

- **Design Totalmente Responsivo:** O layout DEVE se adaptar corretamente quando a janela for maximizada ou redimensionada. Utilizar controles de layout do Avalonia (como `Grid` com `*` e `Auto`, e `Viewbox`) para garantir que não fiquem espaços em branco ou quebras indesejadas na interface.
- Interface minimalista e geométrica.
- Suporte a Tema Light (inspirado no visual legado) e Dark (visual moderno do novo sistema).
- Inicialização automática com o Windows.
- Executável único (Single File).
- Funcionamento 100% offline com banco local.
- Baixo consumo de memória para compatibilidade com computadores modestos.

## Tela Inicial (Dashboard)

A tela principal deve atuar como um Dashboard de entrada, contendo:
- **Mensagem de Saudação:** "Bem vindo. Olá! Vamos ao trabalho" posicionada em destaque.
- **Três grandes cartões de atalho centralizados:**
  1. Nova Ordem de Serviço
  2. Visualizar (Gantt/Agenda)
  3. Banco de Dados
- **Métricas rápidas na parte inferior:** Contadores dinâmicos (ex: Hoje, Nesta semana, Em aberto).

## Cadastro de Ordem de Serviço (O.S.)

O formulário de cadastro deve ser limpo e focado nas etapas da auditoria. Os campos exatos são:

- **O.S N°** (Texto)
- **Descrição / Cliente** (Texto)
- **1. Recebimento da OS no SFIT** (Data)
- **2. Abertura no SFIT** (Data)
- **3. Fiscalização** (Combobox/Dropdown: Direta, Indireta, Mista)
- **4. Prazo para recebimento de documentos [NAD]** (Data)
- **5. Prazo para cumprimento de obrigações [NCO]** (Data)
- **6. Elaboração dos autos** (Data)
- **7. Data final** (Data)

*Nota:* O layout deve agrupar essas datas de forma lógica, mostrando a progressão natural da auditoria.

## Visualização / Agenda (Cronograma GANTT)

Esta é a funcionalidade central de acompanhamento. A visualização de agenda tradicional deve ser substituída ou complementada por um **Cronograma GANTT de O.S.**

- **Visão Temporal e Sequencial:** Uma linha do tempo em formato de grade (dividida por meses: MAR, ABR, MAI, etc.).
- **Barras de Progresso:** Cada O.S. terá uma barra horizontal mostrando sua duração ao longo dos meses.
- **Linha do "HOJE":** Um marcador vertical claro indicando a data atual cortando o gráfico.
- **Código de Cores (Legenda Monocromática / Temática):** As etapas dentro do Gantt devem seguir cores distintas para fácil identificação do status da O.S. (adaptáveis ao Light/Dark mode). A legenda deve ficar visível na parte inferior:
  - [Azul] 1. Recebimento SFIT
  - [Roxo] 2. Abertura SFIT
  - [Amarelo] 3. Fiscalização
  - [Laranja] 4. Prazo NAD
  - [Vermelho] 5. Prazo NCO
  - [Verde] 6. Autos / Final

## Funcionalidades Gerais

- Pesquisa rápida de O.S.
- Histórico por cliente/empresa.
- Exportar PDF e Excel.
- Backup automático e manual (com opção de Restauração).
- Auto Save silencioso.
- Impressão otimizada das listagens e relatórios.
- Sistema de Tags e Favoritos.
- Timeline individual dentro da auditoria (histórico de alterações).
- Logs internos (Serilog).
- Atalhos de teclado (ex: Ctrl+N para Nova O.S, Ctrl+G para Gantt).

## Segurança

Implementar segurança desde a base:

- SQLite criptografado (AES-256 GCM).
- Uso de DPAPI para proteger a chave de criptografia no Windows.
- Arquivos sensíveis (Fotos/PDFs anexados, se houver no futuro) e backups devem ser criptografados.
- Hash SHA-256 para garantir a integridade dos registros essenciais.
- Nenhum dado sensível deve ser salvo em texto puro.
- Não deixar rastros em arquivos temporários.

## Diretrizes de Código

Seguir rigorosamente:

- **Clean Architecture & DDD (onde aplicável):** Separação clara entre Core, Infrastructure, Application e Presentation.
- SOLID, DRY, KISS, YAGNI.
- Código altamente desacoplado, com uso intensivo de Interfaces e Injeção de Dependência.
- Alta testabilidade.
- Nomes claros e autoexplicativos para classes, métodos e variáveis (Clean Code).
- Comentários apenas quando explicarem o "porquê" de uma decisão complexa, nunca o "o quê".

## Objetivo Final

Entregar um aplicativo robusto, responsivo (que lida perfeitamente com redimensionamentos), rápido e intuitivo. Deve fornecer ao Auditor Fiscal uma visão gerencial clara (através do Gantt) e ferramentas ágeis de registro, operando de maneira segura e eficiente, mesmo em hardwares mais antigos.

---

## Status da implementação (atualizado)

O plano do CLAUDE V2.md foi implementado por cima da base já construída a partir do CLAUDE.md original. Decisões de continuidade entre as duas versões:

- **Fluxo SFIT substitui Data/Hora único.** `OrdemServico` agora tem `RecebimentoSfit`, `AberturaSfit`, `PrazoNad`, `PrazoNco`, `ElaboracaoAutos`, `DataFinal` (todas `DateOnly`) e `Fiscalizacao` (enum Direta/Indireta/Mista), validados em sequência crescente (`AberturaSfit >= RecebimentoSfit >= …`).
- **TipoAuditoria (V1) foi descontinuado** em favor do enum `Fiscalizacao`, mais simples e alinhado ao V2. A tabela/seed permanecem no schema por segurança de migração, mas não são mais referenciados pela UI.
- **Situação (Agendada/EmAndamento/Concluída/Adiada/Cancelada)** foi mantida do V1 mesmo não estando na lista literal de campos do V2, pois alimenta o dashboard, os filtros do Banco de Dados e a cor do card no Gantt — sem ela não haveria como responder "o que está em aberto".
- **Cronograma GANTT** (`GanttViewModel`/`GanttView`) substitui a agenda semanal: grade de meses, barra por O.S. dividida em 5 segmentos coloridos entre as 6 datas do fluxo, linha vertical "HOJE" e legenda fixa. É totalmente responsivo via `MultiBinding` com conversores (`ProporcaoParaPixelsConverter`/`ProporcaoParaMargemConverter`) que recalculam pixels a partir da largura real do contêiner.
- **Fotos/Anexos podem ser selecionados antes de salvar** a O.S.: ficam em memória (`NovoArquivoDto`) e só são criptografados e persistidos em disco no momento do primeiro `Salvar`.
- **Tema Light/Dark** com toggle na barra superior, persistido em `preferences.json` (fora do banco — lido antes de a chave mestra existir).
- Backup automático/manual, restaurar, exportar PDF/Excel, imprimir, tags, favoritos e timeline (V1) permanecem ativos e agora operam sobre os novos campos.

Testes: 36/36 passando (13 unitários + 23 integração), incluindo round-trip de banco criptografado, AES-256-GCM, exportação PDF/Excel e o fluxo completo de criação/edição de O.S.