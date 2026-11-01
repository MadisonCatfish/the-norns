import type { GlobalConfig } from 'payload'

export const SourcePage: GlobalConfig = {
  slug: 'source-page',
  label: 'Source & Build Page',
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
    // 1. Hero
    {
      name: 'heroBadge',
      type: 'text',
      label: 'Hero Badge',
      defaultValue: 'OPEN SOURCE · MIT LICENSE · STRICT ZERO-INJECTION',
    },
    {
      name: 'heroTitle',
      type: 'text',
      label: 'Hero Title',
      defaultValue: 'Audit the source. Fly safe in nullsec.',
    },
    {
      name: 'heroSubtitle',
      type: 'textarea',
      label: 'Hero Subtitle',
      defaultValue: 'Full architectural transparency for fleet commanders and capsuleers. Verify that The Norns runs strictly read-only, never touches exefile.exe memory, and compiles into byte-for-byte verifiable binaries directly from GitHub.',
    },
    {
      name: 'githubRepoUrl',
      type: 'text',
      label: 'GitHub Repository URL',
      defaultValue: 'https://github.com/MadisonCatfish/the-norns',
    },

    // 2. Security Section
    {
      name: 'securityTag',
      type: 'text',
      label: 'Security Section Tag',
      defaultValue: '[ 01 / CCP FAIR PLAY COMPLIANCE ]',
    },
    {
      name: 'securityTitle',
      type: 'text',
      label: 'Security Section Title',
      defaultValue: 'Architected to stay out of the game client.',
    },
    {
      name: 'securityDesc',
      type: 'textarea',
      label: 'Security Section Description',
      defaultValue: 'Every architectural boundary in The Norns is designed to ensure zero interference with the game client, zero network leakage, and absolute peace of mind for multi-billion ISK setups.',
    },
    {
      name: 'securityCards',
      type: 'array',
      label: 'Security & Fair Play Cards',
      labels: {
        singular: 'Card',
        plural: 'Cards',
      },
      fields: [
        {
          type: 'row',
          fields: [
            {
              name: 'pill',
              type: 'text',
              label: 'Badge Pill (e.g. [ I/O ISOLATION ])',
              required: true,
              admin: { width: '50%' },
            },
            {
              name: 'status',
              type: 'text',
              label: 'Status Check (e.g. READ-ONLY)',
              required: true,
              admin: { width: '50%' },
            },
          ],
        },
        {
          name: 'title',
          type: 'text',
          label: 'Card Heading',
          required: true,
        },
        {
          name: 'description',
          type: 'textarea',
          label: 'Card Description',
          required: true,
        },
        {
          name: 'codeCallout',
          type: 'text',
          label: 'Code Callout / Verification Proof',
        },
      ],
    },

    // 3. Codebase Architecture
    {
      name: 'archTag',
      type: 'text',
      label: 'Architecture Tag',
      defaultValue: '[ 02 / CODEBASE ARCHITECTURE ]',
    },
    {
      name: 'archTitle',
      type: 'text',
      label: 'Architecture Title',
      defaultValue: 'Clean, zero-dependency C# codebase.',
    },
    {
      name: 'archDesc',
      type: 'textarea',
      label: 'Architecture Description',
      defaultValue: 'No heavy web wrappers, no Electron bloat, no third-party package dependencies. Pure C# targeting native Windows graphics and non-blocking file streaming.',
    },
    {
      type: 'row',
      fields: [
        {
          name: 'archFrameTitle',
          type: 'text',
          label: 'Window Bar Title',
          defaultValue: 'the-norns / native / source-tree',
          admin: { width: '50%' },
        },
        {
          name: 'archBadge',
          type: 'text',
          label: 'Frame Badge',
          defaultValue: 'C# 7.3 · .NET 4.8',
          admin: { width: '50%' },
        },
      ],
    },
    {
      name: 'modules',
      type: 'array',
      label: 'Source Modules Table',
      labels: {
        singular: 'Module',
        plural: 'Modules',
      },
      fields: [
        {
          type: 'row',
          fields: [
            {
              name: 'moduleName',
              type: 'text',
              label: 'Module Name',
              required: true,
              admin: { width: '35%' },
            },
            {
              name: 'sourceFile',
              type: 'text',
              label: 'Source File Path',
              required: true,
              admin: { width: '35%' },
            },
          ],
        },
        {
          name: 'functionality',
          type: 'textarea',
          label: 'Core Functionality Description',
          required: true,
        },
      ],
    },

    // 4. Reproducible Builds
    {
      name: 'buildTag',
      type: 'text',
      label: 'Build Section Tag',
      defaultValue: '[ 03 / REPRODUCIBLE BUILDS ]',
    },
    {
      name: 'buildTitle',
      type: 'text',
      label: 'Build Section Title',
      defaultValue: 'Build your own binary.',
    },
    {
      name: 'buildDesc',
      type: 'textarea',
      label: 'Build Section Description',
      defaultValue: 'The Norns compiles using native Windows developer tools (.NET Framework 4.8 via Roslyn). No Node.js, Python, or external package managers required for compilation.',
    },
    {
      type: 'row',
      fields: [
        {
          name: 'buildTerminalTitle',
          type: 'text',
          label: 'Terminal Title',
          defaultValue: 'Windows PowerShell · C:\\The Norns>',
          admin: { width: '50%' },
        },
        {
          name: 'buildBadge',
          type: 'text',
          label: 'Terminal Badge',
          defaultValue: 'POWERSHELL 5.1+',
          admin: { width: '50%' },
        },
      ],
    },
    {
      name: 'buildSteps',
      type: 'array',
      label: 'Build Terminal Steps',
      labels: {
        singular: 'Step',
        plural: 'Steps',
      },
      fields: [
        {
          name: 'comment',
          type: 'text',
          label: 'Comment / Step Header (e.g. "# 1. Clone the repository")',
          required: true,
        },
        {
          name: 'command',
          type: 'textarea',
          label: 'Terminal Command',
          required: true,
        },
      ],
    },

    // 5. MIT License
    {
      name: 'licenseTag',
      type: 'text',
      label: 'License Section Tag',
      defaultValue: '[ 04 / OPEN SOURCE LICENSE ]',
    },
    {
      name: 'licenseTitle',
      type: 'text',
      label: 'License Section Title',
      defaultValue: 'Perpetual open-source freedom.',
    },
    {
      name: 'licenseDesc',
      type: 'textarea',
      label: 'License Section Description',
      defaultValue: 'Released under the permissive MIT License. You are free to inspect, modify, fork, and distribute.',
    },
    {
      type: 'row',
      fields: [
        {
          name: 'licenseName',
          type: 'text',
          label: 'License Name',
          defaultValue: 'MIT License',
          admin: { width: '50%' },
        },
        {
          name: 'licenseStamp',
          type: 'text',
          label: 'Verification Stamp',
          defaultValue: '✓ VERIFIED PERMISSIVE',
          admin: { width: '50%' },
        },
      ],
    },
    {
      name: 'licenseText',
      type: 'textarea',
      label: 'Full License Legal Text',
      defaultValue: `Copyright (c) 2026 The Norns contributors

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.`,
    },
  ],
}
