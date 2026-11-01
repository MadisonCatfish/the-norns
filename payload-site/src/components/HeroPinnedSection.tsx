'use client'

import React, { useEffect, useState, useRef } from 'react'

export interface PhraseItem {
  text: string
  id?: string | null
}

interface HeroPinnedSectionProps {
  prefix?: string | null
  phrases?: PhraseItem[] | null
  badgeText?: string | null
  badgeUrl?: string | null
  subtitle?: string | null
  installerUrl: string
  portableUrl: string
  versionTag: string
}

export function HeroPinnedSection({
  prefix,
  phrases,
  badgeText,
  badgeUrl,
  subtitle,
  installerUrl,
  portableUrl,
  versionTag,
}: HeroPinnedSectionProps) {
  const defaultPhrases: PhraseItem[] = [
    { text: 'EDENCOM krabbing.' },
    { text: 'Stormbringer fleets.' },
    { text: 'Superkrabbing DPS telemetry.' },
  ]

  const items = phrases && phrases.length > 0 ? phrases : defaultPhrases
  const staticPrefix = prefix ?? 'Fleet HUD for'

  const trackRef = useRef<HTMLDivElement>(null)
  const [activeIdx, setActiveIdx] = useState(0)
  const [displayedIdx, setDisplayedIdx] = useState(0)
  const [phase, setPhase] = useState<'idle' | 'out' | 'in'>('idle')
  const activeIdxRef = useRef(0)

  useEffect(() => {
    if (items.length <= 1) return

    const handleScroll = () => {
      if (!trackRef.current) return
      const rect = trackRef.current.getBoundingClientRect()
      const stickyTop = 70
      
      // Calculate responsive scroll progress through the compact pinning runway (280px)
      const scrolledDist = Math.max(0, stickyTop - rect.top)
      const pinDistance = 280

      const progress = Math.min(Math.max(0, scrolledDist / pinDistance), 0.999)
      const count = items.length
      const segment = 1 / count
      const newIdx = Math.min(Math.floor(progress / segment), count - 1)

      if (newIdx !== activeIdxRef.current) {
        activeIdxRef.current = newIdx
        setActiveIdx(newIdx)
      }
    }

    window.addEventListener('scroll', handleScroll, { passive: true })
    handleScroll()
    return () => window.removeEventListener('scroll', handleScroll)
  }, [items.length])

  // Responsive, snappy blur and fade transition
  useEffect(() => {
    if (activeIdx === displayedIdx) return

    setPhase('out')
    const timer1 = setTimeout(() => {
      setDisplayedIdx(activeIdx)
      setPhase('in')
      const timer2 = setTimeout(() => {
        setPhase('idle')
      }, 30)
      return () => clearTimeout(timer2)
    }, 140)

    return () => clearTimeout(timer1)
  }, [activeIdx, displayedIdx])

  const currentPhrase = items[displayedIdx]?.text || items[0]?.text || ''

  return (
    <div className="hero-scroll-track" ref={trackRef}>
      <div className="hero-sticky-container">
        <section className="hero" id="overview">
          <a
            href={badgeUrl || 'https://github.com/MadisonCatfish/the-norns/releases'}
            className="live-status-pill"
            target="_blank"
            rel="noopener"
          >
            <span className="indicator-dot"></span>
            <span>{badgeText || `THE NORNS ${versionTag}`}</span>
            <span>→</span>
          </a>

          <h1 className="hero-title hero-dynamic-title-wrap">
            <span className="hero-title-prefix">{staticPrefix} </span>
            <span
              className={`accent-text hero-dynamic-phrase phrase-${phase}`}
              style={{
                display: 'inline-block',
                willChange: 'filter, opacity, transform',
                transition:
                  'filter 0.22s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.22s cubic-bezier(0.16, 1, 0.3, 1), transform 0.22s cubic-bezier(0.16, 1, 0.3, 1)',
                ...(phase === 'out'
                  ? { filter: 'blur(14px)', opacity: 0, transform: 'translateY(-8px) scale(0.97)' }
                  : phase === 'in'
                  ? { filter: 'blur(14px)', opacity: 0, transform: 'translateY(8px) scale(0.97)' }
                  : { filter: 'blur(0px)', opacity: 1, transform: 'translateY(0) scale(1)' }),
              }}
            >
              {currentPhrase}
            </span>
          </h1>

          <p className="hero-subtitle">
            {subtitle ||
              "Real-time Vorton telemetry and a warp-to-warp anomaly timer. Passive: it reads the game's logs, sends no input and does no memory injection."}
          </p>

          <div className="cta-group">
            <a href={installerUrl} className="btn btn-primary">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                <polyline points="7 10 12 15 17 10" />
                <line x1="12" y1="15" x2="12" y2="3" />
              </svg>
              Download Standalone (.exe)
            </a>
            <a href={portableUrl} className="btn btn-secondary">
              Portable (.zip)
            </a>
            <a href="/source" className="btn btn-secondary">
              Audit Source &amp; Build →
            </a>
          </div>

          {/* Asynchronous Levitating Hero Showcase */}
          <div className="hero-showcase">
            <div className="hero-card hero-card-1">
              <img src="/assets/norns-client-1.png" alt="The Norns Vorton Projector HUD" loading="eager" />
            </div>
            <div className="hero-card hero-card-2">
              <img src="/assets/norns-client-2.png" alt="The Norns Competition Mode Runner Lead" loading="eager" />
            </div>
            <div className="hero-card hero-card-3">
              <img src="/assets/norns-client-3.png" alt="The Norns Tactical Settings" loading="eager" />
            </div>
            <div className="hero-card hero-card-4">
              <img src="/assets/norns-client-4.png" alt="The Norns Dual Doctrine Smartbomb & Bounty Grid" loading="eager" />
            </div>
          </div>
        </section>
      </div>
    </div>
  )
}
