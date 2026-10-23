#!/bin/sh
# Installs the web server as a launchd service that starts at boot. Run from anywhere in the repo:
#   sudo ./service/install-macos.sh             install or update
#   sudo ./service/install-macos.sh uninstall   remove the service (keeps the folder)
set -e
LABEL=org.projectmeteor.web
DIR=/usr/local/meteor-web
PLIST=/Library/LaunchDaemons/$LABEL.plist
REPO="$(cd "$(dirname "$0")/.." && pwd)"

if [ "$1" = uninstall ]; then
    launchctl bootout system/$LABEL 2>/dev/null || true
    rm -f $PLIST
    echo "Removed the service. $DIR is still there."
    exit 0
fi

launchctl bootout system/$LABEL 2>/dev/null || true
# Build as you, not root, so the repo doesn't fill up with root-owned files.
STAGE="$(mktemp -d)"
chmod 777 "$STAGE"
${SUDO_USER:+sudo -u "$SUDO_USER"} dotnet publish "$REPO/WebServer" -c Release -o "$STAGE"
mkdir -p $DIR
cp -R "$STAGE"/. $DIR/
rm -rf "$STAGE"
# Keep an existing config: it holds your database password.
[ -f $DIR/web_config.ini ] || cp "$REPO/WebServer/web_config.ini" $DIR/

cat > $PLIST <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>Label</key><string>$LABEL</string>
  <key>ProgramArguments</key><array><string>$DIR/WebServer</string></array>
  <key>WorkingDirectory</key><string>$DIR</string>
  <key>RunAtLoad</key><true/>
  <key>KeepAlive</key><true/>
  <key>StandardOutPath</key><string>$DIR/logs/service.log</string>
  <key>StandardErrorPath</key><string>$DIR/logs/service.log</string>
</dict>
</plist>
PLIST
mkdir -p $DIR/logs
launchctl bootstrap system $PLIST
echo "Installed. Settings: $DIR/web_config.ini  Logs: $DIR/logs"
