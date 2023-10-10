all:
	gcc -Wall -Wl,-export-dynamic main.c -o iterkoczeos-notepad `pkg-config --cflags gtk+-3.0` `pkg-config --libs gtk+-3.0`

install:
	cp iterkoczeos-notepad /usr/bin
