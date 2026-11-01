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
      name: 'heroTitlePrefix',
      type: 'text',
      label: 'Hero Title Prefix (Static)',
      defaultValue: 'Fleet HUD for',
      admin: {
        description: 'First part of the hero title that remains static (e.g. "Fleet HUD for")',
      },
    },
    {
      name: 'heroTitlePhrases',
      type: 'array',
      label: 'Hero Title Rotating Phrases (Changes on scroll with blur/fade)',
      labels: {
        singular: 'Phrase',
        plural: 'Phrases',
      },
      minRows: 1,
      maxRows: 6,
      admin: {
        description: 'Dynamic phrases that smoothly morph and blur when the user scrolls down',
      },
      fields: [
        {
          name: 'text',
          type: 'text',
          label: 'Phrase Text',
          required: true,
        },
      ],
      defaultValue: [
        { text: 'EDENCOM krabbing.' },
        { text: 'Stormbringer fleets.' },
        { text: 'Superkrabbing DPS telemetry.' },
      ],
    },
    {
      name: 'heroTitle',
      type: 'text',
      label: 'Hero Title (Legacy Fallback)',
      defaultValue: 'Fleet HUD for <span class="accent-text">EDENCOM krabbing</span>.',
      admin: {
        description: 'Used if dynamic phrases are empty',
      },
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
