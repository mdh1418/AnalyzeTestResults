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
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        // Set this value
        public static String jobID = "b503b87a-f8ac-4c88-bcd4-3d0759167207";

        // Setup http client and job url
        public static HttpClient client = new HttpClient();
        public static String jobURL = $"https://helix.dot.net/api/jobs/{jobID}/workitems?api-version=2019-06-17";

        // Setup Directories
        public static String jobDir = $"jobs/{jobID}/";                             // Holds the jsons of the job and workItems
        public static String workItemJSONDir = $"jobs/{jobID}/workItemJSONs/";      // Holds the jsons of all workItems
        public static String workItemTestResultsDir = $"testResults/{jobID}/";      // Holds the test results of all workItems
        public static String workItemFailedLogDir = $"failedConsoleLog/{jobID}/";   // Holds the console logs of workItem that crashed and didn't produce a test result
        public static String workItemConsoleLogDir = $"ConsoleLog/{jobID}/";        // Holds the console logs of all workItems


        static Task DoSomething(JobJson myJob){
            // For each JobJson with "Tests" in the name
            // We want to grab the console log
            // If the suite crashed/didn't produce test results, we want to save the console log as failed
            // We want to grab the test results
            var workItemJSON = $"{myJob.Name}.json";
            var pathToWorkItemJSON = $"{workItemJSONDir}{workItemJSON}";
            myJob.Url = $"https://helix.dot.net/api/jobs/{myJob.Job}/workitems/{myJob.Name}?api-version=2019-06-17";

            if (!File.Exists(pathToWorkItemJSON)){
                using (var myWebClient = new WebClient()){
                    myWebClient.DownloadFile(myJob.Url, pathToWorkItemJSON);
                }
                // HttpResponseMessage response = await client.GetAsync(myJob.Url);
                // using (var fs = new FileStream(pathToWorkItemJSON, FileMode.OpenOrCreate))
                //     await response.Content.CopyToAsync(fs);
            }

            String workItemJSONString = File.ReadAllText(pathToWorkItemJSON);

            String pattern = @"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)";
            Regex rgx = new Regex(pattern);
            MatchCollection matchList = rgx.Matches(workItemJSONString);

            var hasTestResults = workItemJSONString.Contains("testResults.xml");

            var workItemFileURL = "";
            var workItemFile = hasTestResults ? $"{myJob.Name}.xml" : $"{myJob.Name}.log";
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

            var workItemConsoleURL = "";
            var workItemConsoleLogFile = $"{myJob.Name}.log";
            var pathToWorkItemConsoleLog = $"{workItemConsoleLogDir}/{workItemConsoleLogFile}" ;

            foreach (Match match in matchList){
                if (match.Value.Contains("/console."))
                    workItemConsoleURL = match.Value;
            }

            if (!File.Exists(pathToWorkItemConsoleLog)){
                using (var myWebClient = new WebClient()){
                    myWebClient.DownloadFile(workItemConsoleURL, pathToWorkItemConsoleLog);
                }
            }
            return Task.CompletedTask;
        }

        static void SetupDirectories(){
            if (!Directory.Exists(jobDir))
                Directory.CreateDirectory(jobDir);
            if (!Directory.Exists(workItemJSONDir))
                Directory.CreateDirectory(workItemJSONDir);
            if (!Directory.Exists(workItemTestResultsDir))
                Directory.CreateDirectory(workItemTestResultsDir);
            if (!Directory.Exists(workItemFailedLogDir))
                Directory.CreateDirectory(workItemFailedLogDir);
            if (!Directory.Exists(workItemConsoleLogDir))
                Directory.CreateDirectory(workItemConsoleLogDir);
        }

        static void SummarizeResults(){
            // We want to summarize the test results
            //     Write the Total/Pass/Fail/Skip to textfile and .xlsx (TODO)

            var testResults = Directory.EnumerateFiles(workItemTestResultsDir, "*.xml");

            List<String> summary = new List<String>();
            foreach (var testResult in testResults)
            {
                XElement testResultXML = XElement.Load(testResult);

                IEnumerable<XElement> resultElements = from asm in testResultXML.Elements() select asm;
                foreach (var elem in resultElements) {
                    var name = elem.Attribute("name");
                    var total = elem.Attribute("total");
                    var passed = elem.Attribute("passed");
                    var failed = elem.Attribute("failed");
                    var skipped = elem.Attribute("skipped");
                    summary.Add($"\n{name!.Value}   Total: {total?.Value}  Passed: {passed?.Value}  Failed: {failed?.Value}  Skipped: {skipped?.Value}");
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
                    summary.Add($"    {descendant.Value}");
                }
            }
            File.WriteAllLines($"{jobID}_results.txt", summary);
        }

        static async Task Main(string[] args)
        {
            SetupDirectories();

            // Download job json
            var pathToJobJSON = $"{jobDir}{jobID}.json";
            if (!File.Exists(pathToJobJSON)){
                HttpResponseMessage response = await client.GetAsync(jobURL);
                using (var fs = new FileStream(pathToJobJSON, FileMode.OpenOrCreate))
                    await response.Content.CopyToAsync(fs);
            }

            String jsonArrayString = File.ReadAllText(pathToJobJSON);
            List<JobJson> jobJsonList = JsonSerializer.Deserialize<List<JobJson>>(jsonArrayString).Where(jobJson => jobJson.Name.Contains("Tests")).ToList();

            var tasks = new List<Task>();
            foreach (JobJson jobJson in jobJsonList)
                tasks.Add(Task.Run( () => { DoSomething(jobJson); } ));
            await Task.WhenAll(tasks);

            SummarizeResults();
        }
    }
}