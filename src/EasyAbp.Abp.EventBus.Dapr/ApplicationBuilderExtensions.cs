using Dapr;
using EasyAbp.Abp.EventBus.Dapr;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDaprServiceBus(this IApplicationBuilder app, 
            Action<IDaprServiceBus> configure = null)
        {
            // app.UseRouting();
            // Get services
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("StartUp");
            var serviceBus = app.ApplicationServices.GetRequiredService<IDaprServiceBus> () as DaprServiceBus;
            var serviceScopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            var serializerOptions = app.ApplicationServices.GetRequiredService<JsonSerializerOptions>();
            var serviceBusOptions = app.ApplicationServices.GetRequiredService<IOptions<DaprServiceBusOptions>>();
            var abpDistributedEventBusOptions = app.ApplicationServices.GetRequiredService<IOptions<AbpDistributedEventBusOptions>>();
       
            configure?.Invoke(serviceBus);

            //handlers
            var handlers = abpDistributedEventBusOptions.Value.Handlers;

            // Map endpoints
            app.UseCloudEvents();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscribeHandler();

                foreach (var handler in handlers)
                {
                    var interfaces = handler.GetInterfaces();
                    
                    foreach (var @interface in interfaces)
                    {
                        if (!typeof(IEventHandler).GetTypeInfo().IsAssignableFrom(@interface))
                        {
                            continue;
                        }
                        var genericArgs = @interface.GetGenericArguments();

                        if (genericArgs.Length != 1)
                        {
                            continue;
                        }

                        var typeInfo = handler;
                        var eventType = genericArgs[0];

                        var serviceTypeInfo = typeof(IDistributedEventHandler<>).MakeGenericType(genericArgs[0]);
                        var method = typeInfo
                            .GetMethod(
                                nameof(IDistributedEventHandler<object>.HandleEventAsync),
                                new[] { eventType }
                            );
                        var eventName = EventNameAttribute.GetNameOrDefault(eventType);
                        var topicAttr = method.GetCustomAttributes<TopicAttribute>(true);
                        var topicAttributes = topicAttr.ToList();

                        if (topicAttributes.Count == 0)
                        {                           
                            topicAttributes.Add(new TopicAttribute(serviceBusOptions.Value.PubSubName, eventName));
                        }

                        foreach (var attr in topicAttributes)
                        {
                            logger.LogInformation($"pubsubname: {attr.PubsubName}{ attr.Name}");
                            endpoints.MapPost(attr.Name, HandleMessage)
                                        .WithTopic(attr.PubsubName, attr.Name);

                            serviceBus.Subscribe(genericArgs[0], new IocEventHandlerFactory(serviceScopeFactory, handler));
                        }                        

                    }
                }               
            });
      

           async Task HandleMessage(HttpContext context)
            {
                var handlers = GetHandlersForRequest(context.Request.Path,out string topic);
                logger.LogInformation($"Request handlers count: {handlers.Count}");

                if (handlers != null)
                {
                    foreach (var handler in handlers)
                    {
                        var @event = await GetEventFromRequestAsync(context, topic, handler ,serializerOptions);
                        logger.LogInformation($"Handling event: {@event}");
                        
                        if (serviceBus.EventTypes.TryGetValue(topic, out Type eventType))
                        {
                            serviceBus.TriggerHandlersAsync(eventType, @event);
                           // await (handler.GetHandler().EventHandler as IDistributedEventHandler<dynamic>).HandleEventAsync(@event);
                        }
                    }
                }
            }

            List<IEventHandlerFactory> GetHandlersForRequest(string path,out string topic)
            {
                topic = path.Substring(path.IndexOf("/") + 1);
                logger.LogInformation($"Topic for request: {topic}");
                if (serviceBus.Topics.TryGetValue(topic, out List<IEventHandlerFactory> handlers))
                    return handlers;
                return null;
            }

            async Task<dynamic> GetEventFromRequestAsync(HttpContext context,string topic,
                IEventHandlerFactory handler, JsonSerializerOptions serializerOptions)
            {
                if (serviceBus.EventTypes.TryGetValue(topic, out Type eventType))
                {
                    var value = await JsonSerializer.DeserializeAsync(context.Request.Body, eventType, serializerOptions);
                    return value;
                }
                return null;
            }

            return app;
        }
    }
}
