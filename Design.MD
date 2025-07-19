# PowerUnityMCP Implementation Plan (v2)

**Version:** 2.0
**Date:** 2025-07-19 00:27:02-05:00

This document outlines the revised, simplified plan for developing an autonomous AI assistant for the Unity Editor. This plan prioritizes simplicity and maintainability by embracing the core architectural strengths of the `UnityNaturalMCP` project.

## Revised Architecture

The previous, complex plan involving WebSockets and a custom task queue has been discarded. The new architecture is a clean, client-server model based on standard HTTP requests.

1.  **Unity C# Server (The "Hands"):**
    *   We will use the forked `UnityNaturalMCP` project almost **as-is**.
    *   Its sole responsibility is to expose tools (C# methods) as simple, discoverable HTTP endpoints.
    *   Adding new capabilities is as simple as writing a new C# method and decorating it with a `[Tool]` attribute.
    *   This component is a **passive RPC-style server**, which is simple, robust, and easy to debug.

2.  **Node.js Agent Host (The "Brain"):**
    *   This is a standalone Node.js application that acts as an **MCP client**.
    *   It contains the agent's core logic: the perception-planning-action loop.
    *   It takes a high-level user prompt (e.g., "Create a red bouncing ball").
    *   It uses an LLM to generate a step-by-step plan, which is a sequence of tool calls.
    *   It executes the plan by making a series of **synchronous, `await`-ed HTTP requests** to the Unity C# server, one for each step in the plan.

## The New, Simplified Workflow

This workflow is linear, easy to understand, and avoids all the complexity of asynchronous queues and custom protocols.

1.  **User Prompt:** The user gives the Node.js agent a high-level goal, like "Create a red cube."

2.  **Planning Phase (Node.js):** The agent's `Planner` module queries the LLM with the prompt and the list of available tools (fetched from the Unity server's `/tools` endpoint). The LLM returns a structured JSON plan:
    ```json
    [
        { "tool": "create_gameobject", "params": { "name": "MyCube", "type": "Cube" } },
        { "tool": "create_asset", "params": { "type": "Material", "name": "RedMaterial" } },
        { "tool": "update_material", "params": { "name": "RedMaterial", "color": "#FF0000" } },
        { "tool": "assign_material", "params": { "objectName": "MyCube", "materialName": "RedMaterial" } }
    ]
    ```

3.  **Execution Phase (Node.js):** The agent's `Executor` module iterates through the plan and makes a standard HTTP `POST` request for each step, awaiting the completion of one before starting the next.
    ```javascript
    // Simplified pseudocode
    for (const step of plan) {
        await fetch(`http://localhost:10000/tools/${step.tool}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(step.params)
        });
    }
    ```

4.  **Completion:** Once all HTTP requests have completed successfully, the agent reports the final result to the user.

## Advantages of This Approach

*   **Drastically Simpler:** Eliminates WebSockets, task queues, producers, and consumers.
*   **True to the Fork:** Leverages `UnityNaturalMCP` for its intended purpose.
*   **Easy to Debug:** Standard HTTP requests can be inspected with common tools like cURL or Postman.
*   **Faster Implementation:** Enables a working prototype much more quickly.
