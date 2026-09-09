using System.Security.Cryptography;
using System.Text;

namespace MegaDescontao.Api.Marketplaces.Providers.Shopee;

/// Assinatura das chamadas à Affiliate Open API da Shopee.
///
/// Duas armadilhas conhecidas, e é por isso que isso vive isolado aqui:
/// 1. O segredo é CONCATENADO no final da string, não é chave de HMAC.
/// 2. O payload assinado tem que ser exatamente a mesma string JSON que vai no corpo,
///    byte a byte. Serializar duas vezes (uma para assinar, outra para enviar) produz
///    assinatura inválida se a ordem das propriedades ou o escape mudar.
public static class ShopeeSignature
{
    public static string Compute(string appId, long timestamp, string payload, string secret)
    {
        var raw = $"{appId}{timestamp}{payload}{secret}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string BuildAuthorizationHeader(string appId, long timestamp, string signature) =>
        $"SHA256 Credential={appId}, Timestamp={timestamp}, Signature={signature}";
}
