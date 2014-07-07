﻿using System;
using Microsoft.Azure.Jobs.Host.Listeners;
using Microsoft.Azure.Jobs.Host.Storage;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Microsoft.Azure.Jobs.Host.Queues.Listeners
{
    internal sealed class PollQueueCommand : IIntervalSeparationCommand
    {
        private static TimeSpan _normalSeparationInterval = TimeSpan.FromSeconds(2);
        private static int poisonThreshold = 5;

        private readonly CloudQueue _queue;
        private readonly CloudQueue _poisonQueue;
        private readonly ITriggerExecutor<CloudQueueMessage> _triggerExecutor;

        private TimeSpan _separationInterval;

        public PollQueueCommand(CloudQueue queue, CloudQueue poisonQueue, ITriggerExecutor<CloudQueueMessage> triggerExecutor)
        {
            _queue = queue;
            _poisonQueue = poisonQueue;
            _triggerExecutor = triggerExecutor;
            _separationInterval = TimeSpan.Zero; // Start polling immediately
        }

        public TimeSpan SeparationInterval
        {
            get { return _separationInterval; }
        }

        public void Execute()
        {
            // After starting up, wait two seconds after Execute returns before polling again.
            _separationInterval = _normalSeparationInterval;

            if (!_queue.Exists())
            {
                return;
            }

            // What if job takes longer. Call CloudQueue.UpdateMessage
            TimeSpan visibilityTimeout = TimeSpan.FromMinutes(10); // long enough to process the job
            CloudQueueMessage message;

            do
            {
                try
                {
                    message = _queue.GetMessage(visibilityTimeout);
                }
                catch (StorageException exception)
                {
                    if (exception.IsNotFoundQueueNotFound() ||
                        exception.IsConflictQueueBeingDeletedOrDisabled() ||
                        exception.IsServerSideError())
                    {
                        // Ignore intermittent exceptions.
                        return;
                    }
                    else
                    {
                        throw;
                    }
                }

                if (message != null)
                {
                    bool succeeded;

                    using (IntervalSeparationTimer timer = CreateUpdateMessageVisibilityTimer(_queue, message, visibilityTimeout))
                    {
                        timer.Start(executeFirst: false);

                        succeeded = _triggerExecutor.Execute(message);
                    }

                    // Need to call Delete message only if function succeeded.
                    if (succeeded)
                    {
                        DeleteMessage(message);
                    }
                    else if (_poisonQueue != null)
                    {
                        if (message.DequeueCount >= poisonThreshold)
                        {
                            Console.WriteLine("Queue poison message threshold exceeded. Moving message to queue '{0}'.",
                                _poisonQueue.Name);
                            CopyToPoisonQueue(message);
                            DeleteMessage(message);
                        }
                        else
                        {
                            ReleaseMessage(message);
                        }
                    }
                    else
                    {
                        // For queues without a corresponding poison queue, leave the message invisible when processing
                        // fails to prevent a fast infinite loop.
                        // Specifically, don't call ReleaseMessage(message)
                    }
                }
            } while (message != null);
        }

        private static IntervalSeparationTimer CreateUpdateMessageVisibilityTimer(CloudQueue queue,
            CloudQueueMessage message, TimeSpan visibilityTimeout)
        {
            // Update a message's visibility when it is halfway to expiring.
            TimeSpan normalUpdateInterval = new TimeSpan(visibilityTimeout.Ticks / 2);

            ICanFailCommand command = new UpdateQueueMessageVisibilityCommand(queue, message, visibilityTimeout);
            return LinearSpeedupTimerCommand.CreateTimer(command, normalUpdateInterval, TimeSpan.FromMinutes(1));
        }

        private void DeleteMessage(CloudQueueMessage message)
        {
            try
            {
                _queue.DeleteMessage(message);
            }
            catch (StorageException exception)
            {
                if (exception.IsBadRequestPopReceiptMismatch())
                {
                    // If someone else took over the message; let them delete it.
                    return;
                }
                else if (exception.IsNotFoundMessageOrQueueNotFound() ||
                    exception.IsConflictQueueBeingDeletedOrDisabled())
                {
                    // The message or queue is gone, or the queue is down; no need to delete the message.
                    return;
                }
                else
                {
                    throw;
                }
            }
        }

        private void ReleaseMessage(CloudQueueMessage message)
        {
            try
            {
                // We couldn't process the message. Let someone else try.
                _queue.UpdateMessage(message, TimeSpan.Zero, MessageUpdateFields.Visibility);
            }
            catch (StorageException exception)
            {
                if (exception.IsBadRequestPopReceiptMismatch())
                {
                    // Someone else already took over the message; no need to do anything.
                    return;
                }
                else if (exception.IsNotFoundMessageOrQueueNotFound() ||
                    exception.IsConflictQueueBeingDeletedOrDisabled())
                {
                    // The message or queue is gone, or the queue is down; no need to release the message.
                    return;
                }
                else
                {
                    throw;
                }
            }
        }

        private void CopyToPoisonQueue(CloudQueueMessage message)
        {
            _poisonQueue.AddMessageAndCreateIfNotExists(message);
        }
    }
}
