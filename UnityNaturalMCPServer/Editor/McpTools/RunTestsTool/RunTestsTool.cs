using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityNaturalMCP.Editor.McpTools.RunTestsTool
{
    [McpServerToolType]
    public static class RunTestsTool
    {
        [McpServerTool(Name = "RunTests"), Description(
             "Run tests on Unity Test Runner. Specifying TestMode is required. Recommend filtering by assemblyNames, groupNames, or testNames to narrow down the tests to be executed to the scope of changes.")]
        public static async ValueTask<string> RunTests(
            [Description("An enum flag that specifies if EditMode or PlayMode tests should run.")]
            TestMode testMode,
            [Description(
                "The name of assemblies included in the run. That is the assembly name, without the .dll file extension. E.g., MyTestAssembly")]
            string[] assemblyNames = null,
            [Description(
                "The name of a Category to include in the run. Any test or fixtures runs that have a Category matching the string.")]
            string[] categoryNames = null,
            [Description(
                "The same as testNames, except that it allows for Regex. This is useful for running specific fixtures or namespaces. E.g. \"^MyNamespace\\.\" Runs any tests where the top namespace is MyNamespace.")]
            string[] groupNames = null,
            [Description(
                "The full name of the tests to match the filter. This is usually in the format FixtureName.TestName. If the test has test arguments, then include them in parenthesis. E.g. MyTestClass2.MyTestWithMultipleValues(1).")]
            string[] testNames = null,
            CancellationToken cancellationToken = default)
        {
            var filter = new Filter
            {
                assemblyNames = assemblyNames, categoryNames = categoryNames,
                groupNames = groupNames, testNames = testNames, testMode = testMode,
            };
            var testResultCollector = new TestResultCollector();
            TestRunnerApi.RegisterTestCallback(testResultCollector);

            var testRunner = ScriptableObject.CreateInstance<TestRunnerApi>();
            var guid = testRunner.Execute(new ExecutionSettings(filter));

            try
            {
                var result = await testResultCollector.WaitForRunFinished(cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    return result;
                }

                TestRunnerApi.CancelTestRun(guid);
                return "Test run cancelled.";
            }
            finally
            {
                TestRunnerApi.UnregisterTestCallback(testResultCollector);
                Object.Destroy(testRunner);
            }
        }
    }
}