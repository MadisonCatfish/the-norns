import React from 'react'
import { getPayload } from 'payload'
import config from '@payload-config'
import { FooterECG } from './FooterECG'

export const dynamic = 'force-dynamic'
export const revalidate = 0

export default async function HomePage() {
  let latestRelease: any = null
  let faqItems: any[] = []
  let complianceCards: any[] = []
  let siteSettings: any = null
  let featuresList: any[] = []
  let metricsList: any[] = []

  try {
    const payload = await getPayload({ config })
    
    // Fetch latest release
    const releasesRes = await payload.find({
      collection: 'releases',
      where: { isLatest: { equals: true } },
      limit: 1,
    })
    latestRelease = releasesRes.docs[0] || null

    // Fetch FAQs sorted by order
    const faqRes = await payload.find({
      collection: 'faq',
      sort: 'order',
      limit: 100,
    })
    faqItems = faqRes.docs || []

    // Fetch Compliance Cards sorted by order
    const complianceRes = await payload.find({
      collection: 'compliance',
      sort: 'order',
      limit: 10,
    })
    complianceCards = complianceRes.docs || []

    // Fetch Features
    const featuresRes = await payload.find({
      collection: 'features',
      sort: 'order',
      limit: 20,
    })
    featuresList = featuresRes.docs || []

    // Fetch Metrics
    const metricsRes = await payload.find({
      collection: 'metrics',
      sort: 'order',
      limit: 10,
    })
    metricsList = metricsRes.docs || []

    // Fetch Site Settings
    siteSettings = await payload.findGlobal({
      slug: 'site-settings',
    })
  } catch (err) {
    console.error('Payload fetch error:', err)
  }

  const installerUrl = latestRelease?.installerUrl || 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-Setup-1.4.0.exe'
  const portableUrl = latestRelease?.portableUrl || 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-1.4.0-portable.zip'
  const versionTag = latestRelease?.version || 'v1.5.0'

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
      <a href="#overview" className="active" data-section="overview">Overview</a>
      <a href="#capabilities" data-section="capabilities">Doctrine</a>
      <a href="#pipeline" data-section="pipeline">Compliance</a>
      <a href="source.html">Source & Build</a>
      <a href="roadmap.html">Roadmap</a>
      <a href="eula.txt">EULA</a>
      <a href="https://github.com/MadisonCatfish/the-norns" target="_blank" rel="noopener">GitHub</a>
      <a href="https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-Setup-1.4.0.exe" className="nav-cta">Download v1.4.0</a>
    </div>

    <button type="button" className="nav-toggle" id="drawer-open" aria-label="Menu" aria-expanded="false" aria-controls="mobile-drawer">
      <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
        <line x1="3" y1="6" x2="21" y2="6"></line>
        <line x1="3" y1="12" x2="21" y2="12"></line>
        <line x1="3" y1="18" x2="21" y2="18"></line>
      </svg>
    </button>
  </nav>

  {/*  Mobile Navigation Drawer  */}
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
          <li><a href="#overview" className="active" data-section="overview">Overview</a></li>
          <li><a href="#capabilities" data-section="capabilities">Doctrine</a></li>
          <li><a href="#pipeline" data-section="pipeline">Compliance</a></li>
          <li><a href="#faq" data-section="faq">FAQ</a></li>
          <li><a href="source.html">Source &amp; Build</a></li>
          <li><a href="roadmap.html">Roadmap</a></li>
          <li><a href="eula.txt">EULA</a></li>
          <li><a href="https://github.com/MadisonCatfish/the-norns" target="_blank" rel="noopener">GitHub</a></li>
        </ul>
        <div className="Drawer-footer">
          <a href="https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-Setup-1.4.0.exe" className="btn btn-primary">Download v1.4.0 (.exe)</a>
          <a href="https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-1.4.0-portable.zip" className="btn btn-secondary">Portable (.zip)</a>
        </div>
      </nav>
    </div>
  </div>

  {/*  Container  */}
  <main className="container">
    {/*  Hero  */}
    <section className="hero" id="overview">
      <a href="https://github.com/MadisonCatfish/the-norns/releases/tag/v1.4.0" className="live-status-pill" target="_blank" rel="noopener">
        <span className="indicator-dot"></span>
        <span>THE NORNS 1.4.0</span>
        <span>→</span>
      </a>

      <h1 className="hero-title">
        Fleet HUD for <span className="accent-text">EDENCOM krabbing</span>.
      </h1>

      <p className="hero-subtitle">
        Real-time Vorton telemetry and a warp-to-warp anomaly timer. Passive: it reads the game's logs, sends no input and does no memory injection.
      </p>

      <div className="cta-group">
        <a href="https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-Setup-1.4.0.exe" className="btn btn-primary">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
          Download Standalone (.exe)
        </a>
        <a href="https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-1.4.0-portable.zip" className="btn btn-secondary">
          Portable (.zip)
        </a>
        <a href="source.html" className="btn btn-secondary">
          Audit Source & Build →
        </a>
      </div>

      {/*  Asynchronous Levitating Hero Showcase  */}
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

    {/*  Attio Structural Metric Tiles  */}
    <div className="metric-grid">
      {metricsList && metricsList.length > 0 ? (
        metricsList.map((m: any) => (
          <div key={m.id} className="metric-cell">
            <div className="metric-header">
              <span>{m.indexTag}</span> {m.label}
            </div>
            <div className="metric-val">{m.value}</div>
            <div className="metric-desc">{m.description}</div>
          </div>
        ))
      ) : (
        <>
          <div className="metric-cell">
            <div className="metric-header"><span>[ 01 ]</span> INPUT INTEGRITY</div>
            <div className="metric-val">0 inputs</div>
            <div className="metric-desc">Zero macros, clicks, or input broadcasting.</div>
          </div>
          <div className="metric-cell">
            <div className="metric-header"><span>[ 02 ]</span> MEMORY SCRAPING</div>
            <div className="metric-val">0 DLLs</div>
            <div className="metric-desc">Zero game memory reading or DLL injection.</div>
          </div>
          <div className="metric-cell">
            <div className="metric-header"><span>[ 03 ]</span> POTATO MODE CPU</div>
            <div className="metric-val">~3.5%</div>
            <div className="metric-desc">Of one CPU core, measured on the owner's rig in Potato mode.</div>
          </div>
          <div className="metric-cell">
            <div className="metric-header"><span>[ 04 ]</span> CLIENT DOCK</div>
            <div className="metric-val">8</div>
            <div className="metric-desc">Most clients the Carousel dock shows at once.</div>
          </div>
        </>
      )}
    </div>

    {/*  Capabilities Bento (Attio Style with Real Release Shots & Animated GIF)  */}
    <section className="section-wrap" id="capabilities">
      <div className="section-header">
        <div className="section-tag">[ FLEET DOCTRINE TELEMETRY ]</div>
        <h2 className="section-title">Engineered for nullsec krabbing & Havens.</h2>
        <p className="section-desc">
          Replace guesswork with real-time mathematical telemetry. Track individual gunboat efficiency, instant capital alarms, and wallet bounties in the heat of combat.
        </p>
      </div>

      <div className="attio-bento">
        {featuresList && featuresList.length > 0 ? (
          featuresList.map((f: any) => {
            const imgSrc = (f.mediaImage && typeof f.mediaImage === 'object' && f.mediaImage.url)
              ? f.mediaImage.url
              : (f.imageUrl || '/assets/dps_compact.png')

            return (
              <div key={f.id} className={`bento-box ${f.colSpan || 'col-6'}`}>
                <div className="bento-content">
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span className="bento-pill">{f.pill}</span>
                    {f.hasLiveBadge && (
                      <span className="live-anim-badge">
                        <span className="dot"></span>LIVE ANIMATION
                      </span>
                    )}
                  </div>
                  <h3 className="bento-title">{f.title}</h3>
                  <p className="bento-desc">{f.description}</p>
                </div>
                <div className="bento-image-frame">
                  {f.isVideo ? (
                    <video
                      className="competition-video"
                      autoPlay
                      muted
                      loop
                      playsInline
                      preload="metadata"
                      poster={f.videoPoster || '/assets/the-norns-competition-poster.jpg'}
                      role="img"
                      aria-label={f.title}
                    >
                      <source src={f.videoUrl || '/assets/the-norns-competition.mp4'} type="video/mp4" />
                    </video>
                  ) : (
                    <img
                      loading="lazy"
                      decoding="async"
                      width={538}
                      height={617}
                      src={imgSrc}
                      alt={f.title}
                    />
                  )}
                </div>
              </div>
            )
          })
        ) : (
          <div>Loading capabilities...</div>
        )}
      </div>
    </section>

    {/*  Architecture & Compliance (Attio Style)  */}
    <section className="section-wrap" id="pipeline">
      <div className="section-header">
        <div className="section-tag">[ CCP POLICY NOTE ]</div>
        <h2 className="section-title">Read-only data pipeline.</h2>
        <p className="section-desc">
          How The Norns gets real-time telemetry: it reads the logs the client already writes. We are not affiliated with CCP and cannot promise how CCP treats third-party tools.
        </p>
      </div>

      <div className="pipeline-grid">
        {complianceCards && complianceCards.length > 0 ? (
          complianceCards.map((c) => (
            <div key={c.id} className="pipeline-step">
              <span className="pipeline-num">{c.indexTag}</span>
              <h4 className="pipeline-step-title">{c.title}</h4>
              <p className="pipeline-step-desc">{c.description}</p>
            </div>
          ))
        ) : (
          <>
            <div className="pipeline-step">
              <span className="pipeline-num">[ 01 ] LOCAL GAMELOGS</span>
              <h4 className="pipeline-step-title">Non-Locking Text Tailing</h4>
              <p className="pipeline-step-desc">
                Reads standard text logs generated by the client in <code>Documents\EVE\logs\Gamelogs</code> using <code>FileShare.ReadWrite</code>. Never locks or alters the file stream.
              </p>
            </div>
            <div className="pipeline-step">
              <span className="pipeline-num">[ 02 ] MEMORY BARRIER</span>
              <h4 className="pipeline-step-title">Zero Process Injection</h4>
              <p className="pipeline-step-desc">
                Completely isolated from <code>exefile.exe</code>. No DLL injection, no memory scraping, no network packet sniffing. The optional Carousel module (off by default) uses a window-focus hook and can bring a client window to the front.
              </p>
            </div>
            <div className="pipeline-step">
              <span className="pipeline-num">[ 03 ] PASSIVE HUD</span>
              <h4 className="pipeline-step-title">No Input Broadcasting</h4>
              <p className="pipeline-step-desc">
                We designed it around CCP's 1-action-1-input guideline. It displays telemetry only and sends no clicks, keystrokes or broadcasts.
              </p>
            </div>
          </>
        )}
      </div>
    </section>

    {/*  Download Card  */}
        <section className="section-wrap" id="faq">
      <div className="section-header">
        <div className="section-tag">[ FAQ ]</div>
        <h2 className="section-title">Questions about The Norns.</h2>
      </div>
      <div className="faq-list">
        {faqItems && faqItems.length > 0 ? (
          faqItems.map((item) => (
            <details key={item.id}>
              <summary>{item.question}</summary>
              <p>{item.answer}</p>
            </details>
          ))
        ) : (
          <>
            <details><summary>Which Windows versions does it run on?</summary><p>Windows 10 and 11. It needs .NET Framework 4.8, which Windows 10 and 11 already include.</p></details>
            <details><summary>Does it need a server or an account?</summary><p>No. It runs locally, with no account and no server component. The only network request is the optional update check.</p></details>
            <details><summary>What does the update check send?</summary><p>It asks GitHub for the latest release version. It sends no information about you or your game clients, beyond the normal request to GitHub's API.</p></details>
            <details><summary>How do I turn the update check off?</summary><p>Settings → General, or the "Check for updates at start" switch in the installer.</p></details>
            <details><summary>Does it send keystrokes or clicks to the game?</summary><p>No. The optional Carousel module (off by default) can bring a client window to the front and switch between clients with hotkeys. Nothing else sends input.</p></details>
            <details><summary>Does it read game memory or inject code?</summary><p>No. It reads the text logs in Documents\EVE\logs\Gamelogs and the titles of the EVE windows. It does not inject DLLs or read process memory.</p></details>
            <details><summary>Where are my settings and session data kept?</summary><p>In %LOCALAPPDATA%\The Norns. Installing or updating the app keeps them.</p></details>
            <details><summary>Is it allowed on the official servers?</summary><p>The app does not connect to any server. We cannot promise how CCP treats third-party tools, so read CCP's current policies before you use any tool.</p></details>
            <details><summary>Is it affiliated with CCP?</summary><p>No. The Norns is a free, open-source, fan-made tool under the MIT license.</p></details>
            <details><summary>Can I see the source code?</summary><p>Yes. See the <a href="source.html">Source &amp; Build</a> page.</p></details>
            <details><summary>Which languages does it support?</summary><p>English, Ukrainian, Spanish, German, Italian, French and Chinese (Simplified). Settings → General switches the language.</p></details>
          </>
        )}
      </div>
    </section>

<section className="download-card">
      <div className="download-pill">
        <span className="download-dot"></span>
        <span>READY FOR DEPLOYMENT</span>
      </div>
      <h2>Upgrade your nullsec fleet telemetry today.</h2>
      <p>
        Free, open-source, and offline by design, apart from one optional update check. Download the standalone Windows installer or extract the portable archive.
      </p>

      <div className="cta-group" style={{'marginBottom': '0'}}>
        <a href="https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-Setup-1.4.0.exe" className="btn btn-primary">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
          Download Installer (.exe)
        </a>
        <a href="https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-1.4.0-portable.zip" className="btn btn-secondary">
          Portable (.zip)
        </a>
        <a href="source.html" className="btn btn-secondary">
          Audit Source Code & Build →
        </a>
      </div>
    </section>

  </main>

  {/*  Footer  */}
  
<footer className="site-footer js-footer-ecg" id="site-footer">
    <FooterECG />

    <div className="footer-inner">
      <div className="footer-top">
        <div className="footer-brand-col">
          <a href="/" className="brand-group">
            <img src="/assets/logo-64.png" alt="The Norns Logo" className="brand-logo-img" width="32" height="32" />
            <div className="brand-title-row">
              <span className="brand-name" style={{'fontSize': '15px'}}>THE NORNS</span>
              <span className="brand-katakana" style={{'fontSize': '11px'}}>ノルン</span>
            </div>
          </a>
          <p className="footer-brand-tagline">
            Dedicated telemetry companion for EVE Online EDENCOM &amp; Smartbomb ratting fleets. Passive, read-only, zero injection.
          </p>
          <div className="footer-social-row">
            <a href="https://github.com/MadisonCatfish/the-norns" target="_blank" rel="noopener" className="footer-icon-link" aria-label="GitHub Repository">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor"><path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/></svg>
            </a>
            <a href="https://github.com/MadisonCatfish/the-norns/releases" target="_blank" rel="noopener" className="footer-icon-link" aria-label="Download Releases">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
            </a>
          </div>
        </div>

        <div className="footer-nav-columns">
          <div className="footer-nav-col">
            <span className="footer-nav-heading">Telemetry</span>
            <a href="/#overview">Overview</a>
            <a href="/#capabilities">Doctrine</a>
            <a href="/#pipeline">Compliance</a>
            <a href="/#faq">FAQ</a>
          </div>
          <div className="footer-nav-col">
            <span className="footer-nav-heading">Engineering</span>
            <a href="source.html">Source &amp; Build</a>
            <a href="roadmap.html">Roadmap</a>
            <a href="https://github.com/MadisonCatfish/the-norns/releases" target="_blank" rel="noopener">Changelog</a>
          </div>
          <div className="footer-nav-col">
            <span className="footer-nav-heading">Distribution</span>
            <a href="https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-Setup-1.4.0.exe">Installer (.exe)</a>
            <a href="https://github.com/MadisonCatfish/the-norns/releases/download/v1.4.0/TheNorns-1.4.0-portable.zip">Portable (.zip)</a>
            <a href="https://github.com/MadisonCatfish/the-norns" target="_blank" rel="noopener">GitHub</a>
          </div>
          <div className="footer-nav-col">
            <span className="footer-nav-heading">Legal</span>
            <a href="eula.txt">EULA</a>
            <a href="source.html">MIT License</a>
          </div>
        </div>
      </div>

      <div className="footer-meta">
        <span>&copy; 2026 The Norns (ノルン). Independent open-source companion under the MIT License.</span>
        <span>EVE Online&reg; &amp; CCP&reg; &copy; 2014 CCP hf. Not affiliated with or endorsed by CCP hf.</span>
      </div>
    </div>
  </footer>
    </>
  )
}
