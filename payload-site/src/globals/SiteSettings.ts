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
      label: 'Hero Badge',
      defaultValue: 'EVE HAVOC / EQUINOX COMPATIBLE · ZERO MEMORY INJECTION',
    },
    {
      name: 'heroTitle',
      type: 'text',
      label: 'Hero Title',
      defaultValue: 'Dedicated Telemetry Companion for EVE Online Fleets',
    },
    {
      name: 'heroSubtitle',
      type: 'textarea',
      label: 'Hero Subtitle',
      defaultValue: 'Engineered specifically for Stormbringer, Thunderchild, and Smartbombing superkrabbing fleets. Passive log tailing, multi-client DPS telemetry, and audible dread/titan early warnings.',
    },
    {
      name: 'githubUrl',
      type: 'text',
      label: 'GitHub Repository URL',
      defaultValue: 'https://github.com/the-norns/the-norns',
    },
    {
      name: 'discordUrl',
      type: 'text',
      label: 'Discord Community URL',
      defaultValue: 'https://discord.gg',
    },
    {
      name: 'copyrightNotice',
      type: 'text',
      label: 'Footer Copyright Notice',
      defaultValue: '© 2026 The Norns (ノルン). Independent open-source companion under the MIT License.',
    },
  ],
}
