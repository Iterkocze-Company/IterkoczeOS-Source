lib: libiterkoczeos.cpp
	g++ -std=c++20 -Wall -s -O2 -shared -fPIC -o libiterkoczeos.so libiterkoczeos.cpp

test: libiterkoczeos-test.cpp
	g++ -std=c++20 -Wall -s -O2 libiterkoczeos-test.cpp -o lib-test -literkoczeos

install:
	cp -v libiterkoczeos.h /usr/include/
	cp -v libiterkoczeos.so /usr/lib/
	
clean:
	rm libiterkoczeos.so
	rm lib-test
