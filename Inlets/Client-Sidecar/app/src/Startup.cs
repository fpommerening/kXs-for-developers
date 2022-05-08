using FP.ContainerTraining.RaspiLedMatrix.Business;
using FP.ContainerTraining.RaspiLedMatrix.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FP.ContainerTraining.RaspiLedMatrix
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<MatrixRepository>();
            services.AddSingleton<MatrixRunner>();
            services.AddHostedService<TextWriterService>();
            services.AddHostedService<GrapficService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var runner = app.ApplicationServices.GetRequiredService<MatrixRunner>();
            runner.Run();


        }
    }
}
