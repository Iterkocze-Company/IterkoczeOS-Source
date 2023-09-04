#ifndef LIBITERKOCZEOS_H
#define LIBITERKOCZEOS_H

#include <iostream>
#include <fstream>
#include <unistd.h>

void _Log(std::string msg);
std::string _GetSystemUser();
std::string GetSystemUser();
std::string _GetSystemVersion();
std::string GetSystemVersion();
extern "C" {
	const char* C_GetSystemUser();
	const char* C_GetSystemVersion();
}

#endif
