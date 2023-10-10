import os

print("Iterkocze Kernelworks 1.0.0")

if os.getuid() != 0:
    print("Must be root")
    exit(1)

