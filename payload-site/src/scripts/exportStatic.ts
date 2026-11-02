import fs from 'fs'
import path from 'path'

// Pages reference their bundles as /_next/... ; bundles reference chunks and CSS the same way.
const HTML_REF = /(?:src|href)="(\/_next\/[^"]+)"/g
const ASSET_REF = /\/_next\/static\/[^"'`\s)<>\\]+/g

async function get(url: string): Promise<Response> {
  const res = await fetch(url, { headers: { 'User-Agent': 'Mozilla/5.0' } })
  if (!res.ok) {
    throw new Error(`${url}: ${res.status} ${res.statusText}`)
  }
  return res
}

function refsIn(text: string, html: boolean): string[] {
  const found = new Set<string>()
  for (const m of text.matchAll(html ? HTML_REF : ASSET_REF)) {
    found.add(html ? m[1] : m[0])
  }
  return [...found]
}

async function exportStatic() {
  // Everything comes from one server, so the HTML and the assets it names are from the same build.
  const baseUrl = process.env.SITE_URL || 'https://the-norns-admin.vercel.app'
  const outDir = path.resolve(process.cwd(), 'out')
  const siteDir = path.resolve(process.cwd(), process.env.EXPORT_SITE_DIR || '../site')
  const publicDir = path.resolve(process.cwd(), 'public')

  console.log(`--- Starting Static Site Export from ${baseUrl} ---`)

  const routes = [
    { url: '/', filename: 'index.html' },
    { url: '/roadmap', filename: 'roadmap.html' },
    { url: '/source', filename: 'source.html' },
  ]

  // Fetch everything first, so a failed request leaves the output folders untouched.
  const files = new Map<string, string | Buffer>()
  const htmlPages: string[] = []
  for (const route of routes) {
    console.log(`Fetching ${route.url}...`)
    const html = await (await get(`${baseUrl}${route.url}`)).text()
    htmlPages.push(html)
    files.set(`/${route.filename}`, html)
  }

  const queue = htmlPages.flatMap((html) => refsIn(html, true))
  const seen = new Set<string>()
  while (queue.length > 0) {
    const ref = queue.shift() as string
    if (seen.has(ref)) continue
    seen.add(ref)

    const res = await get(`${baseUrl}${ref}`)
    if ((res.headers.get('content-type') || '').includes('text/html')) {
      throw new Error(`${ref} is HTML, not an asset: the path is wrong`)
    }
    const body = Buffer.from(await res.arrayBuffer())
    files.set(ref, body)
    if (/\.(js|css)$/.test(ref)) {
      queue.push(...refsIn(body.toString('utf-8'), false))
    }
  }
  console.log(`[OK] ${routes.length} pages and ${seen.size} _next assets fetched`)

  // Write to out/ and to the site folder. The old _next folder goes first, so no stale or dev-build files remain.
  for (const dir of [outDir, siteDir]) {
    fs.rmSync(path.join(dir, '_next'), { recursive: true, force: true })
    for (const [rel, content] of files) {
      const dest = path.join(dir, rel)
      fs.mkdirSync(path.dirname(dest), { recursive: true })
      fs.writeFileSync(dest, content)
    }
    if (fs.existsSync(publicDir)) {
      fs.cpSync(publicDir, dir, { recursive: true })
    }
    console.log(`[OK] Wrote ${files.size} files to ${dir}`)
  }

  console.log('--- Static Export Complete! Site is ready for Cloudflare Pages ---')
}

exportStatic().catch((err) => {
  console.error('Export failed:', err)
  process.exit(1)
})
