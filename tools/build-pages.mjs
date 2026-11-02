import fs from 'node:fs';
import path from 'node:path';

const BASE_URL = process.env.SITE_URL || 'https://the-norns-admin.vercel.app';
const routes = [
  { url: '/', file: 'site/index.html' },
  { url: '/roadmap', file: 'site/roadmap.html' },
  { url: '/source', file: 'site/source.html' },
];

const HTML_REF = /(?:src|href)="(\/_next\/[^"]+)"/g;
const ASSET_REF = /\/_next\/static\/[^"'`\s)<>\\]+/g;

async function get(url) {
  const res = await fetch(url, {
    headers: {
      'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
    },
  });
  if (!res.ok) {
    throw new Error(`${url}: ${res.status} ${res.statusText}`);
  }
  return res;
}

function refsIn(text, html) {
  const found = new Set();
  for (const m of text.matchAll(html ? HTML_REF : ASSET_REF)) {
    found.add(html ? m[1] : m[0]);
  }
  return [...found];
}

console.log(`[Cloudflare Build] Fetching latest site from ${BASE_URL}...`);

const files = new Map();
const htmlPages = [];

for (const { url, file } of routes) {
  try {
    const res = await get(`${BASE_URL}${url}`);
    const html = await res.text();
    htmlPages.push(html);
    files.set(file, html);
    console.log(`[OK] Fetched ${url} (${(html.length / 1024).toFixed(1)} KB)`);
  } catch (err) {
    console.error(`[ERROR] Failed fetching ${url}:`, err.message);
    process.exit(1);
  }
}

const queue = htmlPages.flatMap((html) => refsIn(html, true));
const seen = new Set();
while (queue.length > 0) {
  const ref = queue.shift();
  if (seen.has(ref)) continue;
  seen.add(ref);

  try {
    const res = await get(`${BASE_URL}${ref}`);
    if ((res.headers.get('content-type') || '').includes('text/html')) {
      continue;
    }
    const buf = Buffer.from(await res.arrayBuffer());
    files.set(`site${ref}`, buf);
    if (/\.(js|css)$/.test(ref)) {
      queue.push(...refsIn(buf.toString('utf-8'), false));
    }
  } catch (err) {
    console.warn(`[WARN] Skipping asset ${ref}:`, err.message);
  }
}

for (const [relFile, content] of files) {
  const dest = path.resolve(process.cwd(), relFile);
  fs.mkdirSync(path.dirname(dest), { recursive: true });
  fs.writeFileSync(dest, content);
}

console.log(`[Cloudflare Build] Static export completed successfully (${files.size} files written).`);
