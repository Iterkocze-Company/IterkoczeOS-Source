all: main.c
	gcc main.c -Wall -O2 -o seashell
	
install: seashell
	cp -v ./seashell /usr/bin/seashell
	
uninstall:
	rm -v /usr/bin/seashell
