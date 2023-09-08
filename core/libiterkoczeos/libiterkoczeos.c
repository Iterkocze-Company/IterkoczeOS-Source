#include "libiterkoczeos.h"
#include <stdlib.h>
#include <stdio.h>
#include <stdint.h>
#include <string.h>
#include <ctype.h>
#include <stdbool.h>

const char* GetSystemUser() {
    FILE* stream;
    static char buffer[256];
    stream = popen("id -un 1000", "r");
    if (stream == NULL) {
        printf("Failed to get system user. Command failed");
        exit(1);
    }

    fgets(buffer, sizeof(buffer), stream);
    strtok(buffer, "\n"); //KEKW
    return buffer;
}

const char* GetSystemVersion() {
    FILE* fp;
    char* line = NULL;
    size_t len = 0;
    ssize_t read = 0;

    fp = fopen("/etc/os-release", "r");
    if (fp == NULL) {
        printf("Failed to get system version. Can't access file");
        exit(1);
    }
    while ((read = getline(&line, &len, fp)) != -1) {
        if (strstr(line, "VERSION")) {
            // by the gods
            static char pls[3];
            pls[0] = line[9];
            pls[1] = line[10];
            pls[2] = line[11];
            return pls;
        }
    }

    return "VERSION ERROR";
}

bool IsVM() {
    printf("IsVM() IS NOT IMPLEMENTED");
    return false;
}