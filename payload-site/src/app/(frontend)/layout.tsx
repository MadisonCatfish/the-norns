import React from 'react'
import type { Metadata } from 'next'
import './styles.css'

export const metadata: Metadata = {
  title: 'The Norns · Elite Fleet HUD for EVE Online Krabbing & Multi-Boxing',
  description: "Dedicated telemetry companion for EVE Online EDENCOM & Smartbomb ratting fleets. Real-time Vorton bounce DPS, warp-to-warp anomaly timer, and instant dread/titan alarms. Passive: it reads the game's logs and sends no input.",
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
        {children}
      </body>
    </html>
  )
}
