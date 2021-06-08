using AiEngine.LearningBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace AiEngine.DecisionTree
{

    /// <summary>
    /// 
    /// </summary>
    public class DecisionTree : ConceptLearner
    {
        private const string ClassificationUnknown = "UNKNOWN";

        /// <summary>
        /// Returns the classification of the given data.
        /// </summary>
        /// <param name="inQuery"></param>
        /// <remarks>Data is given as an example object, but the classification
        /// calculated is not placed into the query object.</remarks>
        /// <returns>The resulting classification/outcome of the query.</returns>
        public override string Classify(
            ClassificationData inQuery
        )
        {
            string outClassification = ClassificationUnknown;
            DecisionNode nodeToInspect = new(RootNode);

            while (nodeToInspect != null)
            {
                int childCount = nodeToInspect.GetChildren().Count;

                if (childCount == 0)
                {
                    outClassification = Classes.GetValue(nodeToInspect.GetClassIdentifier());

                    break;
                }

                AttributeValueId valueId = inQuery.GetValueIdentifier(nodeToInspect.AttributeId);

                nodeToInspect = nodeToInspect.Children[valueId];
            }

            return outClassification;
        }

        /// <summary>
        /// Saves the decision tree to a file with the given name. If the dave fails then FALSE is returned
        /// </summary>
        /// <param name="output">The stream to save the tree to.</param>
        /// <returns>TRUE if the tree was saved.</returns>
        public bool SavePrebuiltTree(
            [NotNull] StreamWriter output
        )
        {
            output.WriteLine(Classes.Values.Count);

            foreach (string className in Classes.Values)
            {
                output.WriteLine(className);
            }

            output.WriteLine(Attributes.Count);
            foreach (LearningAttribute attribute in Attributes.Values)
            {
                output.WriteLine($"{attribute.Name} {attribute.Values.Count}");

                foreach (string value in attribute.Values)
                {
                    output.Write($" {value}");
                }

                output.WriteLine();
            }

            output.Flush();

            RootNode.Save(output, 0);

            return true;
        }

        /// <summary>
        /// Saves the decision tree to a file with the given name. If the dave fails then FALSE is returned
        /// </summary>
        /// <param name="inFilename">The filename to save the stream to.</param>
        /// <returns>TRUE if the tree was saved.</returns>
        public bool SavePrebuiltTree(
            string inFilename
        )
        {
            using StreamWriter outputStream = new(inFilename);

            return SavePrebuiltTree(outputStream);
        }

        /// <summary>
        /// Loads a decision tree that has already been built. Returns FALSE if the load fails.
        /// </summary>
        /// <param name="input">The stream that we will read the tree from.</param>
        /// <returns>True if the tree was loaded.</returns>
        public bool LoadPrebuiltTree(
            StreamReader input
        )
        {
            Examples.Clear();
            Attributes.Clear();

            List<string> rawClassList = new();

            int numClasses = int.Parse(input.ReadLine());

            for (int i = 0; i < numClasses; ++i)
            {
                string inputString = input.ReadLine().Trim();
                rawClassList.Add(inputString);
            }

            _classes = new Classification(rawClassList);

            int numAttributes = int.Parse(input.ReadLine());
            for (int i = 0; i < numAttributes; ++i)
            {
                string[] tokens = input.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string attributeName = tokens[0];
                int numValues = int.Parse(tokens[1]);

                List<string> attributeValues =
                    input.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                Debug.Assert(
                    numValues == attributeValues.Count,
                    $"Expected {numValues}, got {attributeValues.Count}");
                LearningAttribute newAttribute = new(attributeName, attributeValues);

                Attributes.Add(newAttribute.Id, newAttribute);
            }

            RootNode.SetDecisionTree(this);
            RootNode.Load(input);

            input.Close();

            return true;
        }

        /// <summary>
        /// Loads a decision tree that has already been built. Returns FALSE if the load fails.
        /// </summary>
        /// <param name="inFilename">The file location that contains a pre-built decision tree.</param>
        /// <returns>True if the tree was loaded.</returns>
        public bool LoadPrebuiltTree(
            string inFilename
        )
        {
            using StreamReader inputStream = new(inFilename);

            return LoadPrebuiltTree(inputStream);
        }

        /// <summary>
        /// Trains the decision tree with the given data.
        /// </summary>
        public void Train()
        {
            int exampleCount = Examples.Count;

            // Give the root node all the training
            // examples we have
            RootNode.GetExampleIdentifierList().Clear();

            for (int i = 0; i < exampleCount; ++i)
            {
                RootNode.GetExampleIdentifierList().Add(i);
            }

            // Give the root node all the attributes
            // as possible splits for the data
            RootNode.GetAttributeIdentifierList().Clear();

            foreach (LearningAttribute attributesValue in Attributes.Values)
            {
                RootNode.GetAttributeIdentifierList().Add(attributesValue.Id);
            }

            // Point the root to the proper decision tree
            // and train the tree.
            RootNode.SetDecisionTree(this);
            RootNode.Train();
        }

        public DecisionTree()
        {
            RootNode = new DecisionNode();
        }

        protected DecisionNode RootNode; // The root node of the tree
    }
}
