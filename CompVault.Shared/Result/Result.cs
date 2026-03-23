namespace CompVault.Shared.Result;

/// <summary>
/// Wrapper for resultatet av en service-operasjon. Inneholder enten en verdi (suksess)
/// eller en feil — aldri begge deler. Gjør det enkelt å håndtere feil uten exceptions.
/// </summary>
/// <typeparam name="T">Typen på verdien ved suksess.</typeparam>
public sealed class Result<T>
{
    /// <summary>True hvis alt gikk bra.</summary>
    public bool IsSuccess { get; }

    /// <summary>True hvis noe gikk galt.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Verdien ved suksess. Bare gyldig når <see cref="IsSuccess"/> er true.</summary>
    public T? Value { get; }

    /// <summary>Feilen ved feil. Bare gyldig når <see cref="IsFailure"/> er true.</summary>
    public AppError? Error { get; }

    private Result(bool isSuccess, T? value, AppError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>Lager et vellykket resultat med den gitte verdien.</summary>
    /// <param name="value">Verdien som returneres.</param>
    public static Result<T> Success(T value) =>
        new(true, value, null);

    /// <summary>Lager et mislykket resultat med en feilbeskrivelse.</summary>
    /// <param name="error">Feilen som oppstod.</param>
    public static Result<T> Failure(AppError error) =>
        new(false, default, error);
}

/// <summary>
/// Representerer resultatet av en operasjon uten returverdi. Brukes der en metode
/// enten lykkes eller feiler, istedenfor å kaste en exception. Ved feil inneholder objektet
/// en <see cref="AppError"/> som beskriver hva som gikk galt.
/// </summary>
public sealed class Result
{
    /// <summary>True hvis alt gikk bra.</summary>
    public bool IsSuccess { get; }

    /// <summary>True hvis noe gikk galt.</summary>
    public bool IsFailure => !IsSuccess;


    /// <summary>En AppError-objekt ved feil. Bare gyldig når <see cref="IsFailure"/> er true.</summary>
    public AppError? Error { get; }

    private Result(bool isSuccess, AppError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Lager et vellykket resultat med den gitte verdien.</summary>
    public static Result Success() => new(true, null);

    /// <summary>Lager et mislykket resultat med en feilbeskrivelse.</summary>
    /// <param name="error">Feilen som oppstod.</param>
    public static Result Failure(AppError error) => new(false, error);
}
