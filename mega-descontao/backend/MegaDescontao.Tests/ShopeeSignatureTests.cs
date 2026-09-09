using MegaDescontao.Api.Marketplaces.Providers.Shopee;

namespace MegaDescontao.Tests;

public class ShopeeSignatureTests
{
    private const string AppId = "test-app";
    private const string Secret = "s3cr3t";
    private const long Timestamp = 1700000000;
    private const string Payload = """{"query":"x"}""";

    [Fact]
    public void Compute_bate_com_o_sha256_de_appId_timestamp_payload_secret()
    {
        // Vetor conferido fora do C#: sha256("test-app1700000000{\"query\":\"x\"}s3cr3t").
        // Se alguém trocar a concatenação por HMAC, ou mudar a ordem, este teste quebra.
        const string esperado = "9c8b9b732496c5a5ae43bb44308983b652366baf29264f2cde44298cb55eff13";

        var assinatura = ShopeeSignature.Compute(AppId, Timestamp, Payload, Secret);

        Assert.Equal(esperado, assinatura);
    }

    [Fact]
    public void Compute_sai_em_hexadecimal_minusculo_de_64_caracteres()
    {
        var assinatura = ShopeeSignature.Compute(AppId, Timestamp, Payload, Secret);

        Assert.Equal(64, assinatura.Length);
        Assert.Equal(assinatura.ToLowerInvariant(), assinatura);
        Assert.All(assinatura, c => Assert.True(char.IsAsciiHexDigitLower(c)));
    }

    [Fact]
    public void Payload_diferente_gera_assinatura_diferente()
    {
        // O corpo enviado precisa ser exatamente o corpo assinado. Um byte a mais
        // (espaço, ordem de propriedade, escape) já invalida a chamada.
        var original = ShopeeSignature.Compute(AppId, Timestamp, Payload, Secret);
        var comEspaco = ShopeeSignature.Compute(AppId, Timestamp, """{"query": "x"}""", Secret);

        Assert.NotEqual(original, comEspaco);
    }

    [Fact]
    public void Timestamp_diferente_gera_assinatura_diferente()
    {
        var agora = ShopeeSignature.Compute(AppId, Timestamp, Payload, Secret);
        var depois = ShopeeSignature.Compute(AppId, Timestamp + 1, Payload, Secret);

        Assert.NotEqual(agora, depois);
    }

    [Fact]
    public void Cabecalho_sai_no_formato_que_a_shopee_espera()
    {
        var cabecalho = ShopeeSignature.BuildAuthorizationHeader(AppId, Timestamp, "abc123");

        Assert.Equal("SHA256 Credential=test-app, Timestamp=1700000000, Signature=abc123", cabecalho);
    }
}
