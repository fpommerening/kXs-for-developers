namespace FP.ContainerTraining.EventOperator.Authentication;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;


public class CookieEvents(IUserTokenStore store) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var token = await store.GetTokenAsync(context.Principal!).ConfigureAwait(false);
        if (token.IsError)
        {
            context.RejectPrincipal();
        }

        await base.ValidatePrincipal(context).ConfigureAwait(false);
    }

    public override async Task SigningOut(CookieSigningOutContext context)
    {
        await context.HttpContext.RevokeRefreshTokenAsync().ConfigureAwait(false);
        
        await base.SigningOut(context).ConfigureAwait(false);
    }
}