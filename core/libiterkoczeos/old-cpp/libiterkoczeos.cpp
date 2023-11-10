#include "libiterkoczeos.h"

void _Log(std::string msg) {
	std::cout << "[libiterkoczeos] - " << msg << std::endl;
}

std::string _GetSystemUser() {
	auto user = getuid();
	if (user != 0) {
		_Log("Not running as root. Quitting");
		exit(1);
	}
	std::ifstream shadow_file ("/etc/shadow");
	if (shadow_file.is_open()) {
		std::string tp;
		int line_nr = 0;
		while(getline(shadow_file, tp)) {
			line_nr++;
			if (line_nr == 7) {
				return tp.substr(0, tp.find(":"));
			}
		}
		shadow_file.close();
	}
	return "NONE";
}

std::string GetSystemUser() {
	return _GetSystemUser();
}

std::string _GetSystemVersion() {
	std::ifstream version_file ("/etc/os-release");
	if (version_file.is_open()) {
		std::string tp;
		int line_nr = 0;
		while(getline(version_file, tp)) {
			line_nr++;
			if (line_nr == 2) {
				tp = tp.substr(tp.find("\""), tp.find_last_of("\""));
				std::erase(tp, '"');
				return tp;
			}
		}
		version_file.close();
	}
	return "NONE";
}

std::string GetSystemVersion() {
	return _GetSystemVersion();
}

extern "C" {
	const char* C_GetSystemUser() {
		return _GetSystemUser().c_str();
	}

	const char* C_GetSystemVersion() {
		return _GetSystemVersion().c_str();
	}
}
