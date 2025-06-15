using Cysharp.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityNaturalMCP.Editor.McpTools
{
    internal sealed class TestRunnerCallbacks : ICallbacks
    {
        private readonly UniTaskCompletionSource<ITestResultAdaptor> _tcs = new();

        public async UniTask<ITestResultAdaptor> WaitForRunFinished()
        {
            return await _tcs.Task;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log(
                $"[All test finished] Status:{result.TestStatus} Passed:{result.PassCount} Skipped:{result.SkipCount} Failed:{result.FailCount}");
            _tcs.TrySetResult(result);
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.HasChildren)
            {
                return;
            }

            if (result.TestStatus == TestStatus.Failed)
            {
                Debug.LogError($"{result.FullName}\n{result.Message}\n{result.StackTrace}");
            }
        }
    }
}
