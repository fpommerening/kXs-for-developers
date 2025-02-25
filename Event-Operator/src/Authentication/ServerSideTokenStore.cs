namespace FP.ContainerTraining.EventOperator.Authentication;

using System.Collections.Concurrent;
using System.Security.Claims;
using Duende.AccessTokenManagement.OpenIdConnect;

public class ServerSideTokenStore : IUserTokenStore
{
    private static readonly ConcurrentDictionary<string, UserToken> _tokens = new();

    public Task<UserToken> GetTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null)
    {
        var sub = user.FindFirst("sub")?.Value ?? throw new InvalidOperationException("no sub claim");

        return Task.FromResult(_tokens.TryGetValue(sub, out var value) ? 
            value : 
            new UserToken { Error = "not found" });
    }
    
    public Task StoreTokenAsync(ClaimsPrincipal user, UserToken token, UserTokenRequestParameters? parameters = null)
    {
        var sub = user.FindFirst("sub")?.Value ?? throw new InvalidOperationException("no sub claim");
        _tokens[sub] = token;
        
        return Task.CompletedTask;
    }
    
    public Task ClearTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null)
    {
        var sub = user.FindFirst("sub")?.Value ?? throw new InvalidOperationException("no sub claim");
        
        _tokens.TryRemove(sub, out _);
        return Task.CompletedTask;
    }
}