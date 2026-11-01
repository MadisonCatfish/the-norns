import type { CollectionConfig } from 'payload'

export const Compliance: CollectionConfig = {
  slug: 'compliance',
  labels: {
    singular: 'Compliance Card',
    plural: 'Compliance Cards',
  },
  admin: {
    group: 'Landing Page Blocks',
    useAsTitle: 'title',
    defaultColumns: ['order', 'indexTag', 'title', '_status'],
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
      name: 'indexTag',
      type: 'text',
      required: true,
      label: 'Badge / Index Tag (e.g. [ 01 ] LOCAL GAMELOGS)',
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
      name: 'order',
      type: 'number',
      defaultValue: 1,
      label: 'Display Order',
    },
  ],
}
