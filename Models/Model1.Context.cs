﻿//------------------------------------------------------------------------------
// <auto-generated>
//    這個程式碼是由範本產生。
//
//    對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//    如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebApplication1.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class LiteratureEntities2 : DbContext
    {
        public LiteratureEntities2()
            : base("name=LiteratureEntities2")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<Category> Category { get; set; }
        public DbSet<Collection> Collection { get; set; }
        public DbSet<Comment> Comment { get; set; }
        public DbSet<Paper> Paper { get; set; }
        public DbSet<PaperCategory> PaperCategory { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Likes> Likes { get; set; }
    }
}
