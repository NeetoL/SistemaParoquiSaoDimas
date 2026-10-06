# Diretrizes de implementação da interface

Antes de alterar ou criar uma tela, ler design-system.md e ux-guidelines.md. Buscar um componente equivalente nas views compartilhadas, em wwwroot/src/app.css (base funcional) e wwwroot/ui/produto.css (apresentação de tela). A direção atual está em redesign-web.md.

## Estrutura

A aplicação usa ASP.NET Core MVC e Razor. O layout está em Views/Shared/_Layout.cshtml. Sidebar, barra de contexto, tema, menu do usuário e notificações são partials compartilhadas. Os módulos de eventos/tickets usam HTMX e dialog nativo. Os demais formulários continuam com seu fluxo MVC e antiforgery.

Definir ViewData["Title"] e, quando necessário, ViewData["Descricao"]. A ação principal pertence à seção Acoes; não criar um segundo cabeçalho promocional. Breadcrumb deve refletir o cadastro ou contexto existente.

Exemplo de formulário:

    <form class="ui-folha" method="post">
        <!-- Manter o token antiforgery e os nomes originais dos campos. -->
        <fieldset class="ui-formulario-secao">
            <legend>Dados pessoais</legend>
            <div class="form-field">
                <label for="nome" class="form-label">Nome completo</label>
                <input id="nome" name="nome" class="form-control" required />
            </div>
        </fieldset>
        <div class="ui-formulario-acoes">
            <a href="..." class="btn btn-outline">Cancelar</a>
            <button class="btn btn-primary">Salvar</button>
        </div>
    </form>

Exemplo de tabela:

    <div class="table-wrapper">
        <table class="table">
            <caption class="sr-only">Lançamentos do período</caption>
            <thead><tr><th scope="col">Descrição</th><th scope="col" class="ui-numero">Valor</th></tr></thead>
            <tbody><tr><td>...</td><td class="ui-numero">R$ ...</td></tr></tbody>
        </table>
    </div>

Não adicionar estilos inline para cores, sombras ou espaçamentos. Uma largura proporcional calculada para uma faixa de tickets é conteúdo funcional e pode permanecer inline. Não criar utilitários com hexadecimais arbitrários nas views.

## JavaScript

Preferências visuais: preferencias.js. Navegação: sidebar.js. Modais: modal.js. Notificações: toast.js. Descrições acessíveis e carregamento: ui.js. Inicialização compartilhada: app.js.

Preservar IDs, data-*, hx-*, nomes de campos, URL e tokens usados por fluxos existentes. ui.js não valida regras de negócio nem muda os dados enviados. Não converter os formulários para outro framework.

## Responsividade e acesso

Validar em 390, 768, 1024, 1440 e 1920 px. Tabelas podem rolar dentro da sua área; a página inteira não deve rolar horizontalmente. Os filtros de eventos usam colunas fixas somente quando há espaço no desktop. A tabela tem um contexto de posicionamento próprio para conter textos acessíveis.

Controles e botões pequenos têm áreas clicáveis de pelo menos 44 px no celular. Labels associados, ícones decorativos aria-hidden e botões só com ícone aria-label. Testar teclado, menus, ESC, popover, foco e preferência de movimento reduzido.

## Erros

Estado.cshtml fornece apresentação para respostas HTML vazias 400, 403, 404 e 500. O código HTTP é mantido. Respostas JSON, documentos, fragmentos HTMX e mensagens já produzidas pelos endpoints são preservadas. A tela AcessoNegado usa o mesmo padrão.

## Qualidade

Compilar os assets e o projeto. Conferir telas populadas e vazias nos dois temas. Conferir formulários com erro, carregamento, tabelas selecionadas, modais e retorno por navegação. Rodar os testes relevantes após mudanças estruturais. Usar cópia isolada dos dados para validar operações.

