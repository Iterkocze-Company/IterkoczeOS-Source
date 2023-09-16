#!/usr/bin/python3
import pathlib
import pygubu
import os
import libiterkoczeospy

PROJECT_PATH = pathlib.Path(__file__).parent
PROJECT_UI = PROJECT_PATH / "gretter.ui"


class GretterApp:
    def __init__(self, master=None):
        self.builder = builder = pygubu.Builder()
        builder.add_resource_path(PROJECT_PATH)
        builder.add_from_file(PROJECT_UI)
        # Main widget
        self.mainwindow = builder.get_object("toplevel1", master)
        builder.connect_callbacks(self)

    def run(self):
        if not os.path.exists(f"/home/{libiterkoczeospy.GetSystemUser()}/.firstboot"):
            exit(1)

        self.mainwindow.title("Welcome!")
        self.mainwindow.resizable(False, False)
        self.mainwindow.mainloop()

    def ok_clicked(event, e):
        os.remove(f"/home/{libiterkoczeospy.GetSystemUser()}/.firstboot")
        exit(0)


if __name__ == "__main__":
    app = GretterApp()
    app.run()
