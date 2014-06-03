﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Jobs.Host.Bindings;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Microsoft.Azure.Jobs.Host.Queues.Bindings
{
    internal class CollectionArgumentBindingProvider : IQueueArgumentBindingProvider
    {
        public IArgumentBinding<CloudQueue> TryCreate(ParameterInfo parameter)
        {
            Type parameterType = parameter.ParameterType;

            if (!parameterType.IsGenericType)
            {
                return null;
            }

            Type genericTypeDefinition = parameterType.GetGenericTypeDefinition();

            if (genericTypeDefinition != typeof(ICollection<>))
            {
                return null;
            }

            Type itemType = genericTypeDefinition.GetGenericArguments()[0];

            IArgumentBinding<CloudQueue> itemBinding;

            if (itemType == typeof(CloudQueueMessage))
            {
                itemBinding = new CloudQueueMessageArgumentBinding();
            }
            else if (itemType == typeof(string))
            {
                itemBinding = new StringArgumentBinding();
            }
            else if (itemType == typeof(byte[]))
            {
                itemBinding = new ByteArrayArgumentBinding();
            }
            else
            {
                if (typeof(IEnumerable).IsAssignableFrom(itemType))
                {
                    throw new InvalidOperationException("Nested collections are not supported.");
                }

                itemBinding = new UserTypeArgumentBinding(itemType);
            }

            return CreateCollectionArgumentBinding(itemType, itemBinding);
        }

        private static IArgumentBinding<CloudQueue> CreateCollectionArgumentBinding(Type itemType,
            IArgumentBinding<CloudQueue> itemBinding)
        {
            Type collectionGenericType = typeof(CollectionQueueArgumentBinding<>).MakeGenericType(itemType);
            return (IArgumentBinding<CloudQueue>)Activator.CreateInstance(collectionGenericType, itemBinding);
        }

        private class CollectionQueueArgumentBinding<TItem> : IArgumentBinding<CloudQueue>
        {
            private readonly IArgumentBinding<CloudQueue> _itemBinding;

            public CollectionQueueArgumentBinding(IArgumentBinding<CloudQueue> itemBinding)
            {
                _itemBinding = itemBinding;
            }

            public Type ValueType
            {
                get { return typeof(ICollection<TItem>); }
            }

            public IValueProvider Bind(CloudQueue value, ArgumentBindingContext context)
            {
                return new CollectionValueBinder(value, (IValueBinder)_itemBinding.Bind(value, context));
            }

            private class CollectionValueBinder : IOrderedValueBinder
            {
                private readonly CloudQueue _queue;
                private readonly IValueBinder _itemBinder;
                private readonly ICollection<TItem> _value = new List<TItem>();

                public CollectionValueBinder(CloudQueue queue, IValueBinder itemBinder)
                {
                    _queue = queue;
                    _itemBinder = itemBinder;
                }

                public int StepOrder
                {
                    get { return BindStepOrders.Enqueue; }
                }

                public Type Type
                {
                    get { return typeof(ICollection<TItem>); }
                }

                public object GetValue()
                {
                    return _value;
                }

                public string ToInvokeString()
                {
                    return _queue.Name;
                }

                public void SetValue(object value)
                {
                    // Not ByRef, so can ignore value argument.
                    foreach (TItem item in _value)
                    {
                        _itemBinder.SetValue(item);
                    }
                }
            }
        }
    }
}
