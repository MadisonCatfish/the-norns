import React from 'react'
import { FooterECG } from '../app/(frontend)/FooterECG'

export interface FooterLink {
  label: string
  url: string
  newTab?: boolean | null
}

export interface FooterColumn {
  title: string
  links?: FooterLink[] | null
}

export interface FooterData {
  tagline?: string | null
  githubUrl?: string | null
  releasesUrl?: string | null
  columns?: FooterColumn[] | null
  copyrightNotice?: string | null
  ccpDisclaimer?: string | null
}

const defaultColumns: FooterColumn[] = [
  {
    title: 'Telemetry',
    links: [
      { label: 'Overview', url: '/#overview' },
      { label: 'Doctrine', url: '/#capabilities' },
      { label: 'Compliance', url: '/#pipeline' },
      { label: 'FAQ', url: '/#faq' },
    ],
  },
  {
    title: 'Engineering',
    links: [
      { label: 'Source & Build', url: '/source' },
      { label: 'Roadmap', url: 'roadmap.html' },
      { label: 'Changelog', url: 'https://github.com/MadisonCatfish/the-norns/releases', newTab: true },
    ],
  },
  {
    title: 'Distribution',
    links: [
      { label: 'Installer (.exe)', url: 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.5.3/TheNorns-Setup-1.5.3.exe' },
      { label: 'Portable (.zip)', url: 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.5.3/TheNorns-1.5.3-portable.zip' },
      { label: 'GitHub', url: 'https://github.com/MadisonCatfish/the-norns', newTab: true },
    ],
  },
  {
    title: 'Legal',
    links: [
      { label: 'EULA', url: 'eula.txt' },
      { label: 'MIT License', url: '/source' },
    ],
  },
]

export function Footer({
  data,
  headerData,
  discordUrl,
}: {
  data?: FooterData | null
  headerData?: any
  discordUrl?: string | null
}) {
  const brandName = headerData?.brandName || 'THE NORNS'
  const brandKatakana = headerData?.brandKatakana ?? 'ノルン'
  const logoUrl = headerData?.logoUrl || '/assets/logo-64.png'

  const columns = data?.columns && data.columns.length > 0 ? data.columns : defaultColumns
  const tagline = data?.tagline || 'Dedicated telemetry companion for EVE Online EDENCOM & Smartbomb ratting fleets. Passive, read-only, zero injection.'
  const githubUrl = data?.githubUrl || 'https://github.com/MadisonCatfish/the-norns'
  const releasesUrl = data?.releasesUrl || 'https://github.com/MadisonCatfish/the-norns/releases'
  const copyrightNotice = data?.copyrightNotice || '© 2026 The Norns (ノルン). Independent open-source companion under the MIT License.'
  const ccpDisclaimer = data?.ccpDisclaimer || 'EVE Online® & CCP® © 2014 CCP hf. Not affiliated with or endorsed by CCP hf.'

  return (
    <footer className="site-footer js-footer-ecg" id="site-footer">
      <FooterECG />

      <div className="footer-inner">
        <div className="footer-top">
          <div className="footer-brand-col">
            <a href="/" className="brand-group">
              <img src={logoUrl} alt={`${brandName} Logo`} className="brand-logo-img" width="32" height="32" />
              <div className="brand-title-row">
                <span className="brand-name" style={{ fontSize: '15px' }}>{brandName}</span>
                {brandKatakana && <span className="brand-katakana" style={{ fontSize: '11px' }}>{brandKatakana}</span>}
              </div>
            </a>
            <p className="footer-brand-tagline">
              {tagline}
            </p>
            <div className="footer-social-row">
              <a href={githubUrl} target="_blank" rel="noopener" className="footer-icon-link" aria-label="GitHub Repository">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z" />
                </svg>
              </a>
              <a href={releasesUrl} target="_blank" rel="noopener" className="footer-icon-link" aria-label="Download Releases">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                  <polyline points="7 10 12 15 17 10" />
                  <line x1="12" y1="15" x2="12" y2="3" />
                </svg>
              </a>
              {discordUrl && (
                <a href={discordUrl} target="_blank" rel="noopener" className="footer-icon-link" aria-label="Discord Community">
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0 12.64 12.64 0 0 0-.617-1.25.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057 19.9 19.9 0 0 0 5.993 3.03.078.078 0 0 0 .084-.028c.462-.63.874-1.295 1.226-1.994.021-.041.001-.09-.041-.106a13.107 13.107 0 0 1-1.872-.892.077.077 0 0 1-.008-.128 10.2 10.2 0 0 0 .372-.292.074.074 0 0 1 .077-.01c3.929 1.793 8.18 1.793 12.061 0a.074.074 0 0 1 .078.01c.12.098.246.198.373.292a.077.077 0 0 1-.006.127 12.299 12.299 0 0 1-1.873.894.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028 19.839 19.839 0 0 0 6.002-3.03.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.028zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.955-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.946 2.418-2.157 2.418z" />
                  </svg>
                </a>
              )}
            </div>
          </div>

          <div className="footer-nav-columns">
            {columns.map((col, idx) => (
              <div key={idx} className="footer-nav-col">
                <span className="footer-nav-heading">{col.title}</span>
                {col.links && col.links.map((link, lIdx) => (
                  <a
                    key={lIdx}
                    href={link.url}
                    target={link.newTab ? '_blank' : undefined}
                    rel={link.newTab ? 'noopener' : undefined}
                  >
                    {link.label}
                  </a>
                ))}
              </div>
            ))}
          </div>
        </div>

        <div className="footer-meta">
          <span>{copyrightNotice}</span>
          <span>{ccpDisclaimer}</span>
        </div>
      </div>
    </footer>
  )
}
