/**
 * Cross-platform postbuild script.
 * Copies the TinyMCE self-hosted bundle from node_modules into wwwroot
 * so it is served statically and does not require a CDN or API key.
 *
 * TinyMCE loads skins/plugins/themes dynamically at runtime via URL,
 * so they cannot be bundled by Vite — they must be present as static files.
 * https://www.tiny.cloud/docs/tinymce/latest/npm-projects/
 *
 * Runs on Windows (CI) and macOS/Linux (local) via Node.js built-in fs:
 * node scripts/copy-tinymce.mjs
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const src = path.resolve(__dirname, '../node_modules/tinymce');
const dest = path.resolve(__dirname, '../../wwwroot/spa/libs/tinymce');

fs.rmSync(dest, { recursive: true, force: true });
fs.mkdirSync(dest, { recursive: true });

const entries = ['tinymce.min.js', 'skins', 'plugins', 'themes', 'icons', 'models'];
for (const entry of entries) {
  fs.cpSync(path.join(src, entry), path.join(dest, entry), { recursive: true });
}

console.log(`TinyMCE assets copied to ${dest}`);
