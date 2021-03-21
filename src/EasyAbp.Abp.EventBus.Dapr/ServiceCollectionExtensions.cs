using EasyAbp.Abp.EventBus.Dapr;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add an IServiceBus registration for the given type.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to register with.</param>
        /// <param name="pubSubName">The name of the pubsub component to use.</param>
        /// <returns>The original <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</returns>
        public static IServiceCollection AddDaprServiceBus(
            this IServiceCollection services)
        {
            services.AddControllers()
                .AddDapr();
            services.AddSingleton(new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            });
            services.AddSingleton<IDaprServiceBus, DaprServiceBus>();
            return services;
        }
    }
}
