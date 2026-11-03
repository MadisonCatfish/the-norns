import React from 'react'
import type { Metadata } from 'next'
import './styles.css'
import { MobileNavScript } from '@/components/MobileNavScript'
import { InteractiveGlassScript } from '@/components/InteractiveGlassScript'

export const metadata: Metadata = {
  metadataBase: new URL('https://the-norns.com'),
  title: 'The Norns · Elite Fleet HUD for EVE Online Krabbing & Multi-Boxing',
  description: "Dedicated telemetry companion for EVE Online EDENCOM & Smartbomb ratting fleets. Real-time Vorton bounce DPS, warp-to-warp anomaly timer, and instant dread/titan alarms. Passive: it reads the game's logs and sends no input.",
  openGraph: {
    title: 'The Norns · Elite Fleet HUD for EVE Online',
    description: 'Dedicated telemetry companion for EVE Online EDENCOM & Smartbomb ratting fleets. Zero memory injection, 100% passive log tailing.',
    url: 'https://the-norns.com',
    siteName: 'The Norns',
    images: [
      {
        url: '/assets/the-norns-competition-poster.jpg',
        width: 1200,
        height: 630,
        alt: 'The Norns Telemetry Companion HUD',
      },
    ],
    locale: 'en_US',
    type: 'website',
  },
  twitter: {
    card: 'summary_large_image',
    title: 'The Norns · Elite Fleet HUD for EVE Online',
    description: 'Dedicated telemetry companion for EVE Online EDENCOM & Smartbomb ratting fleets. Zero memory injection, 100% passive log tailing.',
    images: ['/assets/the-norns-competition-poster.jpg'],
  },
  icons: {
    icon: [
      { url: '/favicon.ico' },
      { url: '/assets/logo-32.png', sizes: '32x32', type: 'image/png' },
      { url: '/assets/logo-64.png', sizes: '64x64', type: 'image/png' },
    ],
    apple: '/assets/logo.png',
  },
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <head>
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link
          rel="stylesheet"
          href="https://fonts.googleapis.com/css2?family=Noto+Sans:wght@400;500;600;700;800&family=Orbitron:wght@500;600;700;800;900&family=JetBrains+Mono:wght@400;500;600&display=swap"
        />
      </head>
      <body>
        <div className="bg-canvas" aria-hidden="true" />
        <MobileNavScript />
        <InteractiveGlassScript />
        {children}
      </body>
    </html>
  )
}
