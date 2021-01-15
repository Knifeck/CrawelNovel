namespace CrawelNovel.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _01 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Catalogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        NoteName = c.String(maxLength: 255, storeType: "nvarchar"),
                        Url = c.String(maxLength: 255, storeType: "nvarchar"),
                        Img = c.Binary(),
                        CreateTime = c.DateTime(nullable: false, precision: 0),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Chapters",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        NoteBookId = c.Int(nullable: false),
                        ChapterName = c.String(maxLength: 255, storeType: "nvarchar"),
                        ChapterUrl = c.String(maxLength: 255, storeType: "nvarchar"),
                        ChapterContent = c.String(unicode: false),
                        CreateTime = c.DateTime(nullable: false, precision: 0),
                        IsFinished = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Chapters");
            DropTable("dbo.Catalogs");
        }
    }
}
