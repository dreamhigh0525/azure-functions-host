﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Jobs.Host;
using Microsoft.Azure.Jobs.Host.Bindings;
using Microsoft.Azure.Jobs.Host.Triggers;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Jobs.ServiceBus.Triggers
{
    internal class UserTypeArgumentBindingProvider : IQueueTriggerArgumentBindingProvider
    {
        public ITriggerDataArgumentBinding<BrokeredMessage> TryCreate(ParameterInfo parameter)
        {
            // At indexing time, attempt to bind all types.
            // (Whether or not actual binding is possible depends on the message shape at runtime.)
            return new UserTypeArgumentBinding(parameter.ParameterType);
        }

        private class UserTypeArgumentBinding : ITriggerDataArgumentBinding<BrokeredMessage>
        {
            private readonly Type _valueType;
            private readonly IBindingDataProvider _bindingDataProvider;

            public UserTypeArgumentBinding(Type valueType)
            {
                _valueType = valueType;
                _bindingDataProvider = BindingDataProvider.FromType(_valueType);
            }

            public Type ValueType
            {
                get { return _valueType; }
            }

            public IReadOnlyDictionary<string, Type> BindingDataContract
            {
                get { return _bindingDataProvider != null ? _bindingDataProvider.Contract : null; }
            }

            public async Task<ITriggerData> BindAsync(BrokeredMessage value, ValueBindingContext context)
            {
                IValueProvider provider;
                BrokeredMessage clone = value.Clone();
                string contents;

                using (Stream stream = value.GetBody<Stream>())
                {
                    if (stream == null)
                    {
                        provider = await BrokeredMessageValueProvider.CreateAsync(clone, null, ValueType,
                            context.CancellationToken);
                        return new TriggerData(provider, null);
                    }

                    using (TextReader reader = new StreamReader(stream, StrictEncodings.Utf8))
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();
                        contents = await reader.ReadToEndAsync();
                    }
                }

                object convertedValue;

                try
                {
                    convertedValue = JsonCustom.DeserializeObject(contents, ValueType);
                }
                catch (JsonException e)
                {
                    // Easy to have the queue payload not deserialize properly. So give a useful error. 
                    string msg = string.Format(
    @"Binding parameters to complex objects (such as '{0}') uses Json.NET serialization. 
1. Bind the parameter type as 'string' instead of '{0}' to get the raw values and avoid JSON deserialization, or
2. Change the queue payload to be valid json. The JSON parser failed: {1}
", _valueType.Name, e.Message);
                    throw new InvalidOperationException(msg);
                }

                provider = await BrokeredMessageValueProvider.CreateAsync(clone, convertedValue, ValueType,
                    context.CancellationToken);

                IReadOnlyDictionary<string, object> bindingData = (_bindingDataProvider != null) 
                    ? _bindingDataProvider.GetBindingData(convertedValue) : null;

                return new TriggerData(provider, bindingData);
            }
        }
    }
}
