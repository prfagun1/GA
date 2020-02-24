using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using GALibrary.Models;
using Microsoft.Extensions.Configuration;

namespace GALibrary.Models
{
    public static class DB
    {
        public static GAContext Context = null;
        public static void Initialize_Context_in_Startup(IServiceProvider serviceProvider)
        {
            IServiceScope serviceScope = serviceProvider.GetService<IServiceScopeFactory>().CreateScope();
            Context = serviceScope.ServiceProvider.GetService<GAContext>();
        }

    }


    public class GAContext : DbContext
    {
        /*
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var builder = new ConfigurationBuilder().SetBasePath(System.IO.Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfigurationRoot configuration = builder.Build();
            optionsBuilder.UseMySql(configuration.GetConnectionString("DefaultConnection"));
        }
        */

        public GAContext(){

        }

        public GAContext(DbContextOptions<GAContext> options) : base(options){}

        public DbSet<LoginData> LoginData { get; set; }
        public DbSet<Application> Application { get; set; }
        public DbSet<Command> Command { get; set; }
        public DbSet<DatabaseGA> DatabaseGA { get; set; }
        public DbSet<DatabaseConnection> DatabaseConnection { get; set; }
        public DbSet<Environment> Environment { get; set; }
        public DbSet<File> File { get; set; }
        public DbSet<FileDelete> FileDelete { get; set; }
        public DbSet<FileDeleteFolder> FileDeleteFolder { get; set; }
        public DbSet<FileFolder> FileFolder { get; set; }
        public DbSet<FileHistory> FileHistory { get; set; }
        public DbSet<Folder> Folder { get; set; }
        public DbSet<OS> OS { get; set; }
        public DbSet<Parameter> Parameter { get; set; }
        public DbSet<PermissionGroup> PermissionGroup { get; set; }
        public DbSet<Procedure> Procedure { get; set; }
        public DbSet<ProcedureSchedule> ProcedureSchedule { get; set; }
        public DbSet<ProcedureSteps> ProcedureSteps { get; set; }
        public DbSet<Server> Server { get; set; }
        public DbSet<ServerUser> ServerUser { get; set; }
        public DbSet<Service> Service { get; set; }
        public DbSet<SQL> SQL { get; set; }
        public DbSet<UpdateGA> UpdateGA { get; set; }
        public DbSet<UpdateSteps> UpdateSteps { get; set; }
        public DbSet<AlertMail> AlertMail { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileDeleteFolder>().HasKey(c => new { c.FileDeleteId, c.ApplicationId, c.FolderId });
            modelBuilder.Entity<FileFolder>().HasKey(c => new { c.FileId, c.ApplicationId, c.FolderId });
            modelBuilder.Entity<UpdateSteps>().HasKey(c => new { c.UpdateId, c.Type, c.ProcessId, c.Order });
            modelBuilder.Entity<ProcedureSteps>().HasKey(c => new { c.ProcedureID, c.Type, c.ProcessId, c.Order });
            modelBuilder.Entity<FileHistory>().HasKey(c => new { c.FileId, c.FileName, c.Folder, c.UpdateId});
        }

    }

}
