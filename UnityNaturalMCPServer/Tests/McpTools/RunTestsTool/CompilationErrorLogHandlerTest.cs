using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityNaturalMCP.Editor.McpTools.RunTestsTool
{
    [TestFixture]
    public class CompilationErrorLogHandlerTest
    {
        private const string TriggerMessage = "CompilationErrorLogHandler cancel testing!";

        [Test]
        public void HandleLog_MatchTriggerMessage_Cancel()
        {
            using var sut = new CompilationErrorLogHandler(TriggerMessage);

            LogAssert.Expect(LogType.Error, TriggerMessage);
            Debug.LogError(TriggerMessage);

            var actual = sut.CancellationToken;
            Assert.That(actual.IsCancellationRequested, Is.True);
        }

        [Test]
        public void HandleLog_NotErrorLog_NotCancel()
        {
            using var sut = new CompilationErrorLogHandler(TriggerMessage);

            Debug.Log(TriggerMessage);

            var actual = sut.CancellationToken;
            Assert.That(actual.IsCancellationRequested, Is.False);
        }

        [Test]
        public void HandleLog_NotMatchTriggerMessage_NotCancel()
        {
            using var sut = new CompilationErrorLogHandler(TriggerMessage);

            const string NotTriggerMessage = "Not a trigger message";
            LogAssert.Expect(LogType.Error, NotTriggerMessage);
            Debug.LogError(NotTriggerMessage);

            var actual = sut.CancellationToken;
            Assert.That(actual.IsCancellationRequested, Is.False);
        }
    }
}
