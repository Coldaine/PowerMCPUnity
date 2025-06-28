using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityNaturalMCP.Editor.McpTools.RunTestsTool
{
    [TestFixture]
    public class TestResultCollectorTest
    {
        [Test]
        public void TestFinished_HasChildren_NotCountedUp()
        {
            var hasChildren = new FakeTestResultAdaptor
            {
                HasChildren = true,
                TestStatus = TestStatus.Passed
            };

            var sut = new TestResultCollector();
            sut.TestFinished(hasChildren);

            Assert.That(sut._testResults.passCount, Is.EqualTo(0));
        }

        [Test]
        public void TestFinished_Passed_PassedIsCountedUp()
        {
            var passed = new FakeTestResultAdaptor
            {
                TestStatus = TestStatus.Passed
            };

            var sut = new TestResultCollector();
            sut.TestFinished(passed);
            sut.TestFinished(passed); // twice

            Assert.That(sut._testResults.passCount, Is.EqualTo(2));
        }

        [Test]
        public void TestFinished_Skipped_SkippedIsCountedUp()
        {
            var skipped = new FakeTestResultAdaptor
            {
                TestStatus = TestStatus.Skipped
            };

            var sut = new TestResultCollector();
            sut.TestFinished(skipped);
            sut.TestFinished(skipped); // twice

            Assert.That(sut._testResults.skipCount, Is.EqualTo(2));
        }

        [Test]
        public void TestFinished_Failed_FailedIsCountedUp()
        {
            var failed = new FakeTestResultAdaptor
            {
                TestStatus = TestStatus.Failed,
                Name = "FailedTest",
                FullName = "Fake.FailedTest",
                ResultState = "Failed:Error",
                Duration = 1.23d,
                Message = "Message of Fake.FailedTest",
                StackTrace = "Stack trace of Fake.FailedTest",
                Output = "Output of Fake.FailedTest"
            };

            var sut = new TestResultCollector();
            sut.TestFinished(failed);
            sut.TestFinished(failed); // twice

            Assert.That(sut._testResults.failCount, Is.EqualTo(2));
            Assert.That(sut._testResults.ToJson(), Is.EqualTo(
                "{\"failCount\":2,\"passCount\":0,\"skipCount\":0,\"inconclusiveCount\":0,\"failedTests\":[{\"name\":\"FailedTest\",\"fullName\":\"Fake.FailedTest\",\"resultState\":\"Failed:Error\",\"testStatus\":\"Failed\",\"duration\":1.23,\"message\":\"Message of Fake.FailedTest\",\"stackTrace\":\"Stack trace of Fake.FailedTest\",\"output\":\"Output of Fake.FailedTest\"},{\"name\":\"FailedTest\",\"fullName\":\"Fake.FailedTest\",\"resultState\":\"Failed:Error\",\"testStatus\":\"Failed\",\"duration\":1.23,\"message\":\"Message of Fake.FailedTest\",\"stackTrace\":\"Stack trace of Fake.FailedTest\",\"output\":\"Output of Fake.FailedTest\"}],\"success\":false}"));
        }

        [Test]
        public void TestFinished_Inconclusive_InconclusiveIsCountedUp()
        {
            var inconclusive = new FakeTestResultAdaptor
            {
                TestStatus = TestStatus.Inconclusive,
                Name = "InconclusiveTest",
                FullName = "Fake.InconclusiveTest",
                ResultState = "Inconclusive",
                Duration = 1.23d,
                Message = "Message of Fake.InconclusiveTest",
                StackTrace = "Stack trace of Fake.InconclusiveTest",
                Output = "Output of Fake.InconclusiveTest"
            };

            var sut = new TestResultCollector();
            sut.TestFinished(inconclusive);
            sut.TestFinished(inconclusive); // twice

            Assert.That(sut._testResults.inconclusiveCount, Is.EqualTo(2));
            Assert.That(sut._testResults.ToJson(), Is.EqualTo(
                "{\"failCount\":0,\"passCount\":0,\"skipCount\":0,\"inconclusiveCount\":2,\"failedTests\":[{\"name\":\"InconclusiveTest\",\"fullName\":\"Fake.InconclusiveTest\",\"resultState\":\"Inconclusive\",\"testStatus\":\"Inconclusive\",\"duration\":1.23,\"message\":\"Message of Fake.InconclusiveTest\",\"stackTrace\":\"Stack trace of Fake.InconclusiveTest\",\"output\":\"Output of Fake.InconclusiveTest\"},{\"name\":\"InconclusiveTest\",\"fullName\":\"Fake.InconclusiveTest\",\"resultState\":\"Inconclusive\",\"testStatus\":\"Inconclusive\",\"duration\":1.23,\"message\":\"Message of Fake.InconclusiveTest\",\"stackTrace\":\"Stack trace of Fake.InconclusiveTest\",\"output\":\"Output of Fake.InconclusiveTest\"}],\"success\":false}"));
        }

        [Test]
        [Timeout(5000)]
        public async Task WaitForRunFinished_RunFinished_LeaveAwaiting()
        {
            var sut = new TestResultCollector();
            sut.RunFinished(null);

            var result = await sut.WaitForRunFinished();
            Debug.Log(result);
        }

        [Test]
        [Timeout(5000)]
        public async Task WaitForRunFinished_OnError_LeaveAwaiting()
        {
            var message = "Error occurred!";
            var sut = new TestResultCollector();
            sut.OnError(message);

            var result = await sut.WaitForRunFinished();
            Assert.That(result, Is.EqualTo(message));
        }

        [Test]
        [Timeout(5000)]
        public async Task WaitForRunFinished_Cancel_LeaveAwaiting()
        {
            var sut = new TestResultCollector();
            var cts = new CancellationTokenSource();
            cts.CancelAfter(1000);

            var result = await sut.WaitForRunFinished(cts.Token);
            Assert.That(result, Is.Null);
        }
    }
}
