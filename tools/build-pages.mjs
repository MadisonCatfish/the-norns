import fs from 'node:fs';
import path from 'node:path';

const BASE_URL = process.env.SITE_URL || 'https://the-norns-admin.vercel.app';
const routes = [
  { url: '/', file: 'site/index.html' },
  { url: '/roadmap', file: 'site/roadmap.html' },
  { url: '/source', file: 'site/source.html' },
];

console.log(`[Cloudflare Build] Fetching latest HTML from ${BASE_URL}...`);

for (const { url, file } of routes) {
  try {
    const res = await fetch(`${BASE_URL}${url}`, {
      headers: {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
      },
    });
    if (!res.ok) {
      throw new Error(`Failed to fetch ${url}: ${res.status} ${res.statusText}`);
    }
    const html = await res.text();
    const filePath = path.resolve(process.cwd(), file);
    fs.writeFileSync(filePath, html, 'utf-8');
    console.log(`[OK] Updated ${file} (${(html.length / 1024).toFixed(1)} KB)`);
  } catch (err) {
    console.error(`[ERROR] Failed exporting ${url}:`, err.message);
    process.exit(1);
  }
}

console.log('[Cloudflare Build] Static export completed successfully.');
