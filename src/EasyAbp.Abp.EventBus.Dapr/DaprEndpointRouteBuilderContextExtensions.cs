using Dapr;

using EasyAbp.Abp.EventBus.Dapr;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;

namespace Microsoft.AspNetCore.Builder
{
    internal static class DaprEndpointRouteBuilderContextExtensions
    {
        public static void ConfigDaprServiceBus(this EndpointRouteBuilderContext endpointContext, IServiceProvider serviceProvider)
        {
            // Get services
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("StartUp");
            var serviceBus = serviceProvider.GetRequiredService<IDaprServiceBus>() as DaprServiceBus;
            var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var serializerOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();
            var serviceBusOptions = serviceProvider.GetRequiredService<IOptions<DaprServiceBusOptions>>();
            var abpDistributedEventBusOptions = serviceProvider.GetRequiredService<IOptions<AbpDistributedEventBusOptions>>();

            //handlers
            var handlers = abpDistributedEventBusOptions.Value.Handlers;

            // Map endpoints
            endpointContext.Endpoints.MapSubscribeHandler();

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
                        endpointContext.Endpoints.MapPost(attr.Name, HandleMessage)
                                    .WithTopic(attr.PubsubName, attr.Name);

                        serviceBus.Subscribe(genericArgs[0], new IocEventHandlerFactory(serviceScopeFactory, handler));
                    }

                }
            }

            async Task HandleMessage(HttpContext context)
            {
                var handlers = GetHandlersForRequest(context.Request.Path, out string topic);
                logger.LogInformation($"Request handlers count: {handlers.Count}");

                if (handlers != null)
                {
                    foreach (var handler in handlers)
                    {
                        var @event = await GetEventFromRequestAsync(context, topic, handler, serializerOptions);
                        logger.LogInformation($"Handling event: {@event}");

                        if (serviceBus.EventTypes.TryGetValue(topic, out Type eventType))
                        {
                            await serviceBus.TriggerHandlersAsync(eventType, @event);
                            // await (handler.GetHandler().EventHandler as IDistributedEventHandler<dynamic>).HandleEventAsync(@event);
                        }
                    }
                }
            }

            List<IEventHandlerFactory> GetHandlersForRequest(string path, out string topic)
            {
                topic = path.Substring(path.IndexOf("/") + 1);
                logger.LogInformation($"Topic for request: {topic}");
                if (serviceBus.Topics.TryGetValue(topic, out List<IEventHandlerFactory> handlers))
                    return handlers;
                return null;
            }

            async Task<dynamic> GetEventFromRequestAsync(HttpContext context, string topic,
                IEventHandlerFactory handler, JsonSerializerOptions serializerOptions)
            {
                if (serviceBus.EventTypes.TryGetValue(topic, out Type eventType))
                {
                    var value = await JsonSerializer.DeserializeAsync(context.Request.Body, eventType, serializerOptions);
                    return value;
                }
                return null;
            }
        }
    }
}
