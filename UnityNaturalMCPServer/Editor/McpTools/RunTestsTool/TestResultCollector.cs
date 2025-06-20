using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;

namespace UnityNaturalMCP.Editor.McpTools.RunTestsTool
{
    /// <summary>
    /// Waiting for the test run to finish and collecting results.
    /// </summary>
    public class TestResultCollector : IErrorCallbacks
    {
        internal readonly TestResults _testResults = new TestResults();
        private string _abortMessage;
        private bool _runFinished;

        /// <inheritdoc/>
        public void RunStarted(ITestAdaptor testsToRun)
        {
            // nop
        }

        /// <inheritdoc/>
        public void RunFinished(ITestResultAdaptor result)
        {
            _runFinished = true;
        }

        /// <inheritdoc/>
        public void TestStarted(ITestAdaptor test)
        {
            // nop
        }

        /// <inheritdoc/>
        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.HasChildren)
            {
                return;
            }

            switch (result.TestStatus)
            {
                case TestStatus.Failed:
                    _testResults.failCount++;
                    _testResults.failedTests.Add(CreateFailedTestString(result));
                    break;
                case TestStatus.Passed:
                    _testResults.passCount++;
                    break;
                case TestStatus.Skipped:
                    _testResults.skipCount++;
                    break;
                case TestStatus.Inconclusive:
                    _testResults.inconclusiveCount++;
                    _testResults.failedTests.Add(CreateFailedTestString(result));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string CreateFailedTestString(ITestResultAdaptor result)
        {
            return $"Test {result.FullName} is failed with message: {result.Message}\n{result.StackTrace}";
        }

        /// <inheritdoc/>
        public void OnError(string message)
        {
            _abortMessage = message;
            _runFinished = true;
        }

        /// <summary>
        /// Wait until the run is finished or the cancellation token is triggered.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Test results by JSON string or abort message.</returns>
        public async ValueTask<string> WaitForRunFinished(CancellationToken cancellationToken = default)
        {
            while (_runFinished == false && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(500, cancellationToken);
            }

            return _abortMessage ?? _testResults.ToJson();
        }
    }
}