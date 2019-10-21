//////////////////////////////////////////////////////
// File:
//	attribute.h
//
// Purpose:
//	Holds the definition of an attribute.
//	An attribute is set of values with a common
//	element. For instance the attribute of
//	Temperature may have the values cold, warm, hot.
//////////////////////////////////////////////////////

using System.Collections.Generic;

namespace AiEngine.LearningBase
{
    /// <summary>
    /// 	Holds an attribute and it's values.
    /// </summary>
    public class CAttribute
    {
        /// <summary>
        /// 	Sets the name of the attribute.
        /// </summary>
        /// <param name="szInNewName">The new name of the attribute.</param>
        public void SetName(
            string szInNewName)
        {
            m_szName = szInNewName?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 	Returns the name of attribute.
        /// </summary>
        /// <returns>The name of this attribute.</returns>
        public string GetName() => m_szName;

        /// <summary>
        /// 	Returns the name of the value with the given ID
        /// </summary>
        /// <param name="iInValueId">The id of the value to get.</param>
        /// <returns>The name of the value with the given ID.</returns>
        public string GetValue(
            int iInValueId) => m_rwValues[iInValueId];

        /// <summary>
        /// 	Adds the given value
        /// </summary>
        /// <param name="szInNewName">The new value to add.</param>
        /// <remarks>
        /// 	Does not check for uniqueness.
        /// </remarks>
        public void AddValue(
            string szInNewName) => m_rwValues.Add(szInNewName);

        /// <summary>
        /// 	Returns the ID number of the value given the name
        /// 	via the output parameter. If the identifier can
        /// 	not be found then FALSE is returned.
        /// </summary>
        /// <param name="szInName">The name of the identifier</param>
        /// <param name="outValueId">The found ID of the identifier</param>
        /// <returns>TRUE if the ID was found.</returns>
        public bool GetValueIdentifier(
            string szInName,
            ref int outValueId)
        {
            int iNumValues = GetNumValues();
            bool outIsIdFound = false;

            for (int i = 0; (i < iNumValues) && (!outIsIdFound); ++i)
            {
                outIsIdFound = string.Compare(szInName, m_rwValues[i], System.StringComparison.InvariantCultureIgnoreCase) == 0;
                outValueId = outIsIdFound ? i : outValueId;
            }

            return outIsIdFound;
        }

        /// <summary>
        /// 	Returns the number of values that form the attribute's set.
        /// </summary>
        /// <returns>Returns the number of values that form the attribute's set.</returns>
        public int GetNumValues() => m_rwValues.Count;

        public CAttribute(
            string szInNewAttributeName)
        {
            m_szName = szInNewAttributeName?.Trim();
            m_rwValues = new List<string>();
        }

        public CAttribute()
        {
            m_szName = string.Empty;
            m_rwValues = new List<string>();
        }

        /// <summary>
        /// 	The name of this attribute
        /// </summary>
        protected string m_szName;

        /// <summary>
        /// 	The names of the attributes values
        /// </summary>
        protected List<string> m_rwValues;
    }
}
