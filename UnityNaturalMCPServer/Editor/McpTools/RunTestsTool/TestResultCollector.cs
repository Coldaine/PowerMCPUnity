using System;
using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal readonly TestResults _testResults = new TestResults();

        private string _rootSuiteName;
        private string _abortMessage;
        private bool _runFinished;

        /// <inheritdoc/>
        public void RunStarted(ITestAdaptor testsToRun)
        {
            _rootSuiteName = testsToRun.FullName;
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
                return; // Exclude test suites.
            }

            if (result.FullName == _rootSuiteName)
            {
                return; // Note: When the filter result is 0, the "Passed" node with the same name as the project name is included, so exclude it.
            }

            switch (result.TestStatus)
            {
                case TestStatus.Failed:
                    _testResults.failCount++;
                    _testResults.failedTests.Add(new FailedTestResult(result));
                    break;
                case TestStatus.Passed:
                    _testResults.passCount++;
                    break;
                case TestStatus.Skipped:
                    _testResults.skipCount++;
                    break;
                case TestStatus.Inconclusive:
                    _testResults.inconclusiveCount++;
                    _testResults.failedTests.Add(new FailedTestResult(result));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
