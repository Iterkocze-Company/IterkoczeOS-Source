from Functions import get_package_dependencies, get_formula_file
from typing import Dict

class Formula:
	Properties:Dict[str,str] = {}
	Switches:Dict[str,bool] = {"HOLD":False, "AUTO_REMOVE":False}
	Dependson = []
	InstallCmd = ""
	RemoveCmd = ""
	
	def __init__(self, name):
		self.Dependson = get_package_dependencies(name)
		for _line in get_formula_file(name).readlines():
			line = _line.strip()
			if (line.startswith("SRC_URL")): #OMG WTF :(( The toplevel package gets replaced by the last dependency SRC_URL. HOW
				self.Properties["SRC_URL"] = line.replace("SRC_URL", "").replace("=", "", 1).replace("\"", "")
			if (line.startswith("NAME")):
				self.Properties["NAME"] = line.replace("NAME", "").replace("=", "", 1).replace("\"", "")
			if (line.startswith("HOLD")):
				self.Switches["HOLD"] = True
			if (line.startswith("AUTO_REMOVE")):
				self.Switches["AUTO_REMOVE"] = True
				
		content = get_formula_file(name).read()
		begin_index = content.index("BEGIN INSTALL") + len("BEGIN") + 8
		end_index = content.index("END INSTALL")
		self.InstallCmd = content[begin_index:end_index].strip() 
		
		begin_index = content.index("BEGIN REMOVE") + len("BEGIN") + 8
		end_index = content.index("END REMOVE")
		self.RemoveCmd = content[begin_index:end_index].strip() 
