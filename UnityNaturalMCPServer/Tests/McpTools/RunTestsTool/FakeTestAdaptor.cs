using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEditor.TestTools.TestRunner.Api;
using RunState = UnityEditor.TestTools.TestRunner.Api.RunState;

namespace UnityNaturalMCP.Editor.McpTools.RunTestsTool
{
    public class FakeTestAdaptor : ITestAdaptor
    {
        public string Id { get; }
        public string Name { get; }
        public string FullName { get; set; }
        public int TestCaseCount { get; }
        public bool HasChildren { get; }
        public bool IsSuite { get; }
        public IEnumerable<ITestAdaptor> Children { get; }
        public ITestAdaptor Parent { get; }
        public int TestCaseTimeout { get; }
        public ITypeInfo TypeInfo { get; }
        public IMethodInfo Method { get; }
        public object[] Arguments { get; }
        public string[] Categories { get; }
        public bool IsTestAssembly { get; }
        public RunState RunState { get; }
        public string Description { get; }
        public string SkipReason { get; }
        public string ParentId { get; }
        public string ParentFullName { get; }
        public string UniqueName { get; }
        public string ParentUniqueName { get; }
        public int ChildIndex { get; }
        public TestMode TestMode { get; }
    }
}
