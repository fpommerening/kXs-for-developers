using FP.ContainerTraining.Hpa.Manager.Business;
using FP.ContainerTraining.Hpa.Manager.Services;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using MudBlazor.Services;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddGrpc();
builder.Services.AddOpenTelemetryMetrics(otelBuilder =>
{
    otelBuilder.AddMeter(WorkerMetrics.Metrics.Name);
    otelBuilder.AddPrometheusExporter();
});
builder.Services.AddSingleton<IWorkerRepository,WorkerRepository>();
builder.Services.AddSingleton<IJobRepository,JobRepository>();
builder.Services.AddHostedService<HeartbeatService>();
builder.Services.AddHostedService<JobService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.MapGrpcService<WorkerService>();


app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();