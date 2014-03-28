﻿using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.WindowsAzure.Jobs.Host.Bindings.BinderProviders
{
    // Binder provider for the CloudTable SDK type
    internal class CloudTableBinderProvider : ICloudTableBinderProvider
    {
        public ICloudTableBinder TryGetBinder(Type targetType, bool isReadOnly)
        {
            if (targetType == typeof(CloudTable))
            {
                return new CloudTableBinder();
            }

            return null;
        }

        private class CloudTableBinder : ICloudTableBinder
        {
            public BindResult Bind(IBinderEx bindingContext, Type targetType, string tableName)
            {
                CloudStorageAccount account = Utility.GetAccount(bindingContext.AccountConnectionString);
                CloudTableClient client = account.CreateCloudTableClient();
                CloudTable table = client.GetTableReference(tableName);
                return new BindResult { Result = table };
            }
        }
    }
}
