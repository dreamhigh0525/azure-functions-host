﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Host.Executors
{
    internal class VoidMethodInvoker<TReflected> : IMethodInvoker<TReflected>
    {
        private readonly Action<TReflected, object[]> _lambda;

        public VoidMethodInvoker(Action<TReflected, object[]> lambda)
        {
            _lambda = lambda;
        }

        public Task InvokeAsync(TReflected instance, object[] arguments)
        {
            _lambda.Invoke(instance, arguments);
            return Task.FromResult(0);
        }
    }
}
