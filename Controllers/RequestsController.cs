using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InsuranceApplicationMVC.Models.InsuranceModels;
using CamundaClient;
using NuGet.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections;
using CamundaClient.Dto;
using static System.Net.WebRequestMethods;
using Azure;
using System.Xml;
using System.Text;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;

namespace InsuranceApplicationMVC.Controllers
{
    public class RequestsController : Controller
    {
        private readonly InsuranceDBContext _context;

        public RequestsController(InsuranceDBContext context)
        {
            _context = context;
        }

        // GET: Requests
        public async Task<IActionResult> Index()
        {
              return _context.Requests != null ? 
                          View(await _context.Requests.ToListAsync()) :
                          Problem("Entity set 'InsuranceDBContext.Requests'  is null.");
        }

        // GET: Requests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Requests == null)
            {
                return NotFound();
            }

            var request = await _context.Requests
                .FirstOrDefaultAsync(m => m.id == id);
            if (request == null)
            {
               
                return NotFound();
            }

            return View(request);
        }
        // // ////////////////////////////
        // GET: Requests
        public async Task<IActionResult> PendingApplications()
        {
            var allRequests = await _context.Requests.ToListAsync();
            List <Request> requests = new List <Request>();  

            foreach (var request in allRequests)
            {
                if (request.riskAssessment != null)
                {

                    if (request.riskAssessment.Equals("yellow") & request.reqResult=="") { requests.Add(request); }
                }
            }
            return View(requests);

        }

       
        // ////////

        // GET: Requests/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Requests/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id,name,age,email,carManufacturer,carType")] Request request)
        {
            if (ModelState.IsValid)
            {
                _context.Add(request);
                await _context.SaveChangesAsync(); 
                var camunda = new CamundaEngineClient(new System.Uri("http://localhost:8080/engine-rest/engine/default/"), null, null);
                string processInstanceId = camunda.BpmnWorkflowService.StartProcessInstance("insuranceApplication", new Dictionary<string, object>()
            {
                {"name", request.name},
                {"age", request.age},
                {"email", request.email},
                {"carManufacturer", request.carManufacturer},
                {"carType", request.carType},
                {"decision","" }

            });
                request.processInstanceId= processInstanceId;
                request.reqResult = "";
                await _context.SaveChangesAsync();
                var tasks = camunda.HumanTaskService.LoadTasks(new Dictionary<string, string>() {
                    { "processInstanceId", processInstanceId }
                });
                HttpClient httpClient=new HttpClient();
                string requestUrl = $"{"http://localhost:8080/engine-rest"}/process-instance/{processInstanceId}/variables/age";
                HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var variable = await response.Content.ReadFromJsonAsync<CamundaClient.Dto.Variable>();
                    var variableValue = variable.Value.ToString();
                    
                    Console.WriteLine($"Variable value is  {variableValue}");
                }
                else
                {
                    Console.WriteLine($"Failed to retrieve variable ''. Status code: {response.StatusCode}");
                }
               // Console.WriteLine("c'est la variable ;;;;"+variable.Value.ToString());  
                var var = camunda.BpmnWorkflowService.LoadVariables(tasks[0].Id);
                foreach (CamundaClient.Dto.HumanTask task in tasks)
                {
                    Console.WriteLine(task.Name);
                }
                foreach (string key in var.Keys)
                {
                    Console.WriteLine(key);
                }
                if (tasks.Count() >= 0)
                {
                    var task = tasks[0];
                    var variables = camunda.BpmnWorkflowService.LoadVariables(tasks[0].Id);
                    
                    Console.WriteLine(processInstanceId);
                    
                    var assessment = (string)JObject.Parse(variables["risk"].ToJson())["assessment"];
                    var description = (string)JObject.Parse(variables["risk"].ToJson())["description"];
                    request.riskAssessment = assessment;
                    request.riskDescription = description;
                    await _context.SaveChangesAsync();

                }
                return RedirectToAction("Index","Home");
            }
            return View(request);
        }
        public async Task<IActionResult> DecideAboutApplication(int? id)
        {
            if (id == null || _context.Requests == null)
            {
                return NotFound();
            }

            var request = await _context.Requests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }
            return View(request);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecideAboutApplication(int id, [Bind("id,name,age,email,carManufacturer,carType,riskAssessment,riskDescription,reqResult,processInstanceId")] Request request)
        {
            if (id != request.id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(request);
                    await _context.SaveChangesAsync();
                    HttpClient httpClient = new HttpClient();
                    string camundaEngineUrl = "http://localhost:8080/engine-rest"; // Replace with your Camunda engine URL
                    string processInstanceId = request.processInstanceId; // Replace with the actual process instance ID
                    string taskDefinitionKey = "userTaskAntragEntscheiden"; // Replace with the actual task definition key
                    string requestUrl = $"{camundaEngineUrl}/task?processInstanceId={processInstanceId}&taskDefinitionKey={taskDefinitionKey}";
                    var payload = new
                    {
                        value = request.reqResult,
                        type = "String"
                    };
                    string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                    var content1 = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    HttpResponseMessage response2 = await httpClient.PutAsync($"{camundaEngineUrl}/process-instance/{processInstanceId}/variables/decision", content1);
                    Console.WriteLine(response2.StatusCode);
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                       
                        var tasks = await response.Content.ReadAsStreamAsync();
                        using var streamReader = new StreamReader(tasks);
                        using var jsonReader = new JsonTextReader(streamReader);
                        JsonSerializer serializer = new JsonSerializer();
                        var t= serializer.Deserialize<CamundaClient.Dto.HumanTask[]>(jsonReader);
                        if (t.Length > 0)
                        {
                            var task = t[0];
                            var username = "demo";
                            var password = "demo";
                            string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                                                           .GetBytes(username + ":" + password));
                            string claimUrl = $"{camundaEngineUrl}/task/{task.Id}/claim";
                            // Send a POST request to claim the task
                            var user = new { userId = "demo" };
                            string json = JsonConvert.SerializeObject(user);
                            var content = new StringContent(json, Encoding.UTF8, "application/json");
                            Console.WriteLine(content.GetType());
                            HttpResponseMessage claim = await httpClient.PostAsync(claimUrl, content);
                            claim.Headers.Add("Authorization", "Basic " + encoded);
                            Console.WriteLine("claim : "+claim.StatusCode);
                            var vars = new Dictionary<string, object>{
                                       { "decision", request.reqResult },
                                       };
                            string json1 = JsonConvert.SerializeObject(vars);
                            var variables = new StringContent(json1, Encoding.UTF8, "application/json");                        
                            HttpResponseMessage response3 = await httpClient.PostAsync($"{camundaEngineUrl}/task/{task.Id}/complete", variables);                          
                            Console.WriteLine("voir ici : "+ response3.Content.ReadAsStringAsync().Result);

                        }
                        else
                        {
                            Console.WriteLine("No tasks found for the given process instance ID and task definition key.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to retrieve task. Status code: {response.StatusCode}");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RequestExists(request.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
           
            return RedirectToAction("PendingApplications", "Requests"); ;
            }
            return View(request);
        }
      
        [HttpGet]
        public async Task<IActionResult> ViewResult(int? id)
        {
            if (id == null || _context.Requests == null)
            {
                return NotFound();
            }

            var request = await _context.Requests
                .FirstOrDefaultAsync(m => m.id == id & (m.reqResult.Equals("approve") | m.reqResult.Equals("reject")| m.riskAssessment.Equals("green")));
            HttpClient httpClient = new HttpClient();

            string camundaEngineUrl = "http://localhost:8080/engine-rest"; // Replace with your Camunda engine URL
            if (request.processInstanceId != null)
            {
                string processInstanceId = request.processInstanceId;
                // Replace with the actual process instance ID
                string taskDefinitionKey; 
                if (request.reqResult.Equals("approve") || request.riskAssessment.Equals("green")|| request.riskAssessment.Equals("red")) {
                    if (request.riskAssessment.Equals("green")) { request.reqResult = "approve";_context.SaveChangesAsync(); }
                    if (request.riskAssessment.Equals("red")) { request.reqResult = "reject"; _context.SaveChangesAsync(); }
                    taskDefinitionKey = "ServiceTaskPoliceAusstellen"; }
                else { taskDefinitionKey = "ServiceTaskAblehnungVermerken"; }
                string requestUrl = $"{camundaEngineUrl}/task?processInstanceId={processInstanceId}&taskDefinitionKey={taskDefinitionKey}";
                HttpResponseMessage response = await httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    //var tasks = await response.Content.ReadFromJsonAsync<CamundaClient.Dto.HumanTask[]>();

                    var tasks = await response.Content.ReadAsStreamAsync();
                    using var streamReader = new StreamReader(tasks);
                    using var jsonReader = new JsonTextReader(streamReader);
                    JsonSerializer serializer = new JsonSerializer();
                    var t = serializer.Deserialize<CamundaClient.Dto.HumanTask[]>(jsonReader);
                    if (t.Length > 0)
                    {
                        var task = t[0];
                        var username = "demo";
                        var password = "demo";
                        string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                                                       .GetBytes(username + ":" + password));

                        string claimUrl = $"{camundaEngineUrl}/task/{task.Id}/claim";
                        // Send a POST request to claim the task
                        var user = new { userId = "demo" };
                        string json = JsonConvert.SerializeObject(user);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        Console.WriteLine(content.GetType());
                        HttpResponseMessage claim = await httpClient.PostAsync(claimUrl, content);
                        claim.Headers.Add("Authorization", "Basic " + encoded);
                        Console.WriteLine("claim : " + claim.StatusCode);

                        ////////////// 
                        var vars = new Dictionary<string, object>{
                                       { "decision", request.reqResult },
                                       };
                        string json1 = JsonConvert.SerializeObject(vars);
                        var variables = new StringContent(json1, Encoding.UTF8, "application/json");
                        // Add more variables as needed

                        HttpResponseMessage response3 = await httpClient.PostAsync($"{camundaEngineUrl}/task/{task.Id}/complete", variables);

                        Console.WriteLine("voir ici : " + response3.Content.ReadAsStringAsync().Result);

                    }
                    else
                    {
                        Console.WriteLine("No tasks found for the given process instance ID and task definition key.");
                    }
                }
                ///////////////
                if (request == null)
                {

                    return NotFound();
                }
            }
            return View(request);
        }

        // GET: Requests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Requests == null)
            {
                return NotFound();
            }

            var request = await _context.Requests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }
            return View(request);
        }

        // POST: Requests/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id,name,age,email,carManufacturer,carType,riskAssessment,riskDescription")] Request request)
        {
            if (id != request.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(request);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RequestExists(request.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(request);
        }

        // GET: Requests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Requests == null)
            {
                return NotFound();
            }

            var request = await _context.Requests
                .FirstOrDefaultAsync(m => m.id == id);
            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        // POST: Requests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Requests == null)
            {
                return Problem("Entity set 'InsuranceDBContext.Requests'  is null.");
            }
            var request = await _context.Requests.FindAsync(id);
            if (request != null)
            {
                _context.Requests.Remove(request);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RequestExists(int id)
        {
          return (_context.Requests?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}
