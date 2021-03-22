// using System;
// using System.Diagnostics;

// var psi = new ProcessStartInfo { FileName = "/Users/mdhwang/runtime_wasm_clean/artifacts/bin/microsoft.netcore.app.runtime.browser-wasm/Release/runtimes/browser-wasm/native/cross/browser-wasm/mono-aot-cross" };
// var p = Process.Start(psi);
// p.WaitForExit();
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace AnalyzeTestResults
{
    public class JobJson
    {
        // "DetailsUrl":"https://helix.dot.net/api/jobs/56066ed8-ddff-4e8e-ae21-86e078da44e8/workitems/Common.Tests?api-version=2019-06-17",
        // "Job":"56066ed8-ddff-4e8e-ae21-86e078da44e8",
        // "Name":"Common.Tests",
        // "State":"Finished"
        public string Url { get; set; }
        public string Job { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var jobID = "88d2a49a-346c-4978-93df-72a6256e3d18";
            var jobURL = $"https://helix.dot.net/api/jobs/{jobID}/workitems?api-version=2019-06-17";
            var jobJSON = $"{jobID}.json";
            var jobDir = $"jobs/{jobID}/";
            var pathToJobJSON = $"{jobDir}{jobJSON}";

            var workItemTestResultsDir = $"testResults/{jobID}/";
            var workItemFailedLogDir = $"failedConsoleLog/{jobID}/";

            if (!File.Exists(pathToJobJSON)) {
                Directory.CreateDirectory(jobDir);
                using (var myWebClient = new WebClient()){
                    myWebClient.DownloadFile(jobURL, pathToJobJSON);
                }
            }

            if (!Directory.Exists(workItemTestResultsDir))
                Directory.CreateDirectory(workItemTestResultsDir);

            if (!Directory.Exists(workItemFailedLogDir))
                Directory.CreateDirectory(workItemFailedLogDir);

            var jsonArrayString = File.ReadAllText(pathToJobJSON);
            var jsonSeparators = new string[] { "[{", "},{", "}]" };
            var result = jsonArrayString.Split(jsonSeparators, StringSplitOptions.None);
            foreach (string s in result)
            {
                if (s.Length == 0)
                    continue;

                var workItem = JsonSerializer.Deserialize<JobJson>($"{{{s}}}");

                if (!workItem.Name.Contains("Tests"))
                    continue;

                workItem.Url = $"https://helix.dot.net/api/jobs/{workItem.Job}/workitems/{workItem.Name}?api-version=2019-06-17";

                var workItemJSON = $"{workItem.Name}.json";
                var workItemJSONDir = $"jobs/{jobID}/workItemJSONs/";
                var pathToWorkItemJSON = $"{workItemJSONDir}{workItemJSON}";

                if (!Directory.Exists(workItemJSONDir))
                    Directory.CreateDirectory(workItemJSONDir);
                
                if (!File.Exists(pathToWorkItemJSON)){
                    using (var myWebClient = new WebClient()){
                        myWebClient.DownloadFile(workItem.Url, pathToWorkItemJSON);
                    }
                }

                var workItemJSONString = File.ReadAllText(pathToWorkItemJSON);

                string pattern = @"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)";
                Regex rgx = new Regex(pattern);
                var matchList = rgx.Matches(workItemJSONString);

                var hasTestResults = workItemJSONString.Contains("testResults.xml");

                var workItemFileURL = "";
                var workItemFile = hasTestResults ? $"{workItem.Name}.xml" : $"{workItem.Name}.log";
                var pathToWorkItemTestResults = hasTestResults ? $"{workItemTestResultsDir}/{workItemFile}" : $"{workItemFailedLogDir}/{workItemFile}" ;

                var urlIdentifier = hasTestResults ? "testResults.xml" : "/console.";
                foreach (Match match in matchList){
                    if (match.Value.Contains(urlIdentifier))
                        workItemFileURL = match.Value;
                }

                if (!File.Exists(pathToWorkItemTestResults)){
                    using (var myWebClient = new WebClient()){
                        myWebClient.DownloadFile(workItemFileURL, pathToWorkItemTestResults);
                    }
                }
            }

            var testResults = Directory.EnumerateFiles(workItemTestResultsDir, "*.xml");
            
            foreach (var testResult in testResults)
            {
                XElement testResultXML = XElement.Load(testResult);

                IEnumerable<XElement> resultElements = from asm in testResultXML.Elements() select asm;
                foreach (var elem in resultElements)
                {
                    Console.WriteLine("\n{0}   Total: {1}  Passed: {2}  Failed: {3}  Skipped: {4}", elem.Attribute("name").Value, elem.Attribute("total").Value, elem.Attribute("passed").Value, elem.Attribute("failed").Value, elem.Attribute("skipped").Value);
                }
                var failureList = new List<string>();
                IEnumerable<XElement> descendants = from asm in testResultXML.Descendants("message") select asm;
                foreach (var descendant in descendants)
                {
                    string failure = descendant.Value;
                    if (failureList.Any(f => f.Contains(failure)))
                    {
                        continue;
                    }
                    failureList.Add(failure);
                    Console.WriteLine("    {0}", descendant.Value);
                }
            }
        }
    }
}