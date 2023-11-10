#!/usr/bin/python3
import pathlib
import pygubu
import os
PROJECT_PATH = pathlib.Path(__file__).parent
PROJECT_UI = "/var/ui/iterkoczeos-version-gui.ui"


class IterkoczeosVersionGuiApp:
    def __init__(self, master=None):
        self.builder = builder = pygubu.Builder()
        builder.add_resource_path(PROJECT_PATH)
        builder.add_from_file(PROJECT_UI)
        # Main widget
        self.mainwindow = builder.get_object("toplevel1", master)

        self.ver = None
        builder.import_variables(self, ['ver'])

        self.verText = builder.get_object("verText", master)
        builder.connect_callbacks(self)

    def run(self):
        self.mainwindow.mainloop()


if __name__ == "__main__":
    app = IterkoczeosVersionGuiApp()
    app.ver.set(os.popen("iterkoczeos-version").read())
    app.run()
