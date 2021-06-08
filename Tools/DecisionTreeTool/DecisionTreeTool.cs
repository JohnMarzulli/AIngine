using System;
using System.Linq;
using AiEngine.DecisionTree;
using AiEngine.LearningBase;

public class DecisionTreeTool
{
    public class Decision
    {
        public string Outcome { get; init; }

        public string NoSetter { get; }

        private DateTime _actualTime;

        public DateTime TimeOfDecision {
            get => _actualTime;

            init
            {
                _actualTime = value;

                if (value.Year < 2016)
                {
                    _actualTime = value.AddYears(2016);
                }
            }
        }

        /*
        // Both will not compile.
        public void SetOutcome(
            string whatAreYouDoing
        )
        {
            Outcome = whatAreYouDoing;
            NoSetter = "And here";
        }
        */

        public Decision(
            string outcome
        )
        {
            Outcome = outcome?.Trim() ?? string.Empty;
            NoSetter = "Here";
        }
    }

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
        int successCount = 0;
        int failedCount = 0;

        Console.WriteLine("Starting self test.");

        DecisionTree testTree = new();

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
