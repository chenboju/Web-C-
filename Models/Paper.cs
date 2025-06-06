//------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    
    public partial class Paper
    {
        public Paper()
        {
            this.Collection = new HashSet<Collection>();
            this.Comment = new HashSet<Comment>();
            this.PaperCategory = new HashSet<PaperCategory>();
            this.Likes = new HashSet<Likes>();
        }
    
        public int PaperId { get; set; }
        public string Title { get; set; }
        public string Authors { get; set; }
        public string Abstract { get; set; }
        public string Journal { get; set; }
        public int Year { get; set; }
        public string Keywords { get; set; }
        public string FilePath { get; set; }
        public string PreviewImagePath { get; set; }
        public int ClickCount { get; set; }
        public bool IsActive { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public string ExternalUrl { get; set; }
    
        public virtual ICollection<Collection> Collection { get; set; }
        public virtual ICollection<Comment> Comment { get; set; }
        public virtual ICollection<PaperCategory> PaperCategory { get; set; }
        public virtual ICollection<Likes> Likes { get; set; }
    }
}
