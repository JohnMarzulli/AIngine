using AiEngine.DecisionTree;
using AiEngine.LearningBase;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DecisionTreeTests
{
    [TestClass]
    public class TestDecisionTreesAndNodes
    {
        private const string WeatherPrebuilt = @"2
yes
no
4
outlook 3
 sunny overcast rain
temperature 3
 cool mild hot
humidity 2
 normal high
wind 2
 weak strong
SPLIT humidity
 normal
OUTCOME yes
 high
SPLIT outlook
 sunny
OUTCOME no
 overcast
OUTCOME yes
 rain
SPLIT wind
 weak
OUTCOME yes
 strong
OUTCOME no";

        private const string WeatherExamples = @"classes     2 yes no
attributes  4
outlook     3 sunny overcast rain
temperature 3 cool mild hot
humidity    2 normal high
wind        2 weak strong
examples
no  sunny    hot  high   weak
no  sunny    hot  high   strong
yes overcast hot  high   weak
yes rain     mild high   weak
yes rain     cool normal weak
yes overcast cool normal strong
no  sunny    mild high   weak
yes sunny    cool normal weak
yes rain     mild normal weak
yes sunny    mild normal strong
yes overcast mild high   strong
yes overcast hot  normal weak
no  rain     mild high   strong";

        private static StreamReader GetMemoryStream(
            in string valueToStream
        )
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(valueToStream);
            writer.Flush();
            stream.Position = 0;

            return new StreamReader(stream, System.Text.Encoding.UTF8);
        }

        private static (DecisionTree, DecisionNode) SetupTest()
        {
            var newTree = new DecisionTree();

            newTree.LoadTrainingData(GetMemoryStream(WeatherExamples));

            var rootNode = new DecisionNode(newTree);

            for (var exampleId = 0; exampleId < newTree.Examples.Count; ++exampleId)
            {
                rootNode.AddExample(exampleId);
            }

            return (newTree, rootNode);
        }

        [TestMethod]
        public void TestEmptyConstructor()
        {
            var _ = new DecisionNode();
        }

        [TestMethod]
        public void TestNodeEntropyScore()
        {
            (DecisionTree _, DecisionNode rootNode) = SetupTest();


            float foundEntropy = rootNode.Entropy;
            const float minExpectedEntropy = 0.88f;
            const float maxExpectedEntropy = 0.90f;
            Assert.IsTrue(minExpectedEntropy <= foundEntropy);
            Assert.IsTrue(foundEntropy <= maxExpectedEntropy);
        }

        [TestMethod]
        public void TestEntropyScore()
        {
            (DecisionTree tree, DecisionNode _) = SetupTest();

            float foundEntropy = DecisionNode.GetEntropy(tree.GetNumClasses(), tree.Examples);

            const float minExpectedEntropy = 0.88f;
            const float maxExpectedEntropy = 0.90f;
            Assert.IsTrue(minExpectedEntropy <= foundEntropy);
            Assert.IsTrue(foundEntropy <= maxExpectedEntropy);
        }

        [DataTestMethod]
        [DataRow(0, 0.25f, 0.27f)]
        [DataRow(1, 0.14f, 0.16f)]
        [DataRow(2, 0.34f, 0.36f)]
        [DataRow(3, 0.00f, 0.02f)]
        public void TestNodeInformationGain(
            int index,
            float minExpected,
            float maxExpected
        )
        {
            (DecisionTree tree, DecisionNode rootNode) = SetupTest();

            AttributeId attributeId = tree.Attributes.Keys.ToList()[index];

            float foundGain = rootNode.GetInformationGain(attributeId);

            Assert.IsTrue(foundGain >= 0.0f);

            Assert.IsTrue(foundGain >= minExpected);
            Assert.IsTrue(foundGain <= maxExpected);
        }

        private static ClassificationData GetQuery(
            in DecisionTree tree,
            string queryString
        )
        {
            string[] tokens = queryString?.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            Debug.Assert(tree != null);
            Debug.Assert(tokens != null);
            Debug.Assert(tokens.Length == tree.Attributes.Count);

            var outQuery = new ClassificationData();

            for (var index = 0; index < tokens.Length; ++index)
            {
                string valueName = tokens[index].Trim();
                (LearningAttribute attribute, AttributeValueId foundValueId) = FindAttributeAndValue(tree, valueName);
                Assert.IsNotNull(attribute);
                Assert.IsNotNull(foundValueId);

                Assert.IsFalse(outQuery.Values.ContainsKey(attribute.Id));

                outQuery.SetValueIdentifier(attribute.Id, foundValueId);
            }

            Assert.AreEqual(tokens.Length, outQuery.Values.Keys.Count);

            return outQuery;
        }

        private static (LearningAttribute, AttributeValueId) FindAttributeAndValue(
            [NotNull] in DecisionTree tree,
            [NotNull] string valueString
        )
        {
            Assert.IsNotNull(tree);
            Assert.IsFalse(string.IsNullOrEmpty(valueString));

            foreach (LearningAttribute attribute in tree.Attributes.Values)
            {
                AttributeValueId foundValueId = attribute.GetValueId(valueString);

                if (foundValueId != null)
                {
                    return (attribute, foundValueId);
                }
            }

            return (null, null);
        }

        [DataTestMethod]
        [DataRow("no", "sunny hot high weak")]
        [DataRow("no", "sunny hot high strong")]
        [DataRow("yes", "overcast hot high weak")]
        [DataRow("yes", "rain mild high weak")]
        [DataRow("yes", "rain cool normal weak")]
        [DataRow("yes", "overcast cool normal strong")]
        [DataRow("no", "sunny mild high weak")]
        [DataRow("yes", "sunny cool normal weak")]
        [DataRow("yes", "rain mild normal weak")]
        [DataRow("yes", "sunny mild normal strong")]
        [DataRow("yes", "overcast mild high strong")]
        [DataRow("yes", "overcast hot normal weak")]
        [DataRow("no", "rain mild high strong")]
        public void TestLoadingPrebuiltTree(
            string expectedClassification,
            string queryString
        )
        {
            var tree = new DecisionTree();
            tree.LoadPrebuiltTree(GetMemoryStream(WeatherPrebuilt));
            ClassificationData query = GetQuery(tree, queryString);

            string found = tree.Classify(query);

            Assert.IsTrue(
                string.Equals(expectedClassification, found, StringComparison.InvariantCultureIgnoreCase),
                $"Expected '{queryString}' to result in '{expectedClassification}', but got '{found}'");
        }

        private static string GetNormalizedString(
            string stringForComparison
        ) =>
            Regex.Unescape(
                stringForComparison?.Replace("\r", "").Replace("\t", "").Trim() ?? string.Empty);

        [TestMethod]
        public void TestSavingPrebuiltTree()
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);

            var tree = new DecisionTree();
            tree.LoadPrebuiltTree(GetMemoryStream(WeatherPrebuilt));

            tree.SavePrebuiltTree(writer);

            writer.Flush();
            stream.Position = 0;

            using var reader = new StreamReader(stream);
            string expectedString = GetNormalizedString(WeatherPrebuilt);
            string writtenString = GetNormalizedString(reader.ReadToEnd());

            Assert.IsTrue(
                string.Equals(
                    expectedString,
                    writtenString,
                    StringComparison.InvariantCultureIgnoreCase));
        }

        [DataTestMethod]
        [DataRow("no", "sunny hot high weak")]
        [DataRow("no", "sunny hot high strong")]
        [DataRow("yes", "overcast hot high weak")]
        [DataRow("yes", "rain mild high weak")]
        [DataRow("yes", "rain cool normal weak")]
        [DataRow("yes", "overcast cool normal strong")]
        [DataRow("no", "sunny mild high weak")]
        [DataRow("yes", "sunny cool normal weak")]
        [DataRow("yes", "rain mild normal weak")]
        [DataRow("yes", "sunny mild normal strong")]
        [DataRow("yes", "overcast mild high strong")]
        [DataRow("yes", "overcast hot normal weak")]
        [DataRow("no", "rain mild high strong")]
        public void TestTrainingTree(
            string expectedClassification,
            string queryString
        )
        {
            var tree = new DecisionTree();
            tree.LoadTrainingData(GetMemoryStream(WeatherExamples));
            tree.Train();
            ClassificationData query = GetQuery(tree, queryString);

            string found = tree.Classify(query);

            Assert.IsTrue(
                string.Equals(expectedClassification, found, StringComparison.InvariantCultureIgnoreCase),
                $"Expected '{queryString}' to result in '{expectedClassification}', but got '{found}'");
        }

        [TestMethod]
        public void TestExampleCreation()
        {
            var testClass = new Classification(
                new AttributeId(),
                new Dictionary<ClassificationValueId, string>() {
                    {new ClassificationValueId(), "Yes" },
                    {new ClassificationValueId(), "No" },
                    {new ClassificationValueId(), "Maybe" } });
            ClassificationValueId expectedClassId = testClass.ValueIds.First();
            var testAttribute = new LearningAttribute("Test", new[] { "Manual", "Automated" });
            AttributeValueId expectedValue = testAttribute.ValueIds.First();
            var example = new Example(expectedClassId);
            example.SetValueIdentifier(testAttribute.Id, expectedValue);

            Assert.AreEqual(testClass.ValueIds.First(), example.ClassIdentifier);
            Assert.AreEqual(testAttribute.ValueIds.First(), example.GetValueIdentifier(testAttribute.Id));

            string exampleAsString = GetNormalizedString(example.ToString());
            var expectedResponse = new Regex($"Result:.*{expectedClassId.Id}.*\\(.*{testAttribute.Id.Id}={expectedValue.Id}.*\\)", RegexOptions.IgnorePatternWhitespace);
            Assert.IsTrue(expectedResponse.IsMatch(exampleAsString));
        }

        [DataTestMethod]
        [DataRow("sunny hot high weak")]
        [DataRow("sunny hot high strong")]
        public void TestClassificationData(
            in string queryString
        )
        {
            var tree = new DecisionTree();
            tree.LoadPrebuiltTree(GetMemoryStream(WeatherPrebuilt));
            ClassificationData query = GetQuery(tree, queryString);

            string queryAsString = GetNormalizedString(query.ToString());
            // Value Ids shift due to the auto-incrementer.
            // This means we have two options:
            // - Redo the implementable to build the expected
            // - Check that the pattern is correct.
            //
            // Since the ID decoding is already being tested, lets just go with
            // the pattern.
            var expected = new Regex("\\d+:\\d+,\\s*\\d+:\\d+,\\s*\\d+:\\d+,\\s*\\d+:\\d+");
            Assert.IsTrue(expected.IsMatch(queryAsString));
        }

        [TestMethod]
        public void TestResultRecords()
        {
            DateTime now = DateTime.UtcNow;
            DateTime later = now.AddMinutes(15);

            DecisionTreeResult yes = new("Yes", now);
            DecisionTreeResult yesAgain = new("Yes", now);

            Assert.AreEqual(yes, yesAgain);
            Assert.IsTrue(yes.IsValid);

            DecisionTreeResult copyOfYes = yes;

            Assert.AreEqual(yes, copyOfYes);

            // NOPE - Illegal in the compiler!!!
            //copyOfYes.TimeOfDecision = later;

            DecisionTreeResult yesButLater = new(yes.Outcome, later);

            Assert.AreNotEqual(yes, yesButLater);

            DecisionTreeResult nope = new("Nope", now);

            Assert.AreNotEqual(yes, nope);
        }
    }
}
