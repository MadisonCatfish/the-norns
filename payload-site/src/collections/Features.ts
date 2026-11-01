import type { CollectionConfig } from 'payload'

export const Features: CollectionConfig = {
  slug: 'features',
  labels: {
    singular: 'Feature Card',
    plural: 'Feature Cards (Bento)',
  },
  admin: {
    group: 'Landing Page Blocks',
    useAsTitle: 'title',
    defaultColumns: ['order', 'pill', 'title', '_status'],
  },
  access: {
    read: () => true,
  },
  versions: {
    drafts: true,
    maxPerDoc: 20,
  },
  fields: [
    {
      name: 'pill',
      type: 'text',
      required: true,
      label: 'Badge / Pill (e.g. 01 · COMPETITION & DPS RACE)',
    },
    {
      name: 'title',
      type: 'text',
      required: true,
      label: 'Card Title',
    },
    {
      name: 'description',
      type: 'textarea',
      required: true,
      label: 'Description Text',
    },
    {
      name: 'colSpan',
      type: 'select',
      defaultValue: 'col-6',
      options: [
        { label: 'Standard (Half Width, col-6)', value: 'col-6' },
        { label: 'Wide (7/12 Width, col-7)', value: 'col-7' },
        { label: 'Narrow (5/12 Width, col-5)', value: 'col-5' },
      ],
      label: 'Card Grid Width',
    },
    {
      name: 'hasLiveBadge',
      type: 'checkbox',
      defaultValue: false,
      label: 'Show "LIVE ANIMATION" pulsing badge',
    },
    {
      name: 'mediaImage',
      type: 'upload',
      relationTo: 'media',
      label: 'Uploaded Image (from Media library)',
    },
    {
      name: 'imageUrl',
      type: 'text',
      label: 'Direct Image URL (e.g. /assets/dps_dread_hot.png)',
    },
    {
      name: 'isVideo',
      type: 'checkbox',
      defaultValue: false,
      label: 'Card contains Video instead of static image',
    },
    {
      name: 'videoPoster',
      type: 'text',
      label: 'Video Poster URL (e.g. /assets/the-norns-competition-poster.jpg)',
    },
    {
      name: 'videoUrl',
      type: 'text',
      label: 'Video File URL (e.g. /assets/the-norns-competition.mp4)',
    },
    {
      name: 'order',
      type: 'number',
      defaultValue: 1,
      label: 'Display Order',
    },
  ],
}
