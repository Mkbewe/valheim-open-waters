#!/usr/bin/env node
import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { createInterface } from 'node:readline/promises';
import { stdin, stdout } from 'node:process';

const RELEASE_TYPES = ['patch', 'minor', 'major'];
const repoRoot = process.cwd();
const args = process.argv.slice(2);
const dryRun = args.includes('--dry-run');
const assumeYes = args.includes('--yes');
const requestedType = args.find((argument) => !argument.startsWith('--'));

if (requestedType && !RELEASE_TYPES.includes(requestedType)) {
  console.error(`Unknown release type "${requestedType}". Use patch, minor or major.`);
  process.exit(1);
}

const packagePath = path.join(repoRoot, 'package.json');
const packageLockPath = path.join(repoRoot, 'package-lock.json');
const manifestPath = path.join(repoRoot, 'manifest.json');
const pluginPath = path.join(repoRoot, 'src', 'OpenWaters', 'Plugin.cs');
const readmePath = path.join(repoRoot, 'README.md');
const guidePath = path.join(repoRoot, 'docs', 'USER_GUIDE.md');
const releaseItPath = path.join(repoRoot, 'node_modules', 'release-it', 'bin', 'release-it.js');
const buildScriptPath = path.join(repoRoot, 'tooling', 'build-release.ps1');
const releaseDir = path.join(repoRoot, 'release');

function capture(command, commandArgs, options = {}) {
  return execFileSync(command, commandArgs, {
    cwd: repoRoot,
    encoding: 'utf8',
    ...options,
  }).trim();
}

function run(command, commandArgs) {
  execFileSync(command, commandArgs, {
    cwd: repoRoot,
    stdio: 'inherit',
  });
}

function fail(message) {
  console.error(message);
  process.exit(1);
}

function incrementVersion(version, releaseType) {
  const match = /^(\d+)\.(\d+)\.(\d+)$/.exec(version);
  if (!match) {
    fail(`Unsupported package version "${version}".`);
  }

  const [major, minor, patch] = match.slice(1).map(Number);
  if (releaseType === 'major') {
    return `${major + 1}.0.0`;
  }
  if (releaseType === 'minor') {
    return `${major}.${minor + 1}.0`;
  }
  return `${major}.${minor}.${patch + 1}`;
}

function detectReleaseType(commits, currentVersion) {
  const breaking = /^BREAKING[ -]CHANGE:/m.test(commits) || /^[a-z]+(\([^)]*\))?!:/m.test(commits);
  if (breaking) {
    return currentVersion.startsWith('0.') ? 'minor' : 'major';
  }

  return /^feat(\([^)]*\))?:/m.test(commits) ? 'minor' : 'patch';
}

function replaceRequired(filePath, pattern, replacement, description) {
  const content = readFileSync(filePath, 'utf8');
  const updated = content.replace(pattern, replacement);
  if (updated === content) {
    fail(`Could not update ${description}.`);
  }
  writeFileSync(filePath, updated, 'utf8');
}

function syncVersions(version) {
  replaceRequired(
    manifestPath,
    /"version_number"\s*:\s*"[^"]+"/,
    `"version_number": "${version}"`,
    'manifest.json'
  );
  replaceRequired(
    pluginPath,
    /(PluginVersion\s*=\s*")[^"]+(")/,
    `$1${version}$2`,
    'Plugin.cs'
  );
  replaceRequired(readmePath, /(- Version:\s*`)[^`]+(`)/, `$1${version}$2`, 'README.md');
  replaceRequired(guidePath, /(alpha \()[^)]+(\))/, `$1${version}$2`, 'docs/USER_GUIDE.md');

  const lockFile = JSON.parse(readFileSync(packageLockPath, 'utf8'));
  const rootPackage = lockFile.packages && lockFile.packages[''];
  if (lockFile.version !== version || (rootPackage && rootPackage.version !== version)) {
    lockFile.version = version;
    if (rootPackage) {
      rootPackage.version = version;
    }
    writeFileSync(packageLockPath, `${JSON.stringify(lockFile, null, 2)}\n`, 'utf8');
  }
}

if (!existsSync(releaseItPath)) {
  fail('release-it is not installed. Run npm install first.');
}

const packageJson = JSON.parse(readFileSync(packagePath, 'utf8'));
const currentVersion = packageJson.version;

let baseTag = '';
try {
  baseTag = capture('git', ['describe', '--tags', '--abbrev=0', '--match', 'v*'], {
    stdio: ['ignore', 'pipe', 'ignore'],
  });
} catch {
  console.warn('No v* tag found; the first changelog will include the full history.');
}

const range = baseTag ? `${baseTag}..HEAD` : 'HEAD';
const commits = capture('git', ['log', '--format=%B', range]);
if (!commits) {
  fail(`No commits found since ${baseTag || 'the repository root'}.`);
}

const releaseType = requestedType ?? detectReleaseType(commits, currentVersion);
const nextVersion = incrementVersion(currentVersion, releaseType);
const releaseBranch = `release/v${nextVersion}`;

console.log(`${requestedType ? 'Requested' : 'Detected'} release type: ${releaseType}`);
console.log(`Version: ${currentVersion} -> ${nextVersion}`);
console.log(`Branch: ${releaseBranch}`);

if (dryRun) {
  console.log('Dry run; no files, commits, tags or branches changed.');
  process.exit(0);
}

if (capture('git', ['status', '--porcelain'])) {
  fail('Working tree is not clean. Commit or stash changes before releasing.');
}

const branch = capture('git', ['branch', '--show-current']);
if (branch !== 'main') {
  fail(`Releases must be started from main, current branch is "${branch}".`);
}

if (!assumeYes) {
  if (!stdin.isTTY) {
    fail('Confirmation requires a terminal. Use --yes for non-interactive runs.');
  }
  const readline = createInterface({ input: stdin, output: stdout });
  const answer = (await readline.question('Continue? [y/N] ')).trim().toLowerCase();
  readline.close();
  if (!['y', 'yes', 'tak'].includes(answer)) {
    console.log('Cancelled.');
    process.exit(0);
  }
}

if (capture('git', ['branch', '--list', releaseBranch])) {
  fail(`Branch ${releaseBranch} already exists.`);
}

run('git', ['switch', '-c', releaseBranch]);

try {
  run(process.execPath, [
    releaseItPath,
    nextVersion,
    '--ci',
    '--config',
    'tooling/release-it.config.mjs',
  ]);
} catch {
  fail(`release-it failed. Branch ${releaseBranch} was left in place for inspection.`);
}

syncVersions(nextVersion);

const powershell = process.platform === 'win32' ? 'powershell' : 'pwsh';
const powershellArgs = ['-NoProfile'];
if (process.platform === 'win32') {
  powershellArgs.push('-ExecutionPolicy', 'Bypass');
}
powershellArgs.push('-File', buildScriptPath, '-OutputDir', 'release');

try {
  run(powershell, powershellArgs);
} catch {
  fail(`Release build failed. Branch ${releaseBranch} was left in place for inspection.`);
}

const packageZip = path.join(releaseDir, `MkbewePP-OpenWaters-${nextVersion}.zip`);
if (!existsSync(packageZip)) {
  fail(`Release ZIP not found at ${packageZip}.`);
}

const files = [
  'package.json',
  'package-lock.json',
  'manifest.json',
  'CHANGELOG.md',
  'src/OpenWaters/Plugin.cs',
  'README.md',
  'docs/USER_GUIDE.md',
  path.relative(repoRoot, packageZip).replaceAll('\\', '/'),
].filter((file) => existsSync(path.join(repoRoot, file)));

run('git', ['add', ...files]);

if (!capture('git', ['diff', '--cached', '--name-only'])) {
  fail('No release changes were staged.');
}

run('git', ['commit', '-m', `chore(release): v${nextVersion}`]);

console.log('');
console.log(`Created ${releaseBranch} with chore(release): v${nextVersion}.`);
console.log(`Included release ZIP: ${path.relative(repoRoot, packageZip)}`);
console.log('No tag or push was created.');
console.log(`Next: git push -u origin ${releaseBranch}`);
console.log('After merge, run the Publish Release workflow manually and type "release" to confirm.');
