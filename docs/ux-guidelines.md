# Diretrizes de experiência — São Dimas

## Contexto

Pessoas da secretaria, tesouraria e coordenação utilizam cadastros, reservas, livros, contribuições, eventos e tickets. A interface deve reduzir esforço de consulta e lançamento. A identidade ajuda a reconhecer o produto, enquanto os nomes e a hierarquia ajudam a trabalhar.

## Navegação

Manter a visão da paróquia como acesso direto. As áreas Gestão Paroquial, Rotina da paróquia e Administração são grupos expansíveis. Começam fechadas; ao abrir uma, a anterior fecha. No menu recolhido, clicar em um grupo expande a navegação. Nunca exibir uma ação à qual o perfil não tem acesso.

A barra superior indica a organização e o contexto. O cabeçalho indica a página e as ações. Breadcrumbs detalham a origem quando houver navegação por registro. A edição anual de evento deve continuar explícita, pois muda o contexto financeiro.

## Painel

Exibir contagens reais, próximos compromissos, comunidades, áreas disponíveis e edições. Priorizar próximos compromissos e eventos juntos para aproveitar a área de trabalho. Não usar frases promocionais, brasões decorativos enormes ou dezenas de cartões de atalhos.

## Formulários

Uma folha, grupos com significado. Identificação, contato, comunidade, celebração, horário e registro em livro são grupos diferentes. Obrigatoriedade indicada por label e atributo nativo. Não misturar validação visual com a validação de negócio.

Manter informações enviadas após erro. Associar descrições e mensagens aos campos. Quando o servidor retornar erro geral, conservar o resumo. Carregamento deve mostrar que o envio ocorreu, mantendo os nomes e valores do submitter.

## Tabelas

Conteúdo antes de decoração. Cabeçalho claro, valores à direita, códigos tabulares, ações discretas e situação por texto. Ordenação disponível deve comunicar aria-sort. Manter filtros em paginação. Usar seleção em lote somente quando existir uma operação real, como geração de envelopes.

Informações longas podem quebrar linha ou rolar dentro da tabela. Não ocultar dados essenciais do celular apenas para a tela caber.

## Feedback e decisões

Salvar, cancelar, restaurar e emitir documento têm rótulos específicos. Erro deve explicar como continuar. Estado vazio diferencia ausência de cadastro de pesquisa sem resultado. Operações irreversíveis ou correções financeiras preservam suas confirmações existentes.

Importação continua validando antes de confirmar. Restauração continua mostrando quantidades e pedindo RESTAURAR. Refatoração visual não deve reduzir essas etapas.

## Auditoria realizada

Foram inventariadas as views completas, incluindo os partials de formulários, estados, modais e tickets. Também foram revisados layout, navegação, bibliotecas locais, CSS, inicialização e comportamento JavaScript.

Diagnóstico:
- painel com hero, círculos e slogans;
- cartões e sombras aplicados a informações sem necessidade;
- CSS com base mais overrides decorativos, inclusive regras específicas repetidas para o tema;
- tabela e formulários variando em densidade;
- filtros largos demais em notebook;
- rolagem horizontal da página originada por conteúdo acessível de tabela;
- mensagens de erro HTTP sem apresentação de página.

Resultado:
- tokens azul/grafite e neutros frios;
- login institucional e painel operacional;
- listas de eventos e atalhos em linhas;
- uma folha com fieldsets nos formulários;
- faixas de filtros, indicadores contínuos e tabelas planas;
- temas com tokens compartilhados, foco e contraste;
- correções de responsividade e apresentação de estado HTTP;
- remoção dos antigos heróis, órbitas, blur e overrides decorativos.

Regras de negócio, dados, serviços, repositórios, permissões, autenticação, URLs e antiforgery não fazem parte dessa reformulação. A única adaptação de controller apresenta o acesso negado em uma view, preservando o status 403. O middleware de estado só apresenta respostas HTML vazias.

## Evolução

Toda nova tela utiliza o Design System. Reutilizar antes de criar. Qualquer exceção necessária precisa ter uma razão de uso e deve ser documentada; preferência estética isolada não justifica um novo padrão.


## Validação da reformulação

Compilação sem erros ou avisos. Os 147 testes .NET e os 6 testes de calendário passaram. A inspeção automatizada do navegador aprovou 114 verificações de telas/estados em dois temas e cinco larguras, sem erros de JavaScript, além de 16 verificações de login, teclado, menu exclusivo, popover, navegação recolhida, drawer, salvamento de dizimista/pastoral, envelope PDF e movimento reduzido.

Os testes de lançamento utilizaram cópia isolada de JSON. Respostas 404 HTML, 403, JSON e HTMX foram conferidas. Contraste dos textos e ações principais e das bordas de controle foi revisado; isso não equivale a uma certificação completa de acessibilidade.

## Revisão da direção visual

A composição inicial foi considerada tradicional pelo usuário. A revisão remove serifas, latão e tons de papel; adota tipografia sem serifa, azul, grafite, raios moderados e superfícies mais definidas. O painel mantém a densidade operacional, com tratamento próprio para datas e indicadores. Essa direção substitui a anterior em todas as telas.

## Dashboard: dados e cores

A pedido do usuário, o dashboard usa múltiplas cores de visualização sem vincular a paleta à igreja. O fundo permanece neutro. Contagens por área consideram apenas registros ativos e módulos disponíveis ao perfil; são contagens de cadastros, não movimento financeiro. A agenda semanal usa todos os compromissos do período, e a lista apresenta os quatro próximos. A distribuição de tickets usa os totais existentes. A apresentação não modifica regras de negócio ou permissões.

## Calendário e pagamentos no dashboard

O calendário ocupa a largura principal e consulta o mesmo calendário brasileiro já usado na página da fé. A seleção do dia troca os santos/celebrações e o link do Martirológio; a lista de próximos compromissos permanece identificada separadamente. O Martirológio é uma consulta ampliada externa, não uma lista embutida de todos os santos.

O gráfico interativo cobre doze meses do ano selecionado até hoje no fuso de São Paulo. Usa entradas e despesas reais, exclui cancelados, inclui contribuições e caixa de eventos quando o perfil permite Financeiro. Perfis com acesso somente a contribuições veem somente os recebimentos desse módulo; perfis sem acesso financeiro não recebem os valores na página. Meses vazios são zero. Os valores exatos ficam nos nomes acessíveis dos botões e no resumo do mês selecionado.

## Referência visual aprovada pelo usuário

O usuário pediu explicitamente adaptar o dashboard da imagem fornecida. A composição atual segue suas proporções: indicadores compactos, gráfico anual dominante, resumos laterais e blocos de calendário abaixo. Azul e ciano substituem a paleta anterior. O menu recolhido usa faixa azul; o expandido mantém os grupos com rótulos. O seletor de ano consulta valores reais, sem números ou variações percentuais fictícias. Recebimentos e despesas respeitam as permissões já descritas.


## Redesign web de outubro de 2026

A apresentação atual e o inventário estão em redesign-web.md. Breadcrumb acima do título; navegação em azul grafite; acesso rápido por Ctrl/Command+K; gráfico e agenda juntos; identificação, contato e dados paroquiais em seções distintas. Filtros de data/comunidade podem ser expandidos e mantêm o estado quando preenchidos. Grupos da sidebar continuam fechados e exclusivos.

Apenas apresentação web foi alterada. Controllers, serviços, JSON de produção e geração de documentos permanecem fora do redesign.
