#!/usr/bin/env node

/**
 * Package script - Creates a ZIP file ready for upload to Backend Host.
 *
 * Usage:
 *   node scripts/package.mjs
 *
 * Output:
 *   dist/{pluginName}.{version}.zip
 */

import { execSync } from 'child_process';
import { createWriteStream, readFileSync, mkdirSync, existsSync, readdirSync, statSync } from 'fs';
import { join, basename } from 'path';
import archiver from 'archiver';

const ROOT_DIR = process.cwd();
const PLUGIN_JSON_PATH = join(ROOT_DIR, 'plugin.json');
const PUBLISH_DIR = join(ROOT_DIR, 'src/MyPlugin/bin/Release/net9.0/publish');
const DIST_DIR = join(ROOT_DIR, 'dist');

async function main() {
    console.log('📦 Packaging plugin...\n');

    // Read plugin.json
    const pluginJson = JSON.parse(readFileSync(PLUGIN_JSON_PATH, 'utf-8'));
    const { name, version } = pluginJson;

    console.log(`Plugin: ${name} v${version}`);

    // Build and publish
    console.log('\n🔨 Building...');
    execSync('dotnet publish src/MyPlugin/MyPlugin.csproj -c Release -o src/MyPlugin/bin/Release/net9.0/publish', {
        cwd: ROOT_DIR,
        stdio: 'inherit'
    });

    // Create dist directory
    if (!existsSync(DIST_DIR)) {
        mkdirSync(DIST_DIR, { recursive: true });
    }

    // Create ZIP
    const zipPath = join(DIST_DIR, `${name}.${version}.zip`);
    console.log(`\n📁 Creating ${basename(zipPath)}...`);

    await createZip(PUBLISH_DIR, zipPath, pluginJson);

    console.log(`\n✅ Package created: ${zipPath}`);
    console.log(`\n📤 Upload to Backend Host:`);
    console.log(`   POST /api/admin/plugins/upload`);
    console.log(`   - name: ${name}`);
    console.log(`   - version: ${version}`);
    console.log(`   - file: ${basename(zipPath)}`);
}

async function createZip(sourceDir, zipPath, pluginJson) {
    return new Promise((resolve, reject) => {
        const output = createWriteStream(zipPath);
        const archive = archiver('zip', { zlib: { level: 9 } });

        output.on('close', () => {
            console.log(`   Size: ${(archive.pointer() / 1024).toFixed(2)} KB`);
            resolve();
        });

        archive.on('error', reject);
        archive.pipe(output);

        // Add all published files
        addDirectoryToArchive(archive, sourceDir, '');

        // Add plugin.json at root
        archive.append(JSON.stringify(pluginJson, null, 2), { name: 'plugin.json' });

        archive.finalize();
    });
}

function addDirectoryToArchive(archive, dir, prefix) {
    const files = readdirSync(dir);

    for (const file of files) {
        const filePath = join(dir, file);
        const stat = statSync(filePath);
        const archivePath = prefix ? `${prefix}/${file}` : file;

        if (stat.isDirectory()) {
            addDirectoryToArchive(archive, filePath, archivePath);
        } else {
            archive.file(filePath, { name: archivePath });
        }
    }
}

main().catch(err => {
    console.error('❌ Error:', err.message);
    process.exit(1);
});
