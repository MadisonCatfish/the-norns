import type { CollectionConfig } from 'payload'

export const Roadmap: CollectionConfig = {
  slug: 'roadmap',
  labels: {
    singular: 'Roadmap Item',
    plural: 'Roadmap Items',
  },
  admin: {
    group: 'Site & Navigation',
    useAsTitle: 'title',
    defaultColumns: ['order', 'title', 'itemStatus', 'statusLabel', '_status'],
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
      name: 'order',
      type: 'number',
      required: true,
      label: 'Display Order',
      admin: {
        width: '25%',
      },
    },
    {
      name: 'title',
      type: 'text',
      required: true,
      label: 'Item Title (e.g. 0 · Update check on start)',
      admin: {
        width: '75%',
      },
    },
    {
      type: 'row',
      fields: [
        {
          name: 'itemStatus',
          type: 'select',
          required: true,
          label: 'Status',
          defaultValue: 'todo',
          options: [
            { label: 'Completed (Done)', value: 'done' },
            { label: 'In Progress (WIP)', value: 'wip' },
            { label: 'Planned (To Do)', value: 'todo' },
            { label: 'Needs Research (Need)', value: 'need' },
            { label: 'Cancelled', value: 'cancelled' },
          ],
          admin: {
            width: '50%',
          },
        },
        {
          name: 'statusLabel',
          type: 'text',
          label: 'Custom Status Badge (e.g. "1.4.0" or "WIP")',
          admin: {
            width: '50%',
          },
        },
      ],
    },
    {
      name: 'description',
      type: 'textarea',
      required: true,
      label: 'Description',
    },
    {
      name: 'checklist',
      type: 'array',
      label: 'Checklist / Sub-tasks',
      labels: {
        singular: 'Task',
        plural: 'Tasks',
      },
      fields: [
        {
          type: 'row',
          fields: [
            {
              name: 'text',
              type: 'text',
              label: 'Task Text',
              required: true,
              admin: {
                width: '60%',
              },
            },
            {
              name: 'note',
              type: 'text',
              label: 'Note / Date (Optional)',
              admin: {
                width: '30%',
              },
            },
            {
              name: 'completed',
              type: 'checkbox',
              label: 'Done',
              defaultValue: false,
              admin: {
                width: '10%',
              },
            },
          ],
        },
      ],
    },
  ],
}
