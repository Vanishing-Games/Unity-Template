using System.Collections.Generic;

namespace Core
{
    public sealed class LogTag
    {
        public string Name { get; }
        public List<string> Path { get; }

        private LogTag(string name, List<string> parentPath = null)
        {
            Name = name;
            Path = new List<string>(parentPath ?? new List<string>()) { name };

            if (Path.Count > 3)
            {
                throw new System.Exception(
                    $"Tag hierarchy too deep: {string.Join(" > ", Path)} (max 3 levels)"
                );
            }
        }

        public override string ToString() => $"[{string.Join("][", Path)}]";

        // ========== Base Tags ==========
        public static readonly LogTag Editor = new("Editor");

        // ========== Second Tags ==========
        public static readonly LogTag CodeUnfucker = new("CodeUnfucker", Editor.Path);

        // ========== Third Tags ==========
        public static readonly LogTag CodeUnfucker_3_Sample = new("Save", CodeUnfucker.Path); // MAX depth = 3
    }
}
