import os
from Colors import *

def is_internet_on():
	from urllib import request
	
	try:
		request.urlopen('https://gentoo.org', timeout=1)
		return True
	except request.URLError as err: 
		return False

def cprint(msg, color):
	print(color + msg + bcolors.ENDC)

def init_direct_file():
	if not os.path.exists('/var/paka/direct'):
		os.mknod('/var/paka/direct')

def register_package_as_directly_installed(pkg_name):
	with open('/var/paka/direct') as t:
		for line in t:
			if (line.startswith(pkg_name)):
				return
	
	with open('/var/paka/direct', 'a') as f:
		f.write(pkg_name + "\n")
		
def unregister_package(pkg_name):
	with open("/var/paka/direct", "r") as f:
		lines = f.readlines()
	with open("/var/paka/direct", "w") as f:
		for line in lines:
			if line.strip("\n") != pkg_name:
				f.write(line)
		
def get_directly_installed():
	ret = []
	with open('/var/paka/direct') as t:
		for line in t:
			ret.append(line.strip())
	return ret
	
def get_all_installed_packages():
	return os.listdir("/var/paka/db")
	
def get_all_formula_files():
	return os.listdir("/var/paka/formula")
	
def is_directly_installed(pkg_name):
	if (pkg_name in get_directly_installed()):
		return True
	else:
		return False
		
def is_package_installed(pkg_name):
	for ffile in os.listdir("/var/paka/db"):
		if (ffile == pkg_name.strip()):
			return True
	return False
    
def get_config_string(formula_file:str, conf_str:str):
	for line in get_formula_file(formula_file):
		line = line.replace("\n","")
		if (line.startswith(conf_str)):
			return line.replace(conf_str, "").replace("=", "", 1).replace("\"", "")
			
def get_config_switch(formula_file:str, conf_str:str):
	for line in get_formula_file(formula_file):
		line = line.replace("\n","")
		if (line.startswith(conf_str)):
			return True
	return False
 
# Returns a file object to the corresponding formula file
def get_formula_file(name:str):
	try:
		formula_file = open("/var/paka/formula/{}.formula".format(name))
	except:
		cprint("ERROR! formula file for {} not found!".format(name), bcolors.FAIL)
		exit()
	return formula_file
	
def get_package_desc(name:str):
	return get_config_string(name, "DESC")
	
def get_package_name(pkg_name):
	return get_config_string(pkg_name, "NAME")
	
def get_package_dependencies(package):
	ret = []
	for dep in get_config_string(package, "DEPENDSON").split(","):
		if (dep != ''):
			ret.append(dep)
	return ret
	
def get_all_files(path):
	ret = []
	for path, subdirs, files in os.walk(path):
		for name in files:
			ret.append(os.path.join(path, name))
	return ret
		
def write_db_file(package_name, file_name, content_list):
	file_path = "/var/paka/db/{}/{}".format(package_name, file_name)

	with open(file_path, "a") as f:
		for line in content_list:
			f.write(line + "\n")
			
def read_db_file(package_name, file_name):
	lines = []
	with open("/var/paka/db/{}/{}".format(package_name, file_name), "r") as file:
		lines = [line.rstrip() for line in file]
	return lines
