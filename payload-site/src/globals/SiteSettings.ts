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
      type: 'tabs',
      tabs: [
        {
          label: 'Hero & Identity',
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
          ],
        },
        {
          label: 'Section Headings',
          fields: [
            // Capabilities / Doctrine section
            {
              name: 'featuresTag',
              type: 'text',
              label: 'Doctrine / Capabilities Section Tag',
              defaultValue: '[ FLEET DOCTRINE TELEMETRY ]',
            },
            {
              name: 'featuresTitle',
              type: 'text',
              label: 'Doctrine / Capabilities Section Title',
              defaultValue: 'Engineered for nullsec krabbing & Havens.',
            },
            {
              name: 'featuresDesc',
              type: 'textarea',
              label: 'Doctrine / Capabilities Section Description',
              defaultValue: 'Replace guesswork with real-time mathematical telemetry. Track individual gunboat efficiency, instant capital alarms, and wallet bounties in the heat of combat.',
            },
            // Compliance section
            {
              name: 'complianceTag',
              type: 'text',
              label: 'Compliance / Pipeline Section Tag',
              defaultValue: '[ CCP POLICY NOTE ]',
            },
            {
              name: 'complianceTitle',
              type: 'text',
              label: 'Compliance / Pipeline Section Title',
              defaultValue: 'Read-only data pipeline.',
            },
            {
              name: 'complianceDesc',
              type: 'textarea',
              label: 'Compliance / Pipeline Section Description',
              defaultValue: 'How The Norns gets real-time telemetry: it reads the logs the client already writes. We are not affiliated with CCP and cannot promise how CCP treats third-party tools.',
            },
            // FAQ section
            {
              name: 'faqTag',
              type: 'text',
              label: 'FAQ Section Tag',
              defaultValue: '[ FAQ ]',
            },
            {
              name: 'faqTitle',
              type: 'text',
              label: 'FAQ Section Title',
              defaultValue: 'Questions about The Norns.',
            },
            // Bottom CTA Card
            {
              name: 'ctaPill',
              type: 'text',
              label: 'Download Banner Pill Badge',
              defaultValue: 'READY FOR DEPLOYMENT',
            },
            {
              name: 'ctaTitle',
              type: 'text',
              label: 'Download Banner Title',
              defaultValue: 'Upgrade your nullsec fleet telemetry today.',
            },
            {
              name: 'ctaDesc',
              type: 'textarea',
              label: 'Download Banner Description',
              defaultValue: 'Free, open-source, and offline by design, apart from one optional update check. Download the standalone Windows installer or extract the portable archive.',
            },
          ],
        },
        {
          label: 'Roadmap Page',
          fields: [
            {
              name: 'roadmapTitle',
              type: 'text',
              label: 'Roadmap Page Title',
              defaultValue: 'The Norns · roadmap',
            },
            {
              name: 'roadmapSubtitle',
              type: 'text',
              label: 'Roadmap Page Subtitle',
              defaultValue: 'Features after 1.1.0.',
            },
            {
              name: 'roadmapFooter',
              type: 'text',
              label: 'Roadmap Page Footer Note',
              defaultValue: 'Updated 31.10.2026, 1.5.0 built · Managed via Payload CMS',
            },
          ],
        },
        {
          label: 'Deploy & Cloudflare',
          fields: [
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
        },
      ],
    },
  ],
}
