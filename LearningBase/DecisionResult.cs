using System;

namespace AiEngine.LearningBase
{
    // These two definitions result in almost the
    // exact same behavior

    /// <summary>
    /// Stores the result of a decision and the time it was made.
    /// </summary>
    public record DecisionTreeResult(string Outcome, DateTime TimeOfDecision)
    {
        public bool IsValid => TimeOfDecision.Year >= 2017 && !string.IsNullOrEmpty(Outcome);
    }

    /// <summary>
    /// Stores the result of a decision and the time it was made.
    /// </summary>
    public class DecisionResult
    {
        public string Outcome { get; }
        public DateTime TimeOfDecision { get; }
        public bool IsValid => TimeOfDecision.Year >= 2017 && !string.IsNullOrEmpty(Outcome);

        public override string ToString() =>
            $"( Outcome={Outcome}, TimeOfDecision={TimeOfDecision} )";

        public DecisionResult(
            string outcome,
            DateTime timeOfDecision
        )
        {
            Outcome = outcome;
            TimeOfDecision = timeOfDecision;
        }

        public DecisionResult(
            DecisionResult other
        )
        {
            Outcome = other.Outcome;
            TimeOfDecision = other.TimeOfDecision;
        }
    }
}
