using Microsoft.JSInterop;
namespace CompVault.Frontend.Common.Extensions;

/// <summary>
/// Extension metode for å hente og legge til localstorage
/// </summary>
public static class LocalStorageExtensions
{
    /// <summary>
    /// Lagrer en verdi lokalt i local storage
    /// </summary>
    public static async Task<bool> SetItemAsync(this IJSRuntime js, string key, string value)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", key, value);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }




    /// <summary>
    /// Henter en verdi fra local storage med nøkkelen
    /// </summary>
    public static async Task<string?> GetItemAsync(this IJSRuntime js, string key)
    {
        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch (Exception)
        {
            return null;
        }
    }
}