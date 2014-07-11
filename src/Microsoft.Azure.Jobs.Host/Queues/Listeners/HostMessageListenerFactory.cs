﻿using System;
using Microsoft.Azure.Jobs.Host.Bindings;
using Microsoft.Azure.Jobs.Host.Executors;
using Microsoft.Azure.Jobs.Host.Indexers;
using Microsoft.Azure.Jobs.Host.Listeners;
using Microsoft.Azure.Jobs.Host.Loggers;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Microsoft.Azure.Jobs.Host.Queues.Listeners
{
    internal static class HostMessageListener
    {
        private static readonly TimeSpan maxmimum = TimeSpan.FromMinutes(1);

        public static IListener Create(CloudQueue queue, IExecuteFunction executor, IFunctionIndexLookup functionLookup,
            IFunctionInstanceLogger functionInstanceLogger, HostBindingContext context)
        {
            ITriggerExecutor<CloudQueueMessage> triggerExecutor = new HostMessageExecutor(executor, functionLookup,
                functionInstanceLogger, context);
            ICanFailCommand command = new PollQueueCommand(queue, poisonQueue: null, triggerExecutor: triggerExecutor);
            // Use a shorter maximum polling interval for run/abort from dashboard.
            IntervalSeparationTimer timer = ExponentialBackoffTimerCommand.CreateTimer(command,
                QueuePollingIntervals.Minimum, maxmimum);
            return new TimerListener(timer);
        }
    }
}
