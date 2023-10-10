#include <gtk/gtk.h>
#include <stdlib.h>

const gchar* get_text(gpointer data) {
    GtkTextIter start, end;
    GtkTextView* v = GTK_TEXT_VIEW(data);
    GtkTextBuffer* buffer = gtk_text_view_get_buffer(v);
    gchar* text;

    gtk_text_buffer_get_bounds(buffer, &start, &end);
    text = gtk_text_buffer_get_text(buffer, &start, &end, FALSE);

    return text;
}

void new_button_activate(GtkMenuItem* item, gpointer data) {
    printf(get_text(data));
    fflush(stdout);
}

void quit_button_activate(GtkMenuItem* item, gpointer data) {
    exit(0);
}

void save_as_activate(GtkMenuItem* item, gpointer data) {
    GtkWidget* dialog;
    GtkFileChooserAction action = GTK_FILE_CHOOSER_ACTION_OPEN;
    gint res;

    dialog = gtk_file_chooser_dialog_new(
        "Save file",
        GTK_WINDOW(gtk_widget_get_toplevel(GTK_WINDOW(item))),
        action,
        "Cancel",
        GTK_RESPONSE_CANCEL,
        "Open",
        GTK_RESPONSE_ACCEPT,
        NULL
    );

    res = gtk_dialog_run(GTK_DIALOG(dialog));

    if (res == GTK_RESPONSE_ACCEPT) {
        char* filename;
        GtkFileChooser* chooser = GTK_FILE_CHOOSER(dialog);
        filename = gtk_file_chooser_get_filename(chooser);
        printf(filename);
        fflush(stdout);
    }


    gtk_widget_destroy(dialog);
}

int main(int argc, char* argv[]) {
    gtk_init(&argc, &argv);
    GtkBuilder* builder = gtk_builder_new();
    gtk_builder_add_from_file(builder, "iterkoczeos-notepad.glade", NULL);

    GtkWidget* window = GTK_WIDGET(gtk_builder_get_object(builder, "main_window"));

    gtk_builder_connect_signals(builder, NULL);
    g_object_unref(builder);
    gtk_widget_show_all(window);
    gtk_main();
    return 0;
}