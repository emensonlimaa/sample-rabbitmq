﻿using RabbitMQ.Client;

namespace MessageBroker.RabbitMq.Abstractions;

public interface IRabbitMqClient : IDisposable
{
    IModel? GetOrCreateChannel();
}