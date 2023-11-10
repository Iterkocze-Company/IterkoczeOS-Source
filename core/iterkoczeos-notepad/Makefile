all:
	gcc -Wall -Wl,-export-dynamic main.c -o iterkocze-notepad `pkg-config --cflags gtk+-3.0` `pkg-config --libs gtk+-3.0`

install:
	doas cp -v iterkocze-notepad /usr/bin
	doas cp -v iterkocze-notepad.glade /var/ui
