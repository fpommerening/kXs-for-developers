namespace FP.ContainerTraining.EventOperator.Controllers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[AllowAnonymous]
public class AccountController : ControllerBase
{
    public IActionResult LogIn(string? returnUrl)
    {
        string redirectUri = "/";

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                redirectUri = returnUrl;
            }
        }

        var props = new AuthenticationProperties
        {
            RedirectUri = redirectUri
        };
        
        return Challenge(props);
    }
    
    public IActionResult LogOut()
    {
        return SignOut("cookie", "oidc");
    }
}