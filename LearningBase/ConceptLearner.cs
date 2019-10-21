//////////////////////////////////////////////////////
// File:
//	conceptlearner.h
//
// Purpose:
//	Holds the common elements that concept
//	learning algorithms require
//////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AiEngine.LearningBase
{
    public abstract class CConceptLearner
    {
        const int m_iMaxInputSize = 256;

        public new virtual string ToString()
        {
            var outString = new StringBuilder();

            outString.AppendLine("--------------------------------");

            int iExampleCount = GetNumExamples();

            for (int i = 0; i < iExampleCount; ++i)
            {
                outString.Append($"CLASS:{GetClass(GetExample(i).GetClassIdentifier())}");

                int iAttributeCount = GetNumAttributes();

                for (int j = 0; j < iAttributeCount; ++j)
                {
                    var attribute = GetAttribute(j);

                    outString.Append($"{attribute.GetName()}{attribute.GetValue(GetExample(i).GetValueIdentifier(j))}");
                }

                outString.AppendLine();
            }

            outString.AppendLine("--------------------------------");

            return outString.ToString();
        }

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
        public abstract string Classify(
            CClassificationData inQuery);

        //////////////////////////////////////////////////////
        // Function:
        //	LoadTrainingData
        //
        // Purpose:
        //	Loads the training data used to classify queries.
        //////////////////////////////////////////////////////
        public bool LoadTrainingData(
            string szInFileName)
        {
            using (var pInputFile = new StreamReader(szInFileName))
            {

                int iNumClasses = 0;
                int i = 0;
                int j = 0;

                string szInputString;

                // Read in the classes
                string input = pInputFile.ReadLine();
                input = pInputFile.ReadLine();
                iNumClasses = int.Parse(input);
                //fscanf(pInputFile, "%d", &iNumClasses);

                m_rwExamples.Clear();
                m_rwAttributes.Clear();
                m_rwClasses.Clear();

                // Print out the names of the classes as we read them in
                //cout << "Read " << iNumClasses << " classes";

                // Read in the classifications
                // that we can put the data into
                for (i = 0; i < iNumClasses; ++i)
                {
                    szInputString = pInputFile.ReadLine();
                    //fscanf(pInputFile, "%s", szInputString);

                    m_rwClasses.Add(szInputString.Trim());

                    //cout << " " << m_rwClasses[i];
                }
                //cout << endl;

                // Read in the attributes
                pInputFile.ReadLine();
                //fscanf(pInputFile, "%s", szInputString);

                int iNumAttributes = int.Parse(pInputFile.ReadLine());
                //fscanf(pInputFile, "%d", &iNumAttributes);
                //cout << "Read " << iNumAttributes << " attributes\n";

                // Read in each attribute and it's values
                for (i = 0; i < iNumAttributes; ++i)
                {
                    // Check to make sure we haven't hit
                    // a bad file and EOF
                    if (pInputFile.EndOfStream)
                    {
                        return false;
                    }

                    szInputString = pInputFile.ReadLine().Trim();
                    //fscanf(pInputFile, "%s", szInputString);


                    m_rwAttributes.Add(new CAttribute());
                    m_rwAttributes[i].SetName(szInputString);

                    int iNumValues = int.Parse(pInputFile.ReadLine());
                    //fscanf(pInputFile, "%d", &iNumValues);

                    //cout << "Attribute " << m_rwAttributes[i].GetName() << " has " << iNumValues << " values:";

                    for (j = 0; j < iNumValues; ++j)
                    {
                        szInputString = pInputFile.ReadLine().Trim();
                        //fscanf(pInputFile, "%s", szInputString);
                        m_rwAttributes[i].AddValue(szInputString);

                        //cout << " " << m_rwAttributes[i].GetValue(j);
                    }

                    //cout << endl;
                }

                // We should get the keyword "examples"
                szInputString = pInputFile.ReadLine().Trim();
                //fscanf(pInputFile, "%s", szInputString);

                //cout << "EXAMPLES\n";

                // Read in the examples until we hit EOF
                // If we are running a debug build, print out
                // the example data as we read it in
                while (!pInputFile.EndOfStream)
                {
                    var newExample = new CExample();

                    szInputString = pInputFile.ReadLine().Trim();
                    //int iScanResult(fscanf(pInputFile, "%s", szInputString ) );

                    if (string.IsNullOrEmpty(szInputString))
                    {
                        break;
                    }
                    //cout << szInputString;

                    int iClassIdentifier = 0;

                    if (!GetClassIdentifier(szInputString, ref iClassIdentifier))
                    {
                        Debug.Fail("Unable to find class identifier!");
                        break;
                    }

                    newExample.SetClassIdentifier(iClassIdentifier);

                    for (i = 0; i < m_rwAttributes.Count; ++i)
                    {
                        szInputString = pInputFile.ReadLine().Trim();
                        //fscanf(pInputFile, "%s", szInputString);
                        //cout << " " << szInputString;

                        int iValueIdentifier = 0;

                        if (m_rwAttributes[i].GetValueIdentifier(szInputString, ref iValueIdentifier))
                        {
                            newExample.SetValueIdentifier(i, iValueIdentifier);
                        }
                    }

                    // cout << endl;

                    // The the newly read example into the example pool.
                    AddExample(newExample);

                    //		cout << "Read new example: " << newExample << endl;
                }

                return true;
            }
        }

        //////////////////////////////////////////////////////
        // Mutator:
        //	AddClass
        //
        // Purpose:
        //	Adds a classification to the decision tree
        //////////////////////////////////////////////////////
        public void AddClass(
            string szInNewClassification)
        {
            m_rwClasses.Add(szInNewClassification);
        }

        //////////////////////////////////////////////////////
        // Function:
        //	GetClassID
        //
        // Purpose:
        //	Returns the class ID of the given string.
        //////////////////////////////////////////////////////
        public bool GetClassIdentifier(
            string szInClassName,
            ref int outIdentifier) => FindIdByValue(m_rwClasses, szInClassName, ref outIdentifier);

        //////////////////////////////////////////////////////
        // Function:
        //	GetClass
        //
        // Purpose:
        //	Returns the name of the class with the given ID
        //////////////////////////////////////////////////////
        public string GetClass(
            int iInClassIdentifier) => m_rwClasses[iInClassIdentifier];

        //////////////////////////////////////////////////////
        // Function:
        //	GetNumClasses
        //
        // Purpose:
        //	Returns the number of classes in the decision tree.
        //////////////////////////////////////////////////////
        public int GetNumClasses() => m_rwClasses.Count;

        //////////////////////////////////////////////////////
        // Mutator:
        //	AddAttribute
        //
        // Purpose:
        //	Adds an attribute to the decision tree.
        //////////////////////////////////////////////////////
        public void AddAttribute(
            CAttribute inNewAttribute)
        {
            m_rwAttributes.Add(inNewAttribute);
        }

        //////////////////////////////////////////////////////
        // Function:
        //	GetAttributeID
        //
        // Purpose:
        //	Returns the ID of the attribute with the given name.
        //////////////////////////////////////////////////////
        public bool GetAttributeIdentifier(
            string szInAttributeName,
            ref int iOutIdentifier) => FindIdByValue((from attr in m_rwAttributes select attr.GetName()).ToList(), szInAttributeName, ref iOutIdentifier);

        //////////////////////////////////////////////////////
        // Function:
        //	GetNumAttributes
        //
        // Purpose:
        //	Returns the number of attributes used to make
        //	a decision.
        //////////////////////////////////////////////////////
        public int GetNumAttributes() => m_rwAttributes.Count;

        //////////////////////////////////////////////////////
        // Function:
        //	GetAttribute
        //
        // Purpose:
        //	Returns a pointer to the attribute with the given ID.
        //////////////////////////////////////////////////////
        public CAttribute GetAttribute(
            int iInAttributeID) => m_rwAttributes[iInAttributeID];

        //////////////////////////////////////////////////////
        // Mutator:
        //	AddExample
        //
        // Purpose:
        //	Adds an example to the pool of training data.
        //////////////////////////////////////////////////////
        public void AddExample(
            CExample inNewExample)
        {
            m_rwExamples.Add(inNewExample);
        }

        //////////////////////////////////////////////////////
        // Function:
        //	GetExample
        //
        // Purpose:
        //	Returns a copy of the example with the given ID
        //////////////////////////////////////////////////////
        public CExample GetExample(int iInIdentifier) => m_rwExamples[iInIdentifier];

        //////////////////////////////////////////////////////
        // Function:
        //	GetNumExamples
        //
        // Purpose:
        //	Returns the number of examples in the training data.
        //////////////////////////////////////////////////////
        public int GetNumExamples() => m_rwExamples.Count;

        //////////////////////////////////////////////////////
        // Constructor
        //////////////////////////////////////////////////////
        public CConceptLearner()
        {
            m_rwAttributes = new List<CAttribute>();
            m_rwExamples = new List<CExample>();
            m_rwClasses = new List<string>();
        }

        protected List<CAttribute> m_rwAttributes; // the attributes used to make a decision with
        protected List<CExample> m_rwExamples;   // the examples used from training
        protected List<string> m_rwClasses;    // The names of the classes

        private bool FindIdByValue(
            List<string> inArrayToSearch,
            string inValueToFind,
            ref int outIdentifier
        )
        {
            bool outIdentifierFound = false;

            for (int i = 0; (i < inArrayToSearch.Count()) && (!outIdentifierFound); ++i)
            {
                outIdentifierFound = string.Compare(inArrayToSearch[i], inValueToFind, System.StringComparison.InvariantCultureIgnoreCase) == 0;
                outIdentifier = outIdentifierFound ? i : outIdentifier;
            }

            // No class was found, return the proper error code.
            return outIdentifierFound;
        }
    }
}