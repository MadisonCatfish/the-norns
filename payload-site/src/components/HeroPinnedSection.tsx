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
  primaryShow?: boolean | null
  primaryLabel?: string | null
  primaryUrl?: string | null
  secondaryShow?: boolean | null
  secondaryLabel?: string | null
  secondaryUrl?: string | null
  tertiaryShow?: boolean | null
  tertiaryLabel?: string | null
  tertiaryUrl?: string | null
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
  primaryShow,
  primaryLabel,
  primaryUrl,
  secondaryShow,
  secondaryLabel,
  secondaryUrl,
  tertiaryShow,
  tertiaryLabel,
  tertiaryUrl,
}: HeroPinnedSectionProps) {
  const showBtn1 = primaryShow ?? true
  const labelBtn1 = primaryLabel?.trim() ? primaryLabel : 'Download Standalone (.exe)'
  const urlBtn1 = primaryUrl?.trim() ? primaryUrl : installerUrl

  const showBtn2 = secondaryShow ?? true
  const labelBtn2 = secondaryLabel?.trim() ? secondaryLabel : 'Portable (.zip)'
  const urlBtn2 = secondaryUrl?.trim() ? secondaryUrl : portableUrl

  const showBtn3 = tertiaryShow ?? true
  const labelBtn3 = tertiaryLabel?.trim() ? tertiaryLabel : 'Audit Source & Build →'
  const urlBtn3 = tertiaryUrl?.trim() ? tertiaryUrl : '/source'

  const defaultPhrases: PhraseItem[] = [
    { text: 'EDENCOM krabbing.' },
    { text: 'Stormbringer fleets.' },
    { text: 'Superkrabbing DPS telemetry.' },
  ]

  const items = phrases && phrases.length > 0 ? phrases : defaultPhrases
  const staticPrefix = prefix ?? 'Fleet HUD for'

  const [activeIdx, setActiveIdx] = useState(0)
  const [displayedIdx, setDisplayedIdx] = useState(0)
  const [phase, setPhase] = useState<'idle' | 'out' | 'in'>('idle')
  const activeIdxRef = useRef(0)
  const lastSwitchTimeRef = useRef(0)

  // Scroll lock mechanism:
  // When at the top of the page, intercept wheel/touch events to cycle through phrases.
  // After the last phrase, normal scrolling takes over and catches the whole page!
  useEffect(() => {
    if (items.length <= 1) return

    // Trackpads (macOS especially) send many small wheel events, often under 10 px each, and keep
    // sending them during momentum. Deltas are summed and one gesture steps once; the gesture stays
    // locked until wheel events stop for WHEEL_IDLE_MS, so the momentum tail cannot skip phrases.
    const WHEEL_STEP_PX = 40
    const WHEEL_IDLE_MS = 200
    let wheelAccum = 0
    let wheelLastTime = 0
    let wheelLocked = false

    const handleWheel = (e: WheelEvent) => {
      const scrollY = window.scrollY || window.pageYOffset || 0

      // Only lock when the user is at the top of the page
      if (scrollY > 12) return

      const now = Date.now()
      if (now - wheelLastTime > WHEEL_IDLE_MS) {
        wheelAccum = 0
        wheelLocked = false
      }
      wheelLastTime = now

      const dy = e.deltaMode === 1 ? e.deltaY * 16 : e.deltaY
      const canMoveDown = dy > 0 && activeIdxRef.current < items.length - 1
      const canMoveUp = dy < 0 && activeIdxRef.current > 0

      // After the last phrase (or before the first) the wheel event is left alone and the page scrolls
      if (!canMoveDown && !canMoveUp) {
        wheelAccum = 0
        return
      }
      e.preventDefault()
      if (wheelLocked) return

      wheelAccum += dy
      if (Math.abs(wheelAccum) < WHEEL_STEP_PX) return

      wheelLocked = true
      wheelAccum = 0
      const nextIdx = activeIdxRef.current + (dy > 0 ? 1 : -1)
      activeIdxRef.current = nextIdx
      setActiveIdx(nextIdx)
    }

    let touchStartY = 0
    const handleTouchStart = (e: TouchEvent) => {
      touchStartY = e.touches[0].clientY
    }

    const handleTouchMove = (e: TouchEvent) => {
      const scrollY = window.scrollY || window.pageYOffset || 0
      const currentY = e.touches[0].clientY
      const deltaY = touchStartY - currentY
      const now = Date.now()

      if (scrollY <= 12) {
        if (deltaY > 20) {
          // Swiping up -> scroll down
          if (activeIdxRef.current < items.length - 1) {
            if (e.cancelable) e.preventDefault()
            if (now - lastSwitchTimeRef.current > 300) {
              lastSwitchTimeRef.current = now
              touchStartY = currentY
              const nextIdx = activeIdxRef.current + 1
              activeIdxRef.current = nextIdx
              setActiveIdx(nextIdx)
            }
          }
        } else if (deltaY < -20) {
          // Swiping down -> scroll up
          if (activeIdxRef.current > 0) {
            if (e.cancelable) e.preventDefault()
            if (now - lastSwitchTimeRef.current > 300) {
              lastSwitchTimeRef.current = now
              touchStartY = currentY
              const prevIdx = activeIdxRef.current - 1
              activeIdxRef.current = prevIdx
              setActiveIdx(prevIdx)
            }
          }
        }
      }
    }

    window.addEventListener('wheel', handleWheel, { passive: false })
    window.addEventListener('touchstart', handleTouchStart, { passive: true })
    window.addEventListener('touchmove', handleTouchMove, { passive: false })

    return () => {
      window.removeEventListener('wheel', handleWheel)
      window.removeEventListener('touchstart', handleTouchStart)
      window.removeEventListener('touchmove', handleTouchMove)
    }
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
    }, 130)

    return () => clearTimeout(timer1)
  }, [activeIdx, displayedIdx])

  const currentPhrase = items[displayedIdx]?.text || items[0]?.text || ''

  return (
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
        <span className="hero-title-prefix">{staticPrefix ? staticPrefix.trim() : ""}</span>{" "}
        <span
          className={`accent-text hero-dynamic-phrase phrase-${phase}`}
          style={{
            display: 'inline-block',
            overflow: 'visible',
            paddingTop: '0.14em',
            paddingBottom: '0.28em',
            marginTop: '-0.14em',
            marginBottom: '-0.28em',
            willChange: 'filter, opacity, transform',
            transition:
              'filter 0.2s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.2s cubic-bezier(0.16, 1, 0.3, 1), transform 0.2s cubic-bezier(0.16, 1, 0.3, 1)',
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
        {showBtn1 && (
          <a href={urlBtn1} className="btn btn-primary">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
              <polyline points="7 10 12 15 17 10" />
              <line x1="12" y1="15" x2="12" y2="3" />
            </svg>
            {labelBtn1}
          </a>
        )}
        {showBtn2 && (
          <a href={urlBtn2} className="btn btn-secondary">
            {labelBtn2}
          </a>
        )}
        {showBtn3 && (
          <a href={urlBtn3} className="btn btn-secondary">
            {labelBtn3}
          </a>
        )}
      </div>

      {/* Asynchronous Levitating Hero Showcase */}
      <div className="hero-showcase">
        <div className="hero-card hero-card-1">
          <picture>
            <source type="image/avif" srcSet="/assets/norns-client-1.avif" />
            <source type="image/webp" srcSet="/assets/norns-client-1.webp" />
            <img
              src="/assets/norns-client-1.png"
              alt="The Norns Vorton Projector HUD"
              width={538}
              height={617}
              loading="eager"
              fetchPriority="high"
            />
          </picture>
        </div>
        <div className="hero-card hero-card-2">
          <picture>
            <source type="image/avif" srcSet="/assets/norns-client-2.avif" />
            <source type="image/webp" srcSet="/assets/norns-client-2.webp" />
            <img
              src="/assets/norns-client-2.png"
              alt="The Norns Competition Mode Runner Lead"
              width={538}
              height={617}
              loading="eager"
              decoding="async"
              fetchPriority="low"
            />
          </picture>
        </div>
        <div className="hero-card hero-card-3">
          <picture>
            <source type="image/avif" srcSet="/assets/norns-client-3.avif" />
            <source type="image/webp" srcSet="/assets/norns-client-3.webp" />
            <img
              src="/assets/norns-client-3.png"
              alt="The Norns Tactical Settings"
              width={538}
              height={617}
              loading="eager"
              decoding="async"
              fetchPriority="low"
            />
          </picture>
        </div>
        <div className="hero-card hero-card-4">
          <picture>
            <source type="image/avif" srcSet="/assets/norns-client-4.avif" />
            <source type="image/webp" srcSet="/assets/norns-client-4.webp" />
            <img
              src="/assets/norns-client-4.png"
              alt="The Norns Dual Doctrine Smartbomb & Bounty Grid"
              width={538}
              height={617}
              loading="eager"
              decoding="async"
              fetchPriority="low"
            />
          </picture>
        </div>
      </div>
    </section>
  )
}
