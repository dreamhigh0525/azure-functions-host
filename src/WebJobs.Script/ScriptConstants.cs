﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Script
{
    public static class ScriptConstants
    {
        public const string AzureFunctionsWebHookContextKey = "MS_AzureFunctionsWebHookContext";
        public const string AzureFunctionsWebHookDataKey = "MS_AzureFunctionsWebHookData";
        public const string AzureFunctionsHttpResponseKey = "MS_AzureFunctionsHttpResponse";

        // Define all system parameters we inject with a prefix to avoid collisions
        // with user parameters
        public const string SystemTriggerParameterName = "_triggerValue";
        public const string SystemExecutionContextParameterName = "_context";
        public const string SystemLogParameterName = "_log";
        public const string SystemBinderParameterName = "_binder";

        public const string HostMetadataFileName = "host.json";
        internal const string FunctionMetadataFileName = "function.json";
    }
}
