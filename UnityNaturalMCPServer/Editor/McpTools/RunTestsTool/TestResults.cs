using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace UnityNaturalMCP.Editor.McpTools.RunTestsTool
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TestResults
    {
        /// <summary>
        /// The number of test cases that failed when running the test and all its children.
        /// </summary>
        public int failCount { get; set; }

        /// <summary>
        /// The number of test cases that passed when running the test and all its children.
        /// </summary>
        public int passCount { get; set; }

        /// <summary>
        /// The number of test cases that were skipped when running the test and all its children.
        /// </summary>
        public int skipCount { get; set; }

        /// <summary>
        ///The number of test cases that were inconclusive when running the test and all its children.
        /// </summary>
        public int inconclusiveCount { get; set; }

        /// <summary>
        /// Failed or inconclusive tests.
        /// </summary>
        [SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
        public List<string> failedTests { get; } = new List<string>();

        /// <summary>
        /// Returns true if all tests passed.
        /// </summary>
        public bool success => (failCount + inconclusiveCount) == 0;

        /// <summary>
        /// Returns a JSON representation of the test results.
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}