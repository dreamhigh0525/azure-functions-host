using System.Collections.Generic;

namespace Microsoft.WindowsAzure.Jobs
{
    internal interface IAzureTableReader
    {
        // Lookup an azure entity by the partition and row key.
        // Returns null on missing. 
        // Result dictionary has all entity properties (including row key, partition key, and timestamp)
        IDictionary<string, string> Lookup(string partitionKey, string rowKey);

        // if partitionKey == null, enumerate all entities in the table.
        // else just enumerate that partition. 
        // Resulting dictionaries include the row key, partition key, and timestamp for each.
        IEnumerable<IDictionary<string, string>> Enumerate(string partitionKey = null);
    }

    // Like IAzureTableReader, but has strong binding instead of just IDictionary<>
    internal interface IAzureTableReader<T>
    {
        T Lookup(string partitionKey, string rowKey);

        // if partitionKey == null, enumerate all entities in the table.
        // else just enumerate that partition. 
        IEnumerable<T> Enumerate(string partitionKey = null);
    }

    internal interface IAzureTableWriter
    {
        // Dictionary should have RowKey and PartitionKey
        // This is Upsert by default. 

        // if values is an IDictionary<string,string>, then use that.
        // Else if it's an object (including an anonymous type), convert it to an 
        // IDictionary based on the properties. 
        void Write(string partitionKey, string rowKey, object values);

        // Delete
        // Delete a specific partition and rowKey.
        // if RowKey is null, then delete the entire partition.
        // This blocks until deleted. 
        // No errors if entities does not.
        void Delete(string partitionKey, string rowKey = null);

        // Batching is a critical performance requirement for azure tables.
        void Flush();
    }

    // Interface to request if you want both reading and writing. 
    // This is better than getting a separate reader and writer because this allows them to coordinate
    // on flushing writes before doing reads. Separate table objects may get out of sync.
    internal interface IAzureTable : IAzureTableReader, IAzureTableWriter
    {
    }

    internal interface IAzureTable<TValue> : IAzureTableReader<TValue>, IAzureTableWriter
    {
    }
}
