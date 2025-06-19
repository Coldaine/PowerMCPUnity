using NUnit.Framework;
using UnityNaturalMCP.Editor.McpTools.RunTestsTool;

namespace UnityNaturalMCP.Tests.McpTools.RunTestsTool
{
    [TestFixture]
    public class TestResultsTest
    {
        [Test]
        public void Success_WithFail_ReturnsFalse()
        {
            var sut = new TestResults
            {
                failCount = 1
            };

            Assert.That(sut.success, Is.False);
        }

        [Test]
        public void Success_WithInconclusive_ReturnsFalse()
        {
            var sut = new TestResults
            {
                inconclusiveCount = 1
            };

            Assert.That(sut.success, Is.False);
        }

        [Test]
        public void Success_WithoutFailOrInconclusive_ReturnsTrue()
        {
            var sut = new TestResults
            {
                failCount = 0,
                inconclusiveCount = 0
            };

            Assert.That(sut.success, Is.True);
        }
    }
}