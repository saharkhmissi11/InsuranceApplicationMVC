namespace InsuranceApplicationMVC.Models.InsuranceModels
{
    public class Request
    {
        
        public int id { get; set; }
        public string name { get; set; }
        public int age { get; set; }
        public string email { get; set; }
        public string carManufacturer { get; set; }
        public string carType { get; set; }
        public string? riskAssessment { get; set; }
        public string? riskDescription { get; set; }
        public string? reqResult { get;set; }
        public string? processInstanceId { get; set; }
        
    }
}
