﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Dashboard.Data
{
    public interface IConcurrentMetadataTextStore : IConcurrentTextStore
    {
        IEnumerable<ConcurrentMetadata> List(string prefix);

        new ConcurrentMetadataText Read(string id);

        ConcurrentMetadata ReadMetadata(string id);

        void CreateOrUpdate(string id, IDictionary<string, string> metadata, string text);

        bool TryCreate(string id, IDictionary<string, string> metadata, string text);

        bool TryUpdate(string id, string eTag, IDictionary<string, string> metadata, string text);
    }
}
