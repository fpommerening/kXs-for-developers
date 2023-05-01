using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace FP.ContainerTraining.EventOperator.Authentication;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider 
{
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;
    private readonly ProtectedSessionStorage _sessionStorage;
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private const string UserSessionStorageKey = "UserSession";

    public CustomAuthenticationStateProvider(ILogger<CustomAuthenticationStateProvider> logger,
        ProtectedSessionStorage sessionStorage)
    {
        _logger = logger;
        _sessionStorage = sessionStorage;
    }
    
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        ClaimsPrincipal claimsPrincipal = _anonymous;

        try
        {
            var userSessionStorageResult = await _sessionStorage.GetAsync<UserSession>(UserSessionStorageKey);
            if (!userSessionStorageResult.Success)
            {
                _logger.LogDebug("User session not exists");
            }
            else
            {
                claimsPrincipal = CreateClaimsPrincipalFromUserSession(userSessionStorageResult.Value);
            }
        }
        catch (InvalidOperationException ioe)
        {
            _logger.LogDebug(ioe, "Error on decode user session");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error on decode user session");
        }

        return new AuthenticationState(claimsPrincipal);
    }

    public async Task UpdateAuthenticationState(UserSession? userSession)
    {
        ClaimsPrincipal claimsPrincipal;
        if (userSession == null)
        {
            await _sessionStorage.DeleteAsync(UserSessionStorageKey);
            claimsPrincipal = _anonymous;
        }
        else
        {
            claimsPrincipal = CreateClaimsPrincipalFromUserSession(userSession);
            await _sessionStorage.SetAsync(UserSessionStorageKey, userSession);
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }

    private ClaimsPrincipal CreateClaimsPrincipalFromUserSession(UserSession? userSession)
    {
        if (userSession == null
            || string.IsNullOrEmpty(userSession.Name)
            || string.IsNullOrEmpty(userSession.Role))
        {
            _logger.LogWarning("User session not contains valid data");
            return _anonymous;
        }
        var claimsIdentity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, userSession.Name),
            new Claim(ClaimTypes.Role, userSession.Role)
        }, "CustomAuth");
        return new ClaimsPrincipal(claimsIdentity);
    }
}