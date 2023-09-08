lib_all: lib install test

lib: libiterkoczeos.c
	gcc -Wall -s -O2 -shared -fPIC -o libiterkoczeos.so libiterkoczeos.c

test: test.c
	gcc -Wall -s -O2 test.c -o test -L./libiterkoczeos.so -literkoczeos
	./test

install:
	cp -v libiterkoczeos.h /usr/include/
	cp -v libiterkoczeos.so /usr/lib/
	
clean:
	rm libiterkoczeos.so
	rm test
