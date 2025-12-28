#!/usr/bin/env node

/**
 * Sanitize plugin.json - Remove sensitive values from defaultSettings
 *
 * This script is used by CI/CD pipelines to ensure no credentials are
 * included in the packaged plugin ZIP.
 *
 * Strategy: Replace ALL values in defaultSettings with empty strings.
 * This is 100% safe - no risk of leaking credentials regardless of key names.
 *
 * Usage:
 *   node scripts/sanitize-plugin-json.mjs
 *
 * Input:  plugin.json or plugin.local.json (with real values)
 * Output: plugin.json (with sanitized defaultSettings)
 */

import { readFileSync, writeFileSync, existsSync } from 'fs';
import { join } from 'path';

const ROOT_DIR = process.cwd();
const PLUGIN_JSON_PATH = join(ROOT_DIR, 'plugin.json');
const PLUGIN_LOCAL_PATH = join(ROOT_DIR, 'plugin.local.json');

function main() {
    console.log('🧹 Sanitizing plugin.json...\n');

    // Use plugin.local.json if exists, otherwise plugin.json
    const sourceFile = existsSync(PLUGIN_LOCAL_PATH) ? PLUGIN_LOCAL_PATH : PLUGIN_JSON_PATH;
    console.log(`   Source: ${sourceFile}`);

    // Read plugin.json
    const pluginJson = JSON.parse(readFileSync(sourceFile, 'utf-8'));

    // Sanitize defaultSettings - replace ALL values with empty strings
    if (pluginJson.defaultSettings && typeof pluginJson.defaultSettings === 'object') {
        const keys = Object.keys(pluginJson.defaultSettings);
        console.log(`   Found ${keys.length} default settings to sanitize:`);

        for (const key of keys) {
            const originalValue = pluginJson.defaultSettings[key];
            pluginJson.defaultSettings[key] = '';
            console.log(`      ${key}: "${originalValue}" → ""`);
        }

        console.log(`\n   ✅ All ${keys.length} settings sanitized`);
    } else {
        console.log('   No defaultSettings found - nothing to sanitize');
    }

    // Write sanitized plugin.json
    writeFileSync(PLUGIN_JSON_PATH, JSON.stringify(pluginJson, null, 2) + '\n', 'utf-8');
    console.log(`\n   Output: ${PLUGIN_JSON_PATH}`);
    console.log('   ✅ plugin.json sanitized successfully');
}

main();
