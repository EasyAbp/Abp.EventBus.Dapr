﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.AspNetCore;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace EasyAbp.Abp.EventBus.Dapr
{
    [DependsOn(
        typeof(AbpEventBusModule),
        typeof(AbpAspNetCoreModule)
        )]
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
