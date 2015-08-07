﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Azure.WebJobs.ServiceBus
{
    /// <summary>
    /// This class defines a strategy used for processing ServiceBus messages.
    /// </summary>
    /// <remarks>
    /// Custom <see cref="MessageProcessor"/> implementations can be registered by implementing
    /// a custom <see cref="IMessageProcessorFactory"/> and setting it on the <see cref="ServiceBusConfiguration"/>.
    /// </remarks>
    public class MessageProcessor
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="context">The <see cref="MessageProcessorFactoryContext"/> to use.</param>
        public MessageProcessor(MessageProcessorFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            MessageOptions = context.MessageOptions;
        }

        /// <summary>
        /// Gets the <see cref="OnMessageOptions"/> that will be used by the message receiver.
        /// </summary>
        public OnMessageOptions MessageOptions { get; protected set; }

        /// <summary>
        /// This method is called when there is a new message to process, before the job function is invoked.
        /// This allows any preprocessing to take place on the message before processing begins.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
        /// <returns>True if the message processing should continue, false otherwise.</returns>
        public virtual async Task<bool> BeginProcessingMessageAsync(BrokeredMessage message, CancellationToken cancellationToken)
        {
            return await Task.FromResult<bool>(true);
        }

        /// <summary>
        /// This method completes processing of the specified message, after the job function has been invoked.
        /// </summary>
        /// <param name="message">The message to complete processing for.</param>
        /// <param name="result">The <see cref="FunctionResult"/> from the job invocation.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use</param>
        /// <returns></returns>
        public virtual async Task CompleteProcessingMessageAsync(BrokeredMessage message, FunctionResult result, CancellationToken cancellationToken)
        {
            if (!result.Succeeded)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await message.AbandonAsync();
            }
        }
    }
}
