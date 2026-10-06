# Instruções para alterações neste projeto

## Interface São Dimas

Antes de criar ou alterar interface, ler:
- docs/design-system.md
- docs/ui-guidelines.md
- docs/ux-guidelines.md

Todas as novas telas devem usar esse Design System. Reutilizar os componentes de wwwroot/src/app.css e Views/Shared antes de criar outros. Preservar os dois temas e os grupos exclusivos da navegação.

Não introduzir heróis administrativos, cards decorativos, gradientes azul/roxo, glassmorphism, raios grandes generalizados ou outra paleta por módulo. Não editar CSS/ícones/assets gerados.

## Escopo e validação

Preservar regras de negócio, rotas, nomes de campos, permissões e antiforgery em mudanças visuais. Não usar dados reais para testes de lançamento/importação/restauração; trabalhar com diretório JSON isolado.

Conferir responsividade, teclado, erros, modais e os dois temas. Compilar e rodar verificações apropriadas às alterações. Manter a arquitetura descrita em docs/arquitetura.md.

