import fs from 'fs'
import path from 'path'

async function exportStatic() {
  const baseUrl = process.env.SITE_URL || 'https://the-norns-admin.vercel.app'
  const outDir = path.resolve(process.cwd(), 'out')
  const siteDir = path.resolve(process.cwd(), '../site')
  const nextStaticDir = path.resolve(process.cwd(), '.next/static')

  console.log(`--- Starting Static Site Export from ${baseUrl} ---`)

  // Ensure output directories exist
  if (!fs.existsSync(outDir)) {
    fs.mkdirSync(outDir, { recursive: true })
  }
  if (!fs.existsSync(siteDir)) {
    fs.mkdirSync(siteDir, { recursive: true })
  }

  const routes = [
    { url: '/', filename: 'index.html' },
    { url: '/roadmap', filename: 'roadmap.html' },
    { url: '/source', filename: 'source.html' },
  ]

  for (const route of routes) {
    try {
      console.log(`Fetching ${route.url}...`)
      const res = await fetch(`${baseUrl}${route.url}`, {
        headers: { 'User-Agent': 'Mozilla/5.0' },
      })
      if (!res.ok) {
        throw new Error(`Failed to fetch ${route.url}: ${res.status} ${res.statusText}`)
      }
      const html = await res.text()

      // Write to out/
      const outPath = path.join(outDir, route.filename)
      fs.writeFileSync(outPath, html, 'utf-8')
      console.log(`[OK] Saved ${outPath} (${(html.length / 1024).toFixed(1)} KB)`)

      // Also sync to ../site/
      const sitePath = path.join(siteDir, route.filename)
      fs.writeFileSync(sitePath, html, 'utf-8')
      console.log(`[OK] Synced to ${sitePath}`)
    } catch (err) {
      console.error(`Error exporting ${route.url}:`, err)
      process.exit(1)
    }
  }

  // Copy public assets to out/ and site/
  const publicDir = path.resolve(process.cwd(), 'public')
  if (fs.existsSync(publicDir)) {
    fs.cpSync(publicDir, outDir, { recursive: true })
    fs.cpSync(publicDir, siteDir, { recursive: true })
    console.log(`[OK] Copied public assets to ${outDir} and ${siteDir}`)
  }

  // Copy Next.js static bundle (_next/static) to out/ and site/
  if (fs.existsSync(nextStaticDir)) {
    const outNext = path.join(outDir, '_next/static')
    const siteNext = path.join(siteDir, '_next/static')
    fs.mkdirSync(path.dirname(outNext), { recursive: true })
    fs.mkdirSync(path.dirname(siteNext), { recursive: true })
    fs.cpSync(nextStaticDir, outNext, { recursive: true })
    fs.cpSync(nextStaticDir, siteNext, { recursive: true })
    console.log(`[OK] Copied .next/static bundle to ${outNext} and ${siteNext}`)
  }

  console.log('--- Static Export Complete! Site is ready for Cloudflare Pages ---')
}

exportStatic().catch((err) => {
  console.error('Export failed:', err)
  process.exit(1)
})
