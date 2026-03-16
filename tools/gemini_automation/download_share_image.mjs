import fs from 'node:fs/promises';
import path from 'node:path';
import { chromium } from 'playwright';

function normalizeImageUrl(url) {
  return url.replace(/=s\d+[^?]*/, '=s0');
}

async function main() {
  const [shareUrl, outputPath] = process.argv.slice(2);
  if (!shareUrl || !outputPath) {
    throw new Error('Usage: node download_share_image.mjs <share_url> <output_path>');
  }

  const browser = await chromium.launch({ headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1600, height: 2200 } });
    await page.goto(shareUrl, { waitUntil: 'domcontentloaded', timeout: 120000 });
    await page.waitForFunction(
      () => !!document.querySelector('img[alt=", AI generated"]'),
      { timeout: 120000 }
    );

    const src = await page.$eval('img[alt=", AI generated"]', (img) => img.src);
    const directUrl = normalizeImageUrl(src);
    const response = await fetch(directUrl);
    if (!response.ok) {
      throw new Error(`Failed to download image: ${response.status} ${response.statusText}`);
    }

    const bytes = Buffer.from(await response.arrayBuffer());
    await fs.mkdir(path.dirname(outputPath), { recursive: true });
    await fs.writeFile(outputPath, bytes);
    process.stdout.write(outputPath);
  } finally {
    await browser.close();
  }
}

main().catch((error) => {
  console.error(error.stack || String(error));
  process.exit(1);
});
