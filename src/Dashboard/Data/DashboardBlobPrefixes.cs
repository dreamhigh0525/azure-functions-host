﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Protocols;

namespace Dashboard.Data
{
    // Names of directory prefixes used only by the dashboard (not part of the protocol with hosts).
    internal static class DashboardBlobPrefixes
    {
        public static string CreateByFunctionRelativePrefix(string functionId)
        {
            return functionId + "/";
        }

        public static string CreateByJobRunRelativePrefix(WebJobRunIdentifier webJobRunId)
        {
            return webJobRunId.GetKey() + "/";
        }

        public static string CreateByParentRelativePrefix(Guid parentId)
        {
            return parentId.ToString("N") + "/";
        }
    }
}
