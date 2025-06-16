﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityNaturalMCP.Editor.McpTools
{
    [McpServerToolType, Description("Control Unity Editor tools")]
    internal sealed class McpUnityEditorTool
    {
        [McpServerTool, Description("Execute AssetDatabase. Refresh.It must also be called when compiling a script.")]
        public async ValueTask RefreshAssets()
        {
            try
            {
                await UniTask.SwitchToMainThread();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        [McpServerTool, Description("Get current console logs. The default value is adjusted to reduce token usage. Simply call it with the default value the first time and specify arguments only when necessary.")]
        public async ValueTask<IReadOnlyList<LogEntry>> GetCurrentConsoleLogs(
            [Description(
                "Filter logs by type. Valid values: default or empty(Maches all logs), \"error\", \"warning\", \"log\", \"compile-error\"(This is all you need to check for compilation errors.), \"compile-warning\"")]
            string[] logTypes = null,
            [Description("Filter by regex. If empty, all logs are returned.")]
            string filter = "",
            [Description("Log count limit. Set to 0 for no limit(Not recommended).")]
            int maxCount = 20,
            [Description(
                "Get only first line of the log message. If false, the whole message is returned.")]
            bool onlyFirstLine = true,
            [Description(
                "If true, the logs will be sorted by time in chronological order(oldest first). If false, newest first.")]
            bool isChronological = false)
        {
            try
            {
                await UniTask.SwitchToMainThread();

                return ConsoleLogUtilities.GetLogs(filter, maxCount, onlyFirstLine, isChronological, logTypes ?? Array.Empty<string>());
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        [McpServerTool, Description("Clear console logs.")]
        public async ValueTask ClearConsoleLogs()
        {
            try
            {
                await UniTask.SwitchToMainThread();
                ConsoleLogUtilities.ClearLogs();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        [McpServerTool,
         Description(
             "Get compilation errors. Same as `ClearConsoleLogs();GetCurrentConsoleLogs({\"compile-error\", \"compile-warning\"}, args)`")]
        public async ValueTask<IReadOnlyList<LogEntry>> GetCompileLogs(
            [Description("Filter by regex. If empty, all logs are returned.")]
            string filter = "",
            [Description("Log count limit. Set to 0 for no limit(Not recommended).")]
            int maxCount = 20,
            [Description(
                "Get only first line of the log message. If false, the whole message is returned.(To save tokens, recommend calling this with true.)")]
            bool onlyFirstLine = true,
            [Description(
                "If true, the logs will be sorted by time in chronological order(oldest first). If false, newest first.")]
            bool isChronological = false)
        {
            try
            {
                await UniTask.SwitchToMainThread();

                ConsoleLogUtilities.ClearLogs();
                return ConsoleLogUtilities.GetLogs(filter, maxCount, onlyFirstLine, isChronological, new []
                {
                    "compile-error",
                    "compile-warning"
                });
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        [McpServerTool, Description("Run Edit Mode Tests. Check the results with the GetCurrentConsoleLogs.")]
        public async ValueTask RunEditModeTests()
        {
            TestRunnerApi testRunnerApi = null;
            TestRunnerCallbacks callbacks = null;
            try
            {
                await UniTask.SwitchToMainThread();

                testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                callbacks = new TestRunnerCallbacks();
                testRunnerApi.RegisterCallbacks(callbacks);

                var filter = new Filter
                {
                    testMode = TestMode.EditMode,
                    testNames = null,
                    groupNames = null,
                    categoryNames = null,
                    assemblyNames = null,
                    targetPlatform = null,
                };

                var executionSettings = new ExecutionSettings(filter);
                testRunnerApi.Execute(executionSettings);
                await callbacks.WaitForRunFinished();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
            finally
            {
                if (callbacks != null)
                {
                    testRunnerApi.UnregisterCallbacks(callbacks);
                }
            }
        }
    }
}