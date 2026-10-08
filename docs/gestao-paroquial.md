# Gestão paroquial — guia de uso

O painel e o menu lateral dão acesso aos módulos conforme o perfil da pessoa. Os testes usam diretórios separados dos cadastros reais.

## Acesso e perfis

O primeiro acesso continua sendo **admin / admin**. Em **Usuários**, o administrador cria contas individuais, escolhe o perfil e pode desativar acessos. Novas senhas devem ter de 8 a 128 caracteres. O menu do usuário oferece perfil, troca de senha e saída.

- **Administrador:** todos os módulos, usuários, auditoria e backups.
- **Secretaria:** dizimistas, envelopes, contribuições, agenda, sacramentos, pastorais, voluntários, escalas e relatórios desses módulos.
- **Tesouraria:** dizimistas, envelopes, contribuições, financeiro, agenda e relatórios permitidos.
- **Coordenador:** agenda, pastorais, voluntários, escalas e seus relatórios.

Todos têm acesso ao painel, calendário litúrgico e consultas de eventos. A gestão de caixa dos eventos fica com administrador e tesouraria. As permissões são conferidas no servidor. Alterar senha, perfil ou situação invalida as sessões antigas. O último administrador ativo não pode ser desativado ou perder seu perfil.

## Dízimo e financeiro

Em **Dízimo e contribuições**, escolha o dizimista cadastrado, competência, comunidade, valor e forma de contribuição. A ficha permite emitir recibo PDF e baixar o comprovante.

Em **Financeiro**, registre entradas e despesas com categoria e comprovante. Os totais incluem automaticamente as contribuições e movimentações de recebimento/estorno dos eventos; o fundo de troco não entra como receita. Os filtros selecionam período e comunidade. Para corrigir valores, cancele o lançamento e cadastre o correto: o registro antigo continua no histórico.

Comprovantes podem ser PDF, PNG ou JPEG de até 5 MB e são armazenados junto dos dados JSON.

## Agenda, secretaria e pastorais

- **Agenda paroquial:** missas, reuniões e reservas. O sistema impede sobreposição no mesmo espaço; o painel mostra os próximos compromissos.
- **Secretaria e sacramentos:** batismos, crismas e casamentos com celebrante, livro, folha e termo. O mesmo termo não pode ser registrado duas vezes na mesma comunidade e sacramento. A ficha emite PDF com os dados cadastrados para conferência e assinatura da secretaria.
- **Pastorais:** responsáveis e comunidade.
- **Voluntários:** vínculo com a pastoral, contatos e disponibilidade.
- **Escalas:** serviço e horário de cada voluntário; sobreposições são recusadas.

Registros podem ser editados ou cancelados. Pastorais e voluntários com vínculos ativos exigem ajustar esses vínculos antes de cancelar. Uma edição desatualizada é recusada para preservar alterações feitas por outra pessoa.

## Relatórios e planilhas

**Relatórios** oferece filtros por módulo, período e comunidade, PDF e CSV. O relatório financeiro reúne lançamentos, contribuições e eventos e apresenta saldo; o CSV identifica a origem. A exportação CSV de cada cadastro serve para os dados próprios daquele módulo.

A importação usa **CSV UTF-8 separado por ponto e vírgula**, até 500 registros e 2 MB por arquivo. Baixe o modelo da tela, preencha e envie para validar. Uma prévia mostra até dez linhas; os dados somente são gravados após confirmar e passam por nova validação. Um erro em qualquer linha recusa o lote inteiro. Duplicidades e conflitos são conferidos.

Datas usam AAAA-MM-DD, competências AAAA-MM e horários AAAA-MM-DDTHH:mm. Valores aceitam vírgula ou ponto decimal, sem separador de milhar. Vínculos usam os identificadores da comunidade, dizimista, pastoral ou voluntário; a ficha mostra o identificador usado para importação. No cadastro de dizimistas, status usa Ativo ou Inativo.

## Auditoria e backups

**Auditoria** registra operador, data, ação e referência. Os novos módulos também guardam os valores anteriores e posteriores. Senhas são armazenadas como hash e nunca aparecem no histórico de alterações.

**Backups** permite gerar e baixar uma cópia ou enviar um arquivo salvo em outro dispositivo. Antes de restaurar, a tela mostra quantidades, verifica o arquivo e pede a palavra RESTAURAR. O sistema preserva uma cópia do estado anterior e mantém as contas e o histórico de auditoria atuais para evitar reativar acessos antigos.

Os dados ficam em SaoDimas.MVC/App_Data/sistema.json; os comprovantes fazem parte desse arquivo. Na primeira alteração de cada dia é guardada uma cópia diária do estado anterior, mantendo as 30 mais recentes. Backups manuais e anteriores à restauração também ficam em App_Data/backups. A gravação ainda conserva sistema.json.bak. Baixe periodicamente uma cópia para outro dispositivo.

Se o arquivo principal ficar corrompido e o login não abrir, pare o servidor e substitua sistema.json por uma cópia íntegra guardada, preservando o arquivo corrompido para investigação. A recuperação pelo arquivo também recupera as contas existentes naquela cópia. Depois reinicie e confira os dados.

## Verificação

Compilação sem avisos, testes automatizados de domínio, arquitetura e persistência e verificações no navegador com dados isolados. Incluem saldo, recibos e certidões, importação, conflitos, permissões, invalidação de sessão, restauração, proteção dos formulários, temas e três larguras de tela.

## Rifas

Em **Gestão Paroquial → Rifas**, administrador, secretaria e tesouraria podem criar uma rifa, escolher a comunidade, informar o valor de cada número, a data do sorteio e até 20 prêmios (um por linha). A numeração vai de 0001 até a quantidade cadastrada, limitada a 10.000 números.

Reserve até 100 números por vez, informando comprador, contato e vendedor. O sistema impede reservas repetidas, inclusive entre computadores, e preserva o formulário quando há erro. A listagem permite pesquisar número, comprador ou vendedor, filtrar por situação e navegar em páginas de 100 números.

Confirme o pagamento de cada número, com sua forma de pagamento. O painel da rifa mostra disponíveis, reservados, pagos, valor recebido e pendente. Estes valores são o controle próprio da rifa; ainda não são lançamentos automáticos no módulo Financeiro. Reservas não pagas podem ser liberadas; pagamentos podem ser estornados antes do resultado. Cada alteração fica na auditoria.

Feche as vendas antes de registrar o resultado apurado no sorteio. Escolha o prêmio, informe o número pago vencedor e a referência ou ata, e confirme o registro definitivo. O sistema não realiza sorteio automático. Quando todos os prêmios têm resultado, a rifa é concluída e fica somente para consulta. Vendas podem ser reabertas antes do primeiro resultado. Para cancelar a rifa, estorne os pagamentos e confirme o cancelamento.

**Imprimir bilhetes** abre os bilhetes reservados para impressão em A4; cada número também tem impressão individual. **Exportar CSV** gera a lista de compradores, contatos, vendedores, situação e pagamentos. As rifas ficam no mesmo JSON e são incluídas nos backups e na restauração.

Em cada rifa, Baixar bilhetes PDF gera os números de um intervalo ou de toda a rifa, incluindo os disponíveis para venda. Cada bilhete tem número repetido no canhoto da organização, prêmios, valor, data e espaços para nome e contato. A folha A4 comporta até seis bilhetes; listas longas de prêmios usam menos bilhetes para preservar o texto. Imprima em 100% e corte pelas bordas. Gerar o arquivo não reserva números nem registra pagamentos.

A ficha da rifa organiza prêmios e reserva ao lado do mapa de números. Clique ou use o teclado nos números livres para incluí-los no campo da reserva; selecione novamente para remover. A seleção não grava nada até confirmar a reserva. Reservados e pagos têm identificação por texto e cor; clicar em um número ocupado leva ao comprador. A tabela exibe apenas compradores do intervalo atual, e os controles de fechamento e cancelamento permanecem recolhidos.
