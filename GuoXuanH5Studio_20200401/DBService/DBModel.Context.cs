//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace DBService
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class V3Entities : DbContext
    {
        public V3Entities()
            : base("name=V3Entities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<operation> operation { get; set; }
        public virtual DbSet<operationrecord> operationrecord { get; set; }
        public virtual DbSet<repair> repair { get; set; }
        public virtual DbSet<repairmethod> repairmethod { get; set; }
        public virtual DbSet<repairreason> repairreason { get; set; }
        public virtual DbSet<repairresult> repairresult { get; set; }
        public virtual DbSet<result> result { get; set; }
        public virtual DbSet<uidrecord> uidrecord { get; set; }
        public virtual DbSet<userrole> userrole { get; set; }
        public virtual DbSet<users> users { get; set; }
        public virtual DbSet<computermac> computermac { get; set; }
        public virtual DbSet<mac_operation> mac_operation { get; set; }
    }
}
