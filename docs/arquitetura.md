# Arquitetura — São Dimas Gestão Paroquial

## Persistência atual

O armazenamento ativo é JSON no servidor, sem dependência de SQL Server. Veja [Persistência JSON](persistencia-json.md) para localização, importação do legado, backups e concorrência. Os repositórios EF Core e as migrações abaixo foram mantidos para integração futura.

## Camadas e dependências

```
SaoDimas.MVC ──► SaoDimas.Aplicacao ──► SaoDimas.Dominio ◄── SaoDimas.Infraestrutura
     └──────────── (somente Composition Root) ────────────────────────┘
```

- **Dominio** não depende de nenhum projeto nem de EF Core, ASP.NET Core, SQL Server ou do container de DI.
- **Infraestrutura** referencia **Aplicacao** porque implementa contratos definidos nela (ex.: `IDizimistaConsultas`).
- **MVC** referencia Infraestrutura apenas para `AddInfraestrutura(...)` no `Program.cs`.
- **CrossCutting** contém os adaptadores HTTP (filtro de unidade de trabalho, cookies e limite de tentativas) e a implementação técnica de `ILoginAutenticador`. Depende apenas da Aplicação e do framework web; não referencia MVC ou Infraestrutura. O contrato e a configuração de login pertencem à Aplicação. MVC injeta o contrato na navegação de login e usa o adaptador HTTP no módulo de eventos.

## Convenção Interface/ + Implementation/

Vale para **serviços e contratos que exigem inversão de dependência**. Entities, Value Objects, DTOs, ViewModels,
Enums, Exceptions, EntityTypeConfigurations e Controllers **não** recebem interface.

**O contrato fica na camada que o consome; a implementação, na camada que o implementa.**

| Componente | Contrato (`Interface/`) | Implementação (`Implementation/`) |
|---|---|---|
| Application Service | `Aplicacao/Services/Interface/IXxxAplicacao` | `Aplicacao/Services/Implementation/XxxAplicacao` |
| Repositório (escrita, por agregado) | `Dominio/Repositories/Interface/IXxxRepositorio` | `Infraestrutura/Repositories/Implementation/XxxRepositorio` |
| Consulta de leitura (projeções, listagens paginadas) | `Aplicacao/Queries/Interface/IXxxConsultas` | `Infraestrutura/Queries/Implementation/XxxConsultas` |
| Serviço técnico usado pela Aplicação (PDF, e-mail, arquivos) | `Aplicacao/Services/Interface/IPdfService` | `Infraestrutura/Services/Implementation/PdfService` |
| Domain Service (só quando a regra não pertence a uma entidade) | `Dominio/<Modulo>/Services/Interface/` | `Dominio/<Modulo>/Services/Implementation/` |

Regras:

- Interfaces: `I` + Nome. Implementações: sem prefixo nem sufixos como `Impl`, `Implementation`, `Concrete`.
- Implementações são `sealed`. Na Aplicação e na Infraestrutura são também `internal`: os consumidores só
  enxergam o contrato e o compilador impede `new DizimistaAplicacao()` fora da camada.
- Pastas são criadas somente quando o primeiro componente real existir.

Estrutura atual:

| Projeto | Pastas |
|---|---|
| Dominio | `Entities/`, `Enums/`, `ValueObjects/`, `Repositories/Interface/`, `Resultado.cs` (Result pattern) |
| Aplicacao | `Dtos/`, `Services/{Interface,Implementation}/`, `Queries/Interface/` |
| Infraestrutura | `Persistencia/{Configurations,Migrations}/`, `Repositories/Implementation/`, `Queries/Implementation/` |
| MVC | `Controllers/`, `Models/` (ViewModels), `Views/`, `wwwroot/` |
| CrossCutting | `Filters/`, `Services/Implementation/`, `DependencyInjection.cs` |

## Leitura × escrita

- **Escrita** (cadastro/edição): Controller → `IXxxAplicacao` → entidade de domínio (valida invariantes e retorna
  `Resultado`) → `IXxxRepositorio` → `SessaoSistemaJson`.
- **Leitura** (listagens, detalhes): Controller → `IXxxAplicacao` → `IXxxConsultas`, que projeta direto em DTOs com
  a partir da sessão JSON, aplicando pesquisa, filtros e ordenação **antes** da paginação.
- Erros de negócio esperados retornam `Resultado`/`Erro` (com o campo de origem); exceções ficam para falhas inesperadas.
- Identificadores vindos do navegador (ex.: `ComunidadeId`) são sempre revalidados no backend.

## Comunidades

A Paróquia é composta pela Matriz e por Capelas, registros da tabela `Comunidades` (`TipoComunidade` representa
apenas o tipo). Os nomes iniciais existem somente no seed de `ComunidadeConfiguration`; o restante do sistema
trabalha com `ComunidadeId`. Todo dizimista tem uma comunidade de referência (FK `Restrict`): relatórios e
contribuições por comunidade ou consolidados da paróquia saem de um JOIN/agrupamento por `ComunidadeId`.

## Banco de dados (EF Core)

```bash
dotnet tool restore
dotnet ef migrations add NomeDaMigration --project SaoDimas.Infraestrutura --startup-project SaoDimas.MVC --output-dir Persistencia/Migrations -- --environment Development
dotnet ef database update --project SaoDimas.Infraestrutura --startup-project SaoDimas.MVC -- --environment Development
```

## Dependency Injection

- Somente a DI nativa do .NET. Cada camada registra seus serviços no próprio `DependencyInjection.cs`
  (`AddAplicacao()`, `AddInfraestrutura(configuration)`); o `Program.cs` só compõe.
- Domain Services são registrados em `AddAplicacao()`, pois o Domínio não conhece o container.
- Constructor Injection (primary constructors quando melhorar a leitura). Proibido `new` de serviços
  e `IServiceProvider` como Service Locator.
- Lifetimes:
  - **Scoped** — Application Services, Repositórios, qualquer coisa que use o `SaoDimasDbContext`.
  - **Transient** — serviços leves e stateless, com motivo real.
  - **Singleton** — somente serviços thread-safe sem dependência de DbContext/Scoped.
- **Keyed Services** apenas quando houver múltiplas implementações legítimas do mesmo contrato
  (ex.: `IPdfGenerator` com `"carta"` e `"relatorio"`).
- O container valida registros e lifetimes na inicialização (`ValidateOnBuild` + `ValidateScopes`).

## Garantias automatizadas (`SaoDimas.Tests/Arquitetura`)

- `DependenciasEntreCamadasTests` — direção das referências entre camadas.
- `ConvencoesDeServicosTests` — conteúdo de `Interface/` e `Implementation/`, nomenclatura, visibilidade,
  local dos repositórios, ausência de dependência concreta e de `IServiceProvider` em construtores.
- `InjecaoDeDependenciaTests` — todo contrato em `Interface/` possui registro; o container é construído
  sem registros ausentes nem lifetimes incompatíveis; `DbContext` é Scoped.

## Frontend (SaoDimas.MVC)

Build: `dotnet build` executa `npm ci` (quando necessário) e `npm run build` automaticamente — **requer Node.js**.
Fontes do frontend ficam em `wwwroot/src` (nunca servido nem publicado). Os arquivos gerados em `wwwroot/css`,
`wwwroot/icons`, `wwwroot/fonts`, `wwwroot/lib` e `wwwroot/img` não são versionados.
Durante o desenvolvimento de telas: `npm run watch:css` (em `SaoDimas.MVC/`).

| Onde | O quê |
|---|---|
| `wwwroot/src/app.css` | Design tokens (`@theme`), tokens semânticos (`--sd-*`, prontos para tema escuro) e componentes |
| `wwwroot/src/build-assets.mjs` | Sprite de ícones Lucide (só os usados), fonte Rubik local, HTMX |
| `Views/Shared/_Layout.cshtml` | Shell: cabeçalho da página + `_Sidebar` (menu), `_Topbar`, `_UserMenu`, `_Toast`, `_Scripts` |
| `wwwroot/js/` | ES modules: `sidebar.js`, `toast.js`, `modal.js` (entrada: `app.js`) |

Cada página informa seu cabeçalho e ações:

```cshtml
@{
    ViewData["Title"] = "Dizimistas";
    ViewData["Descricao"] = "Gerencie os dizimistas cadastrados.";
}
@section Breadcrumb {
    <li aria-current="page">Dizimistas</li>
}
@section Acoes {
    <a asp-action="Novo" class="btn btn-primary">
        <svg class="icone" aria-hidden="true"><use href="/icons/lucide.svg#plus" /></svg>Novo Dizimista
    </a>
}
```

- Cores: somente tokens (`primary-*`, `secondary-*`, `accent-*`, `neutral-*`, `success|warning|danger|info-*`
  e semânticos `bg-background`, `bg-surface`, `border-border`, `text-foreground`, `text-muted`, `text-subtle`).
  A paleta padrão do Tailwind está desativada.
- Componentes: `btn btn-primary|secondary|outline|ghost|danger btn-sm|btn-lg` (`aria-busy="true"` = loading),
  `card`, `form-field/form-label/form-control/form-error`, `form-check`, `table-wrapper/table`, `badge-*`,
  `<dialog class="modal">` + `data-modal-abrir`/`data-modal-fechar`, `empty-state`, `spinner`, `skeleton`, `htmx-indicator`.
- Ícones: `<svg class="icone" aria-hidden="true"><use href="/icons/lucide.svg#users" /></svg>`
  (nomes em https://lucide.dev/icons; o sprite com os ícones usados é regenerado no build).
- Menu: itens no `_Sidebar.cshtml` — `Item("Dizimistas", "users", null)`; troque `null` pelo nome do controller
  quando o módulo existir.
- Toast: `mostrarToast(mensagem, tipo)` em `toast.js`, ou via HTMX com o header
  `HX-Trigger: {"toast": {"mensagem": "...", "tipo": "success"}}`.
- Brasão: a logo oficial fica em `wwwroot/src/marca/logo-paroquia-sao-dimas.png` (fonte). O build gera
  `wwwroot/img/brasao.webp` (otimizado) e `wwwroot/img/favicon.png` (somente o símbolo, sem a faixa).

## Envelope de Dízimo (molde físico A4)

Fluxo: `EnvelopesController` → `IEnvelopeDizimoAplicacao` → `IDizimistaConsultas` (projeção: nome, código, comunidade, telefone, endereço, CEP, bairro e nascimento)

Endereço, CEP, bairro e data de nascimento são opcionais no cadastro. O envelope imprime os dados existentes e mantém linhas para preenchimento quando ausentes; o aniversário aparece somente como dia/mês. A migração `DadosCadastraisEnvelope` adiciona colunas anuláveis, preservando os cadastros anteriores. O CPF não é impresso.
→ `IEnvelopeDizimoPdfService` (QuestPDF, Infraestrutura). O navegador envia somente identificadores; os dados
impressos são sempre consultados no backend. Individual (`/Dizimistas/{id}/Envelope`), lote por seleção
(`/Dizimistas/Envelopes?ids=..`) ou todos os ativos de uma comunidade (`/Dizimistas/Envelopes?comunidadeId=..`):
uma folha A4 por dizimista.

CodigoOriginal preserva a coluna Código Sistema das planilhas, separado da chave Id. Codigo utiliza
esse valor quando presente; cadastros sem origem usam o formato interno anterior. O JSON serializa
e reconstitui CodigoOriginal e recupera valores ausentes do arquivo de cadastros iniciais por
comunidade/nome e ID ou contatos/endereço, somente quando a correspondência é única. O BAT realiza
o mesmo preenchimento nas instalações existentes com backup. Nenhuma chave ou referência financeira
é renumerada. A migração SQL adiciona uma coluna opcional, sem impor unicidade entre comunidades.

Geometria (mm, A4 inteiro 210 × 297, sem cortes): abas laterais de 10 mm; fechamento y 0–60;
frente y 60–178,5; verso y 178,5–297. Frente e verso têm exatamente 118,5 mm: a borda inferior
encosta na dobra superior ao fechar a base, dispensando medição. Envelope montado 190 × 118,5 mm.
Primeiro dobrar as laterais; passar cola nas áreas hachuradas; alinhar a borda inferior à dobra
superior e pressionar; colocar a contribuição e fechar a aba. São quatro linhas de dobra.

Verso e aba são impressos girados 180°. Dados pessoais ficam na frente; QR Pix na faixa visível
y 178,5–237; tabela mensal com 13º em y ≥ 237, escondida pela aba fechada. Textos e imagens
respeitam as áreas seguras. Guias úteis ficam a pelo menos 5 mm das bordas, sem exigir impressão
sem margens. Imprimir A4, tamanho real 100%, sem ajustar à página; conferir a régua de 50 mm.
As constantes estão em EnvelopeDizimoPdfService; testes inspecionam o PDF real e verificam o
alinhamento das bordas e a posição do controle após dobrar. A validação é geométrica e visual;
a montagem física depende da escala da impressora e deve ser conferida na primeira folha.
Dados institucionais (nome, contatos, caminho da logo de impressão) vêm da seção `Paroquia` do appsettings
(`ConfiguracaoParoquia`).

## Eventos e Tickets (persistência temporária)

Agregados: `Evento` (com `ProdutoEvento`) e `LoteTicket` (com `DistribuicaoTicket` e `PrestacaoContas`).
Tickets **não** são armazenados um a um: um lote guarda a numeração inicial/final e o preço (snapshot); as
distribuições são faixas com responsável opcional (texto livre, não é dizimista). O domínio rejeita sobreposição,
faixas fora do lote e alterações após a prestação; lacunas e quantidades livres são derivadas. A numeração de um
novo lote é reservada por `ProdutoEvento.ProximoNumeroTicket` dentro do agregado `Evento` (no SQL: concorrência
otimista com rowversion, nunca `MAX(numero) + 1`). Valores esperados e diferenças são sempre calculados no servidor.

**Por enquanto não há tabelas no SQL Server para este módulo.** O estado vive no JSON do servidor; o antigo `localStorage` é usado apenas na importação inicial
(chave `saodimas.eventos.v1`, acesso único em `wwwroot/js/eventos/armazenamento.js`):

1. Todo POST do módulo envia o documento JSON no campo `__estadoEventos` (HTMX `configRequest`).
2. `EstadoEventosNavegadorFilter` carrega o documento em `EstadoEventosNavegador` (Infraestrutura, scoped);
   POST sem o campo ou com documento inválido → 400 (evita sobrescrever dados com um documento vazio).
3. `EventoRepositorio` e `LoteTicketRepositorio` implementam os contratos do domínio sobre esse estado; o
   `SalvarAlteracoesAsync` atribui ids pelas sequências do documento.
4. O documento alterado volta no header `X-Estado-Eventos` (base64) e o JS grava no `localStorage`.

Limitações: os dados ficam somente naquele navegador/perfil (limpar dados do site apaga tudo) e, com várias abas,
vale a última gravação. **Migração para EF Core:** criar as configurações/tabelas e repositórios EF para
`IEventoRepositorio`/`ILoteTicketRepositorio`, trocar o registro na DI e remover a pasta
`Persistencia/Temporaria`, o filtro e `armazenamento.js` — Domínio, Aplicação e views não mudam.

Impressão (`ImpressaoTicketService`, QuestPDF): A4 retrato, 12 tickets por folha (2 × 6, 95 × 46 mm), parte do
cliente + canhoto de controle com o responsável (ou linha para preencher), linhas de corte apenas nas posições
ocupadas. Lote inteiro ou só uma faixa.


O cadastro e a edição exigem o código manual do sistema de origem. A aplicação verifica duplicidade em todas as comunidades, preserva zeros à esquerda e retorna o erro no campo de código. Repetições antigas importadas são preservadas: a edição permite manter o código atual. A persistência JSON serializa as gravações para impedir duplicações simultâneas. O CSV de cadastro exige a coluna codigoOriginal e valida repetições no arquivo e nos registros existentes.
