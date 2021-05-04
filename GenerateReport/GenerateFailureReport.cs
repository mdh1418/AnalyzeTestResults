using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GenerateFailureReport
{
    class Program
    {
        static void Main(string[] args)
        {
            var runs = new List<String>() {
                "Android_x64_one",
                "Android_x86_one",
                "Android_x64_two",
                "Android_x86_two",
                "Android_arm64_two",
                "Android_x64_three",
                "Android_x86_three",
                "Android_arm64_three",
                "Android_x64_four",
                "Android_x86_four",
                "Android_arm64_four"
            };

            // Generate Master Failure List for each assembly
            //     Parse failures.txt in blocks of `*.dll` to \n\n
            //     cat same assemblies into <assembly>.log
            //     sort, uniq, clean
            //     Create List<String>
            Dictionary<String, HashSet<String>> assemblyFailuresDict = new Dictionary<String, HashSet<String>>(); 
            foreach (var file in Directory.EnumerateFiles("../resultsPNSEFailures", "*_failures.txt"))
            {
                String fileContents = File.ReadAllText(file).TrimStart('\n').TrimEnd('\n');
                String[] assemblyFailures = fileContents.Split("\n\n");
                
                foreach (var assemblyFailure in assemblyFailures)
                {
                    String[] assemblyFailureContents = assemblyFailure.Split("\n");
                    String assemblyName = assemblyFailureContents[0].Split(".dll")[0];
                    if (!assemblyFailuresDict.ContainsKey(assemblyName))
                        assemblyFailuresDict.Add(assemblyName, new HashSet<String>());
                    HashSet<String> priorContents = assemblyFailuresDict[assemblyName];
                    HashSet<String> contentsToAdd = new HashSet<String>();
                    foreach (String assemblyFailureMessage in assemblyFailureContents)
                    {
                        if (assemblyFailureMessage.Contains(".dll   Failed"))
                            continue;
                        contentsToAdd.Add(assemblyFailureMessage.Split('(')[0]);
                    }
                    priorContents.UnionWith(contentsToAdd);
                    assemblyFailuresDict[assemblyName] = priorContents;
                }
            }

            foreach (KeyValuePair<String, HashSet<String>> kvp in assemblyFailuresDict)
            {
                Console.WriteLine($"\n{kvp.Key}");
                foreach (String value in kvp.Value)
                    Console.WriteLine($"{value}");
            }
            // For each run, check for each failure in master list
            //     for each line in failures.txt
            //         see if List<String> contains, and 1 in that position if true

            // Combine test checks of all runs into sheet

            // foreach (var file in Directory.EnumerateFiles(".", "*_failures.txt"))
            // {
            //     var found = false;
            //     foreach (var line in File.ReadLines(file))
            //     {
            //         if (line == "System.Net.NetworkInformation.Tests.PingTest.SendPingWithHost")
            //         {
            //             found = true;
            //             break;
            //         }
            //     }
            //     Console.WriteLine($"{file}\n{found}");
            // }
        }
    }
}