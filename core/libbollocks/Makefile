lib:
	gcc -Wall -s -O2 -shared -fPIC -o libbollocks.so libbollocks.c

install:
	doas cp -v libbollocks.so /usr/lib
	doas cp -v libbollocks.h /usr/include
	doas cp -v bollocks /usr/bin

test:
	gcc -Wall -s -O2 -lbollocks -o test test.c

bollocks:
	gcc -Wall -s -O2 -lbollocks -o bollocks bollocks.c