﻿using Microsoft.Extensions.Configuration;
using Surging.Core.CPlatform;
using Surging.Core.CPlatform.EventBus;
using Surging.Core.CPlatform.EventBus.Events;
using Surging.Core.CPlatform.Module;
using Surging.Core.EventBusKafka.Configurations;
using Surging.Core.EventBusKafka.Implementation;
using System;
using System.Collections.Generic; 
using Microsoft.Extensions.DependencyInjection;
using Surging.Core.CPlatform.EventBus.Implementation;

namespace Surging.Core.EventBusKafka
{
    public class EventBusKafkaModule : EnginePartModule
    {
        public override void Initialize(CPlatformContainer serviceProvider)
        {
            base.Initialize(serviceProvider);
            serviceProvider.GetInstances<ISubscriptionAdapt>().SubscribeAt();
        }

        /// <summary>
        /// Inject dependent third-party components
        /// </summary>
        /// <param name="builder"></param>
        protected override void RegisterBuilder(ContainerBuilderWrapper builder)
        {
            base.RegisterBuilder(builder);
            UseKafkaMQTransport(builder).AddKafkaMQAdapt(builder);
        }

        public  EventBusKafkaModule UseKafkaMQTransport(ContainerBuilderWrapper builder)
        {
            var kafkaOptions = new KafkaOptions();
            var section = CPlatform.AppConfig.GetSection("EventBus_Kafka");
            if (section.Exists())
                kafkaOptions = section.Get<KafkaOptions>();
            else if (AppConfig.Configuration != null)
                kafkaOptions = AppConfig.Configuration.Get<KafkaOptions>();
            AppConfig.KafkaConsumerConfig = kafkaOptions.GetConsumerConfig();
            AppConfig.KafkaProducerConfig = kafkaOptions.GetProducerConfig();
            builder.RegisterType(typeof(Implementation.EventBusKafka)).As(typeof(IEventBus)).SingleInstance();
            builder.RegisterType(typeof(DefaultConsumeConfigurator)).As(typeof(IConsumeConfigurator)).SingleInstance();
            builder.RegisterType(typeof(InMemoryEventBusSubscriptionsManager)).As(typeof(IEventBusSubscriptionsManager)).SingleInstance();
            builder.RegisterType(typeof(KafkaProducerPersistentConnection))
           .Named(KafkaConnectionType.Producer.ToString(), typeof(IKafkaPersisterConnection)).SingleInstance();
            builder.RegisterType(typeof(KafkaConsumerPersistentConnection))
            .Named(KafkaConnectionType.Consumer.ToString(), typeof(IKafkaPersisterConnection)).SingleInstance();
            return this;
        }

        public  ContainerBuilderWrapper UseKafkaMQEventAdapt(ContainerBuilderWrapper builder, Func<IServiceProvider, ISubscriptionAdapt> adapt)
        {
            builder.RegisterAdapter(adapt);
            return builder;
        }

        public  EventBusKafkaModule AddKafkaMQAdapt(ContainerBuilderWrapper builder)
        {
              UseKafkaMQEventAdapt(builder,provider =>
             new KafkaSubscriptionAdapt(
                 provider.GetService<IConsumeConfigurator>(),
                 provider.GetService<IEnumerable<IIntegrationEventHandler>>()
                 )
            );
            return this;
        }
    }
}