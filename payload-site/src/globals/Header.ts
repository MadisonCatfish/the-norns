import type { GlobalConfig } from 'payload'

export const Header: GlobalConfig = {
  slug: 'header',
  label: 'Header & Navigation',
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
      type: 'row',
      fields: [
        {
          name: 'brandName',
          type: 'text',
          label: 'Brand Name',
          defaultValue: 'THE NORNS',
          required: true,
          admin: { width: '34%' },
        },
        {
          name: 'brandKatakana',
          type: 'text',
          label: 'Katakana Subtitle',
          defaultValue: 'ノルン',
          admin: { width: '33%' },
        },
        {
          name: 'logoUrl',
          type: 'text',
          label: 'Logo Image URL',
          defaultValue: '/assets/logo-64.png',
          admin: { width: '33%' },
        },
      ],
    },
    {
      name: 'links',
      type: 'array',
      label: 'Navigation Links (Desktop & Mobile)',
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
              label: 'Label',
              required: true,
              admin: { width: '30%' },
            },
            {
              name: 'url',
              type: 'text',
              label: 'URL or Anchor',
              required: true,
              admin: { width: '35%' },
            },
            {
              name: 'sectionId',
              type: 'text',
              label: 'Section ID (e.g. "overview")',
              admin: { width: '20%' },
            },
            {
              name: 'newTab',
              type: 'checkbox',
              label: 'New Tab',
              defaultValue: false,
              admin: { width: '15%' },
            },
          ],
        },
      ],
    },
    {
      type: 'row',
      fields: [
        {
          name: 'showCta',
          type: 'checkbox',
          label: 'Show CTA Download Button in Header',
          defaultValue: true,
          admin: { width: '30%' },
        },
        {
          name: 'ctaLabel',
          type: 'text',
          label: 'CTA Button Label (leave empty for auto "Download {version}")',
          admin: { width: '35%' },
        },
        {
          name: 'ctaUrl',
          type: 'text',
          label: 'CTA Button URL (leave empty for auto latest release)',
          admin: { width: '35%' },
        },
      ],
    },
  ],
}
