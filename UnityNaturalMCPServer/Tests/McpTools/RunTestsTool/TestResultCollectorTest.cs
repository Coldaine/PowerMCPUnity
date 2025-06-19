using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityNaturalMCP.Editor.McpTools.RunTestsTool;

namespace UnityNaturalMCP.Tests.McpTools.RunTestsTool
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
                FullName = "Fake.FailedTest",
                Message = "Message of Fake.FailedTest",
                StackTrace = "Stack trace of Fake.FailedTest"
            };

            var sut = new TestResultCollector();
            sut.TestFinished(failed);
            sut.TestFinished(failed); // twice

            Assert.That(sut._testResults.failCount, Is.EqualTo(2));
            Assert.That(sut._testResults.failedTests, Is.EqualTo(new[]
            {
                "Test Fake.FailedTest is failed with message: Message of Fake.FailedTest\nStack trace of Fake.FailedTest",
                "Test Fake.FailedTest is failed with message: Message of Fake.FailedTest\nStack trace of Fake.FailedTest"
            }));
        }

        [Test]
        public void TestFinished_Inconclusive_InconclusiveIsCountedUp()
        {
            var inconclusive = new FakeTestResultAdaptor
            {
                TestStatus = TestStatus.Inconclusive,
                FullName = "Fake.InconclusiveTest",
                Message = "Message of Fake.InconclusiveTest",
                StackTrace = "Stack trace of Fake.InconclusiveTest"
            };

            var sut = new TestResultCollector();
            sut.TestFinished(inconclusive);
            sut.TestFinished(inconclusive); // twice

            Assert.That(sut._testResults.inconclusiveCount, Is.EqualTo(2));
            Assert.That(sut._testResults.failedTests, Is.EqualTo(new[]
            {
                "Test Fake.InconclusiveTest is failed with message: Message of Fake.InconclusiveTest\nStack trace of Fake.InconclusiveTest",
                "Test Fake.InconclusiveTest is failed with message: Message of Fake.InconclusiveTest\nStack trace of Fake.InconclusiveTest"
            }));
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