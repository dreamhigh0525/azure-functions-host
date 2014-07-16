﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Dashboard.Data
{
    public interface IConcurrentTextStore
    {
        IConcurrentText Read(string id);

        void CreateOrUpdate(string id, string text);

        void DeleteIfExists(string id);

        bool TryCreate(string id, string text);

        bool TryUpdate(string id, string eTag, string text);

        bool TryDelete(string id, string eTag);
    }
}
