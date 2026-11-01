import React from 'react'

interface NavigationProps {
  activePage?: 'overview' | 'doctrine' | 'compliance' | 'source' | 'roadmap' | 'faq'
  installerUrl?: string
  versionTag?: string
  portableUrl?: string
}

export function Navigation({
  activePage = 'overview',
  installerUrl = 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-Setup-1.4.0.exe',
  versionTag = 'v1.5.0',
  portableUrl = 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-1.4.0-portable.zip',
}: NavigationProps) {
  return (
    <>
      <nav className="nav-bar">
        <a href="/" className="brand-group">
          <img src="/assets/logo-64.png" alt="The Norns Logo" className="brand-logo-img" width="32" height="32" />
          <div className="brand-title-row">
            <span className="brand-name">THE NORNS</span>
            <span className="brand-katakana">ノルン</span>
          </div>
        </a>

        <div className="nav-links">
          <a href="/#overview" className={activePage === 'overview' ? 'active' : ''} data-section="overview">Overview</a>
          <a href="/#capabilities" className={activePage === 'doctrine' ? 'active' : ''} data-section="capabilities">Doctrine</a>
          <a href="/#pipeline" className={activePage === 'compliance' ? 'active' : ''} data-section="pipeline">Compliance</a>
          <a href="/source" className={activePage === 'source' ? 'active' : ''}>Source &amp; Build</a>
          <a href="/roadmap" className={activePage === 'roadmap' ? 'active' : ''}>Roadmap</a>
          <a href="/eula.txt">EULA</a>
          <a href="https://github.com/MadisonCatfish/the-norns" target="_blank" rel="noopener">GitHub</a>
          <a href={installerUrl} className="nav-cta">Download {versionTag}</a>
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
                <img src="/assets/logo-64.png" alt="The Norns Logo" className="brand-logo-img" width="32" height="32" />
                <div className="brand-title-row">
                  <span className="brand-name">THE NORNS</span>
                  <span className="brand-katakana">ノルン</span>
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
              <li><a href="/#overview" className={activePage === 'overview' ? 'active' : ''}>Overview</a></li>
              <li><a href="/#capabilities" className={activePage === 'doctrine' ? 'active' : ''}>Doctrine</a></li>
              <li><a href="/#pipeline" className={activePage === 'compliance' ? 'active' : ''}>Compliance</a></li>
              <li><a href="/#faq" className={activePage === 'faq' ? 'active' : ''}>FAQ</a></li>
              <li><a href="/source" className={activePage === 'source' ? 'active' : ''}>Source &amp; Build</a></li>
              <li><a href="/roadmap" className={activePage === 'roadmap' ? 'active' : ''}>Roadmap</a></li>
              <li><a href="/eula.txt">EULA</a></li>
              <li><a href="https://github.com/MadisonCatfish/the-norns" target="_blank" rel="noopener">GitHub</a></li>
            </ul>
            <div className="Drawer-footer">
              <a href={installerUrl} className="btn-drawer btn-drawer-primary">Download {versionTag} (.exe)</a>
              <a href={portableUrl} className="btn-drawer btn-drawer-secondary">Portable (.zip)</a>
            </div>
          </nav>
        </div>
      </div>
    </>
  )
}
