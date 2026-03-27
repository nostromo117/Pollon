using Microsoft.AspNetCore.Components;

namespace Pollon.Backoffice.Web;

public class TokenProvider : IDisposable
{
    private readonly PersistentComponentState _state;
    public string? AccessToken { get; set; }

    public TokenProvider(PersistentComponentState state)
    {
        _state = state;
        Console.WriteLine("[AUTH-DEBUG] TokenProvider: Service instance created.");
    }

    public void Dispose() { }
}
