# Project Meteor Web

The web side of a [Project Meteor](https://github.com/rubymatrix/Project-Meteor-Server) server for
FINAL FANTASY XIV 1.x: the launcher's version check, the login page, sign-up and patch downloads.

| Folder | What it is |
|---|---|
| `WebServer/` | The web server |
| `Tools/PatchTool/` | Makes your patch-signing key and signs patches ([README](Tools/PatchTool/README.md)) |
| `Tools/RetailRedirect/` | A DLL that points the game at your server ([README](Tools/RetailRedirect/README.md)) |

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

Settings are in `web_config.ini` in that folder. Set `[Database]` to the game server's database.

| Port | What |
|---|---|
| 54996 | Launcher version check |
| 80 | Login and sign-up pages |
| 54997, 54998 | Patch downloads (only when patches are set up) |

## Run as a service

These build the server, copy it to a system folder, and start it at boot. Run them again to update.
Your `web_config.ini` is kept. Add `uninstall` to remove the service.

| System | Command | Installs to |
|---|---|---|
| Linux (systemd) | `sudo ./service/install-linux.sh` | `/opt/meteor-web` |
| macOS | `sudo ./service/install-macos.sh` | `/usr/local/meteor-web` |
| Windows (admin PowerShell) | `.\service\install-windows.ps1` | `C:\Program Files\MeteorWeb` |

After the first install, set `[Database]` in the installed `web_config.ini` and restart the service.

## Login page images

The images aren't in this repo. Put them in `WebServer/wwwroot/account/content/img/`:
`logo.png`, `banner.png`, `btLogin.gif`, `btSignup.gif`, `lbSQEXId_mem.gif`, `lbSQEXPass_mem.gif`
and `lbMail.gif`. The pages work without them, just with no pictures.

## License

AGPL v3. See [LICENSE.md](LICENSE.md).
