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
            {"46ae0da4-f98e-4e11-a534-5bb78297813e", "Android_arm64_four"},
            {"b503b87a-f8ac-4c88-bcd4-3d0759167207", "iOSSimulator_x64_one"},
            {"6da38d29-9225-4433-abba-f840975798db", "tvOSSimulator_x64_one"},
            {"7957b651-33d7-4c0c-bde8-0fdb2f29e472", "tvOSSimulator_x64_two"},
            {"376f0e06-a08e-4625-85d4-b9cf9f01f718", "iOSSimulator_x64_three"},
            {"130836dd-4258-407b-8f51-a8929ca3cb38", "tvOSSimulator_x64_three"},
            {"bad2cd8c-84fa-4c39-be5c-98b44a8b9d27", "Android_x64_five"},
            {"7d2d2c49-0fe6-4e31-8acc-eb17e07f6e37", "Android_x86_five"},
            {"2f6cfb1e-63e7-43e4-b852-68c1027484e1", "Android_arm64_five"},
            {"20b63134-8718-43db-962b-6f7e4576e764", "Android_x64_disabled_six"},
            {"278d68ea-cd17-426b-baf1-22a24fea07dd", "Android_x86_disabled_six"},
            {"8f38f687-3877-4631-8004-48b7be92b195", "Android_x64_enabled_six"},
            {"4d52b6ac-d550-4072-967d-a140856d354a", "Android_x86_enabled_six"},
            {"8deb30c3-8481-4212-be3a-9953581b6205", "Android_arm64_enabled_six"},
            {"54fc05a3-c454-49ac-8f42-13efbc19d504", "Android_x64_disabled_seven"},
            {"1a542ef3-f1eb-4d4f-84e1-541dbc263e1d", "Android_x86_disabled_seven"},
            {"5f922f32-1392-4b96-9450-cab6063d7336", "Android_arm64_disabled_seven"},
            {"4c42b89d-c163-4716-8ecb-de173564883a", "Android_x64_disabled_eight"},
            {"85389c43-acbf-48ac-985a-097a36512cac", "Android_x86_disabled_eight"},
            {"8340aa76-feb8-491c-8222-3733b29b9a34", "Android_arm64_disabled_eight"},
            {"8bd2deac-e94f-4e23-94e9-c60af730b4ae", "Android_x64_disabled_nine"},
            {"f0aa0fad-c63b-44a5-89b3-33eaa2b29fcb", "Android_x86_disabled_nine"},
            {"a86ff6d6-3d82-45e5-9d4a-227c66d4ce2b", "Android_arm64_disabled_nine"},
            {"16f05234-cb06-40e2-97e6-d5c8b1aefd8e", "iOSSimulator_x64_four"},
            {"e4cb02e0-1e59-490b-88e1-9392c5bc63d9", "Android_arm64_disabled_ten"},
            {"c7ff4bc0-70b2-4b2f-982b-9726b20e53b7", "iOSSimulator_x64_disabled_five"},
            {"29359cf7-6a14-4859-ad01-bf8791259f4e", "tvOSSimulator_x64_disabled_six"},
            {"09599c1c-a6ac-4121-8524-18a8c486dd7d", "tvOSSimulator_x64_disabled_seven"},
            {"b2e39f2c-dc21-455a-8922-53d2387f4f63", "iOSSimulator_x64_disabled_seven"},
            {"d77acc25-27b9-4f5f-be27-84712c194dc4", "tvOSSimulator_x64_enabled_seven"},
            {"6bd108c8-416f-48b0-94d2-45035d74f5f9", "iOSSimulator_x64_enabled_seven"},
            {"148ee239-9221-46a1-b586-eb7410bc8a25", "tvOSSimulator_x64_disabled_eight"},
            {"994613f9-485a-431b-b8ef-d3afce0b2098", "iOSSimulator_x64_disabled_eight"},
            {"bede7feb-dce1-4ad0-ae3c-a0263c37ae6e", "tvOSSimulator_x64_enabled_eight"},
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
            jobURL = $"https://helix.dot.net/api/jobs/{jobID}/workitems?api-version=2019-06-17";
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
                    Console.WriteLine($"Downloading {myJob.Url} to {pathToWorkItemJSON}");
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

        static void CaptureLocalFailures(string targetDir){
            var testResults = Directory.EnumerateFiles(targetDir, "*.xml");

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
            File.WriteAllLines($"localRuns/local_failures.txt", summary);
        }

        static void CaptureLocalFullFailures(string targetDir){
            var testResults = Directory.EnumerateFiles(targetDir, "*.xml");

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
            File.WriteAllLines($"localRuns/local_full_failures.txt", summary);
        }

        static void CaptureLocalPNSEFailures(string targetDir){
            var testResults = Directory.EnumerateFiles(targetDir, "*.xml");

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
            File.WriteAllLines($"localRuns/local_pnse_failures.txt", summary);
        }

        static async Task Main(string[] args)
        {
            var jobIDList = new List<String>() {}; //"148ee239-9221-46a1-b586-eb7410bc8a25", "994613f9-485a-431b-b8ef-d3afce0b2098", "bede7feb-dce1-4ad0-ae3c-a0263c37ae6e"};

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
            CaptureLocalFailures("localRuns/");
            CaptureLocalFullFailures("localRuns/");
            CaptureLocalPNSEFailures("localRuns/");

        }
    }
}