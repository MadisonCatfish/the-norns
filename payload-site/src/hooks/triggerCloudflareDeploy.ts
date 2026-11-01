import type { CollectionAfterChangeHook, GlobalAfterChangeHook } from 'payload'

export async function notifyCloudflareDeploy(hookUrl?: string) {
  if (!hookUrl) return

  try {
    console.log(`[Auto-Deploy] Triggering Cloudflare Pages Deploy Hook...`)
    const res = await fetch(hookUrl, {
      method: 'POST',
    })
    if (res.ok) {
      console.log(`[Auto-Deploy] ✓ Cloudflare Pages build triggered successfully!`)
    } else {
      console.warn(`[Auto-Deploy] Cloudflare deploy hook returned ${res.status}`)
    }
  } catch (err) {
    console.error(`[Auto-Deploy] Failed to call Cloudflare deploy hook:`, err)
  }
}

export const afterChangeDeployHook: CollectionAfterChangeHook = async ({ req, doc }) => {
  try {
    const settings: any = await req.payload.findGlobal({ slug: 'site-settings' })
    const hookUrl = process.env.CLOUDFLARE_DEPLOY_HOOK || settings?.cloudflareDeployHook

    if (settings?.autoDeployOnSave && hookUrl) {
      // Fire-and-forget background notification so it doesn't block admin saves
      notifyCloudflareDeploy(hookUrl)
    }
  } catch (e) {
    console.error('Error in afterChangeDeployHook:', e)
  }
  return doc
}

export const globalAfterChangeDeployHook: GlobalAfterChangeHook = async ({ req, doc }) => {
  try {
    const settings: any = await req.payload.findGlobal({ slug: 'site-settings' })
    const hookUrl = process.env.CLOUDFLARE_DEPLOY_HOOK || settings?.cloudflareDeployHook

    if (settings?.autoDeployOnSave && hookUrl) {
      notifyCloudflareDeploy(hookUrl)
    }
  } catch (e) {
    console.error('Error in globalAfterChangeDeployHook:', e)
  }
  return doc
}
