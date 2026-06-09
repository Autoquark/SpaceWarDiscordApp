using Google.Cloud.Firestore.V1;

namespace Tumult.Database.Converters;

public abstract class TypeDiscriminatorBase
{
    protected abstract IReadOnlyDictionary<string, string> TypeMappings { get; }
    
    public Type GetConcreteType(IDictionary<string, Value> map)
    {
        var descriptor = map[nameof(IPolymorphicFirestoreData.SubtypeName)].StringValue;
        var temp = Type.GetType(descriptor);
        if (temp == null)
        {
            var backtickIndex = descriptor.IndexOf('`');
            if (backtickIndex == -1)
            {
                backtickIndex = descriptor.Length;
            }

            var beforeBacktick = descriptor[..backtickIndex];
            var afterBacktick = descriptor[backtickIndex..];
            return Type.GetType(TypeMappings[beforeBacktick] + afterBacktick)!;
        }

        return temp;
    }
}