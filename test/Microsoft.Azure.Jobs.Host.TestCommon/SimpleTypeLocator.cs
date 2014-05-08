﻿using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Jobs.Host.TestCommon
{
    public class SimpleTypeLocator : ITypeLocator
    {
        private readonly Type[] _types;

        public SimpleTypeLocator(params Type[] types)
        {
            _types = types;
        }

        public IReadOnlyList<Type> GetTypes()
        {
            return _types;
        }
    }
}
