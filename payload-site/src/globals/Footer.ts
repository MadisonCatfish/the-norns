import type { GlobalConfig } from 'payload'

export const Footer: GlobalConfig = {
  slug: 'footer',
  label: 'Footer Navigation',
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
      name: 'tagline',
      type: 'textarea',
      label: 'Brand Tagline',
      defaultValue: 'Dedicated telemetry companion for EVE Online EDENCOM & Smartbomb ratting fleets. Passive, read-only, zero injection.',
    },
    {
      type: 'row',
      fields: [
        {
          name: 'githubUrl',
          type: 'text',
          label: 'GitHub URL',
          defaultValue: 'https://github.com/MadisonCatfish/the-norns',
          admin: { width: '50%' },
        },
        {
          name: 'releasesUrl',
          type: 'text',
          label: 'Releases URL',
          defaultValue: 'https://github.com/MadisonCatfish/the-norns/releases',
          admin: { width: '50%' },
        },
      ],
    },
    {
      name: 'columns',
      type: 'array',
      label: 'Navigation Columns',
      labels: {
        singular: 'Column',
        plural: 'Columns',
      },
      fields: [
        {
          name: 'title',
          type: 'text',
          label: 'Column Title',
          required: true,
        },
        {
          name: 'links',
          type: 'array',
          label: 'Links',
          labels: {
            singular: 'Link',
            plural: 'Links',
          },
          fields: [
            {
              type: 'row',
              fields: [
                {
                  name: 'label',
                  type: 'text',
                  label: 'Link Label',
                  required: true,
                  admin: { width: '40%' },
                },
                {
                  name: 'url',
                  type: 'text',
                  label: 'URL or Anchor',
                  required: true,
                  admin: { width: '40%' },
                },
                {
                  name: 'newTab',
                  type: 'checkbox',
                  label: 'Open in new tab',
                  defaultValue: false,
                  admin: { width: '20%' },
                },
              ],
            },
          ],
        },
      ],
    },
    {
      name: 'copyrightNotice',
      type: 'text',
      label: 'Copyright Notice',
      defaultValue: '© 2026 The Norns (ノルン). Independent open-source companion under the MIT License.',
    },
    {
      name: 'ccpDisclaimer',
      type: 'text',
      label: 'CCP Legal Disclaimer',
      defaultValue: 'EVE Online® & CCP® © 2014 CCP hf. Not affiliated with or endorsed by CCP hf.',
    },
  ],
}
