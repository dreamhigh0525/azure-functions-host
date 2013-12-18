using System;
using System.Reflection;

// Attributes used by test functions
namespace Microsoft.WindowsAzure.Jobs
{
    // Specify this parameter comes from configuration 
    [AttributeUsage(AttributeTargets.Parameter)]
    internal class ConfigAttribute : Attribute
    {
        // Short name of the file containing the configuration. 
        public string Filename { get; set; }

        public ConfigAttribute()
        {
        }

        public ConfigAttribute(string filename)
        {
            this.Filename = filename;
        }
    }

    // Tells orchestration layer to not listen on this method.
    // This can be useful to avoid the performance impact of listening on a large container. 
    // Method must be invoked explicitly.
    [AttributeUsage(AttributeTargets.Method)]
    internal class NoAutomaticTriggerAttribute : Attribute
    {
        public static NoAutomaticTriggerAttribute Build(CustomAttributeData attr)
        {
            if (attr.Constructor.DeclaringType.FullName != typeof(NoAutomaticTriggerAttribute).FullName)
            {
                return null;
            }
            return new NoAutomaticTriggerAttribute();
        } 
    }

    // Type binds to an Azure table of the given name. 
    [AttributeUsage(AttributeTargets.Parameter)]
    public class TableAttribute : Attribute
    {
        // If empty, infer from the name of the local 
        // Beware of table name restrictions.
        public string TableName { get; set; }

        public TableAttribute(string tableName)
        {
            this.TableName = tableName;
        }

        public static TableAttribute Build(CustomAttributeData attr)
        {
            if (attr.Constructor.DeclaringType.FullName != typeof(TableAttribute).FullName)
            {
                return null;
            }
            string arg = (string)attr.ConstructorArguments[0].Value;
            return new TableAttribute(arg);
        }

        public override string ToString()
        {
            return string.Format("[Table{0})]", TableName);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DescriptionAttribute : Attribute
    {
        public string Description { get; set; }

        public DescriptionAttribute(string description)
        {
            this.Description = description;
        }
        public static DescriptionAttribute Build(CustomAttributeData attr)
        {
            if (attr.Constructor.DeclaringType.FullName != typeof(DescriptionAttribute).FullName)
            {
                return null;
            }
            string arg = (string)attr.ConstructorArguments[0].Value;
            return new DescriptionAttribute(arg);        
        }    
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class QueueInputAttribute : Attribute
    {
        // If empty, infer from the name of the local 
        public string QueueName { get ;set ;}

        public static QueueInputAttribute Build(CustomAttributeData attr)
        {
            if (attr.Constructor.DeclaringType.FullName != typeof(QueueInputAttribute).FullName)
            {
                return null;
            }
            return new QueueInputAttribute(); // $$$
        }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
    public class QueueOutputAttribute : Attribute
    {
        // If empty, infer from the name of the local 
        public string QueueName { get; set; }

        public QueueOutputAttribute()
        {
        }


        public static QueueOutputAttribute Build(CustomAttributeData attr)
        {
            if (attr.Constructor.DeclaringType.FullName != typeof(QueueOutputAttribute).FullName)
            {
                return null;
            }
            return new QueueOutputAttribute(); // $$$
        }
    }


    // Take in multiple inputs. Used for aggregation. 
    // [BlobInputs("container\{deployId}\{date}\{name}.csv"]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class BlobInputsAttribute : Attribute
    {
        public string BlobPathPattern { get; set; }

        public BlobInputsAttribute(string blobPathPattern)
        {
            this.BlobPathPattern = blobPathPattern;
        }

        public static BlobInputsAttribute Build(CustomAttributeData attr)
        {
            if (attr.Constructor.DeclaringType.FullName != typeof(BlobInputsAttribute).FullName)
            {
                return null;
            }

            string arg = (string)attr.ConstructorArguments[0].Value;
            return new BlobInputsAttribute(arg);
        }

        public override string ToString()
        {
            return string.Format("[BlobInputs({0})]", BlobPathPattern);
        }
    }


    // $$$ Only read these once:
    // - orchestration, for knowing what to listen on and when to run it
    // - invoker, for knowing how to bind the parameters
    [AttributeUsage(AttributeTargets.Parameter)]
    public class BlobInputAttribute : Attribute
    {
        public string ContainerName { get; set; }

        public BlobInputAttribute(string containerName)
        {
            this.ContainerName = containerName;
        }

        public static BlobInputAttribute Build(CustomAttributeData attr)
        {
            if (attr.Constructor.DeclaringType.FullName != typeof(BlobInputAttribute).FullName)
            {
                return null;
            }

            string arg = (string)attr.ConstructorArguments[0].Value;
            return new BlobInputAttribute(arg);
        }

        public override string ToString()
        {
            return string.Format("[BlobInput({0})]", ContainerName);
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class BlobOutputAttribute : Attribute
    {
        public string ContainerName { get; set; }

        public BlobOutputAttribute(string containerName)
        {
            this.ContainerName = containerName;
        }

        public static BlobOutputAttribute Build(CustomAttributeData attr)
        {
            if (attr.Constructor.DeclaringType.FullName != typeof(BlobOutputAttribute).FullName)
            {
                return null;
            }

            string arg = (string)attr.ConstructorArguments[0].Value;
            return new BlobOutputAttribute(arg);
        }

        public override string ToString()
        {
            return string.Format("[BlobOutput({0})]", ContainerName);
        }
    }
}
