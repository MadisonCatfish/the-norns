import type { GlobalConfig } from 'payload'

export const SiteSettings: GlobalConfig = {
  slug: 'site-settings',
  label: 'Site Settings',
  admin: {
    group: 'Site & Navigation',
  },
  access: {
    read: () => true,
  },
  versions: {
    drafts: true,
    max: 20,
  },
  fields: [
    {
      name: 'heroBadge',
      type: 'text',
      label: 'Hero Status Pill Text',
      defaultValue: 'THE NORNS 1.5.0',
    },
    {
      name: 'heroTitle',
      type: 'text',
      label: 'Hero Title',
      defaultValue: 'Fleet HUD for <span class="accent-text">EDENCOM krabbing</span>.',
    },
    {
      name: 'heroSubtitle',
      type: 'textarea',
      label: 'Hero Subtitle',
      defaultValue: "Real-time Vorton telemetry and a warp-to-warp anomaly timer. Passive: it reads the game's logs, sends no input and does no memory injection.",
    },
    {
      type: 'row',
      fields: [
        {
          name: 'githubUrl',
          type: 'text',
          label: 'GitHub Repository URL',
          defaultValue: 'https://github.com/MadisonCatfish/the-norns',
          admin: { width: '50%' },
        },
        {
          name: 'discordUrl',
          type: 'text',
          label: 'Discord Community URL',
          defaultValue: 'https://discord.gg',
          admin: { width: '50%' },
        },
      ],
    },
    {
      name: 'copyrightNotice',
      type: 'text',
      label: 'Footer Copyright Notice (Legacy)',
      admin: {
        hidden: true,
      },
    },
  ],
}
