namespace NuvTools.Payment.Sicoob.ApiClient;

/// <summary>
/// Resultado generico de chamadas a API Sicoob.
/// </summary>
public class SicoobApiResult<T>
{
    public bool Succeeded { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static SicoobApiResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static SicoobApiResult<T> Fail(string errorMessage) => new() { Succeeded = false, ErrorMessage = errorMessage };
}
