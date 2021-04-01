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
        public static Dictionary<string, string> jobIDtoNameDict = new Dictionary<string, string>(){
            {"c05cc25d-1ac2-4530-88ba-2c0dbfeca4b0", "Android_x64_one"},
            {"2b778a20-b0b8-4934-b7ad-abef90ed1f93", "Android_x86_one"},
            {"f3eb11fa-06ee-49dc-92b7-d9f04c27bdd6", "Android_x64_two"},
            {"c914cd95-f6bc-4b1a-a303-957401bd95d9", "Android_x86_two"},
            {"0afea0eb-f6fb-4ca8-935b-6ba4373c4ce8", "Android_arm64_two"},
            {"3a7789fa-1fae-456b-b819-6e024d6c01af", "Android_x64_three"},
            {"d28fda8e-107e-4423-9db2-a8297ef049c0", "Android_x86_three"},
            {"9e04cdc2-0499-4e48-b595-61d47c64f794", "Android_arm64_three"},
            {"33934d9c-821a-4407-90de-b264cffc779c", "Android_x64_four"},
            {"d331d0b8-b913-4432-be93-14968baf5fd0", "Android_x86_four"},
            {"46ae0da4-f98e-4e11-a534-5bb78297813e", "Android_arm64_four"}
        };
        // Set this value
        public static String jobID = "33934d9c-821a-4407-90de-b264cffc779c";

        // Setup http client and job url
        public static HttpClient client = new HttpClient();
        public static String jobURL = $"https://helix.dot.net/api/jobs/{jobID}/workitems?api-version=2019-06-17";

        // Setup Directories
        public static String jobDir = $"jobs/{jobIDtoNameDict[jobID]}/";                             // Holds the jsons of the job and workItems
        public static String workItemJSONDir = $"jobs/{jobIDtoNameDict[jobID]}/workItemJSONs/";      // Holds the jsons of all workItems
        public static String workItemTestResultsDir = $"testResults/{jobIDtoNameDict[jobID]}/";      // Holds the test results of all workItems
        public static String workItemFailedLogDir = $"failedConsoleLog/{jobIDtoNameDict[jobID]}/";   // Holds the console logs of workItem that crashed and didn't produce a test result
        public static String workItemConsoleLogDir = $"ConsoleLog/{jobIDtoNameDict[jobID]}/";        // Holds the console logs of all workItems

        static void UpdateValues(String newID){
            jobID = newID;
            jobURL = $"https://helix.dot.net/api/jobs/{jobIDtoNameDict[jobID]}/workitems?api-version=2019-06-17";
            jobDir = $"jobs/{jobIDtoNameDict[jobID]}/";
            workItemJSONDir = $"jobs/{jobIDtoNameDict[jobID]}/workItemJSONs/";
            workItemTestResultsDir = $"testResults/{jobIDtoNameDict[jobID]}/";
            workItemFailedLogDir = $"failedConsoleLog/{jobIDtoNameDict[jobID]}/";
            workItemConsoleLogDir = $"ConsoleLog/{jobIDtoNameDict[jobID]}/";
        }

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
            if (!Directory.Exists("resultsSummary"))
                Directory.CreateDirectory("resultsSummary");
            if (!Directory.Exists("resultsFailures"))
                Directory.CreateDirectory("resultsFailures");
            if (!Directory.Exists("resultsFullFailures"))
                Directory.CreateDirectory("resultsFullFailures");
            if (!Directory.Exists("resultsPNSEFailures"))
                Directory.CreateDirectory("resultsPNSEFailures");
            if (!Directory.Exists("resultsSkips"))
                Directory.CreateDirectory("resultsSkips");
        }

        static void SummarizeResults(){
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
            File.WriteAllLines($"resultsSummary/{jobIDtoNameDict[jobID]}_results.txt", summary);
        }

        static void CaptureFailures(){
            var testResults = Directory.EnumerateFiles(workItemTestResultsDir, "*.xml");

            List<String> summary = new List<String>();
            foreach (var testResult in testResults)
            {
                XElement testResultXML = XElement.Load(testResult);

                IEnumerable<XElement> resultElements = testResultXML.Elements();
                foreach (var elem in resultElements) {
                    var name = elem.Attribute("name");
                    var failed = elem.Attribute("failed");
                    if (failed?.Value == "0")
                        continue;
                    summary.Add($"\n{name!.Value}   Failed: {failed?.Value}");
                }

                IEnumerable<XElement> descendants = testResultXML.Descendants("collection");
                foreach (var descendant in descendants)
                {
                    var result = descendant.Attribute("failed");
                    if (result.Value != "0"){
                        // summary.Add(descendant.Attribute("name").Value);
                        IEnumerable<XElement> tests = descendant.Descendants("test");
                        foreach (var test in tests){
                            var myResult = test.Attribute("result");
                            if (myResult.Value == "Fail")
                                summary.Add($"{test.Attribute("name").Value}");
                        }
                    }
                }
            }
            File.WriteAllLines($"resultsFailures/{jobIDtoNameDict[jobID]}_failures.txt", summary);
        }

        static void CaptureFullFailures(){
            var testResults = Directory.EnumerateFiles(workItemTestResultsDir, "*.xml");

            List<String> summary = new List<String>();
            foreach (var testResult in testResults)
            {
                XElement testResultXML = XElement.Load(testResult);

                IEnumerable<XElement> resultElements = testResultXML.Elements();
                foreach (var elem in resultElements) {
                    var name = elem.Attribute("name");
                    var failed = elem.Attribute("failed");
                    if (failed?.Value == "0")
                        continue;
                    summary.Add($"\n\n{name!.Value}   Failed: {failed?.Value}");
                }

                IEnumerable<XElement> descendants = testResultXML.Descendants("collection");
                foreach (var descendant in descendants)
                {
                    var result = descendant.Attribute("failed");
                    if (result.Value != "0"){
                        summary.Add($"\n{descendant.Attribute("name").Value}");
                        IEnumerable<XElement> tests = descendant.Descendants("test");
                        foreach (var test in tests){
                            var myResult = test.Attribute("result");
                            if (myResult.Value == "Fail")
                                summary.Add($"{test.Attribute("name").Value}");
                            IEnumerable<XElement> messages = test.Descendants("message");
                            foreach (var message in messages){
                                if (message != null)
                                    summary.Add($"    {message.Value}");
                            }
                        }
                    }
                }
            }
            File.WriteAllLines($"resultsFullFailures/{jobIDtoNameDict[jobID]}_full_failures.txt", summary);
        }

        static void CapturePNSEFailures(){
            var testResults = Directory.EnumerateFiles(workItemTestResultsDir, "*.xml");

            List<String> summary = new List<String>();
            foreach (var testResult in testResults)
            {
                XElement testResultXML = XElement.Load(testResult);

                IEnumerable<XElement> resultElements = testResultXML.Elements();
                foreach (var elem in resultElements) {
                    var name = elem.Attribute("name");
                    var failed = elem.Attribute("failed");
                    if (failed?.Value == "0")
                        continue;
                    summary.Add($"\n{name!.Value}   Failed: {failed?.Value}");
                }

                IEnumerable<XElement> descendants = testResultXML.Descendants("collection");
                foreach (var descendant in descendants)
                {
                    var result = descendant.Attribute("failed");
                    if (result.Value != "0"){
                        IEnumerable<XElement> tests = descendant.Descendants("test");
                        foreach (var test in tests){
                            var myResult = test.Attribute("result");
                            IEnumerable<XElement> messages = test.Descendants("message");
                            foreach (var message in messages){
                                if (message != null && message.Value.Contains("PlatformNotSupportedException"))
                                    summary.Add($"{test.Attribute("name").Value}");
                            }
                        }
                    }
                }
            }
            File.WriteAllLines($"resultsPNSEFailures/{jobIDtoNameDict[jobID]}_PNSE_failures.txt", summary);
        }

        static void CaptureSkips(){
            var testResults = Directory.EnumerateFiles(workItemTestResultsDir, "*.xml");

            List<String> summary = new List<String>();
            foreach (var testResult in testResults)
            {
                XElement testResultXML = XElement.Load(testResult);

                IEnumerable<XElement> resultElements = testResultXML.Elements();
                foreach (var elem in resultElements) {
                    var name = elem.Attribute("name");
                    var skipped = elem.Attribute("skipped");
                    if (skipped?.Value == "0")
                        continue;
                    summary.Add($"\n\n{name!.Value}   Skipped: {skipped?.Value}");
                }

                IEnumerable<XElement> descendants = testResultXML.Descendants("collection");
                foreach (var descendant in descendants)
                {
                    var result = descendant.Attribute("skipped");
                    if (result.Value != "0"){
                        IEnumerable<XElement> tests = descendant.Descendants("test");
                        foreach (var test in tests){
                            var myResult = test.Attribute("result");
                            if (myResult.Value == "Skip")
                                summary.Add($"{test.Attribute("name").Value}");
                            IEnumerable<XElement> reasons = test.Descendants("reason");
                            foreach (var reason in reasons){
                                if (reason != null)
                                    summary.Add($"    {reason.Value}");
                            }
                        }
                    }
                }
            }
            File.WriteAllLines($"resultsSkips/{jobIDtoNameDict[jobID]}_skipped.txt", summary);
        }

        static async Task Main(string[] args)
        {
            var jobIDList = new List<String>() {"c05cc25d-1ac2-4530-88ba-2c0dbfeca4b0",
                                                "2b778a20-b0b8-4934-b7ad-abef90ed1f93",
                                                "f3eb11fa-06ee-49dc-92b7-d9f04c27bdd6",
                                                "c914cd95-f6bc-4b1a-a303-957401bd95d9",
                                                "0afea0eb-f6fb-4ca8-935b-6ba4373c4ce8",
                                                "3a7789fa-1fae-456b-b819-6e024d6c01af",
                                                "d28fda8e-107e-4423-9db2-a8297ef049c0",
                                                "9e04cdc2-0499-4e48-b595-61d47c64f794",
                                                "33934d9c-821a-4407-90de-b264cffc779c",
                                                "d331d0b8-b913-4432-be93-14968baf5fd0",
                                                "46ae0da4-f98e-4e11-a534-5bb78297813e"};
            foreach (String thisJobID in jobIDList){
                UpdateValues(thisJobID);

                SetupDirectories();

                // Download job json
                var pathToJobJSON = $"{jobDir}{jobIDtoNameDict[jobID]}.json";
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

                CaptureFailures();

                CaptureFullFailures();

                CapturePNSEFailures();

                CaptureSkips();
            }
        }
    }
}