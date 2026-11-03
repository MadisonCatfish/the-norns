import { NextResponse } from 'next/server'
import { getPayload } from 'payload'
import config from '@payload-config'

export async function POST(req: Request) {
  try {
    const payload = await getPayload({ config })
    const siteSettings: any = await payload.findGlobal({
      slug: 'site-settings',
    })

    const hookUrl = process.env.CLOUDFLARE_DEPLOY_HOOK || siteSettings?.cloudflareDeployHook

    if (!hookUrl) {
      return NextResponse.json(
        {
          success: false,
          message: 'No Cloudflare Deploy Hook URL configured. Please check Site Settings in the admin panel.',
        },
        { status: 400 },
      )
    }

    console.log(`[Cloudflare Deploy] Triggering webhook: ${hookUrl}`)
    const response = await fetch(hookUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
    })

    // Cloudflare returns 304 Not Modified when a build is already active or queued
    if (response.status === 304) {
      return NextResponse.json({
        success: true,
        message: 'Deployment is already queued or currently running on Cloudflare Pages (~30s).',
      })
    }

    if (!response.ok) {
      const errText = await response.text()
      const statusCode = response.status >= 400 && response.status <= 599 ? response.status : 502
      return NextResponse.json(
        {
          success: false,
          message: `Cloudflare returned error ${response.status}: ${errText}`,
        },
        { status: statusCode },
      )
    }

    const data = await response.json().catch(() => ({}))

    return NextResponse.json({
      success: true,
      message: 'Cloudflare Pages deployment triggered successfully! Your site will be live in ~30 seconds.',
      data,
    })
  } catch (err: any) {
    console.error('[Cloudflare Deploy] Error triggering deploy hook:', err)
    return NextResponse.json(
      {
        success: false,
        message: `Deployment trigger failed: ${err.message}`,
      },
      { status: 500 },
    )
  }
}

export async function GET() {
  return NextResponse.json({
    status: 'ok',
    description: 'POST to this endpoint to trigger a Cloudflare Pages static site deployment.',
  })
}
