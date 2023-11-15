#include <stdlib.h>
#include <stdio.h>
#include <time.h>
#include <libbollocks.h>

int main() {
    srand(time(NULL));
    printf("Bollocks: %s\n", bollocks(16));
}