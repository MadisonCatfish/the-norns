'use client'

import React, { useEffect, useState, useRef } from 'react'

export interface PhraseItem {
  text: string
  id?: string | null
}

interface HeroDynamicTitleProps {
  prefix?: string | null
  phrases?: PhraseItem[] | null
  fallbackTitle?: string | null
}

export function HeroDynamicTitle({ prefix, phrases, fallbackTitle }: HeroDynamicTitleProps) {
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

  useEffect(() => {
    if (items.length <= 1) return

    const handleScroll = () => {
      const scrollY = window.scrollY || window.pageYOffset || 0
      // Calculate active phrase index based on scroll position down the page
      // Phrase 0: 0 - 80px
      // Phrase 1: 80 - 220px
      // Phrase 2: 220px+
      const step = 130
      const newIdx = Math.min(
        Math.floor(Math.max(0, scrollY - 40) / step),
        items.length - 1
      )

      if (newIdx !== activeIdxRef.current) {
        activeIdxRef.current = newIdx
        setActiveIdx(newIdx)
      }
    }

    window.addEventListener('scroll', handleScroll, { passive: true })
    handleScroll()
    return () => window.removeEventListener('scroll', handleScroll)
  }, [items.length])

  // When active index changes, trigger smooth blur + fade out -> blur + fade in
  useEffect(() => {
    if (activeIdx === displayedIdx) return

    setPhase('out')
    const timer1 = setTimeout(() => {
      setDisplayedIdx(activeIdx)
      setPhase('in')
      const timer2 = setTimeout(() => {
        setPhase('idle')
      }, 40)
      return () => clearTimeout(timer2)
    }, 200)

    return () => clearTimeout(timer1)
  }, [activeIdx, displayedIdx])

  const currentPhrase = items[displayedIdx]?.text || items[0]?.text || ''

  return (
    <h1 className="hero-title hero-dynamic-title-wrap">
      <span className="hero-title-prefix">{staticPrefix} </span>
      <span
        className={`accent-text hero-dynamic-phrase phrase-${phase}`}
        style={{
          display: 'inline-block',
          willChange: 'filter, opacity, transform',
          transition: 'filter 0.28s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.28s cubic-bezier(0.16, 1, 0.3, 1), transform 0.28s cubic-bezier(0.16, 1, 0.3, 1)',
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
  )
}
