﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

#if PUBLICPROTOCOL
namespace Microsoft.Azure.Jobs.Protocols
#else
namespace Microsoft.Azure.Jobs.Host.Protocols
#endif
{
    /// <summary>Represents credentials used by a host instance.</summary>
#if PUBLICPROTOCOL
    public class CredentialsDescriptor
#else
    internal class CredentialsDescriptor
#endif
    {
        /// <summary>Gets or sets the connection strings used by a host intsance.</summary>
        public IEnumerable<ConnectionStringDescriptor> ConnectionStrings { get; set; }
    }
}
