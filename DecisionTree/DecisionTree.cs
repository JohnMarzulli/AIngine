using AiEngine.LearningBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AiEngine.DecisionTree
{
    /// <summary>
    ///     Node object used to train the decision tree.
    /// </summary>
    public class CDTNode
    {
        /// <summary>
        ///     Prints the given number of tabs to the console.
        //	    Used by CDTNode::Print
        /// </summary>
        /// <param name="inOutputStream">The stream that is being outputted to.</param>
        /// <param name="iNumTabs">The number of tabs to add to the stream.</param>
        private void PrintTabs(
            StreamWriter inOutputStream,
            int iNumTabs = 0)
        {
            while (iNumTabs > 0)
            {
                inOutputStream.Write("    ");
                --iNumTabs;
            }
        }

        //////////////////////////////////////////////////////
        // Procedure:
        //  Stream
        //
        // Purpose:
        //  Outputs a human readable text stream to the given
        //  stream offset, indented to refelect the given
        //  depth of the node.
        //////////////////////////////////////////////////////
        public void Stream(
            StreamWriter inOutputStream,
            int iInDepth = 0)
        {
            int j = (0);
            int i = (0);
            bool bIsBuiltFromExamples = m_pDecisionTree.GetNumExamples() > 0;

            PrintTabs(inOutputStream, iInDepth);

            if (bIsBuiltFromExamples)
            {
                inOutputStream.WriteLine($"Data set had an entropy of {m_fEntropy}");
            }

            if (bIsBuiltFromExamples && m_rwRemainingAttributeIDs.Any() && m_rwInformationGain.Any())
            {
                PrintTabs(inOutputStream, iInDepth);

                inOutputStream.Write($"Node had {m_rwRemainingAttributeIDs.Count()} attributes to choose from with gains of");

                for (i = 0; i < m_rwRemainingAttributeIDs.Count(); i++)
                {
                    int attributeID = (m_rwRemainingAttributeIDs[i]);

                    inOutputStream.Write($" {m_pDecisionTree.GetAttribute(attributeID).GetName()}:{m_rwInformationGain[attributeID]}");
                }

                inOutputStream.WriteLine();
            }

            PrintTabs(inOutputStream, iInDepth);

            if (!m_bIsLeaf)
            {
                inOutputStream.WriteLine();
                PrintTabs(inOutputStream, iInDepth);
                inOutputStream.WriteLine($"Split on attribute {m_pDecisionTree.GetAttribute(m_iAttributeID).GetName()}");

                for (i = 0; i < m_pDecisionTree.GetAttribute(m_iAttributeID).GetNumValues(); ++i)
                {
                    PrintTabs(inOutputStream, iInDepth);
                    inOutputStream.WriteLine($"Value {m_pDecisionTree.GetAttribute(m_iAttributeID).GetValue(i)}");

                    m_rwChildren[i].Stream(inOutputStream, (iInDepth + 1));
                }
            }
            else
            {
                inOutputStream.WriteLine($"Class = {m_pDecisionTree.GetClass(m_iClass)}");
            }

            if (m_rwExampleIDs.Any())
            {
                PrintTabs(inOutputStream, iInDepth);
                inOutputStream.WriteLine("Examples:");
            }

            for (j = 0; j < m_rwExampleIDs.Count(); j++)
            {
                CExample curExample = m_pDecisionTree.GetExample(m_rwExampleIDs[j]);

                PrintTabs(inOutputStream, iInDepth);
                inOutputStream.Write($"Class: {m_pDecisionTree.GetClass(curExample.GetClassIdentifier())}");

                for (i = 0; i < m_pDecisionTree.GetNumAttributes(); i++)
                {
                    inOutputStream.Write($" {m_pDecisionTree.GetAttribute(i).GetName()}:{m_pDecisionTree.GetAttribute(i).GetValue(curExample.GetValueIdentifier(i))}");
                }

                inOutputStream.WriteLine();
            }
        }

        //////////////////////////////////////////////////////
        // Function:
        //  SaveToFile
        //
        // Purpose:
        //  Saves the built decision tree node to the given
        //  file stream. Returns FALSE in case of error
        //////////////////////////////////////////////////////
        public bool SaveToFile(
            StreamWriter pInOutputFile,
            int iInDepth)
        {
            if (pInOutputFile == null)
            {
                return false;
            }

            if (!m_rwChildren.Any())
            {
                pInOutputFile.Write(m_szOutcomeKeyword);
                pInOutputFile.WriteLine($" {m_pDecisionTree.GetClass(m_iClass)}");
            }
            else
            {
                pInOutputFile.Write(m_szSplitKeyword);
                pInOutputFile.WriteLine($" {m_pDecisionTree.GetAttribute(m_iAttributeID).GetName()}");

                for (var iChildIndex = 0; iChildIndex < m_rwChildren.Count(); ++iChildIndex)
                {
                    pInOutputFile.WriteLine($" {m_pDecisionTree.GetAttribute(m_iAttributeID).GetValue(iChildIndex)}");

                    // Make sure that the IO happens in the order we want.
                    // By flushing now we insure that the child's IO does not
                    // get outputted before the IO that just occurred
                    pInOutputFile.Flush();

                    m_rwChildren[iChildIndex].SaveToFile(pInOutputFile, (iInDepth + 1));
                }
            }

            return true;
        }

        //////////////////////////////////////////////////////
        // Function:
        //  LoadFromFile
        //
        // Purpose:
        //  Loads the node from the given IO file.
        //  Returns FALSE in case of error.
        //////////////////////////////////////////////////////	
        public bool LoadFromFile(
            StreamReader pInInputFile)
        {
            m_iClass = 0;
            m_iAttributeID = 0;
            m_fEntropy = 0.0f;

            m_rwInformationGain.Clear();
            m_rwExampleIDs.Clear();
            m_rwChildren.Clear();

            string inputLine = pInInputFile.ReadLine().Trim();
            string[] tokens = inputLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens == null || tokens.Length != 2)
            {
                return false;
            }

            string szNodeType = tokens[0].Trim();
            string szAttributeName = tokens[1].Trim();

            if (string.Compare(m_szSplitKeyword, szNodeType, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                int iAttributeIdentifier = 0;

                if (m_pDecisionTree.GetAttributeIdentifier(szAttributeName, ref iAttributeIdentifier))
                {
                    int iNumChildren = m_pDecisionTree.GetAttribute(iAttributeIdentifier).GetNumValues();
                    m_iAttributeID = iAttributeIdentifier;

                    int iChildIndex = 0;
                    // Create the data first since we could read the
                    // tree in an arbitrary order..
                    for (iChildIndex = 0; iChildIndex < iNumChildren; ++iChildIndex)
                    {
                        var pNewNode = new CDTNode();

                        if (pNewNode == null)
                        {
                            return false;
                        }

                        pNewNode.SetDecisionTree(m_pDecisionTree);

                        m_rwChildren.Add(pNewNode);
                    }

                    m_bIsLeaf = false;
                }
                else
                {
                    return false;
                }

                foreach (CDTNode pLinkNode in m_rwChildren)
                {
                    string szAttributeValue = pInInputFile.ReadLine().Trim();

                    int iValueIdentifer = 0;

                    if (m_pDecisionTree.GetAttribute(m_iAttributeID).GetValueIdentifier(szAttributeValue, ref iValueIdentifer))
                    {
                        pLinkNode.LoadFromFile(pInInputFile);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else if (m_szOutcomeKeyword == szNodeType)
            {
                // Leaf in tree
                int iClassIdentifier = 0;

                m_bIsLeaf = true;

                if (m_pDecisionTree.GetClassIdentifier(szAttributeName, ref iClassIdentifier))
                {
                    m_iClass = iClassIdentifier;
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

        //////////////////////////////////////////////////////
        // Function:
        //  GetChildren
        //
        // Purpose:
        //  Returns the list of children nodes in the tree,
        //////////////////////////////////////////////////////
        public List<CDTNode> GetChildren() => m_rwChildren;

        //////////////////////////////////////////////////////
        // Function:
        //  GetAttributeIdentifier
        //
        // Purpose:
        //  Returns the identifier of the attribute that
        //  this node splits on.
        //////////////////////////////////////////////////////
        public int GetAttributeIdentifier() => m_iAttributeID;

        //////////////////////////////////////////////////////
        // Function:
        //  GetClassIdentifier
        //
        // Purpose:
        //  Returns the identifer of the outcome of this
        //  node.
        //////////////////////////////////////////////////////
        public int GetClassIdentifier() => m_iClass;

        //////////////////////////////////////////////////////
        // Function:
        //	GetAttributeIDList
        //
        // Purpose:
        //	Returns a list of attribute IDs that remain
        //	and may be used to determine a node split.
        //////////////////////////////////////////////////////
        public List<int> GetAttributeIdentifierList() => m_rwRemainingAttributeIDs;

        //////////////////////////////////////////////////////
        // Function:
        //	GetExampleIDList
        //
        // Purpose:
        //	Returns a list of IDs for the examples that may be
        //	used to determine the sub tree.
        //////////////////////////////////////////////////////
        public List<int> GetExampleIdentifierList() => m_rwExampleIDs;


        //////////////////////////////////////////////////////
        // Mutator:
        //	CDTNode::Train
        //
        // Purpose:
        //	Calculates the information gain for the remaining
        //	attributes, the entropy of the remaining examples
        //	and creates splits/sub-nodes
        //////////////////////////////////////////////////////
        public void Train()
        {
            // Note:
            // I need a better approach to situations where noe more attributes
            // are left to split on, but multiple classes exist in the examples.


            // If we don't have any examples to train on, then
            // this node HAS to be a leaf. We will keep the classification
            // given to us which was the most common class from the parent node
            if (m_rwExampleIDs.Count() == 0)
            {
                m_bIsLeaf = true;

                return;
            }

            m_bIsLeaf = false;
            m_fEntropy = GetEntropy();

            //assert( m_fEntropy >= 0.0f );
            //assert( m_fEntropy <= ( (float)m_pDecisionTree->GetNumClasses() - 1.0f ) );

            // If we don't have any entropy, then go ahead and treat this like a leaf
            // since we don't have any reason to calculate any more
            if (m_fEntropy == 0.0f)
            {
                m_bIsLeaf = true;
                m_iClass = m_pDecisionTree.GetExample(m_rwExampleIDs[0]).GetClassIdentifier();

                return;
            }

            // Calculate the gains for the attributes

            int iAttributeID = 0;
            int i = 0;
            int iBestClassCount = 0;                             // The number of times the most common class appears
            int iBestAttributeID = m_rwRemainingAttributeIDs[0]; // Treat the first attribute initially as the best
            float fBestGain = GetInformationGain(iBestAttributeID); // Save the information gain from the first attribute

            m_rwInformationGain = new List<float>();

            for (i = 0; i < m_pDecisionTree.GetNumAttributes(); ++i)
            {
                m_rwInformationGain.Add(0.0f);
            }

            // Put the gain we just calculated into the proper place
            m_rwInformationGain[iBestAttributeID] = fBestGain;

            // Find the information gain for each attribute and store it
            // while keeping track of the best gain and the attribute
            // that it goes with.
            int iRemainingAttributeCount = m_rwRemainingAttributeIDs.Count();
            for (i = 1; i < iRemainingAttributeCount; ++i)
            {
                iAttributeID = m_rwRemainingAttributeIDs[i];
                m_rwInformationGain[iAttributeID] = GetInformationGain(iAttributeID);

                // If we find a better gain, store it
                // and remember which attribute it came from
                if (m_rwInformationGain[iAttributeID] > fBestGain)
                {
                    fBestGain = m_rwInformationGain[iAttributeID];
                    iBestAttributeID = iAttributeID;
                }
            }

            // Set the class counts to 0
            m_rwClassCount = new List<int>();

            int iTreeClassCount = m_pDecisionTree.GetNumClasses();
            for (i = 0; i < iTreeClassCount; ++i)
            {
                m_rwClassCount.Add(0);
            }

            // Store the attribute this splits on
            m_iAttributeID = iBestAttributeID;

            // Modify the list of attributes to give to the new children
            List<int> rwNewAttributeList = m_rwRemainingAttributeIDs;

            // If we don't have any attributes left to split on
            // then this node has to be a leaf, so we need to
            // find the most common class for the examples
            // and make that the node's classification
            m_bIsLeaf = !m_rwRemainingAttributeIDs.Any();

            // If we are not a leaf, then generate the children nodes
            // and give them a list of remaining attributes they can split on
            if (!m_bIsLeaf)
            {
                rwNewAttributeList = (from attributeId in rwNewAttributeList where attributeId != m_iAttributeID select attributeId).ToList();

                // Create the sub-nodes
                CAttribute attributeToSplitOn = m_pDecisionTree.GetAttribute(m_iAttributeID);
                int iAttributeValueCount = attributeToSplitOn.GetNumValues();

                for (i = 0; i < iAttributeValueCount; ++i)
                {
                    CDTNode pNewNode = new CDTNode(m_pDecisionTree, rwNewAttributeList);
                    m_rwChildren.Add(pNewNode);
                }
            }

            // Now we know the best attribute to split/branch on
            // send the examples to their appropriate child nodes
            // while finding the most common classification
            // for this node's examples
            int iExampleCount = m_rwExampleIDs.Count();

            for (i = 0; i < iExampleCount; ++i)
            {
                int iExampleID = m_rwExampleIDs[i];
                CExample curExample = m_pDecisionTree.GetExample(iExampleID);
                int iExampleClassID = curExample.GetClassIdentifier();

                // Save a tally of the occurrence of each classification
                // and which class is the most common
                ++m_rwClassCount[iExampleClassID];

                if (iBestClassCount < m_rwClassCount[iExampleClassID])
                {
                    iBestClassCount = m_rwClassCount[iExampleClassID];
                    m_iClass = iExampleClassID;
                }

                // Match the examples with the sub tree that matches the attribute's value
                if (!m_bIsLeaf)
                {
                    int iValueID = curExample.GetValueIdentifier(m_iAttributeID);

                    // Add the example into the correct sub-tree and remove it from this level
                    m_rwChildren[iValueID].GetExampleIdentifierList().Add(iExampleID);
                }
            }

            // If we are a leaf, then we don't have any children
            // nodes to calculate so just return back
            if (m_bIsLeaf)
            {
                return;
            }

            // No more examples should be associated with this node.
            m_rwExampleIDs.Clear();


            // Calculate all the subtrees for this node

            int iNumValues = m_pDecisionTree.GetAttribute(m_iAttributeID).GetNumValues();

            for (i = 0; i < iNumValues; ++i)
            {
                m_rwChildren[i].m_iClass = m_iClass;
                m_rwChildren[i].Train();
            }
        }

        //////////////////////////////////////////////////////
        // Mutator:
        //	SetDecisionTree
        //
        // Purpose:
        //	Sets the pointer to the decision tree of this
        //	node. Helps with determining how many values
        //	are in an attribute, etc.
        //////////////////////////////////////////////////////
        public void SetDecisionTree(CDecisionTree pInNewTree) { m_pDecisionTree = pInNewTree; }

        //////////////////////////////////////////////////////
        // Function:
        //	GetDecisionTree
        //
        // Purpose:
        //	Returns a pointer to the decision tree used
        //	to create the node.
        //////////////////////////////////////////////////////
        public CDecisionTree GetDecisionTree() => m_pDecisionTree;

        public CDTNode(CDTNode inOtherNode)
        {
            m_pDecisionTree = inOtherNode.m_pDecisionTree;
            m_fEntropy = inOtherNode.m_fEntropy;
            m_iClass = inOtherNode.m_iClass;
            m_bIsLeaf = inOtherNode.m_bIsLeaf;
            m_rwRemainingAttributeIDs = inOtherNode.m_rwRemainingAttributeIDs;
            m_rwExampleIDs = inOtherNode.m_rwExampleIDs;
            m_rwChildren = inOtherNode.m_rwChildren;
            m_rwInformationGain = inOtherNode.m_rwInformationGain;
            m_iAttributeID = inOtherNode.m_iAttributeID;
            m_rwClassCount = inOtherNode.m_rwClassCount;
        }

        public CDTNode(CDecisionTree pInNewDecisionTree, List<int> rwInRemainingAttributeList)
        {
            m_pDecisionTree = pInNewDecisionTree;
            m_fEntropy = 0.0f;
            m_iClass = 0;
            m_bIsLeaf = true;
            m_rwRemainingAttributeIDs = rwInRemainingAttributeList;
            m_rwExampleIDs = new List<int>();
            m_rwChildren = new List<CDTNode>();
            m_rwInformationGain = new List<float>();
            m_iAttributeID = 0;
            m_rwClassCount = new List<int>();
        }

        public CDTNode()
        {
            m_pDecisionTree = null;
            m_fEntropy = 0.0f;
            m_iClass = 0;
            m_bIsLeaf = true;
            m_rwRemainingAttributeIDs = new List<int>();
            m_rwExampleIDs = new List<int>();
            m_rwChildren = new List<CDTNode>();
            m_rwInformationGain = new List<float>();
            m_iAttributeID = 0;
            m_rwClassCount = new List<int>();
        }

        protected List<int> m_rwRemainingAttributeIDs;       // A list of attributes to calculate for potential splits
        protected List<int> m_rwExampleIDs;                  // A list of examples that this subtree needs to classify
        protected CDecisionTree m_pDecisionTree;                 // Pointer to the root node of the decision tree that owns the node
        protected List<CDTNode> m_rwChildren;                    // Pointers to the children nodes of the tree
        protected float m_fEntropy;                      // The entropy of the remaining samples assigned to this tree
        protected List<float> m_rwInformationGain;             // Store the information gain for each attribute
        protected int m_iAttributeID;                  // The attribute this node splits on
        protected int m_iClass;                        // The class of this node
        protected List<int> m_rwClassCount;                  // A count of the number times a classification is the correct answer from the sample data
        protected bool m_bIsLeaf;                       // Is this node a leaf?

        const string m_szOutcomeKeyword = "OUTCOME";
        const string m_szSplitKeyword = "SPLIT";

        //////////////////////////////////////////////////////
        // Function:
        //	GetEntropy
        //
        // Purpose:
        //	Returns the entropy for the examples given to this node
        //////////////////////////////////////////////////////
        protected float GetEntropy()
        {
            int numClasses = m_pDecisionTree.GetNumClasses();
            int numExamples = m_rwExampleIDs.Count();
            int[] classCounts = (numClasses > 0) ? new int[numClasses] : null;
            int i = 0;
            float entropy = 0.0f;
            float proportion = 0.0f;
            double logTwo = System.Math.Log(2.0f);

            Array.Clear(classCounts, 0, numClasses);

            foreach (var exampleId in m_rwExampleIDs)
            {
                int iExampleClass = (m_pDecisionTree.GetExample(exampleId).GetClassIdentifier());

                ++classCounts[iExampleClass];
            }

            for (i = 0; i < numClasses; ++i)
            {
                // Log(0) will return NAN
                // and 0*log2(0) should be equal to 0
                if (classCounts[i] > 0)
                {
                    // The proportion is the number of times the class appears
                    // in the training data divided by the size of the training data
                    proportion = ((float)classCounts[i] / (float)numExamples);
                    entropy += (float)((-proportion) * (Math.Log(proportion) / logTwo));
                }
            }

            return entropy;
        }

        //////////////////////////////////////////////////////
        // Function:
        //	GetEntropy
        //
        // Purpose:
        //	Returns the entropy for the value in the given attribute
        //////////////////////////////////////////////////////
        protected float GetEntropy(
            int iInAttributeID,
            int iInValueID)
        {
            // This is derived from the normal entropy function
            // The two functions could be combined if they took a
            // list of examples to calculate the entropy to, but
            // this is the "faster" approach.
            int numClasses = m_pDecisionTree.GetNumClasses();
            int numExamples = m_rwExampleIDs.Count();
            int[] classCounts = (numClasses > 0) ? new int[numClasses] : null;
            int i = 0;
            int valueCount = 0;
            float entropy = 0.0f;
            float proportion = 0.0f;
            double logTwo = System.Math.Log(2.0f);
            CExample curExample;

            Array.Clear(classCounts, 0, numClasses);

            foreach (var exampleId in m_rwExampleIDs)
            {
                curExample = m_pDecisionTree.GetExample(exampleId);

                if (curExample.GetValueIdentifier(iInAttributeID) == iInValueID)
                {
                    ++classCounts[curExample.GetClassIdentifier()];
                    ++valueCount;
                }
            }

            for (i = 0; (i < numClasses) && (valueCount > 0); ++i)
            {
                if (classCounts[i] > 0)
                {
                    proportion = (float)classCounts[i] / (float)valueCount;
                    entropy += (float)((-proportion) * (Math.Log(proportion) / logTwo));
                }
            }

            return entropy;
        }

        //////////////////////////////////////////////////////
        // Function:
        //	GetProportion
        //
        // Purpose:
        //	Returns the proportion used the in the information
        //	gain formula. This is the number of times
        //	the value appears divided by number of samples.
        //////////////////////////////////////////////////////
        protected float GetProportion(
            int iInAttributeID,
            int iInValueID)
        {
            int iCount = 0;
            CExample curExample;

            foreach (var exampleId in m_rwExampleIDs)
            {
                curExample = m_pDecisionTree.GetExample(exampleId);
                iCount = ((int)curExample.GetValueIdentifier(iInAttributeID) == iInValueID) ? iCount + 1 : iCount;
            }

            return (float)iCount / (float)m_rwExampleIDs.Count();
        }

        //////////////////////////////////////////////////////
        // Function:
        //	GetInformationGain
        //
        // Purpose:
        //	Returns the information gain for the given attribute
        //////////////////////////////////////////////////////
        protected float GetInformationGain(
            int iInAttributeIdentifer)
        {
            // Information gain is the entropy of the examples minus the
            // sum of the entropy of each value in the atttribute times its proportion

            float fSum = 0.0f;
            int iAttributeCount = ((m_pDecisionTree != null) ? m_pDecisionTree.GetAttribute(iInAttributeIdentifer).GetNumValues() : 0);

            // Sum the entropy for all the values
            for (var i = 0; i < iAttributeCount; ++i)
            {
                float fValueProportion = (GetProportion(iInAttributeIdentifer, i));
                float fValueEntropy = (GetEntropy(iInAttributeIdentifer, i));

                fSum += (fValueProportion * fValueEntropy);
            }

            return (m_fEntropy - fSum);
        }
    }

    //////////////////////////////////////////////////////
    // Object
    //	CDecisionTree
    //
    // Purpose:
    //	Base class for a generic decision tree based on
    //	the ID3 algorithm.
    //////////////////////////////////////////////////////
    public class CDecisionTree : CConceptLearner
    {
        //////////////////////////////////////////////////////
        // Function:
        //	Classify
        //
        // Purpose:
        //	Returns the classification of the given data.
        //
        // Note:
        //	Data is given as an example object, but the
        //	classification calculated is not placed
        //	into the query object.
        //////////////////////////////////////////////////////
        public override string Classify(
            CClassificationData inQuery)
        {
            var szOutClassification = "UNKNOWN";
            CDTNode pNodeToInspect = new CDTNode(m_decisionTreeRootNode);

            while (pNodeToInspect != null)
            {
                int iChildCount = pNodeToInspect.GetChildren().Count();

                if (iChildCount == 0)
                {
                    szOutClassification = m_rwClasses[pNodeToInspect.GetClassIdentifier()];

                    break;
                }
                else
                {
                    int iAttributeID = pNodeToInspect.GetAttributeIdentifier();
                    int iValueID = inQuery.GetValueIdentifier(iAttributeID);

                    pNodeToInspect = pNodeToInspect.GetChildren()[iValueID];
                }
            }

            return szOutClassification;
        }

        //////////////////////////////////////////////////////
        // Function:
        //  SavePrebuiltTree
        //
        // Purpose:
        //  Saves the decision tree to a file with the
        //  given name. If the dave fails then FALSE
        //  is returned
        //////////////////////////////////////////////////////
        public bool SavePrebuiltTree(
            string szInFilename)
        {
            var output = new StreamWriter(szInFilename);

            int index;

            int iClassCount = m_rwClasses.Count();
            int iAttributeCount = m_rwAttributes.Count();

            output.WriteLine(iClassCount);

            for (index = 0; index < iClassCount; ++index)
            {
                output.WriteLine(m_rwClasses[index]);
            }

            output.WriteLine(iAttributeCount);
            for (index = 0; index < iAttributeCount; ++index)
            {
                int iAttributeValueCount = (m_rwAttributes[index].GetNumValues());

                output.WriteLine($"{m_rwAttributes[index].GetName()} {iAttributeValueCount}");

                for (int value = 0; value < iAttributeValueCount; ++value)
                {
                    output.Write($" {m_rwAttributes[index].GetValue(value)}");
                }

                output.WriteLine();
            }

            output.Flush();

            m_decisionTreeRootNode.SaveToFile(output, 0);

            output.Close();

            return true;
        }

        //////////////////////////////////////////////////////
        // Function:
        //  LoadPrebuiltTree
        //
        // Purpose:
        //  Loads a decision tree that has already been
        //  built. Returns FALSE if the load fails.
        //////////////////////////////////////////////////////
        public bool LoadPrebuiltTree(
            string szInFilename)
        {
            using (var input = new StreamReader(szInFilename))
            {
                int i = 0;
                int j = 0;
                int iNumClasses = 0;
                int iNumAttributes = 0;
                int numValues = 0;

                string szInputString;
                string szAttributeName;
                string szAttributeValue;

                m_rwClasses.Clear();
                m_rwExamples.Clear();
                m_rwAttributes.Clear();

                //fscanf(input, "%d", &iNumClasses);
                iNumClasses = int.Parse(input.ReadLine());
                for (i = 0; i < iNumClasses; ++i)
                {
                    //fscanf(input, "%s", szInputString);
                    szInputString = input.ReadLine().Trim();
                    m_rwClasses.Add(szInputString);
                }

                //fscanf(input, "%d", &iNumAttributes);
                iNumAttributes = int.Parse(input.ReadLine());
                for (i = 0; i < iNumAttributes; ++i)
                {
                    //fscanf(input, "%s %d", szAttributeName, &numVals);
                    string[] tokens = input.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    szAttributeName = tokens[0];
                    numValues = int.Parse(tokens[1]);

                    var newAttribute = new CAttribute(szAttributeName);

                    for (j = 0; j < numValues; ++j)
                    {
                        //fscanf(input, "%s", szAttributeValue);
                        szAttributeValue = input.ReadLine().Trim();
                        newAttribute.AddValue(szAttributeValue);
                    }

                    m_rwAttributes.Add(newAttribute);
                }

                m_decisionTreeRootNode.SetDecisionTree(this);
                m_decisionTreeRootNode.LoadFromFile(input);

                input.Close();
            }

            return true;
        }

        //////////////////////////////////////////////////////
        // Mutator:
        //	Train
        //
        // Purpose:
        //	Trains the decision tree with the given data.
        //////////////////////////////////////////////////////
        public void Train()
        {
            int i = 0;
            int iExampleCount = m_rwExamples.Count();
            int iAttributeCount = m_rwAttributes.Count();

            // Give the root node all the training
            // examples we have
            m_decisionTreeRootNode.GetExampleIdentifierList().Clear();

            for (i = 0; i < iExampleCount; ++i)
            {
                m_decisionTreeRootNode.GetExampleIdentifierList().Add(i);
            }

            // Give the root node all the attributes
            // as possible splits for the data
            m_decisionTreeRootNode.GetAttributeIdentifierList().Clear();

            for (i = 0; i < iAttributeCount; ++i)
            {
                m_decisionTreeRootNode.GetAttributeIdentifierList().Add(i);
            }

            // Point the root to the proper decision tree
            // and train the tree.
            m_decisionTreeRootNode.SetDecisionTree(this);
            m_decisionTreeRootNode.Train();
        }

        public CDecisionTree()
        {
            m_decisionTreeRootNode = new CDTNode();
        }

        protected CDTNode m_decisionTreeRootNode;    // The root node of the tree
    }
}
