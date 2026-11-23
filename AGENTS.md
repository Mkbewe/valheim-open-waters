# Agent Guidelines

## Commit messages

- Use Conventional Commits.
- Write the body as bullet points, not prose paragraphs.
- Describe changes from the player's or application user's perspective.
- Focus on what was added, changed or fixed, not on internal implementation details.
- Mention technical details only when they are needed to understand user-facing behavior.
- Keep each bullet concise and outcome-focused.

## GitHub

- Board: **Open Waters Board** (project `5`, owner `Mkbewe`)
  - https://github.com/users/Mkbewe/projects/5
- Repository: `Mkbewe/valheim-open-waters`
- List board items: `gh project item-list 5 --owner Mkbewe --limit 100 --format json`
- Issue details: `gh issue view <number> -R Mkbewe/valheim-open-waters`
- Create issue: `gh issue create -R Mkbewe/valheim-open-waters`
- `gh` is installed and authenticated (account `Mkbewe`)
- Issue titles, descriptions, and repository documentation default to English; use
  another language only when the user specifies it
- Statuses: Backlog -> ToDo -> In progress -> Done (merged to `main`) ->
  Deployed to Prod (shipped in a release)
- Every issue carries exactly one `area:*` label (`area:generator`, `area:map`,
  `area:ui`, `area:network`, `area:tooling`, `area:docs`) and one type label
  (`enhancement`, `bug`, `refactor`, `chore`, `documentation`, `perf`, `test`,
  `ci`)
- Priority field: P0 (urgent), P1 (next up), P2 (later)
- New issues are added to the board automatically by the **Auto-add to project**
  workflow
