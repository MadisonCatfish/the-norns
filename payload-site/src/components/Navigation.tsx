import React from 'react'

export interface NavLinkItem {
  id?: string | null
  label: string
  url: string
  sectionId?: string | null
  newTab?: boolean | null
}

export interface HeaderData {
  brandName?: string | null
  brandKatakana?: string | null
  logoUrl?: string | null
  links?: NavLinkItem[] | null
  showCta?: boolean | null
  ctaLabel?: string | null
  ctaUrl?: string | null
}

interface NavigationProps {
  data?: HeaderData | null
  activePage?: 'overview' | 'doctrine' | 'compliance' | 'source' | 'roadmap' | 'faq'
  installerUrl?: string
  versionTag?: string
  portableUrl?: string
}

export function Navigation({
  data,
  activePage = 'overview',
  installerUrl = 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-Setup-1.4.0.exe',
  versionTag = 'v1.5.0',
  portableUrl = 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-1.4.0-portable.zip',
}: NavigationProps) {
  const brandName = data?.brandName || 'THE NORNS'
  const brandKatakana = data?.brandKatakana ?? 'ノルン'
  const logoUrl = data?.logoUrl || '/assets/logo-64.png'

  const defaultLinks: NavLinkItem[] = [
    { label: 'Overview', url: '/#overview', sectionId: 'overview', newTab: false },
    { label: 'Doctrine', url: '/#capabilities', sectionId: 'capabilities', newTab: false },
    { label: 'Compliance', url: '/#pipeline', sectionId: 'pipeline', newTab: false },
    { label: 'Source & Build', url: '/source', sectionId: 'source', newTab: false },
    { label: 'Roadmap', url: '/roadmap', sectionId: 'roadmap', newTab: false },
    { label: 'EULA', url: '/eula.txt', sectionId: 'eula', newTab: false },
    { label: 'GitHub', url: 'https://github.com/MadisonCatfish/the-norns', newTab: true },
  ]

  const links = data?.links && data.links.length > 0 ? data.links : defaultLinks

  const showCta = data?.showCta ?? true
  const ctaLabel = data?.ctaLabel?.trim() ? data.ctaLabel : `Download ${versionTag}`
  const ctaUrl = data?.ctaUrl?.trim() ? data.ctaUrl : installerUrl

  const isLinkActive = (item: NavLinkItem) => {
    if (item.sectionId && item.sectionId.toLowerCase() === activePage) return true
    if (activePage === 'overview' && (item.url === '/#overview' || item.url === '#overview' || item.url === '/')) return true
    if (activePage === 'doctrine' && (item.url.includes('#capabilities'))) return true
    if (activePage === 'compliance' && (item.url.includes('#pipeline'))) return true
    if (activePage === 'source' && (item.url.includes('source'))) return true
    if (activePage === 'roadmap' && (item.url.includes('roadmap'))) return true
    if (activePage === 'faq' && (item.url.includes('#faq'))) return true
    return false
  }

  return (
    <>
      <nav className="nav-bar">
        <a href="/" className="brand-group">
          <img src={logoUrl} alt={`${brandName} Logo`} className="brand-logo-img" width="32" height="32" />
          <div className="brand-title-row">
            <span className="brand-name">{brandName}</span>
            {brandKatakana && <span className="brand-katakana">{brandKatakana}</span>}
          </div>
        </a>

        <div className="nav-links">
          {links.map((link, idx) => {
            const active = isLinkActive(link)
            return (
              <a
                key={link.id || idx}
                href={link.url}
                className={active ? 'active' : ''}
                data-section={link.sectionId || undefined}
                target={link.newTab ? '_blank' : undefined}
                rel={link.newTab ? 'noopener' : undefined}
              >
                {link.label}
              </a>
            )
          })}
          {showCta && (
            <a href={ctaUrl} className="nav-cta">
              {ctaLabel}
            </a>
          )}
        </div>

        <button type="button" className="nav-toggle" id="drawer-open" aria-label="Menu" aria-expanded="false" aria-controls="mobile-drawer">
          <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
            <line x1="3" y1="6" x2="21" y2="6"></line>
            <line x1="3" y1="12" x2="21" y2="12"></line>
            <line x1="3" y1="18" x2="21" y2="18"></line>
          </svg>
        </button>
      </nav>

      {/* Mobile Navigation Drawer */}
      <div className="Drawer" id="mobile-drawer" popover="manual">
        <div className="Drawer-scroller">
          <nav className="Drawer-sheet" tabIndex={-1} aria-label="Mobile Navigation">
            <div className="Drawer-header">
              <a href="/" className="brand-group">
                <img src={logoUrl} alt={`${brandName} Logo`} className="brand-logo-img" width="32" height="32" />
                <div className="brand-title-row">
                  <span className="brand-name">{brandName}</span>
                  {brandKatakana && <span className="brand-katakana">{brandKatakana}</span>}
                </div>
              </a>
              <button type="button" className="Drawer-close" id="drawer-close" aria-label="Close menu">
                <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                  <line x1="18" y1="6" x2="6" y2="18"></line>
                  <line x1="6" y1="6" x2="18" y2="18"></line>
                </svg>
              </button>
            </div>
            <ul className="Drawer-links">
              {links.map((link, idx) => {
                const active = isLinkActive(link)
                return (
                  <li key={link.id || idx}>
                    <a
                      href={link.url}
                      className={active ? 'active' : ''}
                      data-section={link.sectionId || undefined}
                      target={link.newTab ? '_blank' : undefined}
                      rel={link.newTab ? 'noopener' : undefined}
                    >
                      {link.label}
                    </a>
                  </li>
                )
              })}
            </ul>
            <div className="Drawer-footer">
              <a href={ctaUrl} className="btn-drawer btn-drawer-primary">{ctaLabel} (.exe)</a>
              <a href={portableUrl} className="btn-drawer btn-drawer-secondary">Portable (.zip)</a>
            </div>
          </nav>
        </div>
      </div>
    </>
  )
}
