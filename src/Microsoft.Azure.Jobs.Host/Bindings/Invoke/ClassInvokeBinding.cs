﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Jobs.Host.Converters;
using Microsoft.Azure.Jobs.Host.Protocols;

namespace Microsoft.Azure.Jobs.Host.Bindings.Invoke
{
    class ClassInvokeBinding<TValue> : IBinding
        where TValue : class
    {
        private static readonly IObjectToTypeConverter<TValue> _converter = new CompositeObjectToTypeConverter<TValue>(
            new ClassOutputConverter<TValue, TValue>(new IdentityConverter<TValue>()),
            new ClassOutputConverter<string, TValue>(new StringToTConverter<TValue>()));

        private readonly string _parameterName;

        public ClassInvokeBinding(string parameterName)
        {
            _parameterName = parameterName;
        }

        public bool FromAttribute
        {
            get { return false; }
        }

        private Task<IValueProvider> BindAsync(TValue value, FunctionBindingContext context)
        {
            IValueProvider provider = new ObjectValueProvider(value, typeof(TValue));
            return Task.FromResult(provider);
        }

        public Task<IValueProvider> BindAsync(object value, FunctionBindingContext context)
        {
            TValue typedValue = null;

            if (!_converter.TryConvert(value, out typedValue))
            {
                throw new InvalidOperationException("Unable to convert value to " + typeof(TValue).Name + ".");
            }

            return BindAsync(typedValue, context);
        }

        public Task<IValueProvider> BindAsync(BindingContext context)
        {
            throw new InvalidOperationException("No value was provided for parameter '" + _parameterName + "'.");
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new CallerSuppliedParameterDescriptor
            {
                Name = _parameterName
            };
        }
    }
}
