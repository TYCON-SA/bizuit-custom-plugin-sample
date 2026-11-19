#!/usr/bin/env node

/**
 * Remove the example features — leaves the skeleton a new product starts from.
 *
 * WHY THIS EXISTS. A product created from this template must not ship Items, Products or
 * AuditLogs: their migrations create example tables in the customer's database. Removing them
 * by hand had two known traps. The tests are loose files, not per-feature folders, so
 * `rm -rf tests/.../Features/Items` deletes nothing. The registrations live in two different
 * methods of the plugin class. Both left a skeleton that did not compile.
 *
 * The same script runs in two places, so "the skeleton" has one definition:
 *   - the onboarding of a new product (skill bizuit-new-product, step 2);
 *   - this template's own CI, which removes the examples in the runner and then checks the
 *     dependency scan and the 70 % coverage floor a product pipeline enforces.
 *
 * What it removes:
 *   - src/<Plugin>/Features/{Items,Products,AuditLogs}/
 *   - tests/<Plugin>.Tests/Features/{Items,Products,AuditLogs}*Tests.cs
 *   - database/*.sql (the example migrations and setup-database.sql)
 *   - every line of src/<Plugin>/<Plugin>.cs marked with `// example-feature`
 *
 * It works before and after renaming MyPlugin, and running it twice is harmless.
 * It fails (exit 1) when a reference to an example feature is still left in the code.
 *
 * Usage (from the repository root):
 *   node scripts/remove-examples.mjs
 */

import { readdirSync, readFileSync, writeFileSync, rmSync, existsSync, statSync } from 'fs';
import { join, relative } from 'path';

const ROOT = process.cwd();
const EXAMPLES = ['Items', 'Products', 'AuditLogs'];
const MARKER = '// example-feature';
// Code identifiers only, so a comment or a README that names a feature does not fail the check.
const LEFTOVER = /\b(Items|Products|AuditLogs)(Service|Repository|Endpoints)\b|\bFeatures\.(Items|Products|AuditLogs)\b/;

function dirs(path) {
    return existsSync(path) ? readdirSync(path).filter(n => statSync(join(path, n)).isDirectory()) : [];
}

function fail(message) {
    console.error(`❌ ${message}`);
    process.exit(1);
}

function findPluginProject() {
    const candidates = dirs(join(ROOT, 'src'))
        .filter(n => n !== 'DevHost' && existsSync(join(ROOT, 'src', n, `${n}.csproj`)));
    if (candidates.length !== 1) {
        fail(`expected exactly one plugin project under src/ (besides DevHost), found: ${candidates.join(', ') || 'none'}`);
    }
    return candidates[0];
}

function remove(path, removed) {
    if (existsSync(path)) {
        rmSync(path, { recursive: true, force: true });
        removed.push(relative(ROOT, path));
    }
}

function filesUnder(path, ext) {
    if (!existsSync(path)) return [];
    return readdirSync(path, { recursive: true })
        .map(n => join(path, n))
        .filter(f => f.endsWith(ext) && !/[\\/](bin|obj)[\\/]/.test(f));
}

function main() {
    const plugin = findPluginProject();
    const removed = [];

    for (const feature of EXAMPLES) {
        remove(join(ROOT, 'src', plugin, 'Features', feature), removed);
    }

    const testFeatures = join(ROOT, 'tests', `${plugin}.Tests`, 'Features');
    if (existsSync(testFeatures)) {
        for (const file of readdirSync(testFeatures)) {
            if (EXAMPLES.some(f => file.startsWith(f)) && file.endsWith('Tests.cs')) {
                remove(join(testFeatures, file), removed);
            }
        }
    }

    const database = join(ROOT, 'database');
    if (existsSync(database)) {
        for (const file of readdirSync(database).filter(f => f.endsWith('.sql'))) {
            remove(join(database, file), removed);
        }
    }

    const entry = join(ROOT, 'src', plugin, `${plugin}.cs`);
    if (!existsSync(entry)) fail(`plugin class not found: ${relative(ROOT, entry)}`);
    const before = readFileSync(entry, 'utf8').split('\n');
    const kept = before.filter(line => !line.includes(MARKER));
    // Drop the blank lines the removed blocks leave behind: never two in a row.
    const compact = kept.filter((line, i) => !(line.trim() === '' && i > 0 && kept[i - 1].trim() === ''));
    if (compact.length !== before.length) {
        writeFileSync(entry, compact.join('\n'));
        removed.push(`${before.length - kept.length} marked lines in ${relative(ROOT, entry)}`);
    }

    const leftovers = [...filesUnder(join(ROOT, 'src', plugin), '.cs'), ...filesUnder(join(ROOT, 'tests'), '.cs')]
        .flatMap(f => readFileSync(f, 'utf8').split('\n')
            .map((line, i) => ({ f, i, line }))
            .filter(x => LEFTOVER.test(x.line)));
    if (leftovers.length > 0) {
        for (const x of leftovers) console.error(`   ${relative(ROOT, x.f)}:${x.i + 1}: ${x.line.trim()}`);
        fail('references to the example features are still in the code (see above)');
    }

    if (removed.length === 0) {
        console.log('✅ Nothing to remove: the example features were already gone.');
    } else {
        console.log('🧹 Removed:');
        for (const r of removed) console.log(`   - ${r}`);
        console.log('✅ Only the skeleton is left (Me endpoint and SkeletonTests).');
    }
}

main();
