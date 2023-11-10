#!/usr/bin/python3

with open("/etc/os-release") as file:
	for line in file:
		if line.startswith("VERSION="):
			print(line.replace("\"", "").replace("VERSION=", ""), end='')
