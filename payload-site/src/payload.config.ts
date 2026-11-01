import { sqliteAdapter } from '@payloadcms/db-sqlite'
import { lexicalEditor } from '@payloadcms/richtext-lexical'
import path from 'path'
import { buildConfig } from 'payload'
import { fileURLToPath } from 'url'

import { Users } from './collections/Users'
import { Media } from './collections/Media'
import { Releases } from './collections/Releases'
import { FAQ } from './collections/FAQ'
import { Compliance } from './collections/Compliance'
import { SiteSettings } from './globals/SiteSettings'

const filename = fileURLToPath(import.meta.url)
const dirname = path.dirname(filename)
const rootDir = path.resolve(dirname, '..')

export default buildConfig({
  admin: {
    user: Users.slug,
    importMap: {
      baseDir: path.resolve(dirname),
    },
    components: {
      graphics: {
        Logo: './components/admin/AdminLogo#AdminLogo',
        Icon: './components/admin/AdminIcon#AdminIcon',
      },
    },
    meta: {
      titleSuffix: '· The Norns Admin',
      icons: [{ url: '/assets/favicon.ico' }],
    },
  },
  collections: [Users, Media, Releases, FAQ, Compliance],
  globals: [SiteSettings],
  editor: lexicalEditor(),
  secret: process.env.PAYLOAD_SECRET || 'the-norns-telemetry-payload-secret-key-super-secure-2026',
  typescript: {
    outputFile: path.resolve(dirname, 'payload-types.ts'),
  },
  db: sqliteAdapter({
    client: {
      url: process.env.DATABASE_URI || `file:${path.resolve(rootDir, 'payload.db')}`,
    },
  }),
})
