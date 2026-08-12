using Microsoft.AspNetCore.Builder; 
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Oqtane.Infrastructure;
using Yosh.uwa.Repository;
using Yosh.uwa.Services;

namespace Yosh.uwa.Startup
{
    public class ServerStartup : IServerStartup
    {
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // not implemented
        }

        public void ConfigureMvc(IMvcBuilder mvcBuilder)
        {
            // not implemented
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IServerVisualCrudService, ServerVisualCrudService>();
            services.AddTransient<IDatabaseSchemaProvider, SqlServerSchemaProvider>();
            services.AddTransient<IDynamicCrudService, DynamicCrudService>();
            services.AddTransient<IServerBeerPosService, ServerBeerPosService>();
            services.AddDbContextFactory<Context>(opt => { }, ServiceLifetime.Transient);
        }
    }
}
