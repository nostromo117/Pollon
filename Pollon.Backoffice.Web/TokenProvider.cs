using Microsoft.AspNetCore.Components;
using System.Runtime.CompilerServices;

namespace Pollon.Backoffice.Web;

/// <summary>
/// Managed Authentication Token storage and recovery across SSR and SignalR interactive sessions.
/// </summary>
public class TokenProvider : IDisposable
{
    private static readonly ConditionalWeakTable<PersistentComponentState, TokenCache> _globalCache = new();
    
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public TokenProvider(PersistentComponentState state)
    {
        // Use a shared cache linked to the state object instance (which is Scoped per Circuit)
        var cache = _globalCache.GetOrCreateValue(state);

        // 1. Try to recover from JSON state (destructive - only works once per circuit)
        if (_stateHasToken(state, "Pollon_AccessToken", out var token))
        {
            cache.AccessToken = token;
        }
        if (_stateHasToken(state, "Pollon_RefreshToken", out var refreshToken))
        {
            cache.RefreshToken = refreshToken;
        }

        // 2. Load from cache (survives multiple TokenProvider instances in the same circuit)
        AccessToken = cache.AccessToken;
        RefreshToken = cache.RefreshToken;
        
        Console.WriteLine($"[AUTH-DEBUG] TokenProvider[{this.GetHashCode()}]: Initialized with State[{state.GetHashCode()}]. Access: {!string.IsNullOrEmpty(AccessToken)} (Len: {AccessToken?.Length}), Refresh: {!string.IsNullOrEmpty(RefreshToken)} (Len: {RefreshToken?.Length})");
    }

    private bool _stateHasToken(PersistentComponentState state, string key, out string? token)
    {
        return state.TryTakeFromJson<string>(key, out token);
    }

    public void Dispose() { }

    private class TokenCache
    {
        public string? AccessToken;
        public string? RefreshToken;
    }
}
