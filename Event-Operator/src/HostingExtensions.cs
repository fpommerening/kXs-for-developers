using FP.ContainerTraining.EventOperator.Authentication;

namespace FP.ContainerTraining.EventOperator;

public static class HostingExtensions
{
    public static void ConfigureAuthentication(this WebApplicationBuilder builder)
    {
        var oidcSettings = builder.Configuration.GetSection("OIDC").Get<OidcSettings>();
        
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "cookie";
                options.DefaultChallengeScheme = "oidc";
                options.DefaultSignOutScheme = "oidc";
            })
            .AddCookie("cookie", options =>
            {
                options.Cookie.Name = "__Host-blazor";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                options.EventsType = typeof(CookieEvents);
            })
            .AddOpenIdConnect("oidc", options =>
            {
                options.Authority = oidcSettings!.Authority;

                options.ClientId = oidcSettings.ClientId;
                options.ClientSecret = oidcSettings.ClientSecret;
                options.ResponseType = "code";
                options.ResponseMode = "query";

                options.MapInboundClaims = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("offline_access");

                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";

                options.EventsType = typeof(OidcEvents);
            });

        builder.Services.AddOpenIdConnectAccessTokenManagement()
            .AddBlazorServerAccessTokenManagement<ServerSideTokenStore>();
        
        builder.Services.AddTransient<CookieEvents>();
        builder.Services.AddTransient<OidcEvents>();

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("IsEventPortalAdmin", policy => policy.RequireClaim("groups", oidcSettings!.AdminGroup));
    }
}