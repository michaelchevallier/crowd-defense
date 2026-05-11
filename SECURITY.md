# Security Policy

## Supported Versions

This project is currently in active development (pre-release). Only the
latest commit on `main` is considered for security updates.

## Reporting a Vulnerability

If you discover a security vulnerability in this project, please **do not**
open a public issue. Instead, report it privately:

1. Use GitHub's **Private Vulnerability Reporting** feature on this repository
   (Security tab → Report a vulnerability).
2. Provide a clear description, reproduction steps if applicable, and the
   commit hash where you observed the issue.
3. Expect an initial response within 7 days.

## Scope

In-scope:
- Code in `Assets/Scripts/`
- Build/CI configuration in `.github/workflows/` and `Assets/Editor/`
- Game logic that handles user input, save data, or network requests

Out-of-scope:
- Third-party Unity packages (report upstream to their maintainers)
- Vulnerabilities requiring local file system access by the player
- Issues already known and tracked in `docs/`

## Disclosure Policy

We follow coordinated disclosure: reporters are credited (if desired) after
a fix is shipped. Public disclosure timing is decided jointly.
