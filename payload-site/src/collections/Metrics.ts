import type { CollectionConfig } from 'payload'

export const Metrics: CollectionConfig = {
  slug: 'metrics',
  labels: {
    singular: 'Hero Metric',
    plural: 'Hero Metrics',
  },
  admin: {
    useAsTitle: 'label',
    defaultColumns: ['order', 'indexTag', 'label', 'value', '_status'],
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
      label: 'Index Badge (e.g. [ 01 ])',
    },
    {
      name: 'label',
      type: 'text',
      required: true,
      label: 'Metric Label (e.g. INPUT INTEGRITY)',
    },
    {
      name: 'value',
      type: 'text',
      required: true,
      label: 'Main Value (e.g. 0 inputs, ~3.5%)',
    },
    {
      name: 'description',
      type: 'text',
      required: true,
      label: 'Short Description',
    },
    {
      name: 'order',
      type: 'number',
      defaultValue: 1,
      label: 'Display Order',
    },
  ],
}
