import type { CollectionConfig } from 'payload'

export const Releases: CollectionConfig = {
  slug: 'releases',
  labels: {
    singular: 'Release',
    plural: 'Releases',
  },
  admin: {
    group: 'Releases & Media',
    useAsTitle: 'version',
    defaultColumns: ['version', 'isLatest', '_status', 'publishedAt'],
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
      name: 'version',
      type: 'text',
      required: true,
      label: 'Version Tag (e.g. v1.6.0)',
    },
    {
      name: 'isLatest',
      type: 'checkbox',
      defaultValue: false,
      label: 'Set as Active Download on Landing Page',
    },
    {
      name: 'publishedAt',
      type: 'date',
      label: 'Publication Date',
    },
    {
      name: 'installerUrl',
      type: 'text',
      label: 'Windows Installer (.exe) Direct Link',
      defaultValue: 'https://github.com/the-norns/the-norns/releases/latest/download/TheNorns-Setup.exe',
    },
    {
      name: 'portableUrl',
      type: 'text',
      label: 'Portable (.zip) Direct Link',
      defaultValue: 'https://github.com/the-norns/the-norns/releases/latest/download/TheNorns-Portable.zip',
    },
    {
      name: 'githubReleaseUrl',
      type: 'text',
      label: 'GitHub Release URL',
      defaultValue: 'https://github.com/the-norns/the-norns/releases',
    },
    {
      name: 'changelog',
      type: 'textarea',
      label: 'Changelog / Release Highlights',
    },
  ],
}
