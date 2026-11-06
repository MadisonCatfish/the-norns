import React from 'react'
import { getPayload } from 'payload'
import config from '@payload-config'
import { Navigation } from '@/components/Navigation'
import { Footer } from '@/components/Footer'
import { RoadmapView, RoadmapItem } from '@/components/RoadmapView'

export const dynamic = 'force-dynamic'
export const revalidate = 0

export default async function RoadmapPage() {
  let roadmapItems: RoadmapItem[] = []
  let footerData: any = null
  let headerData: any = null
  let latestRelease: any = null

  try {
    const payload = await getPayload({ config })

    // Fetch roadmap items sorted by order
    const roadmapRes = await payload.find({
      collection: 'roadmap',
      sort: 'order',
      limit: 100,
    })
    roadmapItems = (roadmapRes.docs as any) || []

    // Fetch header and footer data
    headerData = await payload.findGlobal({
      slug: 'header',
    })

    footerData = await payload.findGlobal({
      slug: 'footer',
    })

    // Fetch latest release for navigation
    const releasesRes = await payload.find({
      collection: 'releases',
      where: { isLatest: { equals: true } },
      limit: 1,
    })
    latestRelease = releasesRes.docs[0] || null
  } catch (err) {
    console.error('Roadmap page payload fetch error:', err)
  }

  const installerUrl = latestRelease?.installerUrl || 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.5.3/TheNorns-Setup-1.5.3.exe'
  const portableUrl = latestRelease?.portableUrl || 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.5.3/TheNorns-1.5.3-portable.zip'
  const versionTag = latestRelease?.version || 'v1.5.3'

  return (
    <>
      <Navigation
        data={headerData}
        activePage="roadmap"
        installerUrl={installerUrl}
        portableUrl={portableUrl}
        versionTag={versionTag}
      />
      <RoadmapView items={roadmapItems} />
      <Footer data={footerData} />
    </>
  )
}
