import createPreset from 'conventional-changelog-conventionalcommits';

const types = [
  { type: 'feat', section: 'Features' },
  { type: 'fix', section: 'Bug Fixes' },
  { type: 'perf', section: 'Performance Improvements' },
  { type: 'refactor', section: 'Refactors' },
  { type: 'docs', section: 'Documentation', effect: 'changelog' },
  { type: 'style', section: 'Styling', effect: 'hidden' },
  { type: 'test', section: 'Tests', effect: 'hidden' },
  { type: 'build', section: 'Build System', effect: 'hidden' },
  { type: 'ci', section: 'Continuous Integration', effect: 'hidden' },
  { type: 'chore', section: 'Chores', effect: 'hidden' },
];

const { writer } = createPreset({ types });
const baseCommitPartial = writer.commitPartial;

export default {
  plugins: {
    '@release-it/conventional-changelog': {
      preset: { name: 'conventionalcommits', types },
      infile: 'CHANGELOG.md',
      writerOpts: {
        commitPartial: (context, commit) => {
          const entry = baseCommitPartial(context, commit);
          const body = typeof commit.body === 'string' ? commit.body.trim() : '';
          return body ? `${entry}\n\n${body}` : entry;
        },
      },
    },
  },
  git: {
    commit: false,
    tag: false,
    push: false,
    requireCleanWorkingDir: true,
    requireUpstream: false,
    commitMessage: 'chore(release): v${version}',
  },
  npm: {
    publish: false,
  },
  github: false,
};
