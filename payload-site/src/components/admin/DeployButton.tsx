'use client'

import React, { useState } from 'react'

export function DeployButton() {
  const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle')
  const [message, setMessage] = useState<string>('')

  const handleDeploy = async () => {
    setStatus('loading')
    setMessage('Sending deploy webhook to Cloudflare Pages...')

    try {
      const res = await fetch('/api/deploy-cloudflare', {
        method: 'POST',
      })
      const data = await res.json()

      if (res.ok && data.success) {
        setStatus('success')
        setMessage('✓ Deployment triggered! Cloudflare Pages is rebuilding the live site (~30s).')
      } else {
        setStatus('error')
        setMessage(data.message || 'Failed to trigger deploy. Check deploy hook URL.')
      }
    } catch (err: any) {
      setStatus('error')
      setMessage(err.message || 'Network error triggering deploy.')
    }
  }

  return (
    <div style={{
      background: 'rgba(255, 255, 255, 0.03)',
      border: '1px solid rgba(255, 255, 255, 0.12)',
      borderRadius: '10px',
      padding: '20px',
      margin: '20px 0',
      display: 'grid',
      gap: '12px',
    }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '10px' }}>
        <div>
          <div style={{ fontWeight: 700, fontSize: '15px', color: '#ffffff', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span>⚡ Cloudflare Pages Jamstack Deployment</span>
          </div>
          <p style={{ margin: '4px 0 0', fontSize: '13px', color: 'rgba(255, 255, 255, 0.65)' }}>
            Trigger a fresh static build and publish all updated content to Cloudflare CDN edge nodes.
          </p>
        </div>

        <button
          type="button"
          onClick={handleDeploy}
          disabled={status === 'loading'}
          style={{
            background: status === 'loading' ? '#334155' : 'linear-gradient(135deg, #f7ff14 0%, #e2ea00 100%)',
            color: '#07090e',
            fontWeight: 700,
            fontSize: '13px',
            padding: '10px 20px',
            borderRadius: '7px',
            border: 'none',
            cursor: status === 'loading' ? 'not-allowed' : 'pointer',
            boxShadow: '0 2px 10px rgba(247, 255, 20, 0.3)',
            transition: 'all 0.15s ease',
            whiteSpace: 'nowrap',
          }}
        >
          {status === 'loading' ? 'Deploying...' : '🚀 Deploy to Cloudflare Pages'}
        </button>
      </div>

      {message && (
        <div style={{
          padding: '10px 14px',
          borderRadius: '6px',
          fontSize: '13px',
          background: status === 'success' ? 'rgba(52, 211, 153, 0.1)' : status === 'error' ? 'rgba(244, 63, 94, 0.1)' : 'rgba(255, 255, 255, 0.05)',
          color: status === 'success' ? '#34d399' : status === 'error' ? '#f43f5e' : '#cbd5e1',
          border: `1px solid ${status === 'success' ? 'rgba(52, 211, 153, 0.3)' : status === 'error' ? 'rgba(244, 63, 94, 0.3)' : 'rgba(255, 255, 255, 0.1)'}`,
        }}>
          {message}
        </div>
      )}
    </div>
  )
}
