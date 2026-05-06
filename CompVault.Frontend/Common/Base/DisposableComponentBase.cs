using Microsoft.AspNetCore.Components;
namespace CompVault.Frontend.Common.Base;

/// <summary>
/// Gir sider og komponenter mulighet til å arve CancellationToken og Dipose som går igjen i mange komponenenter
/// </summary>
public abstract class DisposableComponentBase : ComponentBase, IDisposable
{
    protected readonly CancellationTokenSource Cts = new();

    public virtual void Dispose()
    {
        Cts.Cancel();
        Cts.Dispose();
    }
}