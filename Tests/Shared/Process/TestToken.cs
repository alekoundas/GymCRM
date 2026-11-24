namespace Tests.Shared
{
    /// <summary>
    /// The running test's cancellation token, under a short name.
    /// </summary>
    internal static class TestToken
    {
        public static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }
    }
}
