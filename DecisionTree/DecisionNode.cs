using AiEngine.LearningBase;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace AiEngine.DecisionTree
{
    /// <summary>
    /// Node object used to train the decision tree.
    /// </summary>
    public class DecisionNode
    {
        /// <summary>
        /// Saves the built decision tree node to the given
        /// file stream. Returns FALSE in case of error
        /// </summary>
        /// <param name="outputStream">The stream to save the node to.</param>
        /// <param name="treeDepth">The depth of the node to save in relation to the rest of the tree.</param>
        /// <returns>True if the node was saved.</returns>
        public bool Save(
            in StreamWriter outputStream,
            in int treeDepth
        )
        {
            if (outputStream == null)
            {
                return false;
            }

            if (!Children.Any())
            {
                outputStream.Write(OutcomeKeyword);
                outputStream.WriteLine($" {DecisionTree.GetClass(ClassId)}");
            }
            else
            {
                outputStream.Write(SplitKeyword);
                outputStream.WriteLine($" {DecisionTree.GetAttribute(AttributeId).Name}");

                foreach ((AttributeValueId valueId, DecisionNode child) in Children)
                {
                    outputStream.WriteLine($" {DecisionTree.GetAttribute(AttributeId).GetValue(valueId)}");

                    // Make sure that the IO happens in the order we want.
                    // By flushing now we insure that the child's IO does not
                    // get outputted before the IO that just occurred
                    outputStream.Flush();

                    child.Save(outputStream, (treeDepth + 1));
                }
            }

            return true;
        }

        /// <summary>
        /// Loads the node from the given IO file.
        /// Returns FALSE in case of error.
        /// </summary>
        /// <param name="inputStream">The stream to load a node from.</param>
        /// <returns>True if the node was loaded.</returns>
        public bool Load(
            [NotNull] StreamReader inputStream
        )
        {
            if (inputStream == null)
            {
                return false;
            }

            InformationGain.Clear();
            ExampleIds.Clear();
            Children.Clear();

            string inputLine = inputStream?.ReadLine()?.Trim();
            string[] tokens = inputLine?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens == null || tokens.Length != 2)
            {
                return false;
            }

            string nodeType = tokens[0].Trim();
            string attributeName = tokens[1].Trim();

            if (string.Compare(SplitKeyword, nodeType, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                AttributeId foundId = DecisionTree.GetAttributeIdentifier(attributeName);

                if (foundId != null)
                {
                    AttributeId = foundId;

                    // Create the data first since we could read the
                    // tree in an arbitrary order..
                    foreach (string childAttrValue in DecisionTree.GetAttribute(AttributeId).Values)
                    {
                        AttributeValueId valueId = DecisionTree.GetAttribute(AttributeId).GetValueId(childAttrValue);
                        Children.Add(valueId, new DecisionNode(DecisionTree));
                    }

                    IsLeaf = false;
                }
                else
                {
                    return false;
                }

                foreach (DecisionNode _ in Children.Values)
                {
                    string attributeValue = inputStream.ReadLine().Trim();

                    AttributeValueId valueIdentifier = DecisionTree.GetAttribute(AttributeId).GetValueId(attributeValue);

                    if (valueIdentifier != null)
                    {
                        Children[valueIdentifier].Load(inputStream);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else if (OutcomeKeyword == nodeType)
            {
                // Leaf in tree
                ClassificationValueId classIdentifier = DecisionTree.GetClassIdentifier(attributeName);

                IsLeaf = true;

                if (classIdentifier != null)
                {
                    ClassId = classIdentifier;
                }
                else
                {
                    return false;
                }

            }
            else
            {
                // Unexpected input
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the list of children nodes in the tree,
        /// </summary>
        /// <returns>The list of children nodes in the tree,</returns>
        public List<DecisionNode> GetChildren() => Children.Values.ToList();

        /// <summary>
        /// Returns the identifier of the outcome of this node.
        /// </summary>
        /// <returns>The identifier of the outcome of this node.</returns>
        public ClassificationValueId GetClassIdentifier() => ClassId;

        /// <summary>
        /// Returns a list of attribute IDs that remain and may be used to determine a node split.
        /// </summary>
        /// <returns>The list of attribute IDs that remain and may be used to determine a node split.</returns>
        public List<AttributeId> GetAttributeIdentifierList() => RemainingAttributeIds;

        /// <summary>
        /// Returns a list of IDs for the examples that may be used to determine the sub tree.
        /// </summary>
        /// <returns>Returns a list of IDs for the examples that may be used to determine the sub tree.</returns>
        public List<int> GetExampleIdentifierList() => ExampleIds;

        /// <summary>
        /// Add an example to the list that this node will consider for splitting on.
        /// </summary>
        /// <param name="exampleId">The Id of the example to add to the list to learn from.</param>
        public void AddExample(
            in int exampleId
        )
        {
            ExampleIds.Add(exampleId);
            ExampleIds = ExampleIds.Distinct().ToList();
        }

        /// <summary>
        /// Calculates the information gain for the remaining attributes,
        /// the entropy of the remaining examples and creates splits/sub-nodes
        /// </summary>
        public void Train()
        {
            // Note:
            // I need a better approach to situations where no more attributes
            // are left to split on, but multiple classes exist in the examples.

            // If we don't have any examples to train on, then
            // this node HAS to be a leaf. We will keep the classification
            // given to us which was the most common class from the parent node
            if (!ExampleIds.Any() || !RemainingAttributeIds.Any())
            {
                IsLeaf = true;

                return;
            }

            IsLeaf = false;

            // Debug.Assert( Entropy >= 0.0f );
            // Debug.Assert( Entropy <= ( (float)DecisionTree.GetNumClasses() - 1.0f ) );

            // If we don't have any entropy, then go ahead and treat this like a leaf
            // since we don't have any reason to calculate any more
            if (Entropy <= 0.0f)
            {
                IsLeaf = true;
                ClassId = DecisionTree.GetExample(ExampleIds[0]).ClassIdentifier;

                return;
            }

            // Find the information gain for each attribute and store it
            // while keeping track of the best gain and the attribute
            // that it goes with.
            Dictionary<AttributeId, float> gains = RemainingAttributeIds.ToDictionary(
                key => key,
                key => GetInformationGain(key));
            InformationGain = gains.OrderByDescending(
                key =>
                    key.Value).ToDictionary(
                key => key.Key,
                value => value.Value);

            // The count of examples by the outcome.
            Dictionary<ClassificationValueId, int> outcomeCounts = ExampleIds.GroupBy(
                key =>
                    DecisionTree.Examples[key].ClassIdentifier,
                value =>
                    DecisionTree.Examples[value].ClassIdentifier).ToDictionary(
                key => key.Key,
                value => value.ToList().Count);

            // Store the attribute this splits on
            AttributeId = InformationGain.Keys.First();

            // If we don't have any attributes left to split on
            // then this node has to be a leaf, so we need to
            // find the most common class for the examples
            // and make that the node's classification
            IsLeaf = !RemainingAttributeIds.Any();

            // If we are a leaf, then we don't have any children
            // nodes to calculate so just return back
            if (IsLeaf)
            {
                return;
            }

            // If we are not a leaf, then generate the children nodes
            // and give them a list of remaining attributes they can split on
            List<AttributeId> newAttributeList = RemainingAttributeIds.Where(
                potentialAttributeId =>
                    !potentialAttributeId.Equals(AttributeId)).ToList();

            ClassId = outcomeCounts.OrderByDescending(value => value.Value).First().Key;

            // Now we know the best attribute to split/branch on
            // send the examples to their appropriate child nodes
            // while finding the most common classification
            // for this node's examples
            foreach (AttributeValueId valueId in DecisionTree.Attributes[AttributeId].ValueIds)
            {
                List<int> examplesForValue = ExampleIds.Where(
                    exampleId =>
                        DecisionTree.Examples[exampleId].GetValueIdentifier(AttributeId).Equals(valueId)).ToList();

                if (!Children.ContainsKey(valueId))
                {
                    Children.Add(valueId, new DecisionNode(DecisionTree, newAttributeList));
                }

                Children[valueId].ExampleIds = examplesForValue;
                Children[valueId].ClassId = ClassId; // $TODO - Needed?
            }

            // Calculate all the subtrees for this node
            foreach (DecisionNode child in Children.Values)
            {
                child.Train();
            }
        }

        /// <summary>
        /// Sets the pointer to the decision tree of this
        ///	node. Helps with determining how many values
        ///	are in an attribute, etc.
        /// </summary>
        /// <param name="newTree">The new tree that this node belongs to.</param>
        public void SetDecisionTree(
            DecisionTree newTree
        )
        {
            DecisionTree = newTree;
        }

        public DecisionNode(
            in DecisionNode nodeToCopy
        )
        {
            DecisionTree = nodeToCopy.DecisionTree;
            ClassId = nodeToCopy.ClassId;
            IsLeaf = nodeToCopy.IsLeaf;
            RemainingAttributeIds = nodeToCopy.RemainingAttributeIds;
            ExampleIds = nodeToCopy.ExampleIds;
            Children = nodeToCopy.Children;
            InformationGain = nodeToCopy.InformationGain;
            AttributeId = nodeToCopy.AttributeId;

            _entropy = nodeToCopy._entropy;
        }

        public DecisionNode(
            DecisionTree pInNewDecisionTree,
            in List<AttributeId> remainingAttributeIds
        )
        {
            DecisionTree = pInNewDecisionTree;
            RemainingAttributeIds = remainingAttributeIds;
            Children = new Dictionary<AttributeValueId, DecisionNode>();
        }

        public DecisionNode()
        {
            DecisionTree = null;
            Children = new Dictionary<AttributeValueId, DecisionNode>();
        }

        public DecisionNode(
            in DecisionTree newTree
        )
        {
            DecisionTree = newTree;
            Children = new Dictionary<AttributeValueId, DecisionNode>();
        }

        private float? _entropy;

        /// <summary>
        /// How much entropy is there in in the examples
        /// this node is responsible for?
        /// </summary>
        public float Entropy
        {
            get
            {
                _entropy ??= GetEntropy(
                    DecisionTree.GetNumClasses(),
                    ExampleIds.Select(id => DecisionTree.GetExample(id)).ToList());

                return _entropy.Value;
            }
        }

        protected List<AttributeId> RemainingAttributeIds = new(); // A list of attributes to calculate for potential splits
        protected List<int> ExampleIds = new(); // A list of examples that this subtree needs to classify
        protected DecisionTree DecisionTree; // Pointer to the root node of the decision tree that owns the node

        public Dictionary<AttributeValueId, DecisionNode> Children { get; init; }

        protected Dictionary<AttributeId, float> InformationGain = new(); // Store the information gain for each attribute
        public AttributeId AttributeId { get; private set; } // The attribute this node splits on
        protected ClassificationValueId ClassId; // The class of this node

        protected bool IsLeaf = true; // Is this node a leaf?

        private const string OutcomeKeyword = "OUTCOME";
        private const string SplitKeyword = "SPLIT";

        public static float GetEntropy(
            in int numberOfClasses,
            in List<Example> examples
        )
        {
            if (numberOfClasses <= 0
                || !examples.Any())
            {
                return 0.0f;
            }

            int numExamples = examples.Count;
            float entropy = 0.0f;
            double logTwo = Math.Log(2.0f);

            // Find out how many examples result in the same decision class.
            // The key is the classId, the value is the number of times that class is the answer
            Dictionary<ClassificationValueId, int> counts = examples.GroupBy(
                example =>
                    example.ClassIdentifier).ToDictionary(
                key => key.Key,
                value => value.ToList().Count);

            foreach (int count in counts.Values)
            {
                // Log(0) will return NAN
                // and 0*log2(0) should be equal to 0
                if (count <= 0)
                {
                    continue;
                }

                // The proportion is the number of times the class appears
                // in the training data divided by the size of the training data
                float proportion = (count / (float)numExamples);
                entropy += (float)((-proportion) * (Math.Log(proportion) / logTwo));
            }

            return entropy;
        }

        /// <summary>
        /// Returns the entropy for the value in the given attribute
        /// </summary>
        /// <param name="attributeId">The attribute to calculate the entropy of.</param>
        /// <param name="valueId">The value Id to calculate the entropy of.</param>
        /// <returns>The remaining entropy in this node / subtree.</returns>
        protected float GetEntropy(
            in AttributeId attributeId,
            in AttributeValueId valueId
        )
        {
            // This is derived from the normal entropy function
            // The two functions could be combined if they took a
            // list of examples to calculate the entropy to, but
            // this is the "faster" approach.
            int numClasses = DecisionTree.GetNumClasses();

            if (numClasses <= 0)
            {
                return 0.0f;
            }

            Dictionary<ClassificationValueId, int> classCounts = DecisionTree.Classes.ValueIds.ToDictionary(
                classId => classId,
                classId => 0);

            int valueCount = 0;
            float entropy = 0.0f;
            double logTwo = Math.Log(2.0f);

            foreach (int exampleId in ExampleIds)
            {
                Example curExample = DecisionTree.GetExample(exampleId);

                if (curExample.GetValueIdentifier(attributeId).Equals(valueId))
                {
                    ++classCounts[curExample.ClassIdentifier];
                    ++valueCount;
                }
            }

            if (valueCount <= 0)
            {
                return entropy;
            }

            foreach (int classCount in classCounts.Values)
            {
                if (classCount <= 0)
                {
                    continue;
                }

                float proportion = classCount / (float)valueCount;
                entropy += (float)((-proportion) * (Math.Log(proportion) / logTwo));
            }

            return entropy;
        }

        /// <summary>
        /// Returns the proportion used the in the information
        ///	gain formula. This is the number of times
        ///	the value appears divided by number of samples.
        /// </summary>
        /// <param name="attributeId">The attribute that we are calculating the gain for.</param>
        /// <param name="valueId">The value that the gain is being calculated for.</param>
        /// <returns>The proportion of information gain for the attribute/value Id.</returns>
        protected float GetProportion(
            in AttributeId attributeId,
            in AttributeValueId valueId
        )
        {
            int count = 0;

            foreach (int exampleId in ExampleIds)
            {
                Example curExample = DecisionTree.GetExample(exampleId);
                count = (curExample.GetValueIdentifier(attributeId).Equals(valueId)) ? count + 1 : count;
            }

            return (float)count / (float)ExampleIds.Count;
        }

        /// <summary>
        /// Returns the information gain if we were to split on the given attribute.
        /// </summary>
        /// <param name="attributeId">The attribute to calculate the information gain of.</param>
        /// <returns>Returns the information gain for the given attribute</returns>
        public float GetInformationGain(
            in AttributeId attributeId
        )
        {
            // Information gain is the entropy of the examples minus the
            // sum of the entropy of each value in the attribute times its proportion

            float informationGainSum = 0.0f;

            // Sum the entropy for all the values
            foreach (AttributeValueId valueId in DecisionTree.GetAttribute(attributeId).ValueIds)
            {
                float fValueProportion = GetProportion(attributeId, valueId);
                float fValueEntropy = GetEntropy(attributeId, valueId);

                informationGainSum += (fValueProportion * fValueEntropy);
            }

            return (Entropy - informationGainSum);
        }
    }
}
