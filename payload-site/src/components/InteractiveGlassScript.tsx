'use client'

import { useEffect } from 'react'

export function InteractiveGlassScript() {
  useEffect(() => {
    // Digital display glass: interactive cursor light revealing fine scratches
    const handleMouseMove = (e: MouseEvent) => {
      const target = (e.target as HTMLElement)?.closest(
        '.metric-grid, .bento-box, .download-card, .hero-card, .master-frame'
      ) as HTMLElement | null
      if (!target) return

      const rect = target.getBoundingClientRect()
      const x = Math.round(e.clientX - rect.left)
      const y = Math.round(e.clientY - rect.top)
      target.style.setProperty('--mouse-x', `${x}px`)
      target.style.setProperty('--mouse-y', `${y}px`)
    }

    window.addEventListener('mousemove', handleMouseMove, { passive: true })
    return () => window.removeEventListener('mousemove', handleMouseMove)
  }, [])

  return null
}
