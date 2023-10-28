#include "libiterkoczeos.h"
#include <stdio.h>

int main() {
    printf("System user: ");
    printf(GetSystemUser());
    printf("\n");
    printf("OS Version: ");
    printf(GetSystemVersion());
    printf("\n");
    return 0;
} 
