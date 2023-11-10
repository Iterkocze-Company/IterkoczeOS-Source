import ctypes

lib = ctypes.CDLL("/usr/lib/libiterkoczeos.so")
_GetSystemUser = lib.GetSystemUser
_GetSystemUser.restype = ctypes.c_char_p

_GetSystemVersion = lib.GetSystemVersion
_GetSystemVersion.restype = ctypes.c_char_p

def GetSystemUser():
	return _GetSystemUser().decode('utf-8')

def GetSystemVersion():
	return _GetSystemVersion().decode('utf-8')
	
