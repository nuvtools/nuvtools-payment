namespace NuvTools.Payment.BancoDoBrasil.ApiClient;

/// <summary>
/// Resultado generico de chamadas a API Banco do Brasil.
/// </summary>
public class BbApiResult<T>
{
    public bool Succeeded { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static BbApiResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static BbApiResult<T> Fail(string errorMessage) => new() { Succeeded = false, ErrorMessage = errorMessage };
}
