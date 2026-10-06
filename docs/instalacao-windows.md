# Iniciar em outro computador

1. Copie a pasta completa `instalacao-windows` para uma pasta em que seu usuario possa gravar, por exemplo `Documentos\SaoDimas`.
2. Clique duas vezes em `INICIAR.bat`.
3. O navegador abre `http://127.0.0.1:5080`. Entre com sua conta; o acesso inicial padrao e admin / admin.

O pacote inclui .NET e os arquivos estaticos. Nao exige Visual Studio, Node.js, IIS, banco de dados ou internet para executar. Destinado a Windows de 64 bits compativel com .NET 10. Extraia o ZIP antes de iniciar; o BAT precisa das pastas que o acompanham.

O processo permanece em segundo plano quando a janela do BAT fecha. Execute INICIAR novamente para reabrir o navegador; ele verifica se o sistema ja esta funcionando. Nao e um servico registrado do Windows e nao inicia automaticamente ao ligar o computador. Para encerrar, finalize SaoDimas.MVC no Gerenciador de Tarefas.

Cadastros, anexos e backups ficam em DADOS. Copie essa pasta para fazer uma copia de seguranca. Nao substitua DADOS ao atualizar o programa. Logs de inicializacao ficam em LOGS. Os dados originais do projeto nao sao apagados ou substituidos pelo BAT.

Acesso restrito a este computador. O lancador utiliza o ambiente Development para permitir o cookie da autenticacao no HTTP local. Esse modo nao deve ser usado para disponibilizar o sistema na rede/internet.

## Quando utilizado na pasta do codigo-fonte

Se SISTEMA ainda nao existe, o mesmo BAT baixa .NET SDK 10 e Node.js LTS 22 dos distribuidores oficiais, instala-os localmente em .ferramentas, prepara o frontend e publica o programa com runtime incluido. Essa primeira preparacao exige internet e pode demorar. Nao instala programas globalmente nem requer administrador.

Downloads do Node sao verificados por SHA-256 contra o manifesto oficial. Falhas interrompem a preparacao e mostram a mensagem. Dados existentes em DADOS nunca sao sobrescritos na inicializacao.

SAODIMAS_PORTA permite escolher outra porta, de 1024 a 65535. SAODIMAS_SEM_NAVEGADOR=1 evita abrir o navegador em verificacoes automatizadas.

Validado: sintaxe PowerShell, pacote self-contained com DOTNET_ROOT apontando para diretorio inexistente, duas execucoes consecutivas, CSS estatico, autenticacao, dashboard e geracao de envelope PDF. O fluxo de download/instalacao numa maquina Windows limpa nao foi executado nesta maquina; o pacote pronto dispensa esse fluxo.
