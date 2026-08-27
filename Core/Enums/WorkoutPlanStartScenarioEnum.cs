namespace Core.Enums
{
    // Which dialog the client shows when the member presses Start.
    public enum WorkoutPlanStartScenarioEnum
    {
        // The plan has no rule - Start is disabled.
        NoRule,

        // A recording is already running for this plan - the button is Stop.
        Running,

        // No previous recording at all.
        FirstEver,

        // Last recording within the gap, still under the week's limit.
        UnderMax,

        // Last recording within the gap, limit reached - advance only.
        AtMax,

        // Nothing recorded for longer than the gap - pick any week.
        AwayTooLong,

        // CurrentWeek no longer exists in the rule - restart from week 1.
        Orphaned
    }
}
