# Project Meteor Web

The web side of a [Project Meteor](https://github.com/Project-Meteor-Server/Project-Meteor-Server) server for FINAL FANTASY XIV 1.x: the launcher's version check, the login page, sign-up and patch downloads.

| Folder | What it is |
| --- | --- |
| `WebServer/` | The web server |
| `Tools/PatchTool/` | Makes your patch-signing key and signs patches ([README](Tools/PatchTool/README.md)) |
| `Tools/RetailRedirect/` | A DLL that points the retail game at your server ([README](Tools/RetailRedirect/README.md)) |

## Build

Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet build
```

## Run

```
cd WebServer/bin/Debug
./WebServer
```

Settings are in `web_config.ini` in that folder. Set `[Database]` to the game server's database: the login page writes the sessions the lobby checks, so both must use the same one.

| Port | What |
| --- | --- |
| 54996 | Launcher version check, and the patch certificate at `/patch/cert` |
| 80 | Login and sign-up pages (`login_port`) |
| 54997 | Patch tracker (only when patches are set up) |
| 54998 | Patch seed (only when patches are set up) |

## Run as a Service

These build the server, copy it to a system folder, and start it at boot. Run them again to update. Your `web_config.ini` is kept. Add `uninstall` to remove the service.

| System | Command | Installs to |
| --- | --- | --- |
| Linux (systemd) | `sudo ./service/install-linux.sh` | `/opt/meteor-web` |
| macOS | `sudo ./service/install-macos.sh` | `/usr/local/meteor-web` |
| Windows (admin PowerShell) | `.\service\install-windows.ps1` | `C:\Program Files\MeteorWeb` |

After the first install, set `[Database]` in the installed `web_config.ini` and restart the service.

---

## Getting Started

*Note: We will not supply any SQEX assets. This includes retail patches, torrents, or imagery for the web pages.*

What you need:

- All retail patches in `patch_dir`
- All retail patch torrent files
- Your signing key `signing_key`
- The server's public address `seed_address`

*Edit these values in `web_config.ini`.*

## Pointing the Game at the Server

1. Compile the [RetailRedirect](https://github.com/Project-Meteor-Server/Meteor-PatchLogin-Server/tree/main/Tools/RetailRedirect) DLL and modify the generated `iphlpapi.ini` with your server address.
2. Place `iphlpapi.dll` and `iphlpapi.ini` in the retail `FINAL FANTASY XIV` installation folder.
3. `ffxivboot.exe` should now resolve and update with your server.

## License

AGPL v3. See [LICENSE.md](LICENSE.md).
