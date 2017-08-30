﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Models;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Extensions
{
    public interface IExtensionsManager
    {
        Task<IList<ExtensionPackageReference>> GetExtensions();

        Task AddExtensions(params ExtensionPackageReference[] reference);

        Task DeleteExtensions(params string[] extensionIds);
    }
}