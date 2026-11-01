'use client'

import React, { useEffect, useRef } from 'react'

export function FooterECG() {
  const canvasRef = useRef<HTMLCanvasElement | null>(null)

  useEffect(() => {
    const rawCanvas = canvasRef.current
    if (!rawCanvas) return
    const canvas: HTMLCanvasElement = rawCanvas
    const footer = (canvas.closest('.js-footer-ecg') as HTMLElement) || canvas.parentElement
    if (!footer) return

    const rawCtx = canvas.getContext('2d')
    if (!rawCtx) return
    const ctx: CanvasRenderingContext2D = rawCtx

    let cancelled = false
    let rafId: number | null = null
    let io: IntersectionObserver | null = null
    let lastTick = 0


        // Deterministic PRNG for consistent stipple & spire architecture
        var seed = 4219;
        function rand() {
          seed = (seed * 16807) % 2147483647;
          return (seed - 1) / 2147483646;
        }

        // 1. Centered multi-peak ECG QRS envelope with moderately crisp spire transitions
        var PEAKS = [
          { center: 0.500, width: 0.028, amp: 1.00, power: 0.88 },
          { center: 0.546, width: 0.021, amp: 0.82, power: 0.88 },
          { center: 0.452, width: 0.029, amp: 0.65, power: 0.90 },
          { center: 0.598, width: 0.031, amp: 0.56, power: 0.90 },
          { center: 0.386, width: 0.038, amp: 0.35, power: 0.94 },
          { center: 0.660, width: 0.038, amp: 0.33, power: 0.94 },
          { center: 0.306, width: 0.048, amp: 0.16, power: 0.98 },
          { center: 0.732, width: 0.048, amp: 0.15, power: 0.98 },
          { center: 0.212, width: 0.062, amp: 0.06, power: 1.04 },
          { center: 0.816, width: 0.062, amp: 0.06, power: 1.04 }
        ];

        function baseEnvelope(u: number) {
          var val = 0;
          for (var i = 0; i < PEAKS.length; i++) {
            var p = PEAKS[i];
            var d = Math.abs(u - p.center) / p.width;
            if (d < 4.5) {
              var contrib = p.amp * Math.exp(-Math.pow(d, p.power));
              if (contrib > val) val = contrib + val * 0.12;
              else val += contrib * 0.14;
            }
          }
          var edgeFade = Math.sin(Math.max(0, Math.min(1, u)) * Math.PI);
          return Math.min(1.10, val * Math.pow(edgeFade, 0.55));
        }

        // Precompute static ECG QRS profile across M=190 nodes with moderately sharp transitions
        var M = 190;
        var nodeEnvelope = new Float32Array(M);
        for (var c = 0; c < M; c++) {
          var u = c / (M - 1);
          var env = baseEnvelope(u);
          var rawWave = 0.5 + 0.5 * Math.sin(c * 1.32 + Math.cos(c * 0.55) * 1.5);
          // Power 1.45 sharpens the transitions between sub-peaks without creating harsh needles
          var harmonic = 0.72 + 0.28 * Math.pow(rawWave, 1.45);
          var spireCore = Math.exp(-Math.pow(Math.abs(u - 0.500) / 0.017, 1.75)) * 0.19
                        + Math.exp(-Math.pow(Math.abs(u - 0.546) / 0.014, 1.75)) * 0.14
                        + Math.exp(-Math.pow(Math.abs(u - 0.452) / 0.018, 1.75)) * 0.11;
          nodeEnvelope[c] = Math.min(1.12, env * harmonic + spireCore);
        }

        function sampleEnvelope(u: number) {
          var f = Math.max(0, Math.min(1, u)) * (M - 1);
          var idx = Math.floor(f);
          var frac = f - idx;
          var next = Math.min(M - 1, idx + 1);
          return nodeEnvelope[idx] * (1 - frac) + nodeEnvelope[next] * frac;
        }

        // 2. Smooth scroll-linked growth state (pure exponential lerp, ZERO spring bounce)
        var currentScale = 0.06;
        var targetScale = 0.06;
        var isFooterVisible = false;

        // 3A. Static stippled body point-cloud (12,000 particles anchored strictly to the static burst)
        var STATIC_STIPPLE_COUNT = 12000;
        var staticStipples: Array<{ u: number; hFrac: number; size: number; alpha: number; hotCore: boolean }> = [];
        for (var s = 0; s < STATIC_STIPPLE_COUNT; s++) {
          var rMode = rand();
          var u = 0.5;
          if (rMode < 0.55) {
            u = 0.50 + (rand() + rand() + rand() - 1.5) * 0.25;
          } else if (rMode < 0.88) {
            u = 0.50 + (rand() + rand() - 1.0) * 0.42;
          } else {
            u = rand();
          }
          u = Math.max(0.008, Math.min(0.992, u));
          if (rand() < 0.72) {
            var cSnap = Math.round(u * (M - 1));
            u = (cSnap + (rand() - 0.5) * 0.62) / (M - 1);
          }
          var env = sampleEnvelope(u);

          var kindRand = rand();
          var hFrac = 0, size = 1, alpha = 1, hotCore = false;

          if (kindRand < 0.82) {
            // Dense interior stipple body
            hFrac = Math.pow(rand(), 0.82) * (0.96 + rand() * 0.05);
            size = rand() < 0.76 ? 1.15 : 1.65;
            var tipFade = hFrac > 0.84 ? Math.max(0.15, 1 - (hFrac - 0.84) / 0.22) : 1.0;
            alpha = (0.44 + 0.56 * (1 - hFrac * 0.38)) * tipFade * (0.60 + rand() * 0.40);
            hotCore = (hFrac < 0.72 && env > 0.25 && rand() < 0.65);
          } else {
            // Feathered static spire tips
            hFrac = 0.97 + Math.pow(rand(), 1.7) * 0.34;
            size = 0.95 + rand() * 1.15;
            alpha = Math.max(0.08, (1 - (hFrac - 0.97) / 0.36) * (0.38 + rand() * 0.52));
            hotCore = rand() < 0.30;
          }

          staticStipples.push({
            u: u,
            hFrac: hFrac,
            size: size,
            alpha: Math.max(0.05, Math.min(1, alpha)),
            hotCore: hotCore
          });
        }

        // 3B. Infinite upward-drifting fine particles ("мелкие частички", 3,400 particles)
        var RISING_COUNT = 3400;
        var risingParticles: Array<{ u: number; startHFrac: number; riseSpan: number; speed: number; phase: number; size: number; alpha: number; hotCore: boolean }> = [];
        for (var r = 0; r < RISING_COUNT; r++) {
          var rMode2 = rand();
          var ru;
          if (rMode2 < 0.56) {
            ru = 0.50 + (rand() + rand() + rand() - 1.5) * 0.24;
          } else if (rMode2 < 0.88) {
            ru = 0.50 + (rand() + rand() - 1.0) * 0.40;
          } else {
            ru = rand();
          }
          ru = Math.max(0.015, Math.min(0.985, ru));
          var renv = sampleEnvelope(ru);

          risingParticles.push({
            u: ru,
            startHFrac: 0.55 + rand() * 0.48,
            riseSpan: 150 + rand() * 360,
            speed: 0.07 + rand() * 0.13,
            phase: rand(),
            size: 0.75 + rand() * 0.75,
            alpha: (0.26 + rand() * 0.58) * (0.30 + 0.70 * Math.pow(renv, 0.30)),
            hotCore: rand() < 0.28
          });
        }

        // 4. Compute Scroll Proximity to Footer Base
        function updateScrollTarget() {
          var rect = footer.getBoundingClientRect();
          var vh = window.innerHeight || document.documentElement.clientHeight;
          var footerH = Math.max(1, footer.offsetHeight || rect.height);

          isFooterVisible = (rect.top < vh + 40) && (rect.bottom > -40);

          var visiblePx = vh - rect.top;
          var startOffset = footerH * 0.18;
          var span = Math.max(1, footerH * 0.80);
          var rawProgress = (visiblePx - startOffset) / span;
          var clamped = Math.max(0, Math.min(1, rawProgress));

          // Smooth cubic S-curve (smoothstep)
          var eased = clamped * clamped * (3 - 2 * clamped);
          targetScale = 0.05 + eased * 0.87;
        }

        
    function onScroll() {
      updateScrollTarget()
      startLoop()
    }

    if ('IntersectionObserver' in window) {
      io = new IntersectionObserver((entries) => {
        for (let i = 0; i < entries.length; i++) {
          isFooterVisible = entries[i].isIntersecting
        }
        if (isFooterVisible) {
          updateScrollTarget()
          startLoop()
        }
      }, { threshold: 0.01 })
      io.observe(footer)
    }
// 5. Canvas Sizing & Continuous Render Loop
        var dpr = 1;
        function resizeCanvas() {
          if (!canvas) return;
          dpr = Math.min(window.devicePixelRatio || 1, 2);
          var w = footer.clientWidth;
          var h = footer.clientHeight;
          if (canvas.width !== Math.round(w * dpr) || canvas.height !== Math.round(h * dpr)) {
            canvas.width = Math.round(w * dpr);
            canvas.height = Math.round(h * dpr);
          }
          updateScrollTarget();
          renderFrame(0.016, performance.now());
          startLoop();
        }

        // (resize listener attached in effect)

        // (rafId managed in effect)
        // (lastTick managed in effect)

        function startLoop() {
          if (!rafId) {
            lastTick = performance.now();
            rafId = requestAnimationFrame(tick);
          }
        }

        function stepSmooth(dt: number) {
          // Responsive first-order exponential smoothing — crisp follow with ZERO spring bounce
          var smoothRate = 1 - Math.exp(-dt * 7.2);
          currentScale += (targetScale - currentScale) * smoothRate;
          if (Math.abs(targetScale - currentScale) < 0.0004) {
            currentScale = targetScale;
          }
        }

        // Corner-tensioned curve (r = 0.24): preserves crisp ECG angles while rounding razor tips
        function traceCrispPath(ptsX: Float32Array, ptsY: Float32Array, count: number) {
          var r = 0.24;
          for (var i = 1; i < count - 1; i++) {
            var x0 = ptsX[i - 1], y0 = ptsY[i - 1];
            var x1 = ptsX[i],     y1 = ptsY[i];
            var x2 = ptsX[i + 1], y2 = ptsY[i + 1];
            var ax = x1 + (x0 - x1) * r;
            var ay = y1 + (y0 - y1) * r;
            var bx = x1 + (x2 - x1) * r;
            var by = y1 + (y2 - y1) * r;
            ctx.lineTo(ax, ay);
            ctx.quadraticCurveTo(x1, y1, bx, by);
          }
          ctx.lineTo(ptsX[count - 1], ptsY[count - 1]);
        }

        function strokeCrispCurve(ptsX: Float32Array, ptsY: Float32Array, count: number) {
          if (count < 2) return;
          ctx.beginPath();
          ctx.moveTo(ptsX[0], ptsY[0]);
          traceCrispPath(ptsX, ptsY, count);
        }

        function renderFrame(dt: number, now: number) {
          if (!canvas) return;
          var w = canvas.width / dpr;
          var h = canvas.height / dpr;
          if (w <= 0 || h <= 0) return;

          ctx.save();
          ctx.scale(dpr, dpr);
          ctx.clearRect(0, 0, w, h);

          var effectiveScale = Math.max(0.05, Math.min(0.95, currentScale));
          var maxSpikeH = h * 0.72;
          var baseLineY = h;
          var timeSec = now * 0.001;

          // Precompute STATIC ECG ridge heights across M nodes (only changes with scroll scale)
          var ptsX = new Float32Array(M);
          var traceY = new Float32Array(M);
          var npcTraceY = new Float32Array(M);
          for (var i = 0; i < M; i++) {
            var u = i / (M - 1);
            ptsX[i] = u * w;
            var env = nodeEnvelope[i];
            var hVal = Math.max(2, env * maxSpikeH * effectiveScale);
            traceY[i] = Math.max(h * 0.28, Math.min(baseLineY - 2, baseLineY - hVal));

            var npcEnv = env * 0.58;
            var nhVal = Math.max(1, npcEnv * maxSpikeH * effectiveScale);
            npcTraceY[i] = Math.max(h * 0.34, Math.min(baseLineY - 2, baseLineY - nhVal));
          }

          // A. Soft atmospheric radial glow behind the central ECG spire (static when scroll stops)
          var peakX = w * 0.50;
          var peakY = h - Math.min(h * 0.78, maxSpikeH * effectiveScale * 0.68);
          var grad = ctx.createRadialGradient(peakX, h, 8, peakX, peakY, Math.max(w * 0.38, h * 0.72));
          grad.addColorStop(0, 'rgba(247, 255, 20, ' + (0.05 + 0.20 * effectiveScale).toFixed(3) + ')');
          grad.addColorStop(0.38, 'rgba(247, 255, 20, ' + (0.02 + 0.06 * effectiveScale).toFixed(3) + ')');
          grad.addColorStop(1, 'rgba(247, 255, 20, 0)');
          ctx.fillStyle = grad;
          ctx.fillRect(0, 0, w, h);

          // B. Inner luminous core fill aligned with traceY
          ctx.beginPath();
          ctx.moveTo(0, baseLineY);
          ctx.lineTo(ptsX[0], traceY[0]);
          traceCrispPath(ptsX, traceY, M);
          ctx.lineTo(w, baseLineY);
          ctx.closePath();
          var coreGrad = ctx.createLinearGradient(0, h - maxSpikeH * Math.max(0.2, effectiveScale), 0, h);
          coreGrad.addColorStop(0, 'rgba(247, 255, 20, 0.04)');
          coreGrad.addColorStop(0.34, 'rgba(247, 255, 20, 0.26)');
          coreGrad.addColorStop(0.70, 'rgba(252, 255, 115, 0.54)');
          coreGrad.addColorStop(1, 'rgba(254, 255, 198, 0.72)');
          ctx.fillStyle = coreGrad;
          ctx.fill();

          var scaleAlpha = Math.min(1.15, 0.25 + 0.85 * effectiveScale);

          // C1. Static stippled point-cloud body (burst remains completely static when not scrolling)
          for (var s = 0; s < staticStipples.length; s++) {
            var st = staticStipples[s];
            var mIdx = Math.min(M - 1, Math.max(0, Math.round(st.u * (M - 1))));
            var colH = baseLineY - traceY[mIdx];
            if (colH < 2) continue;

            var py = baseLineY - colH * st.hFrac;
            if (py < 4 || py > h) continue;
            var px = st.u * w;

            var a = st.alpha * scaleAlpha;
            ctx.fillStyle = st.hotCore
              ? 'rgba(254, 255, 210, ' + Math.min(1, a).toFixed(3) + ')'
              : 'rgba(247, 255, 20, ' + Math.min(1, a).toFixed(3) + ')';
            ctx.fillRect(px - st.size * 0.5, py - st.size * 0.5, st.size, st.size);
          }

          // C2. Infinite upward-drifting fine particles ("мелкие частички постоянно уходят вверх бесконечно")
          var riseScaleFactor = 0.25 + 0.75 * effectiveScale;
          for (var r = 0; r < risingParticles.length; r++) {
            var rp = risingParticles[r];
            var mIdx2 = Math.min(M - 1, Math.max(0, Math.round(rp.u * (M - 1))));
            var colH2 = baseLineY - traceY[mIdx2];
            if (colH2 < 2) continue;

            // Continuous cyclic progress in [0, 1) advancing infinitely upward with timeSec
            var prog = (rp.phase + timeSec * rp.speed) % 1.0;
            var originY = baseLineY - colH2 * rp.startHFrac;
            var py2 = originY - prog * rp.riseSpan * riseScaleFactor;
            if (py2 < 2 || py2 > h) continue;

            var px2 = rp.u * w;
            var fadeIn = Math.min(1, prog / 0.10);
            var fadeOut = Math.pow(1 - prog, 1.35);
            var ra = rp.alpha * fadeIn * fadeOut * scaleAlpha;
            if (ra < 0.015) continue;

            ctx.fillStyle = rp.hotCore
              ? 'rgba(254, 255, 210, ' + Math.min(1, ra).toFixed(3) + ')'
              : 'rgba(247, 255, 20, ' + Math.min(1, ra).toFixed(3) + ')';
            ctx.fillRect(px2 - rp.size * 0.5, py2 - rp.size * 0.5, rp.size, rp.size);
          }

          // D. Crisp Oscilloscope ECG Traces (static contour with moderately sharp transitions)
          strokeCrispCurve(ptsX, npcTraceY, M);
          ctx.strokeStyle = 'rgba(238, 242, 248, 0.34)';
          ctx.lineWidth = 1.0;
          ctx.lineJoin = 'round';
          ctx.stroke();

          strokeCrispCurve(ptsX, traceY, M);
          ctx.lineJoin = 'round';
          ctx.lineCap = 'round';

          ctx.strokeStyle = 'rgba(247, 255, 20, 0.18)';
          ctx.lineWidth = 5.0;
          ctx.stroke();

          ctx.strokeStyle = 'rgba(247, 255, 20, 0.48)';
          ctx.lineWidth = 2.1;
          ctx.stroke();

          ctx.strokeStyle = '#feff9e';
          ctx.lineWidth = 1.15;
          ctx.stroke();

          ctx.restore();
        }

        
    function tick(now: number) {
      if (cancelled) return
      var dt = Math.min(0.033, Math.max(0.008, (now - lastTick) / 1000))
      lastTick = now

      stepSmooth(dt)
      renderFrame(dt, now)

      if (isFooterVisible || Math.abs(targetScale - currentScale) > 0.0005) {
        rafId = requestAnimationFrame(tick)
      } else {
        rafId = null
      }
    }

    window.addEventListener('scroll', onScroll, { passive: true })
    window.addEventListener('resize', resizeCanvas, { passive: true })

    updateScrollTarget()
    currentScale = targetScale
    resizeCanvas()

    return () => {
      cancelled = true
      if (rafId) cancelAnimationFrame(rafId)
      window.removeEventListener('scroll', onScroll)
      window.removeEventListener('resize', resizeCanvas)
      if (io) io.disconnect()
    }
  }, [])

  return (
    <canvas ref={canvasRef} className="footer-ecg-canvas js-footer-ecg-canvas" aria-hidden="true" />
  )
}
