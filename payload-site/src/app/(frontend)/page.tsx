import React from 'react'
import { getPayload } from 'payload'
import config from '@payload-config'
import { Footer } from '@/components/Footer'
import { Navigation } from '@/components/Navigation'
import { HeroPinnedSection } from '@/components/HeroPinnedSection'

export const dynamic = 'force-dynamic'
export const revalidate = 0

export default async function HomePage() {
  let latestRelease: any = null
  let faqItems: any[] = []
  let complianceCards: any[] = []
  let siteSettings: any = null
  let featuresList: any[] = []
  let metricsList: any[] = []
  let footerData: any = null
  let headerData: any = null

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

    // Fetch Footer
    footerData = await payload.findGlobal({
      slug: 'footer',
    })

    // Fetch Header
    headerData = await payload.findGlobal({
      slug: 'header',
    })
  } catch (err) {
    console.error('Payload fetch error:', err)
  }

  const installerUrl = latestRelease?.installerUrl || 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.5.3/TheNorns-Setup-1.5.3.exe'
  const portableUrl = latestRelease?.portableUrl || 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.5.3/TheNorns-1.5.3-portable.zip'
  const versionTag = latestRelease?.version || 'v1.5.3'

  return (
    <>
      <Navigation
        data={headerData}
        activePage="overview"
        installerUrl={installerUrl}
        portableUrl={portableUrl}
        versionTag={versionTag}
      />

  {/*  Container  */}
  <main className="container">
    {/* Pinned Sticky Hero Section */}
    <HeroPinnedSection
      prefix={siteSettings?.heroTitlePrefix}
      phrases={siteSettings?.heroTitlePhrases}
      badgeText={siteSettings?.heroBadge}
      badgeUrl={latestRelease?.githubReleaseUrl || siteSettings?.githubUrl}
      subtitle={siteSettings?.heroSubtitle}
      installerUrl={installerUrl}
      portableUrl={portableUrl}
      versionTag={versionTag}
      primaryShow={siteSettings?.heroPrimaryShow}
      primaryLabel={siteSettings?.heroPrimaryLabel}
      primaryUrl={siteSettings?.heroPrimaryUrl}
      secondaryShow={siteSettings?.heroSecondaryShow}
      secondaryLabel={siteSettings?.heroSecondaryLabel}
      secondaryUrl={siteSettings?.heroSecondaryUrl}
      tertiaryShow={siteSettings?.heroTertiaryShow}
      tertiaryLabel={siteSettings?.heroTertiaryLabel}
      tertiaryUrl={siteSettings?.heroTertiaryUrl}
    />

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
            <div className="metric-desc">Zero macros, clicks or keystrokes.</div>
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
                  {f.isVideo ? (() => {
                    const rawUrl = f.videoUrl || '/assets/the-norns-competition.mp4'
                    const cleanUrl = rawUrl.replace('https://the-norns.pages.dev', '')
                    const webmUrl = cleanUrl.replace(/\.mp4$/, '.webm')
                    const posterUrl = (f.videoPoster || '/assets/the-norns-competition-poster.jpg')
                      .replace('https://the-norns.pages.dev', '')
                      .replace(/\.jpg$/, '.webp')

                    return (
                      <video
                        className="competition-video"
                        autoPlay
                        muted
                        loop
                        playsInline
                        preload="metadata"
                        poster={posterUrl}
                        role="img"
                        aria-label={f.title}
                      >
                        <source src={webmUrl} type="video/webm" />
                        <source src={cleanUrl} type="video/mp4" />
                      </video>
                    )
                  })() : (
                    <picture>
                      <source type="image/avif" srcSet={imgSrc.replace(/\.png$/, '.avif')} />
                      <source type="image/webp" srcSet={imgSrc.replace(/\.png$/, '.webp')} />
                      <img
                        loading="lazy"
                        decoding="async"
                        width={538}
                        height={617}
                        src={imgSrc}
                        alt={f.title}
                      />
                    </picture>
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
              <h4 className="pipeline-step-title">No Input Sent</h4>
              <p className="pipeline-step-desc">
                We designed it around CCP's 1-action-1-input guideline. It displays telemetry only and sends no clicks or keystrokes.
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
        <a href={installerUrl} className="btn btn-primary">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
          Download Installer (.exe)
        </a>
        <a href={portableUrl} className="btn btn-secondary">
          Portable (.zip)
        </a>
        <a href="/source" className="btn btn-secondary">
          Audit Source Code & Build →
        </a>
      </div>
    </section>

  </main>
      <Footer data={footerData} />
    </>
  )
}
