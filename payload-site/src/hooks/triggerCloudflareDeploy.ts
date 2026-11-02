import type {
  CollectionAfterChangeHook,
  CollectionConfig,
  GlobalAfterChangeHook,
  GlobalConfig,
} from 'payload'

// Notify Cloudflare Pages deploy hook
async function notifyDeploy(hookUrl: string) {
  try {
    console.log(`[Auto-Deploy] Triggering Cloudflare Pages Deploy Hook: ${hookUrl}`)
    const res = await fetch(hookUrl, { method: 'POST' })
    if (res.ok) {
      console.log(`[Auto-Deploy] ✓ Cloudflare Pages build triggered successfully!`)
    } else {
      console.warn(`[Auto-Deploy] Cloudflare deploy hook returned ${res.status}`)
    }
  } catch (err) {
    console.error(`[Auto-Deploy] Failed to call Cloudflare deploy hook:`, err)
  }
}

async function deployIfEnabled(req: any, doc: any, where: string) {
  if (doc?._status === 'draft') return
  try {
    const settings: any = await req.payload.findGlobal({ slug: 'site-settings' })
    const hookUrl = process.env.CLOUDFLARE_DEPLOY_HOOK || settings?.cloudflareDeployHook
    if (settings?.autoDeployOnSave && hookUrl) {
      notifyDeploy(hookUrl)
    }
  } catch (e) {
    console.error(`Error in ${where}:`, e)
  }
}

export const afterChangeDeployHook: CollectionAfterChangeHook = async ({ req, doc }) => {
  await deployIfEnabled(req, doc, 'afterChangeDeployHook')
  return doc
}

export const globalAfterChangeDeployHook: GlobalAfterChangeHook = async ({ req, doc }) => {
  await deployIfEnabled(req, doc, 'globalAfterChangeDeployHook')
  return doc
}

export function withDeployHook(collection: CollectionConfig): CollectionConfig {
  return {
    ...collection,
    hooks: {
      ...collection.hooks,
      afterChange: [...(collection.hooks?.afterChange ?? []), afterChangeDeployHook],
    },
  }
}

export function withGlobalDeployHook(global: GlobalConfig): GlobalConfig {
  return {
    ...global,
    hooks: {
      ...global.hooks,
      afterChange: [...(global.hooks?.afterChange ?? []), globalAfterChangeDeployHook],
    },
  }
}
