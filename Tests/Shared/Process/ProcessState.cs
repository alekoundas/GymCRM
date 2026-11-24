namespace Tests.Shared
{
    /// <summary>
    /// Tests that change something the whole process shares - the time zone, an environment
    /// variable. They run on their own, never beside another test.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class ProcessState
    {
        public const string Name = "ProcessState";
    }
}
