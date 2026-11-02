'use client'

import React, { useEffect, useRef } from 'react'

// The footer's live graph: the app's main module across the footer's whole width, in its own band under the content
// (styles.css gives the footer the room). Five pilots' Vorton damage as thin lines (pilot 1 white on top, the others
// greyer), the anchor's smartbombs as a dotted white line, the fleet's DPS as the big number, then the chips and the
// ISK grid filling tick by tick. The lines ease towards each new second as they do in the app.
// The mouse reads the graph (the second, each pilot, the fleet's DPS then); a click is a volley; a fast scroll pushes
// the fleet. All made up in the page: nothing is fetched.

type Tick = { born: number; v: number; big: boolean }
type Area = { left: number; right: number; head: number; top: number; bottom: number; chips: number; grid: number; g: number; cols: number }

const N = 60 // a minute of seconds
const P = 5 // five pilots
const ROWS = 10 // the ISK grid's rows
const BASE = [2300, 3000, 3700, 2300, 3000]
const TICKS = [27000, 27750, 29250, 240000, 260000, 5175, 5400, 5625, 260000, 4050, 4275, 42750, 45000, 47250, 260000, 27750, 30750, 240000]

export function FooterECG() {
  const canvasRef = useRef<HTMLCanvasElement | null>(null)

  useEffect(() => {
    const cv = canvasRef.current
    if (!cv) return
    const footer = (cv.closest('.js-footer-ecg') as HTMLElement | null) || cv.parentElement
    const ctx = cv.getContext('2d')
    if (!footer || !ctx) return
    const reduce = typeof window.matchMedia === 'function' && window.matchMedia('(prefers-reduced-motion: reduce)').matches

    const target: Float32Array[] = []
    const vis: Float32Array[] = []
    for (let i = 0; i < P; i++) {
      target.push(new Float32Array(N))
      vis.push(new Float32Array(N))
    }
    const sb = new Float32Array(N)
    const sbVis = new Float32Array(N)
    let W = 0, H = 0, dpr = 1, maxV = 4000, total = 0, totalTarget = 0, flash = 0, energy = 0
    let t = 0, sec = 0, isk = 0, iskShown = 0, anoms = 3, stars = 0, session = 612.4e6
    let ticks: Tick[] = []
    const rnd = Math.random

    const band = () => (W < 760 ? 470 : 520)
    // the module: the footer's whole width, in the band; the plot, the chips, the ISK grid under them
    function area(): Area {
      const pad = W < 760 ? 16 : 24
      const top = H - band() + 26
      const plotTop = top + 44
      const plotBottom = plotTop + (W < 760 ? 150 : 190)
      const chips = plotBottom + 22
      const grid = chips + 44
      const g = Math.min(16, (H - 28 - grid) / (ROWS - 1))
      return { left: pad, right: W - pad, head: top, top: plotTop, bottom: plotBottom, chips, grid, g, cols: Math.floor((W - 2 * pad - 8) / g) + 1 }
    }
    function resize() {
      dpr = Math.min(window.devicePixelRatio || 1, 2)
      W = footer!.clientWidth
      H = footer!.clientHeight
      cv!.width = Math.round(W * dpr)
      cv!.height = Math.round(H * dpr)
    }

    // smoothed a little, as in the app: one volley does not spike the curve
    const smooth = (s: Float32Array, j: number) => (s[Math.max(0, j - 1)] + 2 * s[j] + s[Math.min(N - 1, j + 1)]) / 4

    // one second of the fleet: each pilot hits most seconds, in slow waves; the anchor's smartbombs every 9 s
    function second() {
      for (let i = 0; i < P; i++) {
        const s = target[i]
        s.copyWithin(0, 1)
        const wave = 0.7 + 0.3 * Math.sin((sec * 2 * Math.PI) / (13 + 3 * i) + i * 1.3)
        s[N - 1] = rnd() < 0.16 ? 0 : BASE[i] * wave * (0.75 + rnd() * 0.5) * (1 + energy * 0.6)
        vis[i].copyWithin(0, 1)
        vis[i][N - 1] = vis[i][N - 2]
      }
      sb.copyWithin(0, 1)
      sbVis.copyWithin(0, 1)
      sbVis[N - 1] = sbVis[N - 2]
      sb[N - 1] = sec % 9 < 2 ? 1500 + rnd() * 600 : sb[N - 2] * 0.55
      // a bounty tick every ~3 s, its size from a real anomaly's ticks; a full grid is the next site
      if (sec % 3 === 0 || rnd() < 0.12) {
        if (ticks.length >= area().cols * ROWS) {
          ticks = []
          isk = 0
          iskShown = 0
          anoms++
        }
        const big = rnd() < 0.05
        const v = big ? 1875000 : TICKS[Math.floor(rnd() * TICKS.length)]
        ticks.push({ born: t, v, big })
        isk += v
        session += v
        if (big) stars++
      }
      // the big number takes the fleet's DPS once a second, as the app's does
      let tot = 0
      for (let p = 0; p < P; p++) tot += smooth(target[p], N - 2)
      totalTarget = tot
      sec++
    }

    function step(dt: number) {
      t += dt
      energy *= Math.exp(-dt * 0.7)
      flash *= Math.exp(-dt * 3)
      while (t >= sec) second()
      const k = 1 - Math.exp(-dt * 2.1) // the app eases 0.08 a frame at 25 fps
      let m = 100
      for (let i = 0; i < P; i++)
        for (let j = 0; j < N; j++) {
          const g = smooth(target[i], j)
          vis[i][j] += (g - vis[i][j]) * k
          if (g > m) m = g
        }
      for (let j = 0; j < N; j++) {
        sbVis[j] += (smooth(sb, j) - sbVis[j]) * k
        if (sb[j] > m) m = sb[j]
      }
      maxV += (m * 1.15 - maxV) * k
      total += (totalTarget - total) * (1 - Math.exp(-dt * 1.6))
      iskShown += (isk - iskShown) * (1 - Math.exp(-dt * 4))
    }

    const grey = (i: number) => {
      const g = Math.round(255 - i * (110 / (P - 1)))
      return g + ',' + g + ',' + Math.min(255, g + 6)
    }
    const fmt = (n: number) => Math.round(n).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ')
    function roundRect(x: number, y: number, w: number, h: number, r: number) {
      ctx!.beginPath()
      ctx!.moveTo(x + r, y)
      ctx!.arcTo(x + w, y, x + w, y + h, r)
      ctx!.arcTo(x + w, y + h, x, y + h, r)
      ctx!.arcTo(x, y + h, x, y, r)
      ctx!.arcTo(x, y, x + w, y, r)
      ctx!.closePath()
    }

    // points of a series on screen: the newest second at the right, the whole scrolling smoothly with the clock
    function pts(s: Float32Array, a: Area) {
      const frac = t - Math.floor(t), dx = (a.right - a.left) / (N - 2), out: number[] = []
      for (let j = 0; j < N; j++) out.push(a.left + (j - frac) * dx, a.bottom - (a.bottom - a.top) * Math.min(1, s[j] / maxV))
      return out
    }
    // a cardinal spline through the points, as GDI+ DrawCurve(tension 0.5) draws the app's lines
    function curve(p: number[]) {
      ctx!.moveTo(p[0], p[1])
      const c = 0.5 / 3
      for (let i = 0; i < p.length - 2; i += 2) {
        const x0 = p[Math.max(0, i - 2)], y0 = p[Math.max(1, i - 1)], x1 = p[i], y1 = p[i + 1], x2 = p[i + 2], y2 = p[i + 3]
        const x3 = p[Math.min(p.length - 2, i + 4)], y3 = p[Math.min(p.length - 1, i + 5)]
        ctx!.bezierCurveTo(x1 + (x2 - x0) * c, y1 + (y2 - y0) * c, x2 - (x3 - x1) * c, y2 - (y3 - y1) * c, x2, y2)
      }
    }

    function draw() {
      const g = ctx!
      g.setTransform(dpr, 0, 0, dpr, 0, 0)
      g.globalCompositeOperation = 'source-over'
      g.clearRect(0, 0, W, H)
      const a = area()

      // the header: the module's title and the fleet's number
      g.textBaseline = 'alphabetic'
      g.font = '500 ' + (W < 760 ? 11 : 13) + 'px Orbitron, sans-serif'
      g.fillStyle = 'rgba(238,242,248,0.9)'
      const title = 'EFF. DPS · VORTON PROJECTOR'
      g.fillText(title, a.left, a.head + 18)
      const tw = g.measureText(title).width
      g.strokeStyle = 'rgba(238,242,248,0.35)'
      g.lineWidth = 1
      roundRect(a.left + tw + 10, a.head + 3, 22, 20, 4)
      g.stroke()
      g.font = 'italic 600 12px "Noto Sans", serif'
      g.fillStyle = 'rgba(238,242,248,0.8)'
      g.fillText('fx', a.left + tw + 15, a.head + 18)
      g.font = '700 ' + (W < 760 ? 22 : 30) + 'px Orbitron, sans-serif'
      g.textAlign = 'right'
      if (flash > 0.02) {
        g.shadowColor = 'rgba(247,255,20,' + (0.8 * flash).toFixed(3) + ')'
        g.shadowBlur = 18 * flash
      }
      g.fillStyle = '#ffffff'
      g.fillText(fmt(total), a.right, a.head + 26)
      g.shadowBlur = 0
      g.textAlign = 'left'

      // the plot: its floor line, the lines clipped to it
      g.fillStyle = 'rgba(238,242,248,0.18)'
      g.fillRect(a.left, a.bottom, a.right - a.left, 1)
      g.save()
      g.beginPath()
      g.rect(a.left - 2, a.top - 6, a.right - a.left + 4, a.bottom - a.top + 8)
      g.clip()
      g.lineJoin = 'round'
      // the smartbombs: a dotted white line
      g.setLineDash([1.5, 4])
      g.lineCap = 'round'
      g.beginPath()
      curve(pts(sbVis, a))
      g.strokeStyle = 'rgba(238,242,248,0.85)'
      g.lineWidth = 1.6
      g.stroke()
      g.setLineDash([])
      // the pilots, pilot 1 last (on top); a soft glow under each
      for (let i = P - 1; i >= 0; i--) {
        const c = grey(i)
        g.beginPath()
        curve(pts(vis[i], a))
        g.globalCompositeOperation = 'lighter'
        g.strokeStyle = 'rgba(' + c + ',0.07)'
        g.lineWidth = 6
        g.stroke()
        g.globalCompositeOperation = 'source-over'
        g.strokeStyle = 'rgb(' + c + ')'
        g.lineWidth = i === 0 ? 1.4 : 1.1
        g.stroke()
      }
      g.restore()
      // the left edge fades into the footer
      g.globalCompositeOperation = 'destination-out'
      const fade = g.createLinearGradient(a.left, 0, a.left + 160, 0)
      fade.addColorStop(0, 'rgba(0,0,0,1)')
      fade.addColorStop(1, 'rgba(0,0,0,0)')
      g.fillStyle = fade
      g.fillRect(a.left - 4, a.top - 8, 164, a.bottom - a.top + 10)
      g.globalCompositeOperation = 'source-over'

      // reading the graph under the mouse: the second, each pilot's dot, the fleet's DPS then
      if (mx > a.left && mx < a.right && my > a.top - 20 && my < a.bottom + 8) {
        const dx = (a.right - a.left) / (N - 2), frac = t - Math.floor(t)
        const jf = (mx - a.left) / dx + frac, j0 = Math.max(0, Math.min(N - 2, Math.floor(jf))), f = jf - j0
        g.setLineDash([2, 4])
        g.strokeStyle = 'rgba(247,255,20,0.55)'
        g.lineWidth = 1
        g.beginPath()
        g.moveTo(mx + 0.5, a.top - 4)
        g.lineTo(mx + 0.5, a.bottom)
        g.stroke()
        g.setLineDash([])
        let sum = 0
        for (let q = 0; q < P; q++) {
          const v = vis[q][j0] * (1 - f) + vis[q][j0 + 1] * f
          sum += v
          const y = a.bottom - (a.bottom - a.top) * Math.min(1, v / maxV)
          g.fillStyle = q === 0 ? '#ffffff' : 'rgb(' + grey(q) + ')'
          g.beginPath()
          g.arc(mx, y, q === 0 ? 3 : 2.3, 0, Math.PI * 2)
          g.fill()
        }
        const ago = Math.max(0, Math.round(N - 1 - jf))
        const label = (ago === 0 ? 'now' : '−' + ago + ' s') + '   ' + fmt(sum) + ' DPS'
        g.font = '500 11px Orbitron, sans-serif'
        const lw = g.measureText(label).width + 16, lx = Math.min(a.right - lw, mx + 10), ly = a.top - 2
        g.fillStyle = 'rgba(7,9,14,0.92)'
        roundRect(lx, ly, lw, 22, 4)
        g.fill()
        g.strokeStyle = 'rgba(247,255,20,0.45)'
        g.stroke()
        g.fillStyle = '#f7ff14'
        g.fillText(label, lx + 8, ly + 15)
      }

      // the chips: anomalies, faction spawns, the session's ISK; the anomaly's ISK on the right
      g.font = '500 ' + (W < 760 ? 11 : 12) + 'px Orbitron, sans-serif'
      let cx = a.left
      const chips = [anoms + ' ANOM', '★ ' + stars, 'Σ ' + (session / 1e6).toFixed(1) + ' M. ISK']
      for (let ci = 0; ci < chips.length; ci++) {
        const cw = g.measureText(chips[ci]).width + 18
        roundRect(cx, a.chips, cw, 24, 4)
        g.fillStyle = ci === 2 ? 'rgba(238,242,248,0.08)' : 'rgba(7,9,14,1)'
        g.fill()
        g.strokeStyle = 'rgba(238,242,248,0.28)'
        g.lineWidth = 1
        g.stroke()
        g.fillStyle = '#eef2f8'
        g.fillText(chips[ci], cx + 9, a.chips + 16)
        cx += cw + 8
      }
      g.font = '700 ' + (W < 760 ? 16 : 20) + 'px Orbitron, sans-serif'
      g.textAlign = 'right'
      g.fillStyle = '#ffffff'
      g.fillText((iskShown / 1e6).toFixed(1) + ' M. ISK', a.right, a.chips + 18)
      g.textAlign = 'left'

      // the ISK grid: one dot a bounty tick, column by column from the top; the bigger the tick the hotter the dot, a
      // commander's in white; a ring where the next one lands
      const r0 = Math.min(4.6, a.g * 0.3)
      for (let col = 0; col < a.cols; col++)
        for (let row = 0; row < ROWS; row++) {
          const x = a.left + 4 + col * a.g, y = a.grid + row * a.g, tk = ticks[col * ROWS + row]
          g.fillStyle = 'rgba(238,242,248,0.07)'
          g.beginPath()
          g.arc(x, y, r0, 0, Math.PI * 2)
          g.fill()
          if (!tk) continue
          const on = Math.min(1, (t - tk.born) / 0.35)
          const heat = tk.big ? 1 : Math.max(0, Math.min(1, Math.log(tk.v / 4000) / Math.log(260000 / 4000)))
          const col2 = tk.big ? '255,255,255' : '247,255,20', al = (0.28 + 0.72 * heat) * on
          if (!reduce && heat > 0.55) {
            const hr = r0 * 3.2, halo = g.createRadialGradient(x, y, 0, x, y, hr)
            halo.addColorStop(0, 'rgba(' + col2 + ',' + (0.3 * al).toFixed(3) + ')')
            halo.addColorStop(1, 'rgba(' + col2 + ',0)')
            g.fillStyle = halo
            g.fillRect(x - hr, y - hr, hr * 2, hr * 2)
          }
          g.fillStyle = 'rgba(' + col2 + ',' + al.toFixed(3) + ')'
          g.beginPath()
          g.arc(x, y, r0, 0, Math.PI * 2)
          g.fill()
        }
      const ncol = Math.floor(ticks.length / ROWS), nrow = ticks.length % ROWS
      if (ncol < a.cols) {
        g.strokeStyle = 'rgba(238,242,248,0.75)'
        g.lineWidth = 1.2
        g.beginPath()
        g.arc(a.left + 4 + ncol * a.g, a.grid + nrow * a.g, r0 + 1.2, 0, Math.PI * 2)
        g.stroke()
      }
    }

    // input: the canvas lets the mouse through to the links, so the footer listens
    let mx = -1, my = -1
    const onMove = (e: PointerEvent) => {
      const r = footer.getBoundingClientRect()
      mx = e.clientX - r.left
      my = e.clientY - r.top
    }
    const onLeave = () => {
      mx = my = -1
    }
    const onDown = () => {
      if (my < area().head - 20) return
      for (let i = 0; i < P; i++) target[i][N - 1] += BASE[i] * (1.1 + rnd() * 0.5)
      flash = 1
    }
    let lastScroll = window.scrollY
    const onScroll = () => {
      const d = Math.abs(window.scrollY - lastScroll)
      lastScroll = window.scrollY
      energy = Math.min(1.5, energy + d / 1200)
    }

    let visible = true, raf = 0, last = 0, cancelled = false
    function tick(now: number) {
      if (cancelled) return
      const dt = Math.min(0.05, (now - last) / 1000)
      last = now
      step(reduce ? dt * 0.5 : dt)
      draw()
      raf = visible ? requestAnimationFrame(tick) : 0
    }
    function start() {
      if (!raf) {
        last = performance.now()
        raf = requestAnimationFrame(tick)
      }
    }
    let io: IntersectionObserver | null = null
    if ('IntersectionObserver' in window) {
      io = new IntersectionObserver((es) => {
        visible = es[0].isIntersecting
        if (visible) start()
      }, { threshold: 0 })
      io.observe(footer)
    }

    footer.addEventListener('pointermove', onMove)
    footer.addEventListener('pointerleave', onLeave)
    footer.addEventListener('pointerdown', onDown)
    window.addEventListener('scroll', onScroll, { passive: true })
    window.addEventListener('resize', resize)
    // the footer's height settles after the fonts and the page's own layout
    const ro = typeof ResizeObserver === 'function' ? new ResizeObserver(resize) : null
    if (ro) ro.observe(footer)

    resize()
    for (let w = 0; w < 64 * 30; w++) step(1 / 30) // a full minute of history at once
    // a site already under way: about 40 % of the grid filled
    const want = Math.round(area().cols * ROWS * 0.4)
    while (ticks.length < want) ticks.unshift({ born: -10, v: TICKS[Math.floor(rnd() * TICKS.length)], big: rnd() < 0.03 })
    ticks = ticks.slice(-want)
    isk = ticks.reduce((s, x) => s + x.v, 0)
    iskShown = isk
    stars = ticks.filter((x) => x.big).length
    total = totalTarget
    start()

    return () => {
      cancelled = true
      if (raf) cancelAnimationFrame(raf)
      footer.removeEventListener('pointermove', onMove)
      footer.removeEventListener('pointerleave', onLeave)
      footer.removeEventListener('pointerdown', onDown)
      window.removeEventListener('scroll', onScroll)
      window.removeEventListener('resize', resize)
      if (io) io.disconnect()
      if (ro) ro.disconnect()
    }
  }, [])

  return <canvas ref={canvasRef} className="footer-ecg-canvas js-footer-ecg-canvas" aria-hidden="true" />
}
