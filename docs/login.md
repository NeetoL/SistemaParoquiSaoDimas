# Login

O sistema exige autenticação para todas as páginas e documentos. A tela de login e os arquivos estáticos necessários à sua apresentação são públicos.

Conta inicial: usuário `admin`, senha `admin`. A configuração `Login:SenhaHash` armazena um hash do ASP.NET Core Identity; não armazena a senha em texto puro. Essa configuração cria a primeira conta; depois, as contas individuais ficam no arquivo de dados JSON. O administrador gerencia usuários, perfis e situação pela tela Usuários. Alterar senha, perfil ou situação invalida as sessões anteriores. Veja o [guia de gestão paroquial](gestao-paroquial.md).

A sessão usa cookie HttpOnly, SameSite=Lax e expira após 30 minutos de inatividade. O cookie é enviado somente por HTTPS em produção. O botão **Sair**, no menu do usuário, encerra a sessão por POST com proteção antiforgery. Login também exige antiforgery e aceita somente destinos locais após autenticação.

Há limite de dez envios de login por minuto por endereço IP. Ao exceder o limite, a resposta é 429 com orientação para aguardar. Credenciais inválidas recebem uma mensagem genérica; a senha não volta preenchida no formulário.

A política global protege também novos controladores. A implementação usa [autenticação por cookies do ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0).

Validação: testes das credenciais iniciais e fluxo real no Edge, incluindo login, logout, rejeição de senha incorreta, páginas/PDF protegidos, antiforgery, tentativa de redirecionamento externo, limite de tentativas e apresentação em computador e celular.
