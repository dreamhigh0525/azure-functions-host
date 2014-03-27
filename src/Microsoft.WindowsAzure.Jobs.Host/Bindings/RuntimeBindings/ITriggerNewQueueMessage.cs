﻿using Microsoft.WindowsAzure.Storage.Queue;

namespace Microsoft.WindowsAzure.Jobs
{
    // extension interface for when the input is triggered bya new queue message.
    internal interface ITriggerNewQueueMessage : IRuntimeBindingInputs
    {
        // If null, then ignore. 
        CloudQueueMessage QueueMessageInput { get; }
    }
}
