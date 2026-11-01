'use client'

import { useEffect } from 'react'

export function MobileNavScript() {
  useEffect(() => {
    const drawer = document.getElementById('mobile-drawer')
    const openBtn = document.getElementById('drawer-open')
    const closeBtn = document.getElementById('drawer-close')
    if (!drawer || !openBtn) return

    const scroller = drawer.querySelector('.Drawer-scroller') as HTMLElement | null

    function openDrawer() {
      if (!drawer) return
      if ('showPopover' in drawer) {
        try {
          ;(drawer as any).showPopover()
        } catch (_) {}
      }
      if (scroller) {
        scroller.scrollTo({ left: scroller.offsetWidth, behavior: 'instant' as any })
      }
      openBtn?.setAttribute('aria-expanded', 'true')
    }

    function closeDrawer() {
      if (!drawer) return
      if ('hidePopover' in drawer) {
        try {
          ;(drawer as any).hidePopover()
        } catch (_) {}
      }
      openBtn?.setAttribute('aria-expanded', 'false')
    }

    openBtn.addEventListener('click', openDrawer)
    closeBtn?.addEventListener('click', closeDrawer)

    const links = drawer.querySelectorAll('a')
    links.forEach((l) => l.addEventListener('click', closeDrawer))

    return () => {
      openBtn.removeEventListener('click', openDrawer)
      closeBtn?.removeEventListener('click', closeDrawer)
      links.forEach((l) => l.removeEventListener('click', closeDrawer))
    }
  }, [])

  return null
}
