using AlphaQuant.Core;
using AlphaQuant.Infrastructure;
using Microsoft.Data.Sqlite;
namespace AlphaQuant.Tests;
public sealed class ResearchDocumentTests:IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"aq-docs-"+Guid.NewGuid());private string PathName=>Path.Combine(root,"research.db");
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
    private static ResearchDocumentDraft Draft()=>new("Synthetic thesis",new("Thesis","Variant","Catalyst","Risk","Invalidate","3 years","Peers","User conclusion"),[],new("Base",ResearchModelTests.Input(),ResearchModelTests.Dcf()),ResearchScreening.Defaults());
    [Fact]public async Task Revisions_reopen_replay_and_conflicts_preserve_unsaved_history()
    {
        var store=new SqliteResearchStore(PathName);await store.InitializeAsync();var first=await store.SaveDocumentAsync(Draft());
        var second=await store.SaveDocumentAsync(Draft() with{Id=first.Id,ExpectedRevision=1,Title="Updated"});
        await Assert.ThrowsAsync<ResearchStorageException>(()=>store.SaveDocumentAsync(Draft() with{Id=first.Id,ExpectedRevision=1}));
        var reopened=await new SqliteResearchStore(PathName).ListDocumentRevisionsAsync();Assert.Equal(2,reopened.Count);Assert.Equal(first.Thesis,reopened[1].Thesis);Assert.Equal(second.Id,reopened[0].Id);
        Assert.Equal(DcfEngine.Calculate(first.Model!.Forecast,first.Model.Dcf).PerShare,DcfEngine.Calculate(reopened[1].Model!.Forecast,reopened[1].Model!.Dcf).PerShare);
        var deleted=await store.SaveDocumentAsync(Draft() with{Id=first.Id,ExpectedRevision=2,Deleted=true});
        await store.SaveDocumentAsync(Draft() with{Id=first.Id,ExpectedRevision=deleted.Revision,Deleted=false});Assert.Equal(4,(await store.ListDocumentRevisionsAsync()).Count);
    }
    [Fact]public async Task Schema3_upgrade_backup_restore_and_missing_links_are_safe()
    {
        var store=new SqliteResearchStore(PathName);await store.InitializeAsync();
        using(var db=new SqliteConnection($"Data Source={PathName};Pooling=False")){db.Open();using var cmd=db.CreateCommand();cmd.CommandText="DROP TABLE research_documents; PRAGMA user_version=3;";cmd.ExecuteNonQuery();}
        await store.InitializeAsync();Assert.Single(Directory.GetFiles(root,"*.backup-*.db"));
        await Assert.ThrowsAsync<ResearchStorageException>(()=>store.SaveDocumentAsync(Draft() with{SnapshotIds=[Guid.NewGuid()]}));
        var first=await store.SaveDocumentAsync(Draft());var backup=await store.BackupAsync();await store.SaveDocumentAsync(Draft() with{Id=first.Id,ExpectedRevision=1});await store.RestoreAsync(backup.Path);Assert.Single(await store.ListDocumentRevisionsAsync());
    }
    [Fact]public async Task Invalid_or_cancelled_models_leave_existing_revision_intact()
    {
        var store=new SqliteResearchStore(PathName);await store.InitializeAsync();var first=await store.SaveDocumentAsync(Draft());
        await Assert.ThrowsAsync<ResearchStorageException>(()=>store.SaveDocumentAsync(Draft() with{Model=new("Base",ResearchModelTests.Input(),ResearchModelTests.Dcf() with{Wacc=0})}));
        using var c=new CancellationTokenSource();c.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>store.SaveDocumentAsync(Draft(),c.Token));Assert.Equal(first.Id,Assert.Single(await store.ListDocumentRevisionsAsync()).Id);
    }    [Fact]public async Task Large_revision_history_and_logically_corrupt_backup_preserve_valid_research()
    {
        var store=new SqliteResearchStore(PathName);await store.InitializeAsync();var current=await store.SaveDocumentAsync(Draft());
        for(var i=1;i<100;i++)current=await store.SaveDocumentAsync(Draft() with{Id=current.Id,ExpectedRevision=current.Revision,Title="Revision "+i});
        Assert.Equal(100,(await store.ListDocumentRevisionsAsync()).Count);
        var backup=await store.BackupAsync();
        using(var db=new SqliteConnection($"Data Source={backup.Path};Pooling=False")){db.Open();using var cmd=db.CreateCommand();cmd.CommandText="UPDATE research_documents SET payload='{}' WHERE revision=1;";cmd.ExecuteNonQuery();}
        await Assert.ThrowsAsync<ResearchStorageException>(()=>store.RestoreAsync(backup.Path));Assert.Equal(100,(await store.ListDocumentRevisionsAsync()).Count);
    }

}
