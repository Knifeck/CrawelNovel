namespace CrawelNovel.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _02 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Catalogs", "UpdateTime", c => c.DateTime(precision: 0));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Catalogs", "UpdateTime");
        }
    }
}
