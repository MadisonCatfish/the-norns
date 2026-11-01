import fs from 'fs'
import path from 'path'

async function exportStatic() {
  const baseUrl = process.env.SITE_URL || 'http://localhost:3000'
  const outDir = path.resolve(process.cwd(), 'out')
  const siteDir = path.resolve(process.cwd(), '../site')

  console.log(`--- Starting Static Site Export from ${baseUrl} ---`)

  // Ensure output directories exist
  if (!fs.existsSync(outDir)) {
    fs.mkdirSync(outDir, { recursive: true })
  }

  const routes = [
    { url: '/', filename: 'index.html' },
    { url: '/roadmap', filename: 'roadmap.html' },
    { url: '/source', filename: 'source.html' },
  ]

  for (const route of routes) {
    try {
      console.log(`Fetching ${route.url}...`)
      const res = await fetch(`${baseUrl}${route.url}`)
      if (!res.ok) {
        throw new Error(`Failed to fetch ${route.url}: ${res.status} ${res.statusText}`)
      }
      const html = await res.text()

      // Write to out/
      const outPath = path.join(outDir, route.filename)
      fs.writeFileSync(outPath, html, 'utf-8')
      console.log(`✓ Saved ${outPath} (${(html.length / 1024).toFixed(1)} KB)`)

      // Also sync to ../site/ for immediate Cloudflare Pages static root compatibility
      if (fs.existsSync(siteDir)) {
        const sitePath = path.join(siteDir, route.filename)
        fs.writeFileSync(sitePath, html, 'utf-8')
        console.log(`✓ Synced to ${sitePath}`)
      }
    } catch (err) {
      console.error(`Error exporting ${route.url}:`, err)
      process.exit(1)
    }
  }

  // Copy public assets to out/
  const publicDir = path.resolve(process.cwd(), 'public')
  if (fs.existsSync(publicDir)) {
    fs.cpSync(publicDir, outDir, { recursive: true })
    console.log(`✓ Copied public assets to ${outDir}`)
  }

  console.log('--- Static Export Complete! Site is ready for Cloudflare Pages ---')
}

exportStatic().catch((err) => {
  console.error('Export failed:', err)
  process.exit(1)
})
