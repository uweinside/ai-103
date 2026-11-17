# Install and run MCP Inspector

MCP Inspector is the official Model Context Protocol developer tool for discovering MCP tools, inspecting their schemas, making calls, and viewing protocol messages. It is an MCP **client**, not an AI agent.

This guide uses Windows PowerShell and Inspector **2.14.0**, the version installed for this workspace. The four C# teaching apps remain independent and do not require Inspector.

## 1. Prerequisites

Install Node.js with npm from [nodejs.org](https://nodejs.org/). Inspector 2.14.0 requires Node `^22.23.4 || ^24.22.1 || >=26.12.1`; this workspace was run with Node 24.22.2.

Check your terminal:

```powershell
node --version
npm --version
```

Internet access is required to install Inspector and connect to the remote servers. Neither server configured here requires an API key, Azure subscription, or sign-in.

## 2. Install Inspector once

```powershell
npm install --global @modelcontextprotocol/inspector@2.14.0
mcp-inspector --version
```

The version command should print `2.14.0`. Do not reinstall after each computer restart.

## 3. Start Inspector, including after a restart

Open a PowerShell terminal and run:

```powershell
Set-Location C:\Demo\ai-103
$env:HOST = '127.0.0.1'
$env:CLIENT_PORT = '6274'
mcp-inspector --web --config .\scripts\mcp-inspector.json
```

Open **http://127.0.0.1:6274** if a browser does not open automatically. Keep the terminal running while using Inspector. To stop it, press **Ctrl+C** in that terminal.

Inspector does not start automatically with Windows. After restarting the computer, repeat this section; the installation and server configuration are preserved.

If you prefer not to install globally, use this command instead of `mcp-inspector`:

```powershell
npx --yes @modelcontextprotocol/inspector@2.14.0 --web --config .\scripts\mcp-inspector.json
```

Check readiness from a second terminal:

```powershell
Invoke-RestMethod http://127.0.0.1:6274/healthz
```

The response should contain `status: ok`.

## 4. Connect and demonstrate a tool call

The workspace configuration is [scripts/mcp-inspector.json](scripts/mcp-inspector.json). Both entries use **Streamable HTTP**:

| Server name | Endpoint | Purpose |
|---|---|---|
| `microsoft-learn` | `https://learn.microsoft.com/api/mcp` | Search Microsoft documentation and code samples, and fetch documentation articles. |
| `deepwiki-public` | `https://mcp.deepwiki.com/mcp` | Read documentation about public GitHub repositories through a third-party service. |

### Microsoft Learn

1. On **Servers**, connect to `microsoft-learn`.
2. Open **Tools** and inspect the discovered tool list.
3. Select `microsoft_docs_search`.
4. Enter `Microsoft Foundry Agent Framework MCP tools` in `query`.
5. Select **Execute Tool** and inspect the returned content and citations.
6. In the monitoring sidebar, select **Protocol** to inspect initialization, discovery, and the `tools/call` request and response.

### DeepWiki

1. Disconnect from Microsoft Learn before switching servers.
2. Connect to `deepwiki-public` and open **Tools**.
3. Select `read_wiki_structure`.
4. Enter `modelcontextprotocol/inspector` in `repoName`.
5. Execute the tool to retrieve the public repository's documentation topics.

Always discover tools before calling them: the remote server owns their names, schemas, and execution, and these can change. Use only public example queries and repository names; tool arguments are sent to the selected external service. DeepWiki is not a Microsoft or MCP-project server.

## 5. Optional command-line demonstration

From the workspace root:

```powershell
mcp-inspector --cli --config .\scripts\mcp-inspector.json --server microsoft-learn --method tools/list

mcp-inspector --cli --config .\scripts\mcp-inspector.json --server microsoft-learn --method tools/call --tool-name microsoft_docs_search --tool-arg 'query=Microsoft Foundry Agent Framework MCP tools'

mcp-inspector --cli --config .\scripts\mcp-inspector.json --server deepwiki-public --method tools/call --tool-name read_wiki_structure --tool-arg 'repoName=modelcontextprotocol/inspector'
```

These are separate, short-lived connections; the browser UI does not need to be running, and its history will not contain these CLI calls.

## Teaching and safety boundaries

In Inspector, the instructor explicitly selects a tool and its arguments. In an agent integration, the model requests a tool call; the client/runtime checks permissions, validates and dispatches the request to the MCP server, then supplies the result to the model. The model does not execute code by itself. Tool output is context, not authorization to follow instructions embedded in that output.

Inspector is not a transparent monitor for the four C# apps. Connecting a server here does not connect it to those apps or to another agent.

Keep Inspector on loopback with authentication enabled. It can launch local processes when configured to do so; do not expose it to the network, disable its authentication, or put credentials into a shared demo.

`--config` opens the server list read-only. To experiment with additional servers through the UI, copy the JSON file to a new local file outside the repository and use `--catalog` with that copy. Review permissions and data handling before connecting; filesystem/write tools and authenticated services require a separate review.

## Troubleshooting

| Symptom | Action |
|---|---|
| `node` or `npm` is not recognized | Install a supported Node version, then reopen PowerShell. |
| `mcp-inspector` is not recognized | Reopen PowerShell after installation, or use the pinned `npx` command above. |
| PowerShell blocks the npm `.ps1` launcher | Use `npm.cmd`, `npx.cmd`, or `mcp-inspector.cmd` in place of the corresponding command; do not weaken the machine's execution policy. |
| Browser cannot connect after a reboot | Start Inspector again using section 3. A browser tab does not start the server. |
| Port 6274 is already occupied | Do not stop an unrelated process. Set `$env:CLIENT_PORT = '6284'` before launching and browse `http://127.0.0.1:6284`. |
| Browsing the Learn MCP endpoint returns 405 | Expected: it is a protocol endpoint, not a webpage. Connect through Inspector using Streamable HTTP, not legacy SSE. |
| Learn reports schema-portability warnings | Inspector 2.14.0 flags nullable `type` arrays in Learn's output schemas. These are legal JSON Schema, not installation failures; the demonstrated search works. Do not rewrite the remote schemas or hide the warnings. |
| Resources or prompts are unavailable | Servers expose different capabilities; a tool-only server need not provide resources or prompts. |
| TLS, proxy, or remote connection error | Inspect the actual error and check service availability and approved proxy/CA settings. Do not disable certificate verification. |

Inspector v2 serves its UI and backend together on port 6274. Older v1 instructions describing a separate proxy on 6277 and URL authentication tokens do not apply.

## Official documentation

- [MCP Inspector repository and installation requirements](https://github.com/modelcontextprotocol/inspector)
- [Inspector server configuration](https://github.com/modelcontextprotocol/inspector/blob/main/docs/mcp-server-configuration.md)
- [Microsoft Learn MCP Server overview](https://learn.microsoft.com/en-us/training/support/mcp)
- [Microsoft Learn MCP developer reference](https://learn.microsoft.com/en-us/training/support/mcp-developer-reference)
- [DeepWiki MCP documentation](https://docs.devin.ai/work-with-devin/deepwiki-mcp)

See also [SETUP.md](SETUP.md) and [SOURCES.md](SOURCES.md).
