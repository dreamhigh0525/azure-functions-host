﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Models
{
    public class ResumeStatus
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ScriptHostState State { get; set; }
    }
}
