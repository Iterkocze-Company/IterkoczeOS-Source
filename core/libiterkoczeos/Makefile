lib_all: lib test

lib: libiterkoczeos.c
	gcc -Wall -s -O2 -shared -fPIC -o libiterkoczeos.so libiterkoczeos.c

test: test.c
	gcc -Wall -s -O2 test.c -o test -L/sources/core/libiterkoczeos -R/sources/core/libiterkoczeos -literkoczeos 
	./test

install:
	doas cp -v libiterkoczeos.h /usr/include/
	doas cp -v libiterkoczeos.so /usr/lib/
	
clean:
	rm libiterkoczeos.so
	rm test
