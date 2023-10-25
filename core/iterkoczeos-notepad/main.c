#include <gtk/gtk.h>
#include <stdlib.h>
#include <stdbool.h>
#include <string.h>

char* savePath = NULL;
GtkTextView* tv = NULL;

const gchar* get_text(void) {
    GtkTextIter start, end;
    GtkTextBuffer* buffer = gtk_text_view_get_buffer(tv);
    gchar* text;

    gtk_text_buffer_get_bounds(buffer, &start, &end);
    text = gtk_text_buffer_get_text(buffer, &start, &end, FALSE);

    return text;
}

void new_button_activate(GtkMenuItem* item, gpointer data) {
    printf(get_text());
    fflush(stdout);
}

void quit_button_activate(GtkMenuItem* item, gpointer data) {
    gtk_main_quit();
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
        FILE* file = fopen(filename, "w");
        fprintf(file, get_text());
        fflush(file);
        savePath = strdup(filename);
    }

    gtk_widget_destroy(dialog);
}

void save_activate(GtkMenuItem* item, gpointer data) { 
    if (savePath == NULL) {
        return;
    }

    FILE* file = fopen(savePath, "w");
    fprintf(file, get_text());
    fflush(file);
}

void delete_mw() {
    gtk_main_quit();
}

void tv_set_text_from_file(char* filename) {
    FILE* file = fopen(filename, "r");
    if (file == NULL) {
        printf("Can't open file %s\n", filename);
        return;
    }
    GtkTextBuffer* tbuffer = gtk_text_buffer_new(NULL);

    char* buffer;
    long lsize;

    fseek(file, 0L, SEEK_END);
    lsize = ftell(file);
    rewind(file);

    buffer = calloc(1, lsize+1);
    fread(buffer, lsize, 1, file);


    gtk_text_buffer_set_text(tbuffer, buffer, -1);
    gtk_text_view_set_buffer(tv, tbuffer);
    
    savePath = strdup(filename);
    free(buffer);
}

void open_activate(GtkMenuItem* item, gpointer data) {
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
        
        tv_set_text_from_file(filename);

        //fclose(filename); //coc
        
    }

    gtk_widget_destroy(dialog);
}



int main(int argc, char* argv[]) {
    gtk_init(&argc, &argv);
    GtkBuilder* builder = gtk_builder_new();
    gtk_builder_add_from_file(builder, "/var/ui/iterkocze-notepad.glade", NULL);

    tv = GTK_TEXT_VIEW(gtk_builder_get_object(builder, "textView"));
    GtkWidget* window = GTK_WIDGET(gtk_builder_get_object(builder, "main_window"));
    gtk_window_set_title(window, "Iterkocze Notepad");

    if (argc > 1) {
        tv_set_text_from_file(argv[1]);
    }

    gtk_builder_connect_signals(builder, NULL);
    g_object_unref(builder);
    gtk_widget_show_all(window);
    gtk_main();
    return 0;
}