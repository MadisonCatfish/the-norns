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
      defaultValue: 'THE NORNS 1.5.1',
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
    // Hero CTA Buttons Management
    {
      type: 'collapsible',
      label: 'Hero CTA Buttons (Call to Action)',
      admin: {
        initCollapsed: false,
      },
      fields: [
        // Button 1: Primary (Installer)
        {
          type: 'row',
          fields: [
            {
              name: 'heroPrimaryShow',
              type: 'checkbox',
              label: 'Show Button 1 (Standalone / Installer)',
              defaultValue: true,
              admin: { width: '25%' },
            },
            {
              name: 'heroPrimaryLabel',
              type: 'text',
              label: 'Button 1 Text',
              defaultValue: 'Download Standalone (.exe)',
              admin: { width: '35%' },
            },
            {
              name: 'heroPrimaryUrl',
              type: 'text',
              label: 'Button 1 Custom URL (leave empty for auto latest .exe release)',
              admin: { width: '40%' },
            },
          ],
        },
        // Button 2: Secondary (Portable)
        {
          type: 'row',
          fields: [
            {
              name: 'heroSecondaryShow',
              type: 'checkbox',
              label: 'Show Button 2 (Portable)',
              defaultValue: true,
              admin: { width: '25%' },
            },
            {
              name: 'heroSecondaryLabel',
              type: 'text',
              label: 'Button 2 Text',
              defaultValue: 'Portable (.zip)',
              admin: { width: '35%' },
            },
            {
              name: 'heroSecondaryUrl',
              type: 'text',
              label: 'Button 2 Custom URL (leave empty for auto latest .zip release)',
              admin: { width: '40%' },
            },
          ],
        },
        // Button 3: Tertiary (Source / Audit)
        {
          type: 'row',
          fields: [
            {
              name: 'heroTertiaryShow',
              type: 'checkbox',
              label: 'Show Button 3 (Audit / Source)',
              defaultValue: true,
              admin: { width: '25%' },
            },
            {
              name: 'heroTertiaryLabel',
              type: 'text',
              label: 'Button 3 Text',
              defaultValue: 'Audit Source & Build →',
              admin: { width: '35%' },
            },
            {
              name: 'heroTertiaryUrl',
              type: 'text',
              label: 'Button 3 URL',
              defaultValue: '/source',
              admin: { width: '40%' },
            },
          ],
        },
      ],
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
    {
      name: 'deployAction',
      type: 'ui',
      admin: {
        components: {
          Field: './components/admin/DeployButton#DeployButton',
        },
      },
    },
    {
      name: 'cloudflareDeployHook',
      type: 'text',
      label: 'Cloudflare Pages Deploy Hook URL',
      admin: {
        description: 'URL from Cloudflare Pages (Project Settings -> Builds & deployments -> Deploy hooks). Payload sends a POST request here to rebuild the static site.',
      },
    },
    {
      name: 'autoDeployOnSave',
      type: 'checkbox',
      label: 'Auto-Deploy to Cloudflare Pages on Content Publish',
      defaultValue: false,
      admin: {
        description: 'When enabled, publishing changes in any section will automatically ping Cloudflare to rebuild the site.',
      },
    },
  ],
}
