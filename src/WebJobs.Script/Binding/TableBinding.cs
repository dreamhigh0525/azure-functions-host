﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings.Path;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script
{
    internal class TableBinding : Binding
    {
        private readonly BindingTemplate _partitionKeyBindingTemplate;
        private readonly BindingTemplate _rowKeyBindingTemplate;
        private readonly TableQuery _tableQuery;

        public TableBinding(JobHostConfiguration config, string name, string tableName, string partitionKey, string rowKey, FileAccess fileAccess, TableQuery tableQuery = null) : base(config, name, "queue", fileAccess, false)
        {
            TableName = tableName;
            PartitionKey = partitionKey;
            RowKey = rowKey;
            _partitionKeyBindingTemplate = BindingTemplate.FromString(PartitionKey);
            if (!string.IsNullOrEmpty(RowKey))
            {
                _rowKeyBindingTemplate = BindingTemplate.FromString(RowKey);
            }

            _tableQuery = tableQuery;
            if (_tableQuery == null)
            {
                _tableQuery = new TableQuery
                {
                    TakeCount = 50
                };
            }
        }

        public string TableName { get; private set; }
        public string PartitionKey { get; private set; }
        public string RowKey { get; private set; }

        public override bool HasBindingParameters
        {
            get
            {
                return _partitionKeyBindingTemplate.ParameterNames.Any() ||
                       (_rowKeyBindingTemplate != null && _rowKeyBindingTemplate.ParameterNames.Any());
            }
        }

        public override async Task BindAsync(IBinder binder, Stream stream, IReadOnlyDictionary<string, string> bindingData)
        {
            string boundPartitionKey = PartitionKey;
            string boundRowKey = RowKey;
            if (bindingData != null)
            {
                boundPartitionKey = _partitionKeyBindingTemplate.Bind(bindingData);
                if (_rowKeyBindingTemplate != null)
                {
                    boundRowKey = _rowKeyBindingTemplate.Bind(bindingData);
                }
            }

            boundPartitionKey = Resolve(boundPartitionKey);
            if (!string.IsNullOrEmpty(boundRowKey))
            {
                boundRowKey = Resolve(boundRowKey);
            }

            if (FileAccess == FileAccess.Write)
            {
                // read the content as a JObject
                JObject jsonObject = null;
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    string content = await streamReader.ReadToEndAsync();
                    jsonObject = JObject.Parse(content);
                }

                // TODO: If RowKey has not been specified in the binding, try to
                // derive from the object properties (e.g. "rowKey" or "id" properties);

                IAsyncCollector<DynamicTableEntity> collector = binder.Bind<IAsyncCollector<DynamicTableEntity>>(new TableAttribute(TableName));
                DynamicTableEntity tableEntity = new DynamicTableEntity(boundPartitionKey, boundRowKey);
                foreach (JProperty property in jsonObject.Properties())
                {
                    EntityProperty entityProperty = EntityProperty.CreateEntityPropertyFromObject((object)property.Value);
                    tableEntity.Properties.Add(property.Name, entityProperty);
                }

                await collector.AddAsync(tableEntity);
            }
            else
            {
                if (!string.IsNullOrEmpty(boundPartitionKey) &&
                    !string.IsNullOrEmpty(boundRowKey))
                {
                    // singleton
                    DynamicTableEntity tableEntity = binder.Bind<DynamicTableEntity>(new TableAttribute(TableName, boundPartitionKey, boundRowKey));
                    if (tableEntity != null)
                    {
                        string json = ConvertEntityToJObject(tableEntity).ToString();
                        using (StreamWriter sw = new StreamWriter(stream))
                        {
                            await sw.WriteAsync(json);
                        }
                    }
                }
                else
                {
                    // binding to entire table (query multiple table entities)
                    CloudTable table = binder.Bind<CloudTable>(new TableAttribute(TableName, boundPartitionKey, boundRowKey));
                    var entities = table.ExecuteQuery(_tableQuery);

                    JArray entityArray = new JArray();
                    foreach (var entity in entities)
                    {
                        entityArray.Add(ConvertEntityToJObject(entity));
                    }

                    string json = entityArray.ToString(Formatting.None);
                    using (StreamWriter sw = new StreamWriter(stream))
                    {
                        await sw.WriteAsync(json);
                    }
                }
            }
        }

        private static JObject ConvertEntityToJObject(DynamicTableEntity tableEntity)
        {
            OperationContext context = new OperationContext();
            var entityProperties = tableEntity.WriteEntity(context);

            JObject jsonObject = new JObject();
            foreach (var entityProperty in entityProperties)
            {
                JValue value = null;
                switch (entityProperty.Value.PropertyType)
                {
                    case EdmType.String:
                        value = new JValue(entityProperty.Value.StringValue);
                        break;
                    case EdmType.Int32:
                        value = new JValue(entityProperty.Value.Int32Value);
                        break;
                    case EdmType.Int64:
                        value = new JValue(entityProperty.Value.Int64Value);
                        break;
                    case EdmType.DateTime:
                        value = new JValue(entityProperty.Value.DateTime);
                        break;
                    case EdmType.Boolean:
                        value = new JValue(entityProperty.Value.BooleanValue);
                        break;
                    case EdmType.Guid:
                        value = new JValue(entityProperty.Value.GuidValue);
                        break;
                    case EdmType.Double:
                        value = new JValue(entityProperty.Value.DoubleValue);
                        break;
                    case EdmType.Binary:
                        value = new JValue(entityProperty.Value.BinaryValue);
                        break;
                }

                jsonObject.Add(entityProperty.Key, value);
            }
            return jsonObject;
        }
    }
}
