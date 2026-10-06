# São Dimas — Design System da interface web

A direção atual está documentada em [redesign-web.md](redesign-web.md). Esta especificação substitui as paletas e composições anteriores.

## Identidade e tokens

Navegação azul grafite, área de trabalho neutra e ações azuis. Superfícies claras ou azul grafite no dark mode, sem gradientes, glassmorphism ou ornamentos. Brasão institucional e monograma SD permanecem. Cores de dados: azul, verde azulado, violeta e âmbar. Os estados litúrgicos conservam suas cores próprias.

A apresentação final e os tokens ficam em **SaoDimas.MVC/wwwroot/ui/produto.css**, CSS autoral carregado exclusivamente em tela. Componentes funcionais, utilitários e estados continuam em **wwwroot/src/app.css**, compilado pelo Tailwind. Nunca editar CSS gerado. Alterações de interface não podem mudar PDF ou impressão.

| Token / papel | Claro | Escuro |
| --- | --- | --- |
| --sd-action / Primary | #3063da | #346bd5 |
| --sd-secondary / Secondary | #26364f | #cad6e9 |
| --sd-accent / Accent | #157b88 | #79cbd4 |
| --sd-background | #f3f5f8 | #111a27 |
| --sd-surface | #ffffff | #1a2637 |
| --sd-surface-muted | #f6f8fb | #223044 |
| --sd-border | #e2e7ef | #334259 |
| --sd-border-strong | #8490a6 | #7e91ac |
| --sd-foreground / Text Primary | #202b40 | #edf3fc |
| --sd-muted / Text Secondary | #56647c | #bdcbe0 |
| --sd-nav | #18283d | #121f30 |

Sucesso/atenção/erro/informação usam success/warning/danger/info existentes. Cor acompanha texto. Estados de foco usam --sd-ring; carregamento usa aria-busy, spinner e skeleton. Controles readonly e disabled têm tratamento próprio; erros conservam suas mensagens e valores.

## Tipografia e espaço

Rubik Variable local, sem dependências externas. Títulos: 28 px, 24 px em celular; seções: 15–18 px; controles e conteúdo: 12–14 px; auxiliares: 10–12 px. Números financeiros tabulares e alinhados à direita; códigos monoespaçados.

Escala: 4/8/12/16/24/32/48 px. Raios: 6 px para chips; 8 px para controles; 12 px para superfícies; 16 px para dialog e composição de login. Sombras pertencem às sobreposições. Movimento de 150 ms com respeito a prefers-reduced-motion.

## Componentes compartilhados

| Necessidade | Componente |
| --- | --- |
| Ações | btn + btn-primary/secondary/outline/ghost/danger |
| Controles | form-field, form-label, form-control, form-check |
| Filtros | ui-filtros, produto-filtros, produto-filtro-avancado |
| Contexto dos resultados | produto-resultados, produto-chip |
| Formulários | ui-folha, ui-formulario-secao, ui-formulario-acoes |
| Tabelas | table-wrapper, table, table-acoes, ui-numero |
| Estados e navegação | badge, toast, alerta-erro, empty-state, skeleton, breadcrumb, paginacao |
| Sobreposições | modal, menu-popover, produto-acesso |
| Dashboard | dashboard-metricas, dashboard-painel, dashboard-grade, dashboard-rede |

Nenhum componente muda regras, rotas, nomes de campos, permissões ou antiforgery. IDs, data-* e hx-* permanecem nos fluxos existentes. Não criar seleção, ordenação ou ação fictícia.

## Composição

Breadcrumb acima do título, descrição curta e ações ao lado. Sidebar com grupos inicialmente fechados e exclusivos; faixa recolhida e drawer móvel. Topbar com organização, acesso rápido, tema e conta. Acesso rápido por Ctrl/Command+K mostra somente destinos já presentes na navegação autorizada.

Dashboard: métricas, gráfico financeiro e agenda em paralelo no desktop, eventos e comunidades abaixo. Gráficos usam dados reais e preservam alternância barras/evolução, seleção mensal e composição por pagamento. Permissões financeiras continuam aplicadas pelo backend. Calendário brasileiro e Martirológio continuam separados.

Formulários: seções por contexto, com números discretos no desktop. Filtros avançados fecham quando vazios e permanecem abertos quando há valores. Tabelas contêm sua rolagem e não ocultam dados essenciais. Login usa formulário compacto centralizado, marca institucional no topo e controle acessível para mostrar a senha.

## Documentos

Não alterar serviços, controllers, templates, imagens, configurações ou CSS de PDF/impressão. A interface utiliza uma folha de estilo de tela isolada. QuestPDF continua gerando documentos sem essa folha.

