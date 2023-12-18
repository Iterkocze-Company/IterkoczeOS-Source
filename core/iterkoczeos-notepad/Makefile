all:
	gcc -Wall -Wl,-export-dynamic main.c -o iterkocze-notepad `pkg-config --cflags gtk+-3.0` `pkg-config --libs gtk+-3.0`

install:
	cp -v iterkoczeos-notepad /usr/bin
	cp -v iterkoczeos-notepad.glade /var/ui
