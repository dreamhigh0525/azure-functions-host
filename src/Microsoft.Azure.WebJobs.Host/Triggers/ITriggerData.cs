﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Host.Triggers
{
    internal interface ITriggerData
    {
        IValueProvider ValueProvider { get; }

        IReadOnlyDictionary<string, object> BindingData { get; }
    }
}
