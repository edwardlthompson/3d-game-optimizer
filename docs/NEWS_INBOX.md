# News Inbox

Auto-collected inbox of news, upstream updates, security fixes, and ideas relevant to this repo, appended daily by News Bot.
A Cursor planning agent should review these items and fold the useful ones into BUILD_PLAN; entries are append-only, newest at the bottom.


## 2026-11-13

**News and updates**

- .NET November servicing (10.0.15 / 9.0.23 / 8.0.34) fixes ~25 CVEs; CVE-2026-118747 also needs a System.Formats.Cbor NuGet update. [source](https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-november-2026-servicing-updates/)
- .NET 11 is GA, including WPF/WinForms/Windows Desktop. [source](https://devblogs.microsoft.com/dotnet/announcing-dotnet-11/)

**Tasks for the planning agent**

- [ ] Rebuild on .NET 8.0.34 and bump Microsoft.* packages — Ships the November .NET security fixes in the WinUI app. Label: `[AGENT]` · Priority: P1 · https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-november-2026-servicing-updates/
- [ ] Plan a .NET 11 target migration — Keeps the app on a current runtime line. Label: `[AGENT]` · Priority: P3 · https://devblogs.microsoft.com/dotnet/announcing-dotnet-11/

## 2026-11-14

**News and updates**

- GitHub secret scanning now detects Cloudflare Access service tokens. [source](https://github.blog/changelog/2026-11-12-secret-scanning-adds-a-detector-for-cloudflare)
- Apex Neural Rendering (ANR): cross-vendor frame-generation beta that replaces DLSS-FG via a proxy nvngx_dlssg.dll (OptiScaler). [source](https://github.com/apex-compute/ANR/releases)

**Tasks for the planning agent**

- [ ] Confirm secret scanning is on before wiring the Cloudflare token — The new Cloudflare detector will catch an accidentally committed Worker token. Label: `[AGENT]` · Priority: P3 · https://github.blog/changelog/2026-11-12-secret-scanning-adds-a-detector-for-cloudflare
- [ ] Detect OptiScaler/ANR proxy DLLs and warn about injection conflicts — Proxy DLLs can clash with ReShade/UEVR/TrueGame 3D injection. Label: `[AGENT]` · Priority: P3 · https://github.com/apex-compute/ANR/releases
