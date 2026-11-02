import { getPayload } from 'payload'
import config from '../payload.config'
import fs from 'fs'
import path from 'path'

async function syncCleanData() {
  console.log('Connecting to database:', process.env.DATABASE_URI ? 'PostgreSQL (Neon)' : 'SQLite')
  const payload = await getPayload({ config })

  // 1. Update Roadmap Items from roadmap_seed.json
  const seedPath = path.resolve(process.cwd(), 'src/roadmap_seed.json')
  const items = JSON.parse(fs.readFileSync(seedPath, 'utf-8'))

  console.log(`Syncing ${items.length} roadmap items...`)
  for (const item of items) {
    const existing = await payload.find({
      collection: 'roadmap',
      where: {
        order: { equals: item.order },
      },
      limit: 1,
    })

    if (existing.totalDocs > 0) {
      const doc = existing.docs[0]
      await payload.update({
        collection: 'roadmap',
        id: doc.id,
        data: {
          title: item.title,
          description: item.description,
          itemStatus: item.status || item.itemStatus,
          statusLabel: item.statusLabel,
          checklist: item.checklist,
          _status: 'published',
        },
      })
      console.log(`[UPDATED] #${item.order} ${item.title}`)
    } else {
      await payload.create({
        collection: 'roadmap',
        data: {
          order: item.order,
          title: item.title,
          description: item.description,
          itemStatus: item.status || item.itemStatus,
          statusLabel: item.statusLabel,
          checklist: item.checklist,
          _status: 'published',
        },
      })
      console.log(`[CREATED] #${item.order} ${item.title}`)
    }
  }

  // 2. Update Footer Global with full CCP disclaimer
  console.log('Updating Footer global with full CCP disclaimer...')
  await payload.updateGlobal({
    slug: 'footer',
    data: {
      copyrightNotice: '© 2026 The Norns (ノルン). Independent open-source companion under the MIT License.',
      ccpDisclaimer: 'EVE related materials © 2014 CCP hf. All rights reserved. "EVE", "EVE Online", "CCP", and all related logos and images are trademarks or registered trademarks of CCP hf. The Norns is not affiliated with, endorsed or sponsored by CCP hf.',
      _status: 'published',
    } as any,
  })
  console.log('✓ Footer updated successfully!')

  process.exit(0)
}

syncCleanData().catch(err => {
  console.error('Error syncing clean data:', err)
  process.exit(1)
})
