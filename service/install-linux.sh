#!/bin/sh
# Installs the web server as a systemd service. Run as root from anywhere in the repo:
#   sudo ./service/install-linux.sh             install or update
#   sudo ./service/install-linux.sh uninstall   remove the service (keeps the folder)
set -e
NAME=meteor-web
DIR=/opt/meteor-web
UNIT=/etc/systemd/system/$NAME.service
REPO="$(cd "$(dirname "$0")/.." && pwd)"

if [ "$1" = uninstall ]; then
    systemctl disable --now $NAME || true
    rm -f $UNIT
    systemctl daemon-reload
    echo "Removed the service. $DIR is still there."
    exit 0
fi

id $NAME >/dev/null 2>&1 || useradd --system --no-create-home --shell /usr/sbin/nologin $NAME
systemctl stop $NAME 2>/dev/null || true
# Build as you, not root, so the repo doesn't fill up with root-owned files.
STAGE="$(mktemp -d)"
chmod 777 "$STAGE"
${SUDO_USER:+sudo -u "$SUDO_USER"} dotnet publish "$REPO/WebServer" -c Release -o "$STAGE"
mkdir -p $DIR
cp -R "$STAGE"/. $DIR/
rm -rf "$STAGE"
# Keep an existing config: it holds your database password.
[ -f $DIR/web_config.ini ] || cp "$REPO/WebServer/web_config.ini" $DIR/
chown -R $NAME:$NAME $DIR

cat > $UNIT <<UNIT
[Unit]
Description=Project Meteor web server
After=network.target mariadb.service mysql.service

[Service]
Type=notify
User=$NAME
WorkingDirectory=$DIR
ExecStart=$DIR/WebServer
Restart=on-failure
# Lets it use port 80 without running as root.
AmbientCapabilities=CAP_NET_BIND_SERVICE

[Install]
WantedBy=multi-user.target
UNIT

systemctl daemon-reload
systemctl enable --now $NAME
echo "Installed. Settings: $DIR/web_config.ini  Logs: journalctl -u $NAME"
