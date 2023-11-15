#include <stdlib.h>
#include "libbollocks.h"

// Shamelessly stolen from: https://codereview.stackexchange.com/questions/29198/random-string-generator-in-c

char* bollocks(int len) {
    static char charset[] = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789,.-#'?!";

    char *randomString = NULL;

    if (len) {
        randomString = malloc(sizeof(char) * (len +1));

        if (randomString) {            
            for (int n = 0;n < len;n++) {            
                int key = rand() % (int)(sizeof(charset) -1);
                randomString[n] = charset[key];
            }

            randomString[len] = '\0';
        }
    }

    return randomString;
}