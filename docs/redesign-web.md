# Redesign da interface web — 6 de outubro de 2026

## Auditoria e cobertura

Inventário: layout, sidebar, topbar, menu de usuário, temas, feedback, estados HTTP; dashboard e gráficos; login/perfil/senha; dizimistas (lista, cadastro, edição, detalhes); calendário; módulos de gestão (lista, formulário, detalhes e importação); eventos e seus partials HTMX; modais de produtos e tickets; usuários, auditoria, backups e restauração. As páginas de relatórios e de preparação de envelopes recebem apenas os componentes globais da interface. Seus links, downloads, exportações e documentos não foram modificados.

Problemas encontrados: breadcrumb competindo com ações, controles e densidade variando entre módulos, filtros de datas permanentemente ocupando espaço, identificação e endereço no mesmo grupo de formulário, calendário afastado do gráfico principal e navegação sem acesso rápido.

## Estrutura e fonte de verdade

`SaoDimas.MVC/wwwroot/ui/produto.css` centraliza os tokens e a apresentação final da interface. É CSS autoral, não gerado. Usa apenas `@media screen` e é carregado com `media="screen"`. O arquivo base `wwwroot/src/app.css` continua responsável pelos componentes funcionais, utilitários, bibliotecas e estados existentes; não alterar estilos de impressão para evoluir a interface.

A linguagem visual usa navegação em azul grafite, área de trabalho neutra, superfícies brancas ou azul grafite no tema escuro, ações em azul e cores distintas apenas para dados e feedback. Breadcrumb fica acima do título; ações ficam ao lado do cabeçalho. O painel organiza gráfico e agenda em duas colunas, com eventos e comunidades abaixo. Formulários usam seções numeradas e separação por contexto. Tabelas conservam todos os campos e ganham espaçamento consistente. Filtros de período/comunidade nos módulos de gestão ficam em um painel expansível; filtros preenchidos reabrem esse painel.

O acesso rápido (`_AcessoRapido.cshtml` e `produto.js`) abre por botão ou Ctrl/Command+K. Seus destinos são extraídos da sidebar já filtrada pelas permissões. Não pesquisa dados pessoais, não cria endpoints e não amplia acesso. Busca ignora acentos; Tab/Enter, seta para baixo e Escape funcionam com dialog nativo.

## Tokens e componentes

Escala de espaço: 4/8/12/16/24/32/48 px. Título: 28 px, 24 px em celular; conteúdo/controle: 12–14 px; seção: 15–18 px; auxiliar: 10–12 px. Rubik local permanece. Raios: 6 px para chips, 8 px para controles, 12 px para superfícies, 16 px apenas para dialog e composição de acesso. Sombras ficam nas sobreposições.

Tokens: `--sd-action` (primary), `--sd-secondary`, `--sd-accent`, `--sd-background`, `--sd-surface`, `--sd-surface-muted`, `--sd-border`, `--sd-border-strong`, `--sd-foreground`, `--sd-muted`, `--sd-subtle`, `--sd-ring`, `--sd-overlay`, `--sd-nav`, `--sd-nav-text`, `--sd-nav-muted`. Feedback reutiliza as famílias success/warning/danger/info existentes.

Reutilizar btn, form-control/form-field/form-check, badge, card, modal, table-wrapper/table, paginacao, breadcrumb, menu-popover, empty-state, toast, skeleton e ui-folha. Filtros compactos novos usam produto-filtros/produto-filtro-avancado; contagem e contexto usam produto-resultados/produto-chip. IDs, nomes de campos, hx-*, tokens, rotas e submitters permanecem funcionais.

## Isolamento de documentos e dados

Nenhum serviço, controller, template, regra de impressão, configuração ou imagem de PDF foi editado. Os documentos são gerados pelos serviços QuestPDF, sem carregar o CSS da interface. Dados JSON de produção não são usados para testes de alteração. Validação de salvamento e modais utiliza um diretório JSON isolado.

## Validação

Compilação final sem erros ou avisos. Passaram 147 testes .NET e seis testes de calendário. Navegador: 114 verificações de páginas/estados nos dois temas e em 390/768/1024/1440/1920 px; 16 verificações adicionais dos fluxos existentes; 19 verificações de acesso rápido, filtros preenchidos, páginas de todos os módulos disponíveis e capturas visuais. Nenhum erro de JavaScript detectado nesses fluxos. Login, dashboard, listagem e formulário foram inspecionados visualmente. Revisados contraste de textos/ações/bordas, labels, foco, teclado, movimento reduzido e contenção de rolagem; isso não equivale a uma certificação formal de acessibilidade.

Os 526 dizimistas de produção permaneceram salvos. Operações de teste utilizaram JSON isolado. Não foram alteradas regras de negócio ou serviços de documentos.
