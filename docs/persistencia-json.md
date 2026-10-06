# Persistência JSON

Todos os registros dos módulos implementados são guardados no servidor em `SaoDimas.MVC/App_Data/sistema.json`: comunidades, dizimistas, endereços, aniversários, eventos-base, edições anuais, produtos, lotes, distribuições, prestações de contas, movimentações de caixa, auditoria, cancelamentos e fechamentos. As contas individuais, contribuições, financeiro, agenda, sacramentos, pastorais, voluntários, escalas e comprovantes também ficam nesse arquivo. A configuração inicial cria admin/admin, e as senhas são guardadas em hash.

O uso diário não exige SQL Server nem connection string. Os contratos de repositórios e aplicação foram preservados; EF Core e migrações continuam disponíveis para a futura integração com banco. O diretório pode ser definido por `Persistencia:Diretorio` ou `Persistencia__Diretorio` (padrão `App_Data`, relativo à raiz da aplicação).

## Dados existentes

Os quatro dizimistas e as quatro comunidades do SQL local foram exportados com os identificadores originais. O banco permaneceu intacto. A fotografia inicial está em `sistema.json.importacao-sql.bak`.

Ao abrir o módulo Eventos no navegador usado anteriormente, o documento antigo de localStorage é importado se o servidor ainda não tiver eventos. A importação ocorre uma vez; documentos antigos ou adulterados não substituem os eventos já existentes no servidor. A cópia local antiga não é apagada. Se houver dados diferentes em mais de um navegador, será necessário conciliá-los antes de uma importação adicional: a aplicação não mistura documentos automaticamente.

Após a importação, qualquer navegador autenticado consulta o mesmo arquivo. LocalStorage fica apenas como origem da importação e para preferências visuais, sem guardar novas operações de eventos.

## Gravação e recuperação

Uma unidade de trabalho por requisição carrega os dados sob exclusão mútua. Um lock de arquivo também impede que duas instâncias da aplicação gravem simultaneamente. Cadastro, validação de CPF, reserva de IDs e gravação ocorrem dentro dessa unidade, evitando registros perdidos e identificadores repetidos.

Cada gravação escreve um arquivo temporário, confirma sua escrita em disco e substitui o principal. A versão anterior fica em `sistema.json.bak`. Falhas de leitura ou versão inválida interrompem a operação sem recriar um documento vazio. Para restaurar, encerre a aplicação, preserve o arquivo atual e copie a versão de segurança para `sistema.json`.

`App_Data` fica fora de wwwroot, não é servido como arquivo estático, não é versionado e não é incluído no publish. Em uma mudança de computador/servidor, copie o diretório de dados junto com o sistema e configure o mesmo caminho. Além da versão anterior, o sistema guarda até 30 cópias diárias do estado anterior à primeira alteração do dia. A tela Backups oferece criação, download, envio de arquivo e restauração validada; mantém uma cópia anterior à restauração e preserva as contas e o histórico de auditoria atuais. Veja o [guia de gestão paroquial](gestao-paroquial.md).

## Validação

Testes cobrem cadastro/edição, consultas e envelope após reinício, eventos, tickets e caixa, gravações simultâneas em duas instâncias, CPF duplicado, arquivo corrompido e importação do navegador. O fluxo completo foi validado no Edge sem conexão SQL, incluindo caixa, relatório PDF, nova edição anual e acesso por outro contexto sem localStorage.
