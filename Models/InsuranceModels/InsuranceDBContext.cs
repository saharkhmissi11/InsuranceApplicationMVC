using Microsoft.EntityFrameworkCore;

namespace InsuranceApplicationMVC.Models.InsuranceModels
{
    public class InsuranceDBContext:DbContext
    {
        public InsuranceDBContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<Request> Requests { get; set; } 
    }
}
