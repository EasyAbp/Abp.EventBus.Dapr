# Abp.EventBus.Dapr

[![ABP version](https://img.shields.io/badge/dynamic/xml?style=flat-square&color=yellow&label=abp&query=%2F%2FProject%2FPropertyGroup%2FAbpVersion&url=https%3A%2F%2Fraw.githubusercontent.com%2FEasyAbp%2FAbp.EventBus.CAP%2Fmaster%2FDirectory.Build.props)](https://abp.io)
[![Discord online](https://badgen.net/discord/online-members/S6QaezrCRq?label=Discord)](https://discord.gg/S6QaezrCRq)
[![GitHub stars](https://img.shields.io/github/stars/EasyAbp/Abp.EventBus.Dapr?style=social)](https://www.github.com/EasyAbp/Abp.EventBus.Dapr)

ABP vNext framework Dapr EventBus module that integrated the [Dapr](https://github.com/dapr/dapr/) with the [ABP](https://github.com/abpframework/abp) framework.

## Installation

1. Install the following NuGet packages. ([see how](https://github.com/EasyAbp/EasyAbpGuide/blob/master/docs/How-To.md#add-nuget-packages))

    * EasyAbp.Abp.EventBus.Dapr 

1. Add `DependsOn(typeof(AbpDaprEventBusModule))` attribute to configure the module dependencies. ([see how](https://github.com/EasyAbp/EasyAbpGuide/blob/master/docs/How-To.md#add-module-dependencies))

1. Configure the Dapr default pubsub name.
    ```csharp
     public override void ConfigureServices(ServiceConfigurationContext context)
     {
            var configuration = context.Services.GetConfiguration();
            context.Services.Configure<DaprServiceBusOptions>(options =>
            {
                options.PubSubName = "pubsub";
            });
     }

    ```

## Usage

See the [ABP distributed event bus document](https://docs.abp.io/en/abp/latest/Distributed-Event-Bus).

## How Do We Integrate Dapr?

After ABP 5.0 released, the distributed event bus was redesigned. See: https://github.com/abpframework/abp/issues/6126

```c#
// ABP 5.0
Task PublishAsync<TEvent>(TEvent eventData, bool onUnitOfWorkComplete = true, bool useOutbox = true);

// ABP 4.0
Task PublishAsync<TEvent>(TEvent eventData);
```

Before ABP 5.0, when you invoke PublishAsync, the bus will push the event to MQ at once.

As you can see, after ABP 5.0, events are sent using outbox on UOW complete by default. CAP has a built-in transactional outbox, so we can implement it easily.