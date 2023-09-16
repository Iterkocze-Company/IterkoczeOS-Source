#/bin/bash

cp -v ./* /programs/greeter
chmod +x /programs/greeter/greeter.py
chown 1000:1000 /programs/greeter/greeter.py