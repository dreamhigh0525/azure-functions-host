﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Dashboard.Data
{
    public interface IRecentInvocationIndexByParentWriter
    {
        void CreateOrUpdate(Guid parentId, DateTimeOffset timestamp, Guid id);

        void DeleteIfExists(Guid parentId, DateTimeOffset timestamp, Guid id);
    }
}
