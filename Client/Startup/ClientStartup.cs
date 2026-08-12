using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Oqtane.Services;
using Yosh.uwa.Services;

namespace Yosh.uwa.Startup
{
    public class ClientStartup : IClientStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            if (!services.Any(s => s.ServiceType == typeof(IVisualCrudService)))
            {
                services.AddScoped<IVisualCrudService, VisualCrudService>();
            }
            if (!services.Any(s => s.ServiceType == typeof(ICrudPageGenerator)))
            {
                services.AddScoped<ICrudPageGenerator, CrudPageGenerator>();
            }
            if (!services.Any(s => s.ServiceType == typeof(IBeerPosService)))
            {
                services.AddScoped<IBeerPosService, BeerPosService>();
            }
        }
    }
}
