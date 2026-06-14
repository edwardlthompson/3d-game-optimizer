# Threat Model (WinUI 3 Desktop)

## Scope

| Item | Value |
|---|---|
| Product | 3D Game Optimizer |
| Platform | WinUI 3 + .NET 8 desktop app |
| Method | STRIDE focused on local desktop trust boundaries |

## Trust Boundaries

```text
[User Session]
    |
    v
[WinUI 3 App Process] ---- reads/writes ---- [Local App Data + Seed JSON]
    |   \
    |    \---- launches ---- [Third-party Tool Processes]
    |
    \---- optional user-triggered network ---- [Steam/Metadata Endpoints]
```

Boundary notes:

- Main app process is trusted only for signed release binaries.
- External tools are semi-trusted and isolated via process-level boundaries.
- Network operations are optional and explicitly user-initiated.

## STRIDE Summary

| Threat | Desktop-Specific Example | Mitigation |
|---|---|---|
| Spoofing | Malicious executable posing as known tool installer | verify path/signature/hash when possible; trusted source allowlist |
| Tampering | Local seed JSON modified to inject unsafe args | schema validation + argument sanitization + read-only packaged defaults |
| Repudiation | User cannot determine what automation changed | local audit trail of actions, timestamps, and result states |
| Information Disclosure | Logs expose full library paths or personal folder names | structured log redaction and minimal field logging |
| Denial of Service | Hanging tool process blocks setup | process timeout, cancellation, and fallback flow |
| Elevation of Privilege | Tool launch requests admin unexpectedly | explicit elevation gate with user confirmation and rationale |

## Top Abuse Cases

1. Supply-chain replacement of a known tool binary.
2. Seed file manipulation to pass unsafe silent arguments.
3. Social engineering through fake "recommended profile" updates.
4. Excessive local scanning beyond user-selected Steam libraries.
5. Accidental persistence of sensitive file paths in logs.

## Security Controls in v1

- Signed release artifacts and checksum publication.
- Central process runner with policy checks before launch.
- Schema-based seed validation at load time.
- Privacy-first log policy with redaction defaults.
- Manual and automated review of tool manifest changes.
