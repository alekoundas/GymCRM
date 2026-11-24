using System.Runtime.CompilerServices;

namespace Tests.Shared
{
    /// <summary>
    /// Text compared with a file a person read once and approved, committed beside the test.
    ///
    /// On a difference the new text is written next to it as <c>.received</c> and the test fails,
    /// naming the first line that differs. Intended: replace the approved file with it. Not: fix the
    /// code. Line endings are not part of the comparison.
    /// </summary>
    internal static class ApprovedFile
    {
        public static void ShouldMatch(string actual, string name, [CallerFilePath] string testFile = "")
        {
            string folder = Path.GetDirectoryName(testFile) ?? string.Empty;
            string approvedPath = Path.Combine(folder, $"{name}.approved.txt");
            string receivedPath = Path.Combine(folder, $"{name}.received.txt");

            string received = actual.ReplaceLineEndings("\n").TrimEnd() + "\n";

            if (!File.Exists(approvedPath))
            {
                File.WriteAllText(receivedPath, received);
                Assert.Fail($"There is no approved file yet. Read {receivedPath} and, if it is right, rename it to {Path.GetFileName(approvedPath)}.");
            }

            string approved = File.ReadAllText(approvedPath).ReplaceLineEndings("\n").TrimEnd() + "\n";
            if (approved == received)
            {
                File.Delete(receivedPath);
                return;
            }

            File.WriteAllText(receivedPath, received);
            Assert.Fail($"Differs from {Path.GetFileName(approvedPath)}. {Differences(approved, received)} The new text is in {receivedPath}.");
        }

        private static string Differences(string approved, string received)
        {
            string[] expected = approved.Split('\n');
            string[] actual = received.Split('\n');

            List<string> gone = expected.Except(actual).Take(10).ToList();
            List<string> added = actual.Except(expected).Take(10).ToList();

            return $"Gone: [{string.Join(" | ", gone)}]. New: [{string.Join(" | ", added)}].";
        }
    }
}
