import { getPayload } from 'payload'
import config from '@payload-config'
import { Footer } from '@/components/Footer'
import React from 'react'
import type { Metadata } from 'next'

export const metadata: Metadata = {
  title: 'The Norns · Source Code & Security Audit · EVE Online',
  description: 'Audit The Norns source code, security transparency, MIT license, and reproducible local build instructions for EVE Online capsuleers.',
}

export const dynamic = 'force-dynamic'
export const revalidate = 0

export default async function SourcePage() {
  let footerData: any = null
  try {
    const payload = await getPayload({ config })
    footerData = await payload.findGlobal({ slug: 'footer' })
  } catch (e) {
    console.error('Error fetching footer for source page:', e)
  }
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
      <a href="/">Overview</a>
      <a href="/#capabilities">Doctrine</a>
      <a href="/#pipeline">Compliance</a>
      <a href="/source" className="active">Source & Build</a>
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
          <li><a href="/">Overview</a></li>
          <li><a href="/#capabilities">Doctrine</a></li>
          <li><a href="/#pipeline">Compliance</a></li>
          <li><a href="/#faq">FAQ</a></li>
          <li><a href="/source" className="active">Source &amp; Build</a></li>
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

    {/*  Hero Header  */}
    <section className="hero">
      <div className="live-status-pill">
        <span className="indicator-dot"></span>
        <span>OPEN SOURCE · MIT LICENSE · STRICT ZERO-INJECTION</span>
      </div>

      <h1 className="hero-title">
        Audit the source.<br />
        <span className="accent-text">Fly safe in nullsec.</span>
      </h1>

      <p className="hero-subtitle">
        Full architectural transparency for fleet commanders and capsuleers. 
        Verify that The Norns runs strictly read-only, never touches exefile.exe memory, and compiles into byte-reproducible Windows binaries with native tools.
      </p>

      <div className="cta-group">
        <a href="https://github.com/MadisonCatfish/the-norns/archive/refs/heads/main.zip" className="btn btn-primary">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
          Download Source (.zip)
        </a>
        <a href="https://github.com/MadisonCatfish/the-norns" className="btn btn-secondary" target="_blank" rel="noopener">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor"><path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/></svg>
          GitHub Repository →
        </a>
        <a href="https://github.com/MadisonCatfish/the-norns/releases" className="btn btn-secondary" target="_blank" rel="noopener">
          Release Checksums
        </a>
      </div>
    </section>

    {/*  Structural Attio Metric Grid  */}
    <div className="metric-grid">
      <div className="metric-cell">
        <div className="metric-header">[ MEMORY BARRIER ]</div>
        <div className="metric-val">0</div>
        <div className="metric-desc">Injected bytes. Zero RAM scraping and zero DLLs in exefile.exe. The optional Carousel module (off by default) uses window hooks.</div>
      </div>
      <div className="metric-cell">
        <div className="metric-header">[ NATIVE ENGINE ]</div>
        <div className="metric-val">.NET 4.8</div>
        <div className="metric-desc">Standard C# targeting .NET Framework 4.8 via native Windows Roslyn.</div>
      </div>
      <div className="metric-cell">
        <div className="metric-header">[ LICENSE ]</div>
        <div className="metric-val">MIT</div>
        <div className="metric-desc">Unrestricted open-source freedom for audit, fork, and code study.</div>
      </div>
      <div className="metric-cell">
        <div className="metric-header">[ INTEGRITY ]</div>
        <div className="metric-val">SHA-256</div>
        <div className="metric-desc">Automated GitHub Actions workflow builds public matching hashes.</div>
      </div>
    </div>

    {/*  Security & Policy Bento Grid  */}
    <section className="section-wrap">
      <div className="section-header">
        <div className="section-tag">[ 01 / CCP FAIR PLAY COMPLIANCE ]</div>
        <h2 className="section-title">Architected to stay out of the game client.</h2>
        <p className="section-desc">
          Every architectural boundary in The Norns is designed to ensure zero interference with the game client, zero network leakage, and absolute peace of mind for multi-billion ISK setups.
        </p>
      </div>

      <div className="security-grid">
        {/*  Card 1  */}
        <div className="security-card">
          <div className="card-top">
            <span className="badge-pill">[ I/O ISOLATION ]</span>
            <span className="status-check">✓ READ-ONLY</span>
          </div>
          <h3 className="card-heading">Read-Only Gamelog Streaming</h3>
          <p className="card-desc">
            The Norns reads solely from standard plain-text logs written by EVE Online in <code>Documents\EVE\logs\Gamelogs</code> and OS window titles using non-locking <code>FileShare.ReadWrite</code> handles.
          </p>
          <div className="card-code-callout">
            <span className="code-dot"></span>
            <code>new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)</code>
          </div>
        </div>

        {/*  Card 2  */}
        <div className="security-card">
          <div className="card-top">
            <span className="badge-pill">[ PROCESS BARRIER ]</span>
            <span className="status-check">✓ ZERO INJECTION</span>
          </div>
          <h3 className="card-heading">Zero Memory or Process Hooks</h3>
          <p className="card-desc">
            The Norns never calls <code>OpenProcess</code> with VM access, never inspects or alters <code>exefile.exe</code> virtual memory, and injects zero DLLs. It is completely isolated in its own userland process.
          </p>
          <div className="card-code-callout">
            <span className="code-dot"></span>
            <code>VirtualAllocEx / WriteProcessMemory / CreateRemoteThread: NOT LINKED</code>
          </div>
        </div>

        {/*  Card 3  */}
        <div className="security-card">
          <div className="card-top">
            <span className="badge-pill">[ INPUT INTEGRITY ]</span>
            <span className="status-check">✓ PASSIVE TELEMETRY</span>
          </div>
          <h3 className="card-heading">Zero Input Broadcasting</h3>
          <p className="card-desc">
            No simulated keystrokes, no clicks, and no input broadcasting. The optional Carousel module (off by default) can activate a client window. The telemetry display is passive; the design follows CCP's 1-action-1-input guideline.
          </p>
          <div className="card-code-callout">
            <span className="code-dot"></span>
            <code>SendInput / keybd_event / mouse_event: NOT PRESENT</code>
          </div>
        </div>

        {/*  Card 4  */}
        <div className="security-card">
          <div className="card-top">
            <span className="badge-pill">[ PRIVACY GUARANTEE ]</span>
            <span className="status-check">✓ LOCAL FIRST</span>
          </div>
          <h3 className="card-heading">No Silent Telemetry or Beacons</h3>
          <p className="card-desc">
            No accounts, no user analytics, no tracking beacons. The only outbound network call is the transparent, toggleable startup check against GitHub Releases API for software updates.
          </p>
          <div className="card-code-callout">
            <span className="code-dot"></span>
            <code>api.github.com/repos/MadisonCatfish/the-norns/releases/latest (Toggleable)</code>
          </div>
        </div>
      </div>
    </section>

    {/*  Codebase Architecture Section  */}
    <section className="section-wrap">
      <div className="section-header">
        <div className="section-tag">[ 02 / CODEBASE ARCHITECTURE ]</div>
        <h2 className="section-title">Clean, zero-dependency C# codebase.</h2>
        <p className="section-desc">
          No heavy web wrappers, no Electron bloat, no third-party package dependencies. Pure C# targeting native Windows graphics and non-blocking file streaming.
        </p>
      </div>

      <div className="master-frame">
        <div className="frame-bar">
          <div className="frame-controls">
            <span></span><span></span><span></span>
          </div>
          <div className="frame-title">the-norns / native / source-tree</div>
          <div className="frame-badge">C# 7.3 · .NET 4.8</div>
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
            <tr>
              <td><span className="module-tag">Log Ingestion</span></td>
              <td><span className="file-chip">native/DpsGraph.cs</span></td>
              <td>Tails game logs non-destructively; parses damage hits, bounties, and fleet warp events (<code>GameLogTail</code>, <code>GameLogParser</code>).</td>
            </tr>
            <tr>
              <td><span className="module-tag">Terminal Chassis</span></td>
              <td><span className="file-chip">native/Body.cs</span></td>
              <td>Industrial OLED hardware chassis, double-buffered GDI+ rendering, mode switching, carousel window switcher.</td>
            </tr>
            <tr>
              <td><span className="module-tag">Fleet Physics</span></td>
              <td><span className="file-chip">native/Formula.cs</span></td>
              <td>Calculates Vorton bounce decay, frigate runner impedance, and interactive damped-spring hull ECG physics.</td>
            </tr>
            <tr>
              <td><span className="module-tag">Localization & Config</span></td>
              <td><span className="file-chip">native/Settings.cs</span></td>
              <td>6 native languages, profile serialization in <code>%LOCALAPPDATA%</code>, update check management.</td>
            </tr>
            <tr>
              <td><span className="module-tag">Setup Installer</span></td>
              <td><span className="file-chip">native/setup/Setup.cs</span></td>
              <td>Lightweight standalone installer with privacy switches and automated desktop/start menu integration.</td>
            </tr>
            <tr>
              <td><span className="module-tag">Build Orchestration</span></td>
              <td><span className="file-chip">native/build.ps1</span></td>
              <td>70-line native PowerShell build orchestrator invoking <code>csc.exe</code> with zero third-party packages.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    {/*  Reproducible Build Section  */}
    <section className="section-wrap">
      <div className="section-header">
        <div className="section-tag">[ 03 / REPRODUCIBLE BUILDS ]</div>
        <h2 className="section-title">Build your own binary.</h2>
        <p className="section-desc">
          The Norns compiles using native Windows developer tools (.NET Framework 4.8 via Roslyn). No Node.js, Python, or external package managers required for compilation.
        </p>
      </div>

      <div className="master-frame">
        <div className="frame-bar">
          <div className="frame-controls">
            <span></span><span></span><span></span>
          </div>
          <div className="frame-title">Windows PowerShell · C:\The Norns&gt;</div>
          <div className="frame-badge">POWERSHELL 5.1+</div>
        </div>

        <div className="terminal-body">
          <div className="block">
            <span className="comment"># 1. Clone the repository</span><br />
            <span className="cmd">git clone</span> https://github.com/MadisonCatfish/the-norns.git<br />
            <span className="cmd">cd</span> the-norns
          </div>

          <div className="block">
            <span className="comment"># 2. Build the application binary (TheNorns.exe)</span><br />
            <span className="cmd">powershell</span> <span className="flag">-ExecutionPolicy</span> Bypass <span className="flag">-File</span> native\build.ps1 <span className="flag">-Out</span> bin
          </div>

          <div className="block">
            <span className="comment"># 3. Package the setup installer and portable zip</span><br />
            <span className="cmd">powershell</span> <span className="flag">-ExecutionPolicy</span> Bypass <span className="flag">-File</span> native\build.ps1 <span className="flag">-Out</span> release <span className="flag">-Setup</span>
          </div>

          <div className="block">
            <span className="comment"># 4. Run automated self-tests</span><br />
            bin\TheNorns.exe <span className="flag">--selftest-dps</span> selftest_output
          </div>
        </div>
      </div>
    </section>

    {/*  MIT License Section  */}
    <section className="section-wrap">
      <div className="section-header">
        <div className="section-tag">[ 04 / OPEN SOURCE LICENSE ]</div>
        <h2 className="section-title">Perpetual open-source freedom.</h2>
        <p className="section-desc">
          Released under the permissive MIT License. You are free to inspect, modify, fork, and distribute.
        </p>
      </div>

      <div className="license-card">
        <div className="license-header">
          <span style={{'fontWeight': '700', 'color': '#ffffff'}}>MIT License</span>
          <span className="license-stamp">✓ VERIFIED PERMISSIVE</span>
        </div>
        <div className="license-text">
          Copyright (c) 2026 The Norns contributors<br /><br />
          Permission is hereby granted, free of charge, to any person obtaining a copy
          of this software and associated documentation files (the "Software"), to deal
          in the Software without restriction, including without limitation the rights
          to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
          copies of the Software, and to permit persons to whom the Software is
          furnished to do so, subject to the following conditions:<br /><br />
          The above copyright notice and this permission notice shall be included in all
          copies or substantial portions of the Software.<br /><br />
          THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
          IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
          FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
          AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
          LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
          OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
          SOFTWARE.
        </div>
      </div>
    </section>

  </main>
      <Footer data={footerData} />
    </>
  )
}
