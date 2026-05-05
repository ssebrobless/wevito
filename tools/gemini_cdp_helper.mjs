import fs from "node:fs/promises";

const port = Number(process.env.GEMINI_CHROME_PORT || 9222);
const command = process.argv[2] || "status";
const defaultGeminiUrl = "https://gemini.google.com/u/1/app";

async function getJson(path) {
  const response = await fetch(`http://127.0.0.1:${port}${path}`);
  if (!response.ok) {
    throw new Error(`CDP request failed: ${response.status} ${response.statusText}`);
  }
  return response.json();
}

async function findPage() {
  const targets = await getJson("/json/list");
  const pages = targets.filter((target) => target.type === "page");
  const preferred =
    pages.find((target) => target.url.includes("gemini.google.com")) ||
    pages.find((target) => target.url.includes("accounts.google.com")) ||
    pages[0];

  if (!preferred?.webSocketDebuggerUrl) {
    throw new Error("No controllable Chrome page target found.");
  }

  return preferred;
}

function createClient(webSocketDebuggerUrl) {
  const socket = new WebSocket(webSocketDebuggerUrl);
  let nextId = 1;
  const pending = new Map();

  socket.addEventListener("message", (event) => {
    const payload = JSON.parse(event.data);
    if (!payload.id || !pending.has(payload.id)) {
      return;
    }

    const { resolve, reject } = pending.get(payload.id);
    pending.delete(payload.id);
    if (payload.error) {
      reject(new Error(`${payload.error.message}: ${payload.error.data || ""}`));
    } else {
      resolve(payload.result || {});
    }
  });

  const ready = new Promise((resolve, reject) => {
    socket.addEventListener("open", resolve, { once: true });
    socket.addEventListener("error", reject, { once: true });
  });

  return {
    async send(method, params = {}) {
      await ready;
      const id = nextId++;
      const message = JSON.stringify({ id, method, params });
      const result = new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
      socket.send(message);
      return result;
    },
    async close() {
      await ready;
      socket.close();
    },
  };
}

async function withPage(callback) {
  const page = await findPage();
  const client = createClient(page.webSocketDebuggerUrl);
  try {
    return await callback(client, page);
  } finally {
    await client.close();
  }
}

async function status() {
  const version = await getJson("/json/version");
  const targets = await getJson("/json/list");
  console.log(JSON.stringify({ port, browser: version.Browser, targets }, null, 2));
}

async function openUrl() {
  const url = process.argv[3] || defaultGeminiUrl;
  await withPage(async (client) => {
    await client.send("Page.bringToFront");
    await client.send("Page.navigate", { url });
  });
  console.log(JSON.stringify({ opened: url }, null, 2));
}

async function screenshot() {
  const output = process.argv[3] || "vnext/artifacts/workflow-runs/gemini-cdp-screenshot.png";
  await withPage(async (client) => {
    await client.send("Page.enable");
    await client.send("Page.bringToFront");
    const result = await client.send("Page.captureScreenshot", { format: "png", fromSurface: true });
    await fs.mkdir(output.split(/[\\/]/).slice(0, -1).join("/") || ".", { recursive: true });
    await fs.writeFile(output, Buffer.from(result.data, "base64"));
  });
  console.log(JSON.stringify({ screenshot: output }, null, 2));
}

async function evaluate() {
  const expression = process.argv.slice(3).join(" ");
  if (!expression) {
    throw new Error("Provide a JavaScript expression to evaluate.");
  }

  await withPage(async (client) => {
    await client.send("Runtime.enable");
    await client.send("Page.bringToFront");
    const result = await client.send("Runtime.evaluate", {
      expression,
      returnByValue: true,
      awaitPromise: true,
    });
    console.log(JSON.stringify(result.result?.value ?? result.result, null, 2));
  });
}

const commands = {
  status,
  open: openUrl,
  screenshot,
  eval: evaluate,
};

if (!commands[command]) {
  throw new Error(`Unknown command "${command}". Use status, open, screenshot, or eval.`);
}

await commands[command]();
