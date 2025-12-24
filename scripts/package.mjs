#!/usr/bin/env node

/**
 * Package script - Creates a ZIP file ready for upload to Backend Host.
 *
 * Usage:
 *   node scripts/package.mjs
 *
 * Output:
 *   dist/{pluginName}.{version}-{shortHash}.zip
 *
 * The script automatically enriches plugin.json with git metadata:
 *   - commitHash: Full git commit SHA
 *   - releaseNotes: Git commit message (last commit)
 *   - repositoryUrl: Git remote origin URL (converted to HTTPS if needed)
 *   - buildDate: ISO 8601 timestamp
 */

import { execSync } from 'child_process';
import { createWriteStream, readFileSync, mkdirSync, existsSync, readdirSync, statSync } from 'fs';
import { join, basename } from 'path';
import archiver from 'archiver';

const ROOT_DIR = process.cwd();
const PLUGIN_JSON_PATH = join(ROOT_DIR, 'plugin.json');
const PUBLISH_DIR = join(ROOT_DIR, 'src/MyPlugin/bin/Release/net9.0/publish');
const DIST_DIR = join(ROOT_DIR, 'dist');

/**
 * Get git metadata for the current repository
 */
function getGitMetadata() {
    const metadata = {
        commitHash: null,
        shortHash: null,
        releaseNotes: null,
        repositoryUrl: null,
        buildDate: new Date().toISOString()
    };

    try {
        // Get commit hash
        metadata.commitHash = execSync('git rev-parse HEAD', { encoding: 'utf-8' }).trim();
        metadata.shortHash = metadata.commitHash.substring(0, 7);
        console.log(`   Commit: ${metadata.shortHash}`);
    } catch (e) {
        console.warn('   Warning: Could not get git commit hash');
    }

    try {
        // Get commit message as release notes
        metadata.releaseNotes = execSync('git log -1 --pretty=%B', { encoding: 'utf-8' }).trim();
        const firstLine = metadata.releaseNotes.split('\n')[0];
        console.log(`   Release notes: ${firstLine.substring(0, 50)}${firstLine.length > 50 ? '...' : ''}`);
    } catch (e) {
        console.warn('   Warning: Could not get commit message');
    }

    try {
        // Get repository URL
        let repoUrl = execSync('git remote get-url origin', { encoding: 'utf-8' }).trim();

        // Convert SSH to HTTPS if needed
        if (repoUrl.startsWith('git@')) {
            // git@github.com:user/repo.git -> https://github.com/user/repo
            repoUrl = repoUrl
                .replace(/^git@([^:]+):/, 'https://$1/')
                .replace(/\.git$/, '');
        } else {
            // Remove .git suffix from HTTPS URLs
            repoUrl = repoUrl.replace(/\.git$/, '');
        }

        metadata.repositoryUrl = repoUrl;
        console.log(`   Repository: ${repoUrl}`);
    } catch (e) {
        console.warn('   Warning: Could not get repository URL');
    }

    return metadata;
}

async function main() {
    console.log('📦 Packaging plugin...\n');

    // Read plugin.json
    const pluginJson = JSON.parse(readFileSync(PLUGIN_JSON_PATH, 'utf-8'));
    const { name, version } = pluginJson;

    console.log(`Plugin: ${name} v${version}`);

    // Get git metadata
    console.log('\n📋 Collecting metadata...');
    const gitMetadata = getGitMetadata();

    // Enrich plugin.json with metadata
    const enrichedPluginJson = {
        ...pluginJson,
        commitHash: gitMetadata.commitHash,
        releaseNotes: gitMetadata.releaseNotes,
        repositoryUrl: gitMetadata.repositoryUrl,
        buildDate: gitMetadata.buildDate
        // buildUrl is added by CI/CD pipeline
    };

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

    // Create ZIP with short hash in filename
    const shortHash = gitMetadata.shortHash || 'local';
    const zipPath = join(DIST_DIR, `${name}.${version}-${shortHash}.zip`);
    console.log(`\n📁 Creating ${basename(zipPath)}...`);

    await createZip(PUBLISH_DIR, zipPath, enrichedPluginJson);

    console.log(`\n✅ Package created: ${zipPath}`);
    console.log(`\n📤 Upload to Backend Host:`);
    console.log(`   POST /api/admin/plugins/upload`);
    console.log(`   - name: ${name}`);
    console.log(`   - version: ${version}`);
    console.log(`   - file: ${basename(zipPath)}`);

    // Show metadata included
    console.log(`\n📋 Metadata included:`);
    if (gitMetadata.commitHash) console.log(`   - commitHash: ${gitMetadata.shortHash}`);
    if (gitMetadata.releaseNotes) console.log(`   - releaseNotes: (from commit message)`);
    if (gitMetadata.repositoryUrl) console.log(`   - repositoryUrl: ${gitMetadata.repositoryUrl}`);
    console.log(`   - buildDate: ${gitMetadata.buildDate}`);
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

        // Add enriched plugin.json at root
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
