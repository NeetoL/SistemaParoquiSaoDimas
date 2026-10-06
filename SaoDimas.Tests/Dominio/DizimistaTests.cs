using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;

namespace SaoDimas.Tests.Dominio;

public sealed class DizimistaTests
{
    private static readonly Comunidade CapelaAtiva = Criar.Comunidade(2);

    [Fact]
    public void Dados_do_envelope_sao_normalizados_e_atualizacao_invalida_preserva_cadastro()
    {
        var resultado = Dizimista.Criar("Ana", null, null, CapelaAtiva, Criar.Hoje,
            StatusDizimista.Ativo, RelogioFixo.Padrao, " Rua  das Flores, 10 ", "21775-280", " Padre  Miguel ", new DateOnly(1990, 7, 12));
        Assert.True(resultado.Sucesso);
        var dizimista = resultado.Valor;
        Assert.Equal("Rua das Flores, 10", dizimista.Endereco);
        Assert.Equal("21775280", dizimista.Cep);
        Assert.Equal("Padre Miguel", dizimista.Bairro);
        var invalido = dizimista.Atualizar("Outro nome", null, null, CapelaAtiva, Criar.Hoje,
            StatusDizimista.Ativo, RelogioFixo.Padrao, "Outro endereço", "123", "Outro bairro", Criar.Hoje.AddDays(1));
        Assert.False(invalido.Sucesso);
        Assert.Equal("Ana", dizimista.Nome);
        Assert.Equal("21775280", dizimista.Cep);
        Assert.Contains(invalido.Erros, erro => erro.Campo == nameof(Dizimista.DataNascimento));
    }

    [Fact]
    public void Dizimista_valido_e_vinculado_a_comunidade()
    {
        var resultado = Dizimista.Criar(
            "  João   da Silva ", "123.456.789-09", "(11) 98765-4321", CapelaAtiva, Criar.Hoje, StatusDizimista.Ativo, RelogioFixo.Padrao);

        Assert.True(resultado.Sucesso);
        var dizimista = resultado.Valor;
        Assert.Equal("João da Silva", dizimista.Nome);
        Assert.Equal(2, dizimista.ComunidadeId);
        Assert.Equal("12345678909", dizimista.Cpf!.Valor);
        Assert.Equal("11987654321", dizimista.Telefone!.Valor);
    }

    [Fact]
    public void Cpf_e_telefone_sao_opcionais()
    {
        var resultado = Dizimista.Criar("Ana Lima", null, " ", CapelaAtiva, Criar.Hoje, StatusDizimista.Ativo, RelogioFixo.Padrao);

        Assert.True(resultado.Sucesso);
        Assert.Null(resultado.Valor.Cpf);
        Assert.Null(resultado.Valor.Telefone);
    }

    [Fact]
    public void Nao_pode_ser_cadastrado_em_comunidade_inativa()
    {
        var resultado = Dizimista.Criar(
            "José Santos", null, null, Criar.Comunidade(3, ativa: false), Criar.Hoje, StatusDizimista.Ativo, RelogioFixo.Padrao);

        Assert.False(resultado.Sucesso);
        Assert.Equal(nameof(Dizimista.ComunidadeId), Assert.Single(resultado.Erros).Campo);
    }

    [Fact]
    public void Todos_os_erros_sao_retornados_juntos()
    {
        var resultado = Dizimista.Criar(
            "", "000.000.000-01", "123", CapelaAtiva, Criar.Hoje.AddDays(1), (StatusDizimista)9, RelogioFixo.Padrao);

        Assert.Equal(
            [nameof(Dizimista.Nome), nameof(Dizimista.Cpf), nameof(Dizimista.Telefone), nameof(Dizimista.DataEntrada), nameof(Dizimista.Status)],
            resultado.Erros.Select(erro => erro.Campo));
    }

    [Fact]
    public void Edicao_pode_manter_a_comunidade_atual_mesmo_que_inativa()
    {
        var comunidade = Criar.Comunidade(4);
        var dizimista = Criar.Dizimista(1, comunidade);
        comunidade.Inativar(RelogioFixo.Padrao);

        var resultado = dizimista.Atualizar("Maria das Graças Souza", null, null, comunidade, Criar.Hoje, StatusDizimista.Ativo, RelogioFixo.Padrao);

        Assert.True(resultado.Sucesso);
        Assert.Equal("Maria das Graças Souza", dizimista.Nome);
        Assert.NotNull(dizimista.AtualizadoEm);
    }

    [Fact]
    public void Edicao_nao_pode_mover_para_comunidade_inativa_e_nao_altera_o_estado()
    {
        var dizimista = Criar.Dizimista(1, CapelaAtiva);

        var resultado = dizimista.Atualizar(
            "Outro Nome", null, null, Criar.Comunidade(3, ativa: false), Criar.Hoje, StatusDizimista.Ativo, RelogioFixo.Padrao);

        Assert.False(resultado.Sucesso);
        Assert.Equal("Maria das Graças", dizimista.Nome);
        Assert.Equal(CapelaAtiva.Id, dizimista.ComunidadeId);
        Assert.Null(dizimista.AtualizadoEm);
    }

    [Fact]
    public void Codigo_tem_seis_digitos()
    {
        Assert.Equal("000042", Dizimista.FormatarCodigo(42));
    }
}
