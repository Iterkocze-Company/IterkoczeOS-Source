package main

import (
    "fmt"
    "bufio"
    "os"
    "os/exec"
    "strings"
    "time"
)

func parseVersionFile(filename string) (map[string]string, error) {
	file, err := os.Open(filename)
    if err != nil {
        return nil, err
    }
    defer file.Close()
    
    scanner := bufio.NewScanner(file)
    data := make(map[string]string)

    for scanner.Scan() {
        line := scanner.Text()
        parts := strings.Split(line, "=")
        if len(parts) == 2 {
            key := parts[0]
            value := parts[1]
            data[key] = value
        }
    }
    if err := scanner.Err(); err != nil {
        return nil, err
    }

    return data, nil
}

func checkUpdates() {
	err := exec.Command("wget", "-O", "/tmp/version.remote", "https://iterkoczeos.xlx.pl/storage/version").Run()
	if err != nil {
		fmt.Println("Error:", err)
		return
    }

    remoteVersion, err := parseVersionFile("/tmp/version.remote")
    if err != nil {
        fmt.Println("[ITos-update-srv] Error reading file:", err)
        return
    }
    localVer, err := parseVersionFile("/var/paka/version")
    if err != nil {
        fmt.Println("[ITos-update-srv] Error reading file:", err)
        return
    }
    if remoteVersion["PAKA"] != localVer["PAKA"] {
		exec.Command("doas", "-u", "1000", "notify-send", "-t", "0", "IterkoczeOS Update Service", "There is a new update available. Run 'paka --update' as root", "--action=OK").Start()
	}
	if remoteVersion["FORMULA"] != localVer["FORMULA"] {
		exec.Command("doas", "-u", "1000", "notify-send", "-t", "0", "IterkoczeOS Update Service", "Remote repository has new formula files. Run 'paka --sync' as root", "--action=OK").Start()
	}

}

func main() {
	args := os.Args
	if len(args) > 1 && args[1] == "--check-now" {
		fmt.Println("Checking for new updates...")
		checkUpdates()
		fmt.Println("Done")
		os.Exit(0)
	}
	for {
		time.Sleep(3 * time.Hour)
		checkUpdates()
	}
}
