using Microsoft.AspNetCore.Components;

namespace Pollon.Backoffice.Web;

public class TokenProvider : IDisposable
{
    private readonly PersistentComponentState _state;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public TokenProvider(PersistentComponentState state)
    {
        _state = state;
        if (_state.TryTakeFromJson<string>("Pollon_AccessToken", out var token))
        {
            AccessToken = token;
        }

        if (_state.TryTakeFromJson<string>("Pollon_RefreshToken", out var refreshToken))
        {
            RefreshToken = refreshToken;
        }
        
        Console.WriteLine($"[AUTH-DEBUG] TokenProvider initialized. AccessToken: {!string.IsNullOrEmpty(AccessToken)}, RefreshToken: {!string.IsNullOrEmpty(RefreshToken)}");
    }

    public void Dispose() { }
}
