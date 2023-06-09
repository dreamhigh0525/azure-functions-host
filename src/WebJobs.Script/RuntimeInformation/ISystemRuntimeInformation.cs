﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.Azure.WebJobs.Script
{
    public interface ISystemRuntimeInformation
    {
        Architecture GetOSArchitecture();

        OSPlatform GetOSPlatform();
    }
}
