﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Jobs.Host.Bindings;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Microsoft.Azure.Jobs.Host.Queues.Bindings
{
    internal class ByteArrayArgumentBinding : IArgumentBinding<CloudQueue>
    {
        public Type ValueType
        {
            get { return typeof(byte[]); }
        }

        public IValueProvider Bind(CloudQueue value, FunctionBindingContext context)
        {
            return new ByteArrayValueBinder(value);
        }

        private class ByteArrayValueBinder : IOrderedValueBinder
        {
            private readonly CloudQueue _queue;

            public ByteArrayValueBinder(CloudQueue queue)
            {
                _queue = queue;
            }

            public int StepOrder
            {
                get { return BindStepOrders.Enqueue; }
            }

            public Type Type
            {
                get { return typeof(byte[]); }
            }

            public object GetValue()
            {
                return null;
            }

            public string ToInvokeString()
            {
                return _queue.Name;
            }

            public void SetValue(object value)
            {
                byte[] bytes = (byte[])value;

                _queue.AddMessageAndCreateIfNotExists(new CloudQueueMessage(bytes));
            }
        }
    }
}
