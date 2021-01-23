using System.IO;
using System.Linq;

namespace AiEngine.DecisionTree
{
    public static class StreamUtilities
    {
        /// <summary>
        ///     Prints the given number of tabs to the console.
        ///	    Used by CDTNode::Print
        /// </summary>
        /// <param name="outputStream">The stream that is being outputted to.</param>
        /// <param name="numberOfTabs">The number of tabs to add to the stream.</param>
        public static void PrintTabs(
            in StreamWriter outputStream,
            in int numberOfTabs = 0
        )
        {
            string tabbedString = string.Concat(Enumerable.Repeat("    ", numberOfTabs));

            outputStream?.Write(tabbedString);
        }
    }
}