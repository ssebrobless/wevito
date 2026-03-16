import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "playwright";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const PROJECT_ROOT = path.resolve(__dirname, "..", "..");
const HANDOFF_ROOT = path.join(PROJECT_ROOT, "incoming_sprites", "gemini_handoff");
const DEFAULT_PROFILE_ROOT = path.join(PROJECT_ROOT, ".codex-cache", "gemini-profile");
const DEFAULT_DOWNLOAD_ROOT = path.join(PROJECT_ROOT, "incoming_sprites", "gemini_downloads");

function parseArgs(argv) {
  const args = {
    species: "rat",
    age: "adult",
    gender: "male",
    url: "",
    cdpUrl: "",
    profileDir: DEFAULT_PROFILE_ROOT,
    downloadDir: DEFAULT_DOWNLOAD_ROOT,
    headless: false,
    keepOpen: false,
    sendOnly: false,
    setupOnly: false,
    timeoutMs: 180000,
  };

  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];
    const next = argv[index + 1];
    switch (arg) {
      case "--species":
        args.species = next;
        index += 1;
        break;
      case "--age":
        args.age = next;
        index += 1;
        break;
      case "--gender":
        args.gender = next;
        index += 1;
        break;
      case "--url":
        args.url = next;
        index += 1;
        break;
      case "--cdp-url":
        args.cdpUrl = next;
        index += 1;
        break;
      case "--profile-dir":
        args.profileDir = next;
        index += 1;
        break;
      case "--download-dir":
        args.downloadDir = next;
        index += 1;
        break;
      case "--headless":
        args.headless = true;
        break;
      case "--keep-open":
        args.keepOpen = true;
        break;
      case "--send-only":
        args.sendOnly = true;
        break;
      case "--setup-only":
        args.setupOnly = true;
        break;
      case "--timeout-ms":
        args.timeoutMs = Number(next);
        index += 1;
        break;
      default:
        break;
    }
  }

  return args;
}

async function ensureDir(dir) {
  await fs.mkdir(dir, { recursive: true });
}

function variantDir(args) {
  return path.join(HANDOFF_ROOT, args.species, args.age, args.gender);
}

async function requireVariant(args) {
  const dir = variantDir(args);
  const files = {
    dir,
    base: path.join(dir, "1-base-pose.png"),
    board: path.join(dir, "2-editable-board.png"),
    runtime: path.join(dir, "3-runtime-reference-blue.png"),
    prompt: path.join(dir, "4-prompt.txt"),
    saveDir: path.join(dir, "5-save-edited-board-here"),
  };

  for (const target of Object.values(files)) {
    try {
      await fs.access(target);
    } catch {
      throw new Error(`Missing required handoff file or directory: ${target}`);
    }
  }

  return files;
}

async function readPrompt(promptPath) {
  return fs.readFile(promptPath, "utf8");
}

async function launchContext(args) {
  await ensureDir(args.profileDir);
  await ensureDir(args.downloadDir);

  return chromium.launchPersistentContext(args.profileDir, {
    headless: args.headless,
    acceptDownloads: true,
    downloadsPath: args.downloadDir,
    viewport: null,
  });
}

async function connectOverCdp(cdpUrl) {
  const browser = await chromium.connectOverCDP(cdpUrl);
  const context = browser.contexts()[0];
  if (!context) {
    throw new Error(`Connected to ${cdpUrl}, but no browser context was available.`);
  }
  return { browser, context, attachedByCdp: true };
}

async function pickPage(context, url, attachedByCdp) {
  let page = context.pages().find((candidate) => candidate.url().includes("gemini.google.com"));
  if (!page) {
    if (attachedByCdp) {
      throw new Error("No existing Gemini page was found in the connected Chrome session. Open the target Gemini chat first.");
    }
    page = context.pages()[0] ?? await context.newPage();
  }

  if (url) {
    await page.goto(url, { waitUntil: "domcontentloaded" });
  } else if (!attachedByCdp && (!page.url() || page.url() === "about:blank")) {
    await page.goto("https://gemini.google.com/", { waitUntil: "domcontentloaded" });
  }

  return page;
}

async function waitForPromptSurface(page, timeoutMs) {
  const selectors = [
    'textarea',
    '[contenteditable="true"][role="textbox"]',
    '[contenteditable="true"]',
  ];

  for (const selector of selectors) {
    const locator = page.locator(selector).first();
    try {
      await locator.waitFor({ state: "visible", timeout: 2500 });
      return locator;
    } catch {
      // keep trying
    }
  }

  await page.waitForTimeout(timeoutMs);
  throw new Error("Could not find Gemini prompt input. Make sure the browser profile is logged in and the correct chat is open.");
}

async function trySetFilesDirectly(page, filePaths) {
  const input = page.locator('input[type="file"]').first();
  if (await input.count()) {
    try {
      await input.setInputFiles(filePaths);
      return true;
    } catch {
      return false;
    }
  }
  return false;
}

async function clickAttachmentAndUpload(page, filePaths) {
  const selectors = [
    'button[aria-label*="upload" i]',
    'button[aria-label*="attach" i]',
    'button[aria-label*="add file" i]',
    'button[aria-label*="add files" i]',
    'button[aria-label*="plus" i]',
    '[role="button"][aria-label*="upload" i]',
    '[role="button"][aria-label*="attach" i]',
  ];

  for (const selector of selectors) {
    const button = page.locator(selector).first();
    if (!await button.count()) {
      continue;
    }

    try {
      await button.scrollIntoViewIfNeeded();
      const chooserPromise = page.waitForEvent("filechooser", { timeout: 4000 }).catch(() => null);
      await button.click({ timeout: 4000 });
      const chooser = await chooserPromise;
      if (chooser) {
        await chooser.setFiles(filePaths);
        return true;
      }
      if (await trySetFilesDirectly(page, filePaths)) {
        return true;
      }
    } catch {
      // continue trying
    }
  }

  return false;
}

async function uploadInputs(page, files) {
  const filePaths = [files.base, files.board, files.runtime];
  if (await trySetFilesDirectly(page, filePaths)) {
    return;
  }

  if (await clickAttachmentAndUpload(page, filePaths)) {
    return;
  }

  throw new Error("Could not upload files to Gemini automatically. Keep the target chat open and verify the attachment control is visible.");
}

async function fillPrompt(locator, prompt) {
  const tagName = await locator.evaluate((element) => element.tagName.toLowerCase());
  await locator.click();
  if (tagName === "textarea") {
    await locator.fill(prompt);
  } else {
    await locator.evaluate((element, value) => {
      element.textContent = "";
      element.dispatchEvent(new InputEvent("input", { bubbles: true, inputType: "deleteContentBackward" }));
      element.textContent = value;
      element.dispatchEvent(new InputEvent("input", { bubbles: true, inputType: "insertText", data: value }));
    }, prompt);
  }
}

async function clickSend(page) {
  const selectors = [
    'button[aria-label*="send" i]',
    '[role="button"][aria-label*="send" i]',
    'button:has-text("Send")',
  ];

  for (const selector of selectors) {
    const button = page.locator(selector).first();
    if (!await button.count()) {
      continue;
    }

    try {
      await button.click({ timeout: 4000 });
      return true;
    } catch {
      // continue trying
    }
  }

  return false;
}

async function waitForDownloadButton(page) {
  const selectors = [
    'button[aria-label*="download" i]',
    '[role="button"][aria-label*="download" i]',
    'a[download]',
  ];

  const deadline = Date.now() + 180000;
  while (Date.now() < deadline) {
    for (const selector of selectors) {
      const locator = page.locator(selector).last();
      if (await locator.count()) {
        try {
          await locator.waitFor({ state: "visible", timeout: 1500 });
          return locator;
        } catch {
          // ignore and continue
        }
      }
    }
    await page.waitForTimeout(1500);
  }

  return null;
}

async function downloadLatestImage(page, files, args) {
  const downloadButton = await waitForDownloadButton(page);
  if (!downloadButton) {
    throw new Error("Gemini response appeared to complete, but no download control was found.");
  }

  const downloadPromise = page.waitForEvent("download", { timeout: args.timeoutMs });
  await downloadButton.click();
  const download = await downloadPromise;

  const targetPath = path.join(
    files.saveDir,
    `${args.species}-${args.age}-${args.gender}-edited-board${path.extname(download.suggestedFilename()) || ".png"}`
  );

  await download.saveAs(targetPath);
  return targetPath;
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const files = await requireVariant(args);
  const prompt = await readPrompt(files.prompt);
  const launchResult = args.cdpUrl ? await connectOverCdp(args.cdpUrl) : { context: await launchContext(args), attachedByCdp: false };
  const page = await pickPage(launchResult.context, args.url, launchResult.attachedByCdp);

  console.log(`Using handoff folder: ${files.dir}`);
  console.log(launchResult.attachedByCdp ? `Connected to Chrome CDP: ${args.cdpUrl}` : `Profile: ${args.profileDir}`);
  console.log(`Current page: ${page.url()}`);

  if (args.setupOnly) {
    console.log("Setup-only mode: log into Gemini, open the target chat, then rerun without --setup-only.");
    if (!args.keepOpen && !launchResult.attachedByCdp) {
      await page.bringToFront();
      await page.waitForTimeout(3000);
      await launchResult.context.close();
    }
    return;
  }

  await page.bringToFront();
  const promptSurface = await waitForPromptSurface(page, 5000);
  await uploadInputs(page, files);
  await fillPrompt(promptSurface, prompt);

  const sent = await clickSend(page);
  if (!sent) {
    throw new Error("Could not find Gemini send button automatically.");
  }

  console.log("Prompt submitted.");

  if (args.sendOnly) {
    console.log("Send-only mode: leaving download step to the user.");
    if (!args.keepOpen && !launchResult.attachedByCdp) {
      await launchResult.context.close();
    }
    return;
  }

  const savedPath = await downloadLatestImage(page, files, args);
  console.log(`Downloaded edited board to: ${savedPath}`);

  if (!args.keepOpen && !launchResult.attachedByCdp) {
    await launchResult.context.close();
  }
}

main().catch((error) => {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
});
