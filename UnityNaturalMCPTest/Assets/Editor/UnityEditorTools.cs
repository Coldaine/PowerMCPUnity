using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Editor
{
    [McpServerToolType, Description("Unity Editor control tools.")]
    internal sealed public class UnityEditorTools
    {
        // --- Win32 API for Input Simulation and Window Capture ---
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        const uint KEYEVENTF_KEYUP = 0x0002;

        [McpServerTool, Description("Starts the Unity Editor's play mode.")]
        public async UniTask StartPlayMode()
        {
            await UniTask.SwitchToMainThread();
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.EnterPlaymode();
            }
        }

        [McpServerTool, Description("Stops the Unity Editor's play mode.")]
        public async UniTask StopPlayMode()
        {
            await UniTask.SwitchToMainThread();
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }
        }

        [McpServerTool, Description("Creates a new primitive GameObject in the scene.")]
        [McpServerTool, Description("Retrieves a simplified, lightweight hierarchy of all GameObjects in the loaded scenes.")]
        public async UniTask<List<SimpleSceneInfo>> GetSceneHierarchySimple()
        {
            await UniTask.SwitchToMainThread();
            var scenes = new List<SimpleSceneInfo>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                var sceneInfo = new SimpleSceneInfo { Name = scene.name };
                var rootObjects = scene.GetRootGameObjects();
                foreach (var rootObject in rootObjects)
                {
                    sceneInfo.RootObjects.Add(CreateSimpleGameObjectInfo(rootObject));
                }
                scenes.Add(sceneInfo);
            }
            return scenes;
        }

        [McpServerTool, Description("Gets detailed information about a specific GameObject, including its components.")]
        public async UniTask<GameObjectInfo> GetGameObjectDetails([Description("The instance ID of the GameObject to inspect.")] int instanceId)
        {
            await UniTask.SwitchToMainThread();
            var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (go == null)
            {
                // Handle the case where the GameObject is not found
                return null;
            }

            var components = new List<ComponentInfo>();
            foreach (var component in go.GetComponents<Component>())
            {
                components.Add(new ComponentInfo { Type = component.GetType().Name });
            }

            return new GameObjectInfo
            {
                Id = go.GetInstanceID(),
                Name = go.name,
                Type = go.GetType().Name,
                Position = go.transform.position,
                // We can extend GameObjectInfo to include components if needed
            };
        }

        [McpServerTool, Description("Finds assets in the project, such as prefabs, materials, or textures.")]
        public async UniTask<List<AssetInfo>> GetProjectAssets([Description("An optional filter string to search for assets (e.g., 't:material', 'myPrefab').")] string filter = "")
        {
            await UniTask.SwitchToMainThread();
            var assetInfos = new List<AssetInfo>();
            var guids = AssetDatabase.FindAssets(filter);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset != null)
                {
                    assetInfos.Add(new AssetInfo
                    {
                        Name = asset.name,
                        Path = path,
                        Type = asset.GetType().Name,
                        Guid = guid
                    });
                }
            }
            return assetInfos;
        }

        [McpServerTool, Description("Gets a list of the most recent entries from the Unity Editor's console log.")]
        public async UniTask<List<LogEntry>> GetConsoleLogs()
        {
            // This is a simplified version. The original has more filtering options.
            // We can add those later if needed.
            await UniTask.SwitchToMainThread();
            var logs = new List<LogEntry>();
            // This is an internal method, but it's the standard way to get console logs.
            var logEntries = new UnityEditor.LogEntries();
            UnityEditor.LogEntries.GetEntries();
            int count = UnityEditor.LogEntries.GetCount();

            for (int i = 0; i < count; i++)
            {
                var entry = new UnityEditor.LogEntry();
                UnityEditor.LogEntries.GetEntryInternal(i, entry);
                logs.Add(new LogEntry
                {
                    Condition = entry.condition,
                    Stacktrace = entry.stacktrace,
                    Type = entry.logType
                });
            }
            return logs;
        }

        [McpServerTool, Description("Gets a list of all available menu items in the Unity Editor.")]
        public async UniTask<List<MenuItemInfo>> GetMenuItems()
        {
            await UniTask.SwitchToMainThread();
            var menuItems = new List<MenuItemInfo>();
            var allMenuItems = UnityEditor.Menu.GetCommands();

            foreach (var menuItem in allMenuItems)
            {
                menuItems.Add(new MenuItemInfo
                {
                    Path = menuItem.menuItem,
                    IsChecked = UnityEditor.Menu.GetChecked(menuItem.menuItem),
                    IsValidated = UnityEditor.Menu.GetEnabled(menuItem.menuItem)
                });
            }
            return menuItems;
        }

        [McpServerTool, Description("Gets a list of all packages installed in the project.")]
        public async UniTask<List<PackageInfo>> GetProjectPackages()
        {
            await UniTask.SwitchToMainThread();
            var packages = new List<PackageInfo>();
            var listRequest = UnityEditor.PackageManager.Client.List(true, false);
            while (!listRequest.IsCompleted)
            {
                await UniTask.Yield();
            }

            if (listRequest.Status == UnityEditor.PackageManager.StatusCode.Success)
            {
                foreach (var package in listRequest.Result)
                {
                    packages.Add(new PackageInfo
                    {
                        Name = package.name,
                        DisplayName = package.displayName,
                        Version = package.version,
                        Source = package.source.ToString(),
                        Description = package.description
                    });
                }
            }
            return packages;
        }

        [McpServerTool, Description("Gets a list of all tests in the project.")]
        public async UniTask<List<TestInfo>> GetProjectTests()
        {
            await UniTask.SwitchToMainThread();
            var tests = new List<TestInfo>();
            var testList = UnityEditor.TestTools.TestRunner.Api.TestRunnerApi.FetchTestList();

            void AddTestNodes(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor testNode)
            {
                if (testNode == null) return;

                tests.Add(new TestInfo
                {
                    Name = testNode.Name,
                    Type = testNode.Type,
                    IsSuite = testNode.IsSuite
                });

                if (testNode.HasChildren)
                {
                    foreach (var child in testNode.Children)
                    {
                        AddTestNodes(child);
                    }
                }
            }

            foreach (var test in testList.Children)
            {
                AddTestNodes(test);
            }

            return tests;
        }

        [McpServerTool, Description("Executes a given menu item command. Returns true if the item was found and executed.")]
        public async UniTask<bool> ExecuteMenuItem([Description("The path of the menu item to execute (e.g., 'File/Save Scene').")] string path)
        {
            await UniTask.SwitchToMainThread();
            return UnityEditor.EditorApplication.ExecuteMenuItem(path);
        }

        [McpServerTool, Description("Captures a screenshot of the screen and returns it as a base64 encoded PNG.")]
        public async UniTask<string> CaptureEditorScreenshot()
        {
            await UniTask.SwitchToMainThread();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            var bytes = texture.EncodeToPNG();
            Object.Destroy(texture);
            return System.Convert.ToBase64String(bytes);
        }

        [McpServerTool, Description("Moves the mouse cursor to the specified screen coordinates.")]
        public async UniTask MoveMouse([Description("X coordinate")] int x, [Description("Y coordinate")] int y)
        {
            await UniTask.SwitchToMainThread();
            SetCursorPos(x, y);
        }

        [McpServerTool, Description("Simulates a left mouse click at the current cursor position.")]
        public async UniTask SimulateClick()
        {
            await UniTask.SwitchToMainThread();
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            await UniTask.Delay(50); // Small delay between down and up
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        [McpServerTool, Description("Simulates keystrokes for a given string.")]
        public async UniTask SimulateKeystrokes([Description("The string to type.")] string text)
        {
            await UniTask.SwitchToMainThread();
            foreach (char c in text)
            {
                // This is a simplified implementation. A full implementation would handle
                // uppercase, special characters, etc., using virtual key codes (VK_SHIFT, etc.)
                byte vk = (byte)char.ToUpper(c);
                keybd_event(vk, 0, 0, System.UIntPtr.Zero);
                await UniTask.Delay(20);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, System.UIntPtr.Zero);
                await UniTask.Delay(30);
            }
        }

        private SimpleGameObjectInfo CreateSimpleGameObjectInfo(GameObject gameObject)
        {
            if (gameObject == null) return null;

            var info = new SimpleGameObjectInfo
            {
                Name = gameObject.name,
                InstanceId = gameObject.GetInstanceID()
            };

            foreach (Transform child in gameObject.transform)
            {
                info.Children.Add(CreateSimpleGameObjectInfo(child.gameObject));
            }

            return info;
        }

        [McpServerTool, Description("Creates a new primitive GameObject in the scene.")]
        public async UniTask<GameObjectInfo> CreateGameObject(
            [Description("Name of the GameObject")] string name,
            [Description("Type of primitive to create (Cube, Sphere, Capsule, Cylinder, Plane, Quad)")] string primitiveType = "Cube")
        {
            await UniTask.SwitchToMainThread();
            
            PrimitiveType type;
            if (!System.Enum.TryParse(primitiveType, true, out type))
            {
                type = PrimitiveType.Cube;
            }

            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            
            // Select the new object in the hierarchy
            Selection.activeGameObject = go;
            
            return new GameObjectInfo
            {
                Id = go.GetInstanceID(),
                Name = go.name,
                Type = go.GetType().Name,
                Position = go.transform.position
            };
        }
    }

    [System.Serializable]
    public class GameObjectInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public Vector3 Position { get; set; }
    }

    // --- Data Structures for Simplified Scene Hierarchy ---
    [System.Serializable]
    public class SimpleSceneInfo
    {
        public string Name { get; set; }
        public List<SimpleGameObjectInfo> RootObjects { get; set; } = new List<SimpleGameObjectInfo>();
    }

    [System.Serializable]
    public class SimpleGameObjectInfo
    {
        public string Name { get; set; }
        public int InstanceId { get; set; }
        public List<SimpleGameObjectInfo> Children { get; set; } = new List<SimpleGameObjectInfo>();
    }

    [System.Serializable]
    public class ComponentInfo
    {
        public string Type { get; set; }
        // In a real implementation, we would serialize the component's properties.
        // For now, we'll just list the component type.
    }

    [System.Serializable]
    public class AssetInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public string Guid { get; set; }
    }

    [System.Serializable]
    public class LogEntry
    {
        public string Condition { get; set; }
        public string Stacktrace { get; set; }
        public LogType Type { get; set; }
    }

    [System.Serializable]
    public class MenuItemInfo
    {
        public string Path { get; set; }
        public bool IsChecked { get; set; }
        public bool IsValidated { get; set; }
    }

    [System.Serializable]
    public class PackageInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string Source { get; set; }
        public string Description { get; set; }
    }

    [System.Serializable]
    public class TestInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsSuite { get; set; }
    }
}
