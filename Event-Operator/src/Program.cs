using FP.ContainerTraining.EventOperator;
using FP.ContainerTraining.EventOperator.Business;
using FP.ContainerTraining.EventOperator.CustomResources;
using FP.ContainerTraining.EventOperator.Services;
using k8s;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

builder.ConfigureAuthentication();
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

builder.Services.AddSingleton<IKubernetes, Kubernetes>(_ =>
{
    var config = KubernetesClientConfiguration.IsInCluster()
        ? KubernetesClientConfiguration.InClusterConfig()
        : KubernetesClientConfiguration.BuildConfigFromConfigFile();
    return new Kubernetes(config);
});

builder.Services.AddCustomResources();
builder.Services.AddCustomResourceHandler();
builder.Services.AddCustomResourceWatcher(builder.Configuration);
builder.Services.AddHostedService<EventPortalService>();

builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddSingleton<SnippetsRepository>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();