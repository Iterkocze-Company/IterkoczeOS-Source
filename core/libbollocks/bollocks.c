#include <stdlib.h>
#include <stdio.h>
#include <time.h>
#include <libbollocks.h>

int main(int argc, char** argv) {
    int len = 16;
    if (argc == 2) {
        len = atoi(argv[1]);
    }
    srand(time(NULL));
    printf(bollocks(len));
}