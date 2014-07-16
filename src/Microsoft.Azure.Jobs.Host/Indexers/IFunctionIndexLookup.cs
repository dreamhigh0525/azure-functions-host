﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.Azure.Jobs.Host.Indexers
{
    internal interface IFunctionIndexLookup
    {
        IFunctionDefinition Lookup(string functionId);

        IFunctionDefinition Lookup(MethodInfo method);
    }
}
