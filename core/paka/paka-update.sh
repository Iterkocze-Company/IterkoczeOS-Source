#!/bin/sh

mkdir -p /tmp/paka-update
wget -q --show-progress https://github.com/Iterkocze-Company/IterkoczeOS-Packages-Main/raw/main/paka/paka.7z -O "/tmp/paka-update/paka.7z"
if (( $? > 0 )); then
    echo "wget exited with non zero code"
    exit 1
fi

cd /tmp/paka-update
7z x ./paka.7z
mv -v ./paka /bin/paka

chmod +x /bin/paka
rm -rf /tmp/paka-update