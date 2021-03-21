using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;

namespace EasyAbp.Abp.EventBus.Dapr
{
    public interface IDaprServiceBus : IDistributedEventBus
    {
        ConcurrentDictionary<string, List<IEventHandlerFactory>> Topics { get; }

        ConcurrentDictionary<string, Type> EventTypes { get; }

        void AddHandler(string topic, List<IEventHandlerFactory>  handlerFactory);
    }
}
