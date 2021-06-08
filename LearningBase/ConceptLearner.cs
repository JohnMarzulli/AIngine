using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace AiEngine.LearningBase
{
    /// <summary>
    /// Holds the common elements that concept learning algorithms require.
    /// </summary>
    public abstract class ConceptLearner
    {
        /// <summary>
        /// Returns the classification of the given data.
        /// </summary>
        /// <param name="inQuery">The collection of attributes to classify.</param>
        /// <returns>The resulting classification of the query.</returns>
        /// <remarks>
        /// Data is given as an example object, but the
        ///	classification calculated is not placed
        ///	into the query object.
        /// </remarks>
        public abstract DecisionResult Classify(ClassificationData inQuery);

        /// <summary>
        /// Loads the training data used to classify queries.
        /// </summary>
        /// <param name="streamReader">The stream that holds the training data.</param>
        /// <returns>True if the data was loaded.</returns>
        public bool LoadTrainingData(
            StreamReader streamReader
        )
        {
            Debug.Assert(
                streamReader != null,
                "Invalid stream.");

            Examples.Clear();
            Attributes.Clear();

            // Read in the classes
            // Example line:
            // classes     2 yes no
            string[] tokens = GetTokenizedInput(streamReader);
            int numClasses = int.Parse(tokens[1]);
            _classes = new Classification(tokens.Skip(2).ToList());

            Debug.Assert(
                numClasses == _classes.Values.Count,
                $"Found {_classes.Values.Count} classes, but expected {numClasses}");

            // Read in the attributes
            // Example line:
            // attributes  4
            int numAttributes = int.Parse(GetTokenizedInput(streamReader)[1]);

            // Read in each attribute and it's values
            for (int i = 0; i < numAttributes; ++i)
            {
                // Example line of an attribute entry:
                // outlook     3 sunny overcast rain
                string[] attributeTokens = GetTokenizedInput(streamReader);

                string attributeName = attributeTokens[0];
                int attributeCount = int.Parse(attributeTokens[1]);
                List<string> attributeValues = attributeTokens.Skip(2).ToList();

                Debug.Assert(
                    attributeCount == attributeValues.Count,
                    $"Found {attributeValues.Count} classes, but expected {attributeCount}");

                LearningAttribute newAttribute = new(attributeName, attributeValues);

                Attributes.Add(newAttribute.Id, newAttribute);
            }

            // We should get the keyword "examples"
            // Example line:
            // examples
            Debug.Assert(
                string.Compare(
                    streamReader.ReadLine()?.Trim(),
                    "examples", StringComparison.InvariantCultureIgnoreCase) == 0,
                "Unable to find the expected `examples` line.");

            // Read in the examples until we hit EOF
            // If we are running a debug build, print out
            // the example data as we read it in
            while (!streamReader.EndOfStream)
            {
                // Example line:
                // no  sunny    hot  high   weak
                string[] exampleTokens = GetTokenizedInput(streamReader);

                if (exampleTokens == null || !exampleTokens.Any())
                {
                    break;
                }

                ClassificationValueId classIdentifier = GetClassIdentifier(exampleTokens[0]);

                Debug.Assert(
                    classIdentifier != null,
                    "Unable to find class identifier!");

                Example newExample = new(classIdentifier);
                int exampleIndex = 0;

                foreach (LearningAttribute attribute in Attributes.Values)
                {
                    AttributeValueId valueId = attribute.GetValueId(exampleTokens[exampleIndex + 1]);

                    if (valueId != null)
                    {
                        newExample.SetValueIdentifier(attribute.Id, valueId);
                    }

                    ++exampleIndex;
                }

                // The the newly read example into the example pool.
                AddExample(newExample);
            }

            return true;
        }

        /// <summary>
        /// Loads the training data used to classify queries.
        /// </summary>
        /// <param name="inFileName">The file that contains the training data.</param>
        /// <returns>True if the data was loaded.</returns>
        public bool LoadTrainingData(
            string inFileName
        )
        {
            StreamReader streamReader = new(inFileName);

            return LoadTrainingData(streamReader);
        }

        private static string[] GetTokenizedInput(StreamReader streamReader) =>
            streamReader.ReadLine()?.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// Returns the class ID of the given string.
        /// </summary>
        /// <param name="outcomeName">The name of the outcome that we need the Id of.</param>
        /// <returns>Any id found for the classification.</returns>
        public ClassificationValueId GetClassIdentifier(
            string outcomeName
        ) => _classes.GetValueId(outcomeName);

        /// <summary>
        /// Returns the name of the class with the given ID
        /// </summary>
        /// <param name="classId">The id of the class outcome/value we want.</param>
        /// <returns>The name of the class with the given ID</returns>
        public string GetClass(
            in ClassificationValueId classId
        ) => _classes.GetValue(classId);

        /// <summary>
        /// Returns the number of classes in the decision tree.
        /// </summary>
        /// <returns>The number of classes in the decision tree.</returns>
        public int GetNumClasses() =>
            _classes.Values.Count;

        /// <summary>
        /// Returns the ID of the attribute with the given name.
        /// </summary>
        /// <param name="inAttributeName">The the name of attribute we need the id of.</param>
        /// <returns>Any found id of an attribute with the given name.</returns>
        public AttributeId GetAttributeIdentifier(
            string inAttributeName
        ) =>
            Attributes.Values.FirstOrDefault(
                value =>
                        (string.Compare(
                            inAttributeName,
                            value.Name,
                            StringComparison.InvariantCultureIgnoreCase) == 0))?.Id;

        /// <summary>
        /// Returns a reference to the attribute with the given Id.
        /// </summary>
        /// <param name="inAttributeId">The attribute Id we need to get the values for.</param>
        /// <returns>Any attribute found with the given Id.</returns>
        public LearningAttribute GetAttribute(
            [NotNull] in AttributeId inAttributeId
        )
        {
            Debug.Assert(inAttributeId != null);
            Debug.Assert(Attributes != null);
            Debug.Assert(Attributes.ContainsKey(inAttributeId));

            return Attributes[inAttributeId];
        }

        /// <summary>
        /// Adds an example to the pool of training data.
        /// </summary>
        /// <param name="inNewExample">The example to add to the training pool.</param>
        public void AddExample(
            Example inNewExample
        ) =>
            Examples.Add(inNewExample);

        /// <summary>
        /// Returns the example at the given index.
        /// </summary>
        /// <param name="exampleIndex">The index of the example to get.</param>
        /// <returns>The example at the index.</returns>
        public Example GetExample(
            in int exampleIndex
        ) => Examples[exampleIndex];

        protected ConceptLearner()
        {
            Attributes = new Dictionary<AttributeId, LearningAttribute>();
            Examples = new List<Example>();
        }

        public Dictionary<AttributeId, LearningAttribute> Attributes { get; } // the attributes used to make a decision with
        public List<Example> Examples { get; } // the examples used from training

        public Classification Classes => _classes;

        protected Classification _classes; // The names of the classes
    }
}