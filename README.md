# LAN//ARENA — Public Tournament Site

The first ASP.NET Core MVC vertical slice for the COD4 + NFS Most Wanted 2005 tournament. Built for .NET 10 with three application layers and feature-oriented use cases.

## Run locally

```powershell
dotnet run --project src/LanArena.Web
```

Use the printed localhost URL. The first request creates `src/LanArena.Web/App_Data/tournament-public.json` with sample event data. That runtime file is git-ignored because registration records contain personal information.

To serve the public site on the local network, run the `lan` profile:

```powershell
dotnet run --project src/LanArena.Web --launch-profile lan
```

Then open `http://<tournament-server-LAN-IP>:5080` from a connected device. Configure the Windows firewall to allow inbound TCP 5080 on the private LAN.

## Public routes

- `/` — event landing page and live highlights
- `/schedule` — COD4 matches and NFS heats
- `/rules` — COD4, NFS, and event fair-play rules
- `/teams` and `/racers` — approved public entries
- `/leaderboard/cod4` and `/leaderboard/nfs` — public standings, refreshed every 10 seconds
- `/register/cod4` and `/register/nfs` — tournament registration

Registration submissions are pending review. COD4 team entry captures five main nicknames and GUIDs, plus an optional substitute. GUIDs and contact details are not rendered on public pages. Duplicate team names, short names, player nicknames, GUIDs, NFS nicknames, and NFS phone numbers are rejected.

## Architecture

```text
src/LanArena.Web              ASP.NET Core MVC presentation, routes, Razor views
src/LanArena.Application      Public-site query and registration use cases/contracts
src/LanArena.Infrastructure   JSON-file persistence adapter and sample event data
```

Vertical slices currently cover the public event view, schedule, team/racer directories, leaderboards, and COD4/NFS registration. The infrastructure store writes a local JSON file so this first slice runs without external services. Replace it with SQL Server before operating the tournament with concurrent admins or relying on production-grade backup/audit controls; login, registration moderation UI, detailed live/result entry, and RCON are not implemented yet.
