using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AspNetCore;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;

namespace EasyAbp.Abp.EventBus.Dapr
{
    [DependsOn(
        typeof(AbpEventBusModule),
        typeof(AbpAspNetCoreModule),
        typeof(AbpUnitOfWorkModule)
        )]
    public class AbpDaprEventBusModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

            // Add Dapr service bus
            context.Services.AddDaprServiceBus();

            Configure<AbpEndpointRouterOptions>(options =>
            {
                options.EndpointConfigureActions.Add(endpointContext =>
                {
                    //endpointContext.ScopeServiceProvider will be dispose
                    endpointContext.ConfigDaprServiceBus(context.Services.GetServiceProviderOrNull());
                });
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            app.UseCloudEvents();
        }
    }
}
