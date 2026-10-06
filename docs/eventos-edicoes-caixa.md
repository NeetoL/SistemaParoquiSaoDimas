# Eventos, edições e caixa

A evolução integra os mesmos agregados, repositórios, páginas MVC, layout, Tailwind, HTMX e impressão de tickets. Não introduz banco de dados, migrations ou uma segunda persistência para o módulo.

## Identidade e compatibilidade

- `EventoBase` representa a identidade permanente, registrada uma vez em `eventosBase`.
- A entidade operacional existente `Evento` representa uma edição. Seu identificador continua sendo a chave usada por produtos, lotes e rotas, preservando os dados anteriores. Uma futura tabela pode mapear esta entidade como `EdicaoEvento` sem alterar as regras.
- `Ano` e `Base.Id` identificam a edição. Não se permite repetir o mesmo ano no mesmo evento-base.
- O documento temporário passa da versão 1 para a versão 2 de forma aditiva. A chave de armazenamento anterior continua válida. Eventos antigos recebem identidades determinísticas; não são agrupados automaticamente por nome.
- `ModeloEvento` contém apenas produtos e configurações reaproveitáveis. Uma edição copiada recebe novos produtos, preços independentes e numeração inicial 1; nenhum lote, responsável ou registro financeiro é copiado.
- Sem copiar preços, os produtos começam com preço zero e inativos até a configuração. Uma edição vazia não copia produtos.
- O modelo de impressão A4 existente permanece comum ao módulo. A implementação anterior não possuía configurações individuais de layout a copiar.

## Operação financeira

`IGestaoEventoAplicacao` coordena os repositórios existentes. `CaixaEvento` controla abertura, recebimentos, estornos, conferência, auditoria e fechamento. Valores monetários e decisões financeiras são calculados no backend com `decimal`.

O valor potencial corresponde aos lotes gerados; o devido corresponde exclusivamente às vendas confirmadas, com os preços preservados nos lotes. O prestado soma recebimentos e contrapartidas de estorno. O saldo é devido menos prestado. O dinheiro físico soma fundo de troco e movimentos em dinheiro; o troco nunca entra na receita.

Cada entrega gera um lançamento próprio, com responsável pelos tickets, operador, forma, data e observação. A confirmação possui identificador para impedir duplicidade por repetição da mesma requisição. Estornos preservam o original, referenciam seu identificador e geram movimento negativo. Não existe exclusão de movimentação confirmada.

Vendas e devoluções são confirmadas por faixa, inclusive parcialmente durante o evento, e cada atualização fica registrada no histórico. Os totais acumulados não podem reduzir quantidades já confirmadas; tickets ainda em posse continuam pendentes até serem vendidos ou devolvidos. Tickets livres podem ser cancelados por faixa com motivo e operador; não serão distribuídos, impressos nem renumerados. Faixas atribuídas precisam ser resolvidas antes do cancelamento dos tickets.

Prestações do documento antigo são mantidas. Na abertura do caixa, os recebimentos anteriores são importados como `Outro`, preservando valores e datas e informando explicitamente que o cadastro antigo não possuía operador e forma de recebimento. O caminho antigo de gravação de prestação foi bloqueado e seus atalhos direcionam à central.

## Conferência e fechamento

Cada operação relevante invalida a conferência anterior pela revisão do caixa. A conferência aceita contagem direta ou auxílio opcional por cédulas e moedas. Diferenças exigem justificativa.

O fechamento normal exige conferência atualizada, ausência de saldos pendentes e situação confirmada para todos os tickets, incluindo cancelamentos. O fechamento excepcional exige motivo, justificativa e operador e preserva suas pendências. O estado final bloqueia cadastro, preços, geração, distribuição, vendas, prestações, estornos e caixa. Não há edição silenciosa nem reabertura direta de uma edição fechada.

O PDF A4 usa a logo oficial e reúne identificação, abertura, troco, conferência, fechamento, resultados por produto e responsável, formas de recebimento, pendências, justificativas, movimentos e auditoria. A identificação do evento, comunidade e período fica preservada no fechamento para futuras reemissões.

## Persistência e próximos passos arquiteturais

O armazenamento continua isolado em `IEstadoEventosNavegador` e nos repositórios temporários. O JavaScript transporta o documento e oferece previews; não aplica regras financeiras. As requisições do módulo são serializadas na página para evitar respostas concorrentes sobrescrevendo seu estado.

O estado agora é persistido no JSON do servidor; veja `persistencia-json.md`. Para SQL Server, substituir os repositórios e a unidade de trabalho por EF Core, mantendo as regras. A implementação relacional deverá impor unicidade de evento-base/ano, identificadores de confirmação, integridade das faixas e concorrência transacional. Os contratos e registros de lançamento também permitem emissão futura de recibos. As datas das edições permitem integração com calendário, e a separação entre edições permite comparação histórica.

## Validação executada

- Compilação de toda a solução e do frontend: zero erros e avisos, com saída isolada em `.artifacts` enquanto a aplicação original estava aberta.
- 120 testes automatizados: arquitetura, DI, domínio, cálculos, persistência entre requisições, legado, numeração, impressão, vendas e prestações parciais, estorno, conferência, fechamento normal e excepcional, cancelamento e independência entre edições.
- Navegador Edge em contexto de teste separado: cadastro real, situação 42 vendidos/8 devolvidos, troco R$ 300, prestações R$ 500/R$ 300/R$ 250 em formas diferentes, duplicidade, busca sem acentos, contagem de cédulas, fechamento, download de PDF, recarga, repetição para 2027 e alteração de preço sem modificar 2026.
- Verificação em 1440 e 390 pixels, sem rolagem horizontal no celular e sem erros JavaScript ou de console. PDF renderizado para inspeção visual.

Comandos usados: `dotnet build SaoDimas.sln --artifacts-path .artifacts` e `dotnet test SaoDimas.sln --artifacts-path .artifacts`. A aplicação previamente aberta precisa ser reiniciada para carregar os novos assemblies.
