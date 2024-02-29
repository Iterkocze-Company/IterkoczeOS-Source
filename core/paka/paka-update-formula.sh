#!/bin/sh

mkdir -p /tmp/paka-update-formula
wget https://github.com/Iterkocze-Company/IterkoczeOS-Packages-Main/raw/main/paka/formula.tar -O "/tmp/paka-update-formula/formula.tar"

if (( $? > 0 )); then
    echo "wget exited with non zero code"
    exit 1
fi

cd /tmp/paka-update-formula
tar xf formula.tar
mv -v ./formula/* /programs/system/paka/formula/

rm -rf /tmp/paka-update-formula