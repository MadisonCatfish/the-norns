'use client'

import { useEffect } from 'react'

// The mobile menu: a popover whose scroller holds the sheet on the left and an empty column the width of the screen
// (styles.css). Scrolled to its end the sheet is out of view (closed); scrolled to 0 it is in (open). Opening shows
// the popover at the closed end and slides the sheet in; a swipe back, a tap on the empty column, the close key or a
// link slide it out and hide the popover.
export function MobileNavScript() {
  useEffect(() => {
    const drawer = document.getElementById('mobile-drawer')
    const openBtn = document.getElementById('drawer-open')
    const closeBtn = document.getElementById('drawer-close')
    if (!drawer || !openBtn) return

    const scroller = drawer.querySelector('.Drawer-scroller') as HTMLElement | null
    const sheet = drawer.querySelector('.Drawer-sheet') as HTMLElement | null
    const closedAt = () => (scroller ? scroller.scrollWidth - scroller.clientWidth : 0)
    let open = false
    let arrived = false // the sheet has come in since it was opened (the slide starts at the closed end)
    let settleTimer: number | undefined

    function hide() {
      open = false
      if ('hidePopover' in drawer!) {
        try {
          ;(drawer as any).hidePopover()
        } catch (_) {}
      }
      openBtn?.setAttribute('aria-expanded', 'false')
    }

    function openDrawer() {
      if (!drawer) return
      if ('showPopover' in drawer) {
        try {
          ;(drawer as any).showPopover()
        } catch (_) {}
      }
      open = true
      arrived = false
      openBtn?.setAttribute('aria-expanded', 'true')
      if (!scroller) return
      // start closed, then slide in on the next frame
      scroller.scrollTo({ left: closedAt(), behavior: 'instant' as ScrollBehavior })
      requestAnimationFrame(() => {
        scroller.scrollTo({ left: 0, behavior: 'smooth' })
        sheet?.focus({ preventScroll: true })
      })
    }

    function closeDrawer() {
      if (!open) return
      if (!scroller) return hide()
      scroller.scrollTo({ left: closedAt(), behavior: 'smooth' })
      // hidden when the sheet is out (onScroll); a fallback in case no scroll event comes
      window.clearTimeout(settleTimer)
      settleTimer = window.setTimeout(() => {
        if (open) hide()
      }, 600)
    }

    // the sheet swiped out, or slid out by closeDrawer: hide the popover
    function onScroll() {
      if (!open || !scroller) return
      if (scroller.scrollLeft < closedAt() / 2) arrived = true
      if (arrived && scroller.scrollLeft >= closedAt() - 2) {
        window.clearTimeout(settleTimer)
        hide()
      }
    }

    // a tap on the empty column beside the sheet
    function onScrollerClick(e: MouseEvent) {
      if (e.target === scroller) closeDrawer()
    }

    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape' && open) closeDrawer()
    }

    openBtn.addEventListener('click', openDrawer)
    closeBtn?.addEventListener('click', closeDrawer)
    scroller?.addEventListener('scroll', onScroll, { passive: true })
    scroller?.addEventListener('click', onScrollerClick)
    document.addEventListener('keydown', onKey)

    const links = drawer.querySelectorAll('a')
    links.forEach((l) => l.addEventListener('click', closeDrawer))

    return () => {
      window.clearTimeout(settleTimer)
      openBtn.removeEventListener('click', openDrawer)
      closeBtn?.removeEventListener('click', closeDrawer)
      scroller?.removeEventListener('scroll', onScroll)
      scroller?.removeEventListener('click', onScrollerClick)
      document.removeEventListener('keydown', onKey)
      links.forEach((l) => l.removeEventListener('click', closeDrawer))
    }
  }, [])

  return null
}
