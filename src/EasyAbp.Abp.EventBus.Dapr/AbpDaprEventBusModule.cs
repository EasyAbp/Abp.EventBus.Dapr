using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace EasyAbp.Abp.EventBus.Dapr
{
    [DependsOn(typeof(AbpEventBusModule))]
    public class AbpDaprEventBusModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //context.Services.Configure<DaprServiceBusOptions>(options =>
            //{
            //    options.PubSubName = "pubsub";
            //});

            // Add Dapr service bus
            context.Services.AddDaprServiceBus();
        }
    }
}
