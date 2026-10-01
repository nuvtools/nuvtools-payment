using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

/// <summary>
/// Resposta de <c>GET /proximos-numeros-requisicao</c> — números de requisição disponíveis para uso.
/// </summary>
public class NextRequisitionNumbersResponse
{
    [JsonPropertyName("listaProximosNumeros")]
    public List<long> ListaProximosNumeros { get; set; } = [];
}
