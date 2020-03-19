using Microsoft.Extensions.DependencyInjection;

namespace RdJNL.TextTemplatingCore.TestProject
{
    public sealed class Test
    {
        public void Run(IServiceCollection services)
        {
            services.AddScoped<IHello>();
        }
    }
}
