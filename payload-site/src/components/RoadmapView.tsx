'use client'

import React, { useState, useMemo } from 'react'

export interface RoadmapChecklistItem {
  id?: string | null
  text: string
  note?: string | null
  completed?: boolean | null
}

export interface RoadmapItem {
  id: string | number
  order: number
  title: string
  itemStatus?: 'done' | 'wip' | 'todo' | 'need' | 'cancelled' | null
  status?: 'done' | 'wip' | 'todo' | 'need' | 'cancelled' | null
  statusLabel?: string | null
  description: string
  checklist?: RoadmapChecklistItem[] | null
}

interface RoadmapViewProps {
  items: RoadmapItem[]
}

export function RoadmapView({ items }: RoadmapViewProps) {
  const [activeFilter, setActiveFilter] = useState<'all' | 'done' | 'wip' | 'todo' | 'cancelled'>('all')

  const getItemStatus = (i: RoadmapItem) => i.itemStatus || i.status || 'todo'

  // Calculate overall steps completed vs total steps
  const { totalSteps, completedSteps, progressPct } = useMemo(() => {
    let total = 0
    let completed = 0

    items.forEach((item) => {
      const st = getItemStatus(item)
      if (item.checklist && item.checklist.length > 0) {
        item.checklist.forEach((step) => {
          total++
          if (step.completed) completed++
        })
      } else {
        total++
        if (st === 'done') completed++
      }
    })

    const pct = total > 0 ? Math.round((completed / total) * 100) : 0
    return { totalSteps: total, completedSteps: completed, progressPct: pct }
  }, [items])

  // Filter items
  const filteredItems = useMemo(() => {
    if (activeFilter === 'all') return items
    if (activeFilter === 'done') return items.filter((i) => getItemStatus(i) === 'done')
    if (activeFilter === 'wip') return items.filter((i) => getItemStatus(i) === 'wip')
    if (activeFilter === 'todo') return items.filter((i) => getItemStatus(i) === 'todo' || getItemStatus(i) === 'need')
    if (activeFilter === 'cancelled') return items.filter((i) => getItemStatus(i) === 'cancelled')
    return items
  }, [items, activeFilter])

  // Count items per category for filter buttons
  const counts = useMemo(() => {
    return {
      all: items.length,
      done: items.filter((i) => getItemStatus(i) === 'done').length,
      wip: items.filter((i) => getItemStatus(i) === 'wip').length,
      todo: items.filter((i) => getItemStatus(i) === 'todo' || getItemStatus(i) === 'need').length,
      cancelled: items.filter((i) => getItemStatus(i) === 'cancelled').length,
    }
  }, [items])

  return (
    <main className="roadmap-main">
      <header className="roadmap-header">
        <h1>The Norns · roadmap</h1>
        <p>Features after 1.1.0.</p>
        
        {/* Dynamic Progress Bar */}
        <div className="roadmap-progress-bar" aria-label="Roadmap progress">
          <i style={{ width: `${progressPct}%` }} />
        </div>
        <div className="roadmap-count">
          {completedSteps} / {totalSteps} steps ({progressPct}%)
        </div>

        {/* Filter Chips */}
        <div className="roadmap-filters">
          <button
            type="button"
            className={`roadmap-filter-btn ${activeFilter === 'all' ? 'active' : ''}`}
            onClick={() => setActiveFilter('all')}
          >
            All ({counts.all})
          </button>
          <button
            type="button"
            className={`roadmap-filter-btn ${activeFilter === 'done' ? 'active' : ''}`}
            onClick={() => setActiveFilter('done')}
          >
            Completed ({counts.done})
          </button>
          <button
            type="button"
            className={`roadmap-filter-btn ${activeFilter === 'wip' ? 'active' : ''}`}
            onClick={() => setActiveFilter('wip')}
          >
            In Progress ({counts.wip})
          </button>
          <button
            type="button"
            className={`roadmap-filter-btn ${activeFilter === 'todo' ? 'active' : ''}`}
            onClick={() => setActiveFilter('todo')}
          >
            Planned ({counts.todo})
          </button>
          {counts.cancelled > 0 && (
            <button
              type="button"
              className={`roadmap-filter-btn ${activeFilter === 'cancelled' ? 'active' : ''}`}
              onClick={() => setActiveFilter('cancelled')}
            >
              Cancelled ({counts.cancelled})
            </button>
          )}
        </div>
      </header>

      {/* Roadmap Items List */}
      <div style={{ display: 'grid', gap: '14px' }}>
        {filteredItems.map((item) => {
          const st = getItemStatus(item)
          const chipClass = `chip ${st}`
          const chipLabel = item.statusLabel || st.toUpperCase()

          return (
            <article key={item.id} className="roadmap-card">
              <div className="roadmap-card-head">
                <h2>{item.title}</h2>
                <span className={chipClass}>{chipLabel}</span>
              </div>
              <p className="roadmap-desc">{item.description}</p>
              {item.checklist && item.checklist.length > 0 && (
                <ul className="roadmap-checklist">
                  {item.checklist.map((step, idx) => (
                    <li key={step.id || idx} className={`roadmap-task-item ${step.completed ? 'ok' : ''}`}>
                      <b>{step.completed ? '✓' : '·'}</b>
                      <span className="s">
                        {step.text}
                        {step.note && <span className="roadmap-task-note">{step.note}</span>}
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </article>
          )
        })}
      </div>

      <footer style={{ color: 'var(--fg-muted, #9aa0a6)', fontSize: '13px', marginTop: '12px', textAlign: 'center' }}>
        Updated 06.11.2026, 1.5.3 built · Managed via Payload CMS
      </footer>
    </main>
  )
}
