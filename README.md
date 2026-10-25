# Project Meteor Web

The web side of a [Project Meteor](https://github.com/Project-Meteor-Server/Project-Meteor-Server)
server for FINAL FANTASY XIV 1.x: the launcher's version check, the login page, sign-up and patch
downloads.

| Folder | What it is |
|---|---|
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

Settings are in `web_config.ini` in that folder. Set `[Database]` to the game server's database:
the login page writes the sessions the lobby checks, so both must use the same one.

| Port | What |
|---|---|
| 54996 | Launcher version check, and the patch certificate at `/patch/cert` |
| 80 | Login and sign-up pages (`login_port`) |
| 54997 | Patch tracker (only when patches are set up) |
| 54998 | Patch seed (only when patches are set up) |

## Run as a service

These build the server, copy it to a system folder, and start it at boot. Run them again to update.
Your `web_config.ini` is kept. Add `uninstall` to remove the service.

| System | Command | Installs to |
|---|---|---|
| Linux (systemd) | `sudo ./service/install-linux.sh` | `/opt/meteor-web` |
| macOS | `sudo ./service/install-macos.sh` | `/usr/local/meteor-web` |
| Windows (admin PowerShell) | `.\service\install-windows.ps1` | `C:\Program Files\MeteorWeb` |

After the first install, set `[Database]` in the installed `web_config.ini` and restart the service.

## Patches

Installs older than `game_version` need patches. To serve them, set these under `[Patch]` in
`web_config.ini`:

- `patch_dir`: the retail patches, laid out like a retail `downloads\ffxiv` folder
  (`<repo id>/metainfo/D<version>.torrent` and `<repo id>/patch/D<version>.patch`).
- `signing_key`: the private key from [PatchTool](Tools/PatchTool/README.md) `keygen`. Its
  `patch_cert.der` goes beside it; players' RetailRedirect downloads it from `/patch/cert`.
- `seed_address`: the server's public address. The tracker hands it to players to download from.

## Pointing the game at the server

The retail client has Square Enix's host names built in. [RetailRedirect](Tools/RetailRedirect/README.md)
sends them to your server without editing the hosts file or any game file. The login page must
answer on port 80, because the login window always opens
`http://account.square-enix.com/account/content/ffxivlogin`.

## Login page

The page follows the retail login window's layout. Its text comes in English and Japanese,
picked from the language the login window reports; add `?lang=ja-jp` to preview another language in
a normal browser.

The images are Square Enix artwork, so they aren't in this repo. Put them in
`WebServer/wwwroot/account/content/img/`: `logo.png`, `banner.png`, `btLogin.png`, `lbOneTime.png`,
`lbSignup.png`, `lbSQEXId_mem.gif`, `lbSQEXPass_mem.gif` and `lbMail.gif`. The pages work without
them, just with no pictures.

## License

AGPL v3. See [LICENSE.md](LICENSE.md).
