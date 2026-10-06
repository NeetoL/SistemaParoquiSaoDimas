# Iniciar em outro computador

1. Copie a pasta completa `instalacao-windows` para uma pasta em que seu usuario possa gravar, por exemplo `Documentos\SaoDimas`.
2. Clique duas vezes em `INICIAR.bat`.
3. O navegador abre `http://127.0.0.1:5080`. Entre com sua conta; o acesso inicial padrao e admin / admin.

O pacote inclui .NET e os arquivos estaticos. Nao exige Visual Studio, Node.js, IIS, banco de dados ou internet para executar. Destinado a Windows de 64 bits compativel com .NET 10. Extraia o ZIP antes de iniciar; o BAT precisa das pastas que o acompanham.

O processo permanece em segundo plano quando a janela do BAT fecha. Execute INICIAR novamente para reabrir o navegador; ele verifica se o sistema ja esta funcionando. Nao e um servico registrado do Windows e nao inicia automaticamente ao ligar o computador. Para encerrar, finalize SaoDimas.MVC no Gerenciador de Tarefas.

Cadastros, anexos e backups ficam em DADOS. Copie essa pasta para fazer uma copia de seguranca. Nao substitua DADOS ao atualizar o programa. Logs de inicializacao ficam em LOGS. Os dados originais do projeto nao sao apagados ou substituidos pelo BAT.

Acesso restrito a este computador. O lancador utiliza o ambiente Development para permitir o cookie da autenticacao no HTTP local. Esse modo nao deve ser usado para disponibilizar o sistema na rede/internet.

## Quando utilizado na pasta do codigo-fonte

Na pasta do repositorio, INICIAR verifica o Git e executa pull quando nao existem alteracoes locais. Se estiver sem conexao ou o Git nao conseguir atualizar, avisa e utiliza o codigo disponivel. Alteracoes locais sao preservadas.

O BAT compara o codigo com a versao publicada e recompila quando houver mudancas, inclusive nos envelopes. Na primeira execucao com este atualizador, a publicacao antiga sem marcador tambem e recompilada. Baixa .NET SDK 10 e Node.js LTS 22 dos distribuidores oficiais quando necessario e instala-os localmente em .ferramentas. Essa preparacao pode exigir internet e demorar. Nao instala programas globalmente nem requer administrador.

A nova versao e preparada em outra pasta antes de parar somente a aplicacao desta instalacao. Depois, a pasta anterior fica guardada em SISTEMA_ANTERIOR com data e hora, e o sistema inicia com a nova versao. Se a compilacao falhar, a instalacao anterior permanece intacta. DADOS nao participa da substituicao. Sem mudancas no codigo, o BAT apenas inicia ou abre o sistema existente. Pacotes ZIP sem codigo-fonte continuam iniciando a versao incluida; para atualiza-los, substitua a pasta SISTEMA pelo pacote novo, mantendo DADOS.

Downloads do Node sao verificados por SHA-256 contra o manifesto oficial. Falhas interrompem a preparacao e mostram a mensagem. Dados existentes em DADOS nunca sao sobrescritos na inicializacao.

SAODIMAS_PORTA permite escolher outra porta, de 1024 a 65535. SAODIMAS_SEM_NAVEGADOR=1 evita abrir o navegador em verificacoes automatizadas.

Validado: sintaxe PowerShell, pacote self-contained com DOTNET_ROOT apontando para diretorio inexistente, duas execucoes consecutivas, CSS estatico, autenticacao, dashboard e geracao de envelope PDF. O fluxo de download/instalacao numa maquina Windows limpa nao foi executado nesta maquina; o pacote pronto dispensa esse fluxo.

## Cadastros incluidos no repositorio

A pasta dados-iniciais contem os 526 cadastros e comunidades. INICIAR.bat copia esses dados na primeira inicializacao ou acrescenta os ausentes quando ja existe DADOS/sistema.json. Uma copia anterior e salva em DADOS/backups antes da inclusao. Os registros ja existentes, contas e movimentacoes permanecem preservados. Reexecutar nao duplica os cadastros. Se a aplicacao estiver gravando no instante da importacao, aguarde e execute novamente.

A conta inicial e criada normalmente pelo aplicativo. O arquivo de cadastros publicado nao inclui senhas, usuarios, auditoria ou movimentacoes do computador de origem.

Os 522 registros importados das planilhas preservam o campo codigoOriginal, exatamente como a coluna Codigo Sistema da origem. INICIAR completa esse campo nos cadastros existentes quando estiver ausente, com backup antes da gravacao, sem trocar IDs nem duplicar pessoas. O envelope, os detalhes e a busca usam esse codigo. A correspondencia considera comunidade e nome, mais o ID original ou os contatos/endereco; nomes repetidos nao recebem um codigo quando a correspondencia for ambigua. O aplicativo tambem recupera o codigo ao ler um JSON antigo, usando dados-iniciais/cadastros.json, e o preserva nas gravacoes seguintes. Cadastros sem codigo de origem continuam com o codigo gerado pelo sistema.
