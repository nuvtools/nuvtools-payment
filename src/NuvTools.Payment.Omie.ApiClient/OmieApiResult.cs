namespace NuvTools.Payment.Omie.ApiClient;

/// <summary>
/// Resultado generico de chamadas a API Omie.
/// </summary>
public class OmieApiResult<T>
{
    public bool Succeeded { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static OmieApiResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static OmieApiResult<T> Fail(string errorMessage) => new() { Succeeded = false, ErrorMessage = errorMessage };
}
