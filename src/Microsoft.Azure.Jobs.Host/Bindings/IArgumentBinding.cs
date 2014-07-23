﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Jobs.Host.Bindings
{
    internal interface IArgumentBinding<TArgument>
    {
        Type ValueType { get; }

        Task<IValueProvider> BindAsync(TArgument value, FunctionBindingContext context);
    }
}
