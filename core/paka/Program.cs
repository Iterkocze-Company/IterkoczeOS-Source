using System;
using System.CommandLine;

class Program {
    static void Main(string[] args) {
        var rootCommand = new RootCommand("Iterkocze Paka 4");
        var testOption = new Option<int>(name: "--int");
        testOption.IsRequired = true;
        rootCommand.Add(testOption);

        rootCommand.SetHandler((testOption) => {
            System.Console.WriteLine(testOption);
        }, testOption);

        rootCommand.Invoke(args);
    }
}