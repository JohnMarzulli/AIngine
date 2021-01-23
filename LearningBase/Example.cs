using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AiEngine.LearningBase
{
    /// <summary>
    /// Holds a set of values from each attribute
    ///	so that a classification may be generated
    /// 
    /// Holds the definition and constants needed to
    ///	construct an example. Examples are used to
    ///	train learning mechanisms that learn
    ///	classification or concepts
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public class ClassificationData
    {
        public override string ToString()
        {
            var keyValuePairs = new List<string>();

            foreach ((AttributeId key, AttributeValueId value) in ValueIdentifiers)
            {
                keyValuePairs.Add($"{key.Id}:{value.Id}");
            }

            return string.Join(',', keyValuePairs);
        }

        /// <summary>
        /// 	Sets the ID of the value for the given attribute.
        /// </summary>
        /// <param name="attributeId">The index/id of the attribute to set.</param>
        /// <param name="valueIdentifier">The value of the attribute.</param>
        public void SetValueIdentifier(
            in AttributeId attributeId,
            in AttributeValueId valueIdentifier
        )
        {
            if (ValueIdentifiers.ContainsKey(attributeId))
            {
                ValueIdentifiers[attributeId] = valueIdentifier;
            }
            else
            {
                ValueIdentifiers.Add(attributeId, valueIdentifier);
            }
        }

        /// <summary>
        /// Returns the ID of the value for the given attribute
        /// </summary>
        /// <param name="id">The type of attribute we want to get the value of.</param>
        /// <returns>The valueId for the attribute of this example</returns>
        public AttributeValueId GetValueIdentifier(
            AttributeId id
        )
        {
            Debug.Assert(ValueIdentifiers.ContainsKey(id));

            return ValueIdentifiers[id];
        }

        public Dictionary<AttributeId, AttributeValueId> Values => ValueIdentifiers;

        protected Dictionary<AttributeId, AttributeValueId> ValueIdentifiers = new Dictionary<AttributeId, AttributeValueId>(); // The IDs of each value
    };

    /// <summary>
    /// Holds an example. An example is formed by a set
    ///	of values from each attribute and the correct
    ///	classification
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public class Example : ClassificationData
    {
        public new virtual string ToString()
        {
            var outString = new StringBuilder();

            outString.Append($"Result: {ClassIdentifier.Id}");

            foreach ((AttributeId attributeId, AttributeValueId valueId) in ValueIdentifiers)
            {
                outString.Append($" ( {attributeId.Id}={valueId.Id} )");
            }

            outString.AppendLine();

            return outString.ToString();
        }

        /// <summary>
        ///     Create a new example with a known class identifier.
        /// </summary>
        /// <param name="inClassIdentifier"></param>
        public Example(
            in ClassificationValueId inClassIdentifier
        )
        {
            ClassIdentifier = inClassIdentifier;
        }

        /// <summary>
        /// The ID of the class for this example.
        /// </summary>
        public ClassificationValueId ClassIdentifier { get; }
    }
}
