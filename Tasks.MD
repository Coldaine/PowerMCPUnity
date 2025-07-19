# PowerUnityMCP Detailed Implementation Tasks (v2)

**Version:** 2.0
**Date:** 2025-07-19 00:27:02-05:00

This document provides a detailed, step-by-step guide for implementing the revised, simplified design outlined in `Design.MD` (v2).

---

## Phase 1: Unity Server Extension

**Objective:** Fork `UnityNaturalMCP` and extend it with a few basic tools. The server should be testable with simple HTTP requests.

*   **Task 1.1: Fork and Clone `UnityNaturalMCP`**
    *   Action: Fork the `CoderGamester/UnityNaturalMCP` repository on GitHub and clone it to your local machine.

*   **Task 1.2: Create the `[Tool]` Attribute**
    *   File: `Assets/UnityNaturalMCP/ToolAttribute.cs`
    *   Action: Create a simple C# attribute to decorate our tool methods. This is used by the server to discover tools via reflection.
    ```csharp
    // Assets/UnityNaturalMCP/ToolAttribute.cs
    using System;

    [AttributeUsage(AttributeTargets.Method)]
    public class ToolAttribute : Attribute
    {
        public string Description { get; set; }
        public ToolAttribute(string description = "")
        {
            Description = description;
        }
    }
    ```

*   **Task 1.3: Implement New Tools**
    *   File: `Assets/UnityNaturalMCP/McpServer.cs` (or a new `Tools.cs` file for better organization).
    *   Action: Add new public methods decorated with the `[Tool]` attribute.
    ```csharp
    // In McpServer.cs or a new Tools.cs
    using UnityEditor;
    using UnityEngine;

    // ... inside the server class

    [Tool("Starts the Unity Editor's play mode.")]
    public void StartPlayMode()
    {
        // This must be run on the main thread
        EditorApplication.EnterPlaymode();
    }

    [Tool("Creates a new primitive GameObject.")]
    public void CreateGameObject(string name, string type)
    {
        PrimitiveType primitiveType;
        if (Enum.TryParse(type, true, out primitiveType))
        {
            var go = GameObject.CreatePrimitive(primitiveType);
            go.name = name;
        }
    }
    ```

*   **Task 1.4: Test with cURL**
    *   Action: Run the Unity project to start the server. From a terminal, send a `POST` request to test a new tool.
    ```bash
    # Example for testing StartPlayMode
    curl -X POST http://localhost:10000/tools/startplaymode

    # Example for testing CreateGameObject
    curl -X POST -H "Content-Type: application/json" -d '{"name":"TestCube", "type":"Cube"}' http://localhost:10000/tools/creategameobject
    ```

---

## Phase 2: Node.js Agent Host

**Objective:** Create the Node.js application that will act as the agent's "brain," planning and executing tasks via HTTP requests to the Unity server.

*   **Task 2.1: Node.js Project Setup**
    *   Action: Create a new directory `AgentHost`, run `npm init -y`, `npx tsc --init`, and install dependencies: `npm install typescript ts-node nodemon axios` (using Axios for simpler HTTP requests).

*   **Task 2.2: Implement the `Planner`**
    *   File: `src/Planner.ts`
    *   Action: Create a class that takes a user prompt and returns a structured plan from an LLM.
    ```typescript
    // src/Planner.ts
    interface ToolCall { tool: string; params: any; }

    class Planner {
        // This would interact with an LLM API (e.g., OpenAI, Ollama)
        async generatePlan(prompt: string, toolSchemas: any): Promise<ToolCall[]> {
            console.log(`Generating plan for: ${prompt}`);
            // In a real implementation, this would be an LLM call.
            // For now, we can return a mocked plan for testing.
            if (prompt.includes("red cube")) {
                return [
                    { tool: "creategameobject", params: { name: "MyCube", type: "Cube" } },
                    // ... other steps for making it red
                ];
            }
            return [];
        }
    }
    ```

*   **Task 2.3: Implement the `Executor`**
    *   File: `src/Executor.ts`
    *   Action: Create a class that takes a plan and executes it by making HTTP requests.
    ```typescript
    // src/Executor.ts
    import axios from 'axios';

    class Executor {
        private unityServerUrl = 'http://localhost:10000';

        async executePlan(plan: ToolCall[]): Promise<void> {
            for (const step of plan) {
                try {
                    const url = `${this.unityServerUrl}/tools/${step.tool.toLowerCase()}`;
                    console.log(`Executing: POST ${url} with params`, step.params);
                    await axios.post(url, step.params);
                    console.log(`Step '${step.tool}' completed successfully.`);
                } catch (error) {
                    console.error(`Error executing step '${step.tool}':`, error.message);
                    // Stop execution on failure
                    return;
                }
            }
        }
    }
    ```

*   **Task 2.4: Create the Main Agent Loop**
    *   File: `src/main.ts`
    *   Action: Tie everything together. This script will take input, run the planner, and then the executor.
    ```typescript
    // src/main.ts
    const planner = new Planner();
    const executor = new Executor();

    async function runAgent(prompt: string) {
        // In a real app, tool schemas would be fetched from the Unity server
        const toolSchemas = {}; 
        const plan = await planner.generatePlan(prompt, toolSchemas);
        if (plan && plan.length > 0) {
            await executor.executePlan(plan);
            console.log("Agent has completed the plan.");
        } else {
            console.log("Could not generate a plan for the given prompt.");
        }
    }

    // Get prompt from command line arguments
    const userPrompt = process.argv[2];
    if (userPrompt) {
        runAgent(userPrompt);
    }
    ```
