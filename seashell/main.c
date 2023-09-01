#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <pwd.h>

#define MAX_COMMAND_LENGTH 100
#define MAX_ARGUMENTS 10
#define PATH_MAX_LENGTH 256
#define LOGIN_NAME_MAX 256

void read_command(char* command) {
    char cwd[PATH_MAX_LENGTH];

    if (getcwd(cwd, sizeof(cwd)) == NULL) {
        perror("[SeaShell] getcwd() can't get current directory");
    }
    uid_t uid = getuid();
    struct passwd *pw = getpwuid(uid);
    if (pw == NULL) {
        perror("[SeaShell] getpwuid() failed");
    }
    if (strcmp(pw->pw_name, "root") == 0) {
        printf("[%s] \033[1;31m%s\033[0m@SeaShell> ", cwd, pw->pw_name);
    } else {
        printf("[%s] %s@SeaShell> ", cwd, pw->pw_name);
    }
    fgets(command, MAX_COMMAND_LENGTH, stdin);
    command[strcspn(command, "\n")] = '\0';  
}

int tokenize_command(char* command, char** arguments) {
    int count = 0;
    char* resolved_arguments[MAX_ARGUMENTS];
    char* token = strtok(command, " ");
    
    while (token != NULL && count < MAX_ARGUMENTS) {
        if (token[0] == '$') {
            // It's an environmental variable token
            char* variable_name = &token[1];  // Skip the '$' symbol
            char* variable_value = getenv(variable_name);
            if (variable_value != NULL) {
                resolved_arguments[count] = variable_value;
            } else {
                printf("[SeaShell] Environmental variable '%s' not found\n", variable_name);
                return -1;
            }
        } else {
            // If it doesn't begin with '$', means it's a regular token
            resolved_arguments[count] = token;
        }

        token = strtok(NULL, " ");
        count++;
    }
    resolved_arguments[count] = NULL; //Terminate the array

    // Copy resolved arguments to the output array
    for (int i = 0; i <= count; i++) {
        arguments[i] = resolved_arguments[i];
    }

    return count;
}

void execute_command(char** arguments, int num_arguments) {
    if (strcmp(arguments[0], "cd") == 0) {
        if (num_arguments != 2) {
            printf("[SeaShell] cd: Expected exactly one argument\n");
            return;
        }

        if (chdir(arguments[1]) != 0) {
            perror("[SeaShell] cd failed");
        }
    } else {
        pid_t pid = fork();

        if (pid < 0) {
            perror("[SeaShell] Fork failed");
            exit(1);
        } else if (pid == 0) {
            if (execvp(arguments[0], arguments) < 0) {
                perror("[SeaShell] Command execution failed");
                exit(1);
            }
        } else {
            signal(SIGCHLD, SIG_IGN);  // Ignore child termination signal
            waitpid(pid, NULL, 0);  
        }
    }
}

int main() {
    char command[MAX_COMMAND_LENGTH];
    char* arguments[MAX_ARGUMENTS];
    
    while (1) {
        read_command(command);
        
        if (strcmp(command, "exit") == 0) {
            break;
        }
        
        int num_arguments = tokenize_command(command, arguments);
        
        if (num_arguments > 0) {
            execute_command(arguments, num_arguments);
        }
    }
    
    return 0;
}
