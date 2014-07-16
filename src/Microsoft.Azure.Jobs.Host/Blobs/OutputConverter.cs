﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Azure.Jobs.Host.Converters;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Azure.Jobs.Host.Blobs
{
    internal class OutputConverter<TInput> : IObjectToTypeConverter<ICloudBlob>
        where TInput : class
    {
        private readonly IConverter<TInput, ICloudBlob> _innerConverter;

        public OutputConverter(IConverter<TInput, ICloudBlob> innerConverter)
        {
            _innerConverter = innerConverter;
        }

        public bool TryConvert(object input, out ICloudBlob output)
        {
            TInput typedInput = input as TInput;

            if (typedInput == null)
            {
                output = null;
                return false;
            }

            output = _innerConverter.Convert(typedInput);
            return true;
        }
    }
}
