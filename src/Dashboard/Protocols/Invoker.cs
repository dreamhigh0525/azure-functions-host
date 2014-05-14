﻿using System;
using System.Diagnostics;
using Microsoft.Azure.Jobs;
using Microsoft.Azure.Jobs.Host.Storage.Queue;
using Microsoft.Azure.Jobs.Protocols;

namespace Dashboard.Protocols
{
    internal class Invoker : IInvoker
    {
        private readonly ICloudQueueClient _client;

        public Invoker(ICloudQueueClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            _client = client;
        }

        public void TriggerAndOverride(Guid hostId, TriggerAndOverrideMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            ICloudQueue queue = _client.GetQueueReference(QueueNames.GetHostQueueName(hostId));
            Debug.Assert(queue != null);
            queue.CreateIfNotExists();
            string content = JsonCustom.SerializeObject(message);
            Debug.Assert(content != null);
            ICloudQueueMessage queueMessage = queue.CreateMessage(content);
            queue.AddMessage(queueMessage);
        }
    }
}
