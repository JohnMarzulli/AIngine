using System;
using System.Diagnostics;

namespace AiEngine.LearningBase
{
    /// <summary>
    /// Stores the result of a decision and the time it was made.
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public class DecisionResult
    {
        public string Outcome { get; }
        public DateTime TimeOfDecision { get; init; }
        public bool IsValid => TimeOfDecision.Year >= 2017 && !string.IsNullOrEmpty(Outcome);

        public override string ToString() =>
            $"( Outcome={Outcome}, TimeOfDecision={TimeOfDecision} )";

        public DecisionResult(
            string outcome
        )
        {
            Outcome = outcome;
            TimeOfDecision = DateTime.UtcNow;
        }

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