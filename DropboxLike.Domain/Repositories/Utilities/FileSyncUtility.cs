namespace DropboxLike.Domain.Repositories.Utilities;

public class FileSyncUtility
{
    // DIFFING ALGORITHM
    public static List<Diff> SimpleTextDiff(string originalText, string newText)
    {
        // Initializes a list to hold the differences between the two texts.
        var diffs = new List<Diff>();

        // Creates a 2D array for dynamic programming, to store the length of the longest common subsequence at each point.
        var dp = new int[originalText.Length + 1, newText.Length + 1];

        // Iterate through each character of the original text and the new text.
        for (var i = 0; i <= originalText.Length; i++)
        {
            for (var j = 0; j <= newText.Length; j++)
            {
                // Initialize the first row and column of the DP table to 0.
                if (i == 0 || j == 0)
                {
                    dp[i, j] = 0;
                }
                // If characters match, increment the value from the diagonal.
                else if (originalText[i - 1] == newText[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                }
                // If characters don't match, take the maximum value from left or top cell.
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }

        // Start from the bottom-right corner of the matrix.
        var iIndex = originalText.Length;
        var jIndex = newText.Length;

        // Traverse back from the bottom-right corner to the top-left corner of the DP matrix.
            while (iIndex > 0 || jIndex > 0)
            {
                // If the value is the same as the value in the cell above, it's a deletion in the original text.
                if (iIndex > 0 && dp[iIndex, jIndex] == dp[iIndex - 1, jIndex])
                {
                    diffs.Insert(0, new Diff(DiffType.Deletion, originalText[iIndex - 1].ToString()));
                    iIndex--;
                }
                // If the value is the same as the value in the cell to the left, it's an addition in the new text.
                else if (jIndex > 0 && dp[iIndex, jIndex] == dp[iIndex, jIndex - 1])
                {
                    diffs.Insert(0, new Diff(DiffType.Addition, newText[jIndex - 1].ToString()));
                    jIndex--;
                }
                // If neither of the above, move diagonally up-left in the matrix.
                else
                {
                    iIndex--;
                    jIndex--;
                }
            }

            // Return the list of differences.
            return diffs;
        }

        // Enum to represent the type of difference: addition, deletion, or no change.
        public enum DiffType
        {
            Addition,
            Deletion,
            NoChange
        }

        // Class to represent a single difference.
        public class Diff
        {
            // Property to store the type of the difference.
            public DiffType Type { get; }

            // Property to store the text that was added or deleted.
            public string Text { get; }

            // Constructor to create a new Diff object with a specified type and text.
            public Diff(DiffType type, string text)
            {
                Type = type;
                Text = text;
            }
        }
}