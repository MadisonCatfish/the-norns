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
      setMessage(err.message || 'Network error triggering deployment.')
    }
  }

  return (
    <div
      style={{
        padding: '16px 20px',
        backgroundColor: '#0c1017',
        border: '1px solid #1f293d',
        borderRadius: '6px',
        marginBottom: '24px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        flexWrap: 'wrap',
        gap: '12px',
      }}
    >
      <div>
        <div style={{ fontWeight: 600, color: '#f3f4f6', fontSize: '14px', marginBottom: '4px' }}>
          Publish to Cloudflare Pages
        </div>
        <div style={{ color: '#9ca3af', fontSize: '13px' }}>
          Deploys the latest content changes from this CMS to the live site (<strong>the-norns.pages.dev</strong>).
        </div>
        {message && (
          <div
            style={{
              marginTop: '8px',
              fontSize: '13px',
              color: status === 'error' ? '#ef4444' : status === 'success' ? '#10b981' : '#38bdf8',
            }}
          >
            {message}
          </div>
        )}
      </div>

      <button
        type="button"
        onClick={handleDeploy}
        disabled={status === 'loading'}
        style={{
          backgroundColor: status === 'loading' ? '#374151' : '#2563eb',
          color: '#ffffff',
          border: 'none',
          padding: '8px 18px',
          borderRadius: '4px',
          fontWeight: 600,
          fontSize: '13px',
          cursor: status === 'loading' ? 'not-allowed' : 'pointer',
          transition: 'background-color 0.15s ease',
        }}
      >
        {status === 'loading' ? 'Triggering...' : 'Deploy to Cloudflare Pages'}
      </button>
    </div>
  )
}
