using System;
using System.Linq;
using AiEngine.DecisionTree;
using AiEngine.LearningBase;

public class DecisionTreeTool
{
    public static int Main(string[] args)
    {
        if (!args.Any())
        {
            Console.Error.WriteLine("Usage:  decision file.examples");

            return -1;
        }

        bool passed = args.Aggregate(
            true,
            (current, exampleFile) => current & TestComplex(exampleFile));

        return passed ? 0 : -1;
    }

    private static bool TestComplex(
        string trainingDataFilename
    )
    {
        var successCount = 0;
        var failedCount = 0;

        Console.WriteLine("Starting self test.");

        var testTree = new DecisionTree();

        bool outIsTestSuccessful = testTree.LoadTrainingData(trainingDataFilename);

        if (outIsTestSuccessful)
        {
            Console.WriteLine($"Starting self test using example file `{trainingDataFilename}`");
            Console.WriteLine("\nTraining");
            testTree.Train();
            Console.WriteLine("Resulting tree:");
            Console.WriteLine(testTree);


            foreach (Example example in testTree.Examples)
            {
                string correctAnswer = testTree.GetClass(example.ClassIdentifier);
                string calculatedAnswer = testTree.Classify(example);

                bool isExampleMathWithClassification = correctAnswer == calculatedAnswer;
                successCount += isExampleMathWithClassification ? 1 : 0;
                failedCount += isExampleMathWithClassification ? 0 : 1;

                Console.WriteLine("----");
                Console.WriteLine($"Example has outcome of '{correctAnswer}'");
                Console.WriteLine($"Classified example '{example.ToString()}' as class `{calculatedAnswer}`");
                Console.WriteLine($"SELF TEST {(isExampleMathWithClassification ? "SUCCEEDED" : "FAILED")}!");
            }

            Console.WriteLine($"\nDone. {successCount} examples matched their classification, {failedCount} did not.");
            Console.WriteLine($"Saving to {trainingDataFilename}.dts");

            string outputFileName = $"{trainingDataFilename}.dts";

            testTree.SavePrebuiltTree($"{outputFileName}.reprocessed");

            Console.WriteLine("Finished saving. Now attempting load!");

            bool bIsLoadSuccess = testTree.LoadPrebuiltTree(outputFileName);

            Console.WriteLine($"Save = {bIsLoadSuccess}");
            Console.WriteLine("Loaded tree");
            Console.WriteLine(testTree);
        }
        else
        {
            Console.Error.WriteLine($"ERROR - Unable to open test file '{trainingDataFilename}'");
        }

        return outIsTestSuccessful;
    }
}
