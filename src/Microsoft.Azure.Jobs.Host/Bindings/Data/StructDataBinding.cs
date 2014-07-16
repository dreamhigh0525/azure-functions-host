﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Jobs.Host.Converters;
using Microsoft.Azure.Jobs.Host.Protocols;

namespace Microsoft.Azure.Jobs.Host.Bindings.Data
{
    internal class StructDataBinding<TBindingData> : IBinding
        where TBindingData : struct
    {
        private static readonly IObjectToTypeConverter<TBindingData> _converter =
            new CompositeObjectToTypeConverter<TBindingData>(
                new StructOutputConverter<TBindingData, TBindingData>(new IdentityConverter<TBindingData>()),
                new ClassOutputConverter<string, TBindingData>(new StringToTConverter<TBindingData>()));

        private readonly string _parameterName;
        private readonly IArgumentBinding<TBindingData> _argumentBinding;

        public StructDataBinding(string parameterName, IArgumentBinding<TBindingData> argumentBinding)
        {
            _parameterName = parameterName;
            _argumentBinding = argumentBinding;
        }

        public bool FromAttribute
        {
            get { return false; }
        }

        private IValueProvider Bind(TBindingData bindingDataItem, FunctionBindingContext context)
        {
            return _argumentBinding.Bind(bindingDataItem, context);
        }

        public IValueProvider Bind(object value, FunctionBindingContext context)
        {
            TBindingData typedValue;

            if (!_converter.TryConvert(value, out typedValue))
            {
                throw new InvalidOperationException("Unable to convert value to " + typeof(TBindingData).Name + ".");
            }

            return Bind(typedValue, context);
        }

        public IValueProvider Bind(BindingContext context)
        {
            IReadOnlyDictionary<string, object> bindingData = context.BindingData;

            if (!bindingData.ContainsKey(_parameterName))
            {
                throw new InvalidOperationException(
                    "Binding data does not contain expected value '" + _parameterName + "'.");
            }

            object untypedValue = bindingData[_parameterName];

            if (!(untypedValue is TBindingData))
            {
                throw new InvalidOperationException("Binding data for '" + _parameterName +
                    "' is not of expected type " + typeof(TBindingData).Name + ".");
            }

            TBindingData typedValue = (TBindingData)untypedValue;
            return Bind(typedValue, context.FunctionContext);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new BindingDataParameterDescriptor
            {
                Name = _parameterName
            };
        }
    }
}
