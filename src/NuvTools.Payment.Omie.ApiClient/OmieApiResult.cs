namespace NuvTools.Payment.Omie.ApiClient;

/// <summary>
/// Generic result wrapper for Omie API calls.
/// </summary>
public class OmieApiResult<T>
{
    public bool Succeeded { get; set; }
    public T? Data { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public static OmieApiResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static OmieApiResult<T> Fail(string errorMessage) => new() { Succeeded = false, ErrorMessage = errorMessage };
}
