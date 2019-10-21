//////////////////////////////////////////////////////
// File:
//	example.h
//
// Purpose:
//	Holds the definition and constants needed to
//	construct an example. Examples are used to
//	train learning mechanisms that learn
//	classification or concepts
//////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AiEngine.LearningBase
{
    /// <summary>
    /// 	Holds a set of values from each attribute
    ///		so that a classification may be generated
    /// </summary>
    public class CClassificationData
    {
        /// <summary>
        /// 	Sets the ID of the value for the given attribute.
        /// </summary>
        /// <param name="iInAttributeIndex">The index/id of the attribute to set.</param>
        /// <param name="iInNewValueIdentifier">The value of the attribute.</param>
        public void SetValueIdentifier(
            int iInAttributeIndex,
            int iInNewValueIdentifier)
        {
            int iValueIdentifierCount = m_rwValueIdentifiers.Count;

            // Create the "array" indices with a "safe" value ID
            // if we are trying to access an ID out of range
            if (iValueIdentifierCount <= iInAttributeIndex)
            {
                int iIndexDifference = (iValueIdentifierCount - iInAttributeIndex) + 1;

                while (iIndexDifference > 0)
                {
                    m_rwValueIdentifiers.Add(0);

                    --iIndexDifference;
                }
            }

            m_rwValueIdentifiers[iInAttributeIndex] = iInNewValueIdentifier;
        }

        //////////////////////////////////////////////////////
        // Function:
        //	GetValueID
        //
        // Purpose:
        //	Returns the ID of the value for the given attribute
        //////////////////////////////////////////////////////
        public int GetValueIdentifier(
            int iInAttributeIndex)
        {
            Debug.Assert(iInAttributeIndex < m_rwValueIdentifiers.Count);

            return m_rwValueIdentifiers[iInAttributeIndex];
        }

        public int GetAttributeCount() => m_rwValueIdentifiers.Count;

        public CClassificationData()
        {
            m_rwValueIdentifiers = new List<int>();
        }

        protected List<int> m_rwValueIdentifiers; // The IDs of each value
    };

    //////////////////////////////////////////////////////
    // Object:
    //	CExample
    //
    // Purpose:
    //	Holds an example. An example is formed by a set
    //	of values from each attribute and the correct
    //	classification
    //////////////////////////////////////////////////////
    public class CExample : CClassificationData
    {
        public new virtual string ToString()
        {
            var outString = new StringBuilder();

            int iAttributeCount = GetAttributeCount();

            outString.Append($"Result: {GetClassIdentifier()}");

            for (int iAttributeIndex = 0; iAttributeIndex < iAttributeCount; ++iAttributeIndex)
            {
                outString.Append($" ( {iAttributeIndex}={GetValueIdentifier(iAttributeIndex)} )");
            }

            outString.AppendLine();

            return outString.ToString();
        }

        //////////////////////////////////////////////////////
        // Mutator:
        //	SetClassID
        //
        // Purpose:
        //	Sets the ID of the classification for this example
        //////////////////////////////////////////////////////
        public void SetClassIdentifier(
            int iInNewClassIdentifier)
        {
            m_iClassIdentifier = iInNewClassIdentifier;
        }

        //////////////////////////////////////////////////////
        // Function:
        //	GetClassID
        //
        // Purpose:
        //	Returns the ID of this example's class
        //////////////////////////////////////////////////////
        public int GetClassIdentifier() => m_iClassIdentifier;

        public CExample()
        {
            m_iClassIdentifier = 0;
        }

        protected int m_iClassIdentifier;  // The ID of the class for this example
    }
}
