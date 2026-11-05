import React from 'react'
import { getPayload } from 'payload'
import config from '@payload-config'
import { Navigation } from '@/components/Navigation'
import { Footer } from '@/components/Footer'
import type { Metadata } from 'next'

export const metadata: Metadata = {
  title: 'The Norns · Source Code & Security Audit · EVE Online',
  description: 'Audit The Norns source code, security transparency, MIT license, and reproducible local build instructions for EVE Online capsuleers.',
}

export const dynamic = 'force-dynamic'
export const revalidate = 0

export default async function SourcePage() {
  let sourceData: any = null
  let headerData: any = null
  let footerData: any = null
  let latestRelease: any = null

  try {
    const payload = await getPayload({ config })

    // Fetch SourcePage global
    sourceData = await payload.findGlobal({
      slug: 'source-page',
    })

    // Fetch Header & Footer
    headerData = await payload.findGlobal({
      slug: 'header',
    })

    footerData = await payload.findGlobal({
      slug: 'footer',
    })

    // Fetch latest release
    const releasesRes = await payload.find({
      collection: 'releases',
      where: { isLatest: { equals: true } },
      limit: 1,
    })
    latestRelease = releasesRes.docs[0] || null
  } catch (err) {
    console.error('Source page payload fetch error:', err)
  }

  const installerUrl = latestRelease?.installerUrl || 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.5.1/TheNorns-Setup-1.5.1.exe'
  const portableUrl = latestRelease?.portableUrl || 'https://github.com/MadisonCatfish/the-norns/releases/download/v1.5.1/TheNorns-1.5.1-portable.zip'
  const versionTag = latestRelease?.version || 'v1.5.1'
  const githubUrl = sourceData?.githubRepoUrl || 'https://github.com/MadisonCatfish/the-norns'
  const showBtn1 = sourceData?.heroPrimaryShow ?? true
  const labelBtn1 = sourceData?.heroPrimaryLabel?.trim() ? sourceData.heroPrimaryLabel : 'Inspect GitHub Repo →'
  const urlBtn1 = sourceData?.heroPrimaryUrl?.trim() ? sourceData.heroPrimaryUrl : githubUrl

  const showBtn2 = sourceData?.heroSecondaryShow ?? true
  const rawLabelBtn2 = sourceData?.heroSecondaryLabel?.trim() ? sourceData.heroSecondaryLabel : 'Download {version} (.exe)'
  const labelBtn2 = rawLabelBtn2.replace('{version}', versionTag)
  const urlBtn2 = sourceData?.heroSecondaryUrl?.trim() ? sourceData.heroSecondaryUrl : installerUrl

  const showBtn3 = sourceData?.heroTertiaryShow ?? true
  const labelBtn3 = sourceData?.heroTertiaryLabel?.trim() ? sourceData.heroTertiaryLabel : 'Review Architecture'
  const urlBtn3 = sourceData?.heroTertiaryUrl?.trim() ? sourceData.heroTertiaryUrl : '#compliance'

  return (
    <>
      <Navigation
        data={headerData}
        activePage="source"
        installerUrl={installerUrl}
        portableUrl={portableUrl}
        versionTag={versionTag}
      />

      <main className="container">
        {/* Hero Header */}
        <section className="hero">
          <div className="live-status-pill">
            <span className="indicator-dot"></span>
            <span>{sourceData?.heroBadge || 'OPEN SOURCE · MIT LICENSE · STRICT ZERO-INJECTION'}</span>
          </div>

          <h1 className="hero-title">
            {sourceData?.heroTitle ? (
              <span dangerouslySetInnerHTML={{ __html: sourceData.heroTitle }} />
            ) : (
              <>
                Audit the source.<br />
                <span className="accent-text">Fly safe in nullsec.</span>
              </>
            )}
          </h1>

          <p className="hero-subtitle">
            {sourceData?.heroSubtitle ||
              'Full architectural transparency for fleet commanders and capsuleers. Verify that The Norns runs strictly read-only, never touches exefile.exe memory, and compiles into byte-for-byte verifiable binaries directly from GitHub.'}
          </p>

          {(showBtn1 || showBtn2 || showBtn3) && (
            <div className="cta-group">
              {showBtn1 && (
                <a
                  href={urlBtn1}
                  target={urlBtn1.startsWith('http') ? '_blank' : undefined}
                  rel={urlBtn1.startsWith('http') ? 'noopener' : undefined}
                  className="btn btn-primary"
                >
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0024 12c0-6.63-5.37-12-12-12z" />
                  </svg>
                  {labelBtn1}
                </a>
              )}
              {showBtn2 && (
                <a
                  href={urlBtn2}
                  target={urlBtn2.startsWith('http') ? '_blank' : undefined}
                  rel={urlBtn2.startsWith('http') ? 'noopener' : undefined}
                  className="btn btn-secondary"
                >
                  {labelBtn2}
                </a>
              )}
              {showBtn3 && (
                <a
                  href={urlBtn3}
                  target={urlBtn3.startsWith('http') ? '_blank' : undefined}
                  rel={urlBtn3.startsWith('http') ? 'noopener' : undefined}
                  className="btn btn-secondary"
                >
                  {labelBtn3}
                </a>
              )}
            </div>
          )}
        </section>

        {/* Section 1: Security & Fair Play */}
        <section className="section-wrap" id="compliance">
          <div className="section-header">
            <div className="section-tag">{sourceData?.securityTag || '[ 01 / CCP FAIR PLAY COMPLIANCE ]'}</div>
            <h2 className="section-title">{sourceData?.securityTitle || 'Architected to stay out of the game client.'}</h2>
            <p className="section-desc">
              {sourceData?.securityDesc ||
                'Every architectural boundary in The Norns is designed to ensure zero interference with the game client, zero network leakage, and absolute peace of mind for multi-billion ISK setups.'}
            </p>
          </div>

          <div className="security-grid">
            {sourceData?.securityCards?.map((card: any, idx: number) => (
              <div key={card.id || idx} className="security-card">
                <div className="card-top">
                  <span className="badge-pill">{card.pill}</span>
                  <span className="status-check">{card.status}</span>
                </div>
                <h3 className="card-heading">{card.title}</h3>
                <p className="card-desc">{card.description}</p>
                {card.codeCallout && (
                  <div className="card-code-callout">
                    <span className="code-dot"></span>
                    <code>{card.codeCallout}</code>
                  </div>
                )}
              </div>
            ))}
          </div>
        </section>

        {/* Section 2: Codebase Architecture */}
        <section className="section-wrap" id="codebase">
          <div className="section-header">
            <div className="section-tag">{sourceData?.archTag || '[ 02 / CODEBASE ARCHITECTURE ]'}</div>
            <h2 className="section-title">{sourceData?.archTitle || 'Clean, zero-dependency C# codebase.'}</h2>
            <p className="section-desc">
              {sourceData?.archDesc ||
                'No heavy web wrappers, no Electron bloat, no third-party package dependencies. Pure C# targeting native Windows graphics and non-blocking file streaming.'}
            </p>
          </div>

          <div className="master-frame">
            <div className="frame-bar">
              <div className="frame-controls">
                <span></span><span></span><span></span>
              </div>
              <div className="frame-title">{sourceData?.archFrameTitle || 'the-norns / native / source-tree'}</div>
              <div className="frame-badge">{sourceData?.archBadge || 'C# 7.3 · .NET 4.8'}</div>
            </div>

            <table className="arch-table">
              <thead>
                <tr>
                  <th>Module</th>
                  <th>Source File</th>
                  <th>Core Functionality</th>
                </tr>
              </thead>
              <tbody>
                {sourceData?.modules?.map((mod: any, idx: number) => (
                  <tr key={mod.id || idx}>
                    <td><span className="module-tag">{mod.moduleName}</span></td>
                    <td><span className="file-chip">{mod.sourceFile}</span></td>
                    <td>{mod.functionality}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        {/* Section 3: Reproducible Builds */}
        <section className="section-wrap" id="build">
          <div className="section-header">
            <div className="section-tag">{sourceData?.buildTag || '[ 03 / REPRODUCIBLE BUILDS ]'}</div>
            <h2 className="section-title">{sourceData?.buildTitle || 'Build your own binary.'}</h2>
            <p className="section-desc">
              {sourceData?.buildDesc ||
                'The Norns compiles using native Windows developer tools (.NET Framework 4.8 via Roslyn). No Node.js, Python, or external package managers required for compilation.'}
            </p>
          </div>

          <div className="master-frame">
            <div className="frame-bar">
              <div className="frame-controls">
                <span></span><span></span><span></span>
              </div>
              <div className="frame-title">{sourceData?.buildTerminalTitle || 'Windows PowerShell · C:\\The Norns>'}</div>
              <div className="frame-badge">{sourceData?.buildBadge || 'POWERSHELL 5.1+'}</div>
            </div>

            <div className="terminal-body">
              {sourceData?.buildSteps?.map((step: any, idx: number) => (
                <div key={step.id || idx} className="block">
                  <span className="comment">{step.comment}</span><br />
                  <span style={{ whiteSpace: 'pre-line' }}>{step.command}</span>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* Section 4: MIT License */}
        <section className="section-wrap" id="license">
          <div className="section-header">
            <div className="section-tag">{sourceData?.licenseTag || '[ 04 / OPEN SOURCE LICENSE ]'}</div>
            <h2 className="section-title">{sourceData?.licenseTitle || 'Perpetual open-source freedom.'}</h2>
            <p className="section-desc">
              {sourceData?.licenseDesc ||
                'Released under the permissive MIT License. You are free to inspect, modify, fork, and distribute.'}
            </p>
          </div>

          <div className="license-card">
            <div className="license-header">
              <span style={{ fontWeight: 700, color: '#ffffff' }}>{sourceData?.licenseName || 'MIT License'}</span>
              <span className="license-stamp">{sourceData?.licenseStamp || '✓ VERIFIED PERMISSIVE'}</span>
            </div>
            <div className="license-text" style={{ whiteSpace: 'pre-line' }}>
              {sourceData?.licenseText}
            </div>
          </div>
        </section>
      </main>

      <Footer data={footerData} headerData={headerData} />
    </>
  )
}
