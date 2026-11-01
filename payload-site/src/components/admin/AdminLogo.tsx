import React from 'react'

export const AdminLogo: React.FC = () => {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
      <img
        src="/assets/logo-64.png"
        alt="The Norns Logo"
        width={32}
        height={32}
        style={{ borderRadius: '6px' }}
      />
      <div style={{ display: 'flex', flexDirection: 'column' }}>
        <span style={{ fontWeight: 800, fontSize: '15px', letterSpacing: '1px' }}>
          THE NORNS
        </span>
        <span style={{ fontSize: '10px', color: '#f7ff14', letterSpacing: '2px', fontWeight: 600 }}>
          ノルン · HUD ADMIN
        </span>
      </div>
    </div>
  )
}
