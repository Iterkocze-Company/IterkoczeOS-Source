#include "libiterkoczeos.h"
#include "libiterkoczeos.cpp"

int main() {
	std::cout << GetSystemUser() << std::endl;
	std::cout << C_GetSystemVersion() << std::endl;
	return 0;
}
