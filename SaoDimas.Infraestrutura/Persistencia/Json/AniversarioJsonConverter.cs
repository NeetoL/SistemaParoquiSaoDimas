using System.Text.Json;
using System.Text.Json.Serialization;
using SaoDimas.Dominio.ValueObjects;
namespace SaoDimas.Infraestrutura.Persistencia.Json;
internal sealed class AniversarioJsonConverter : JsonConverter<Aniversario>
{
    public override Aniversario Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && Aniversario.TryParse(reader.GetString(), out var aniversario)
            ? aniversario : throw new JsonException("Aniversário inválido: use dia/mês.");
    public override void Write(Utf8JsonWriter writer, Aniversario value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}
