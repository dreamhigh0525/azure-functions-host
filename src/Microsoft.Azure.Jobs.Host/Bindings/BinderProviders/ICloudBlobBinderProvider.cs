﻿using System;

namespace Microsoft.Azure.Jobs
{
    internal interface ICloudBlobBinderProvider
    {
        // Can this binder read/write the given type?
        // This could be a straight type match, or a generic type.
        ICloudBlobBinder TryGetBinder(Type targetType);
    }
}
