using Dapr;
using Dapr.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.MultiTenancy;

namespace EasyAbp.Abp.EventBus.Dapr
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IDistributedEventBus),typeof(IDaprServiceBus), typeof(DaprServiceBus))]
    public class DaprServiceBus : EventBusBase, IDaprServiceBus, ISingletonDependency
    {
        private readonly DaprClient _dapr;
        private readonly IOptions<DaprServiceBusOptions> _options;
        protected AbpDistributedEventBusOptions AbpDistributedEventBusOptions { get; }

        public ConcurrentDictionary<string, Type> EventTypes { get; }

        public ConcurrentDictionary<Type, List<IEventHandlerFactory>> HandlerFactories { get; }

        public ConcurrentDictionary<string, List<IEventHandlerFactory>> Topics { get; }

        public DaprServiceBus(IServiceScopeFactory serviceScopeFactory,
           IOptions<AbpDistributedEventBusOptions> distributedEventBusOptions,
           IOptions<DaprServiceBusOptions> options,
           DaprClient dapr,
           ICurrentTenant currentTenant)
           : base(serviceScopeFactory, currentTenant)
        {
            _dapr = dapr;
            _options = options;
            AbpDistributedEventBusOptions = distributedEventBusOptions.Value;
            HandlerFactories = new ConcurrentDictionary<Type, List<IEventHandlerFactory>>();
            EventTypes = new ConcurrentDictionary<string, Type>();
            Topics = new ConcurrentDictionary<string, List<IEventHandlerFactory>>();
        }


        public override async Task PublishAsync(Type eventType, object eventData)
        {
            if (eventData is null) 
                throw new ArgumentNullException(nameof(eventData));
            var topic = EventNameAttribute.GetNameOrDefault(eventType);  

            // We need to make sure that we pass the concrete type to PublishEventAsync,
            // which can be accomplished by casting the event to dynamic. This ensures
            // that all event fields are properly serialized.
            await _dapr.PublishEventAsync(_options.Value.PubSubName, topic, (dynamic)eventData);
        }


        public override IDisposable Subscribe(Type eventType, IEventHandlerFactory factory)
        {
            var handlerFactories = GetOrCreateHandlerFactories(eventType);

            if (factory.IsInFactories(handlerFactories))
            {
                return NullDisposable.Instance;
            }

            handlerFactories.Add(factory);

            if (handlerFactories.Count == 1) //TODO: Multi-threading!
            {
                var topic = EventNameAttribute.GetNameOrDefault(eventType);
                AddHandler(topic, handlerFactories);
            }

            return new EventHandlerFactoryUnregistrar(this, eventType, factory);
           
        }

        private List<IEventHandlerFactory> GetOrCreateHandlerFactories(Type eventType)
        {
            return HandlerFactories.GetOrAdd(
                eventType,
                type =>
                {
                    var eventName = EventNameAttribute.GetNameOrDefault(type);
                    EventTypes[eventName] = type;
                    return new List<IEventHandlerFactory>();
                }
            );
        }

        public IDisposable Subscribe<TEvent>(IDistributedEventHandler<TEvent> handler) where TEvent : class
        {
            return Subscribe(typeof(TEvent), handler);
        }

        public override void Unsubscribe<TEvent>(Func<TEvent, Task> action)
        {
            throw new NotImplementedException();
        }

        public override void Unsubscribe(Type eventType, IEventHandler handler)
        {
            throw new NotImplementedException();
        }

        public override void Unsubscribe(Type eventType, IEventHandlerFactory factory)
        {
            throw new NotImplementedException();
        }

        public override void UnsubscribeAll(Type eventType)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<EventTypeWithEventHandlerFactories> GetHandlerFactories(Type eventType)
        {
            var handlerFactoryList = new List<EventTypeWithEventHandlerFactories>();

            foreach (var handlerFactory in HandlerFactories.Where(hf => ShouldTriggerEventForHandler(eventType, hf.Key)))
            {
                handlerFactoryList.Add(new EventTypeWithEventHandlerFactories(handlerFactory.Key, handlerFactory.Value));
                var topic = EventNameAttribute.GetNameOrDefault(handlerFactory.Key);
                AddHandler(topic, handlerFactory.Value);
            }

            return handlerFactoryList.ToArray();
        }

        private static bool ShouldTriggerEventForHandler(Type targetEventType, Type handlerEventType)
        {
            //Should trigger same type
            if (handlerEventType == targetEventType)
            {
                return true;
            }

            //TODO: Support inheritance? But it does not support on subscription to RabbitMq!
            //Should trigger for inherited types
            if (handlerEventType.IsAssignableFrom(targetEventType))
            {
                return true;
            }

            return false;
        }

        public void AddHandler(string topic, List<IEventHandlerFactory> handlerFactory)
        {
            if (!Topics.ContainsKey(topic))
            {
                Topics.TryAdd(topic, handlerFactory);
            }
            else if (Topics[topic] != handlerFactory)
            {
                Topics[topic].AddRange(handlerFactory);
            }
        }
    }
}
