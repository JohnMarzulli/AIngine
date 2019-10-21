using AiEngine.DecisionTree;
using AiEngine.LearningBase;
using System;
using System.Linq;

public class DecisionTreeTool
{
    public static int Main(string[] args)
    {
        if (!args.Any())
        {
            Console.Error.WriteLine("Usage:  decision myfile.examples");

            return -1;
        }

        bool passed = true;

        foreach (string exampleFile in args)
        {
            passed &= TestComplex(exampleFile);
        }

        return passed ? 0 : -1;
    }

    private static bool TestComplex(string szInFilename)
    {
        bool bOutIsTestSuccessful = false;
        int iSuccessCount = 0;
        int iFailedCount = 0;

        Console.WriteLine("Starting self test.");

        var testTree = new CDecisionTree();

        bOutIsTestSuccessful = testTree.LoadTrainingData(szInFilename);

        //	cout << "Examples:" << endl;
        //	
        //	for( unsigned int iIndex = 0; iIndex < testTree.GetNumExamples(); ++iIndex )
        //	{
        //		cout << testTree.GetExample( iIndex );
        //	}
        //	
        //	cout << endl;

        if (bOutIsTestSuccessful)
        {
            Console.WriteLine($"Starting self test using example file `{szInFilename}`");
            Console.WriteLine("\nTraining");
            testTree.Train();
            Console.WriteLine("Resulting tree:");
            Console.WriteLine(testTree);

            int iExampleCount = testTree.GetNumExamples();

            for (int iExampleIndex = 0; iExampleIndex < iExampleCount; ++iExampleIndex)
            {
                CExample example = testTree.GetExample(iExampleIndex);
                string szCorrectAnswer = testTree.GetClass(example.GetClassIdentifier());
                string szCalculatedAnswer = testTree.Classify(example);

                bool bIsExampleAMatchWithClassification = szCorrectAnswer == szCalculatedAnswer;
                iSuccessCount += bIsExampleAMatchWithClassification ? 1 : 0;
                iFailedCount += bIsExampleAMatchWithClassification ? 0 : 1;

                Console.WriteLine("----");
                Console.WriteLine($"Example has outcome of '{szCorrectAnswer}'");
                Console.WriteLine($"Classified example '{iExampleIndex}' as class `{szCalculatedAnswer}`");
                Console.WriteLine($"SELF TEST {(bIsExampleAMatchWithClassification ? "SUCCEEDED" : "FAILED")}!");
            }

            Console.WriteLine($"\nDone. {iSuccessCount} examples matched their classification, {iFailedCount} did not.");
            Console.WriteLine($"Saving to {szInFilename}.dts");

            string szOutputFileName = $"{szInFilename}.dts";

            testTree.SavePrebuiltTree(szOutputFileName);

            Console.WriteLine("Finished saving. Now attempting load!");

            bool bIsLoadSuccess = testTree.LoadPrebuiltTree(szOutputFileName);

            Console.WriteLine($"Save = {bIsLoadSuccess}");
            Console.WriteLine("Loaded tree");
            Console.WriteLine(testTree);
        }
        else
        {
            Console.Error.WriteLine($"ERROR - Unable to open test file '{szInFilename}'");
        }

        return bOutIsTestSuccessful;
    }
}
