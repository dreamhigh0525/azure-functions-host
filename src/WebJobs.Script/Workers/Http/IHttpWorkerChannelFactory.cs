﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Script.Diagnostics;

namespace Microsoft.Azure.WebJobs.Script.Workers
{
    public interface IHttpWorkerChannelFactory
    {
        IHttpWorkerChannel Create(string scriptRootPath, IMetricsLogger metricsLogger, int attemptCount);
    }
}
