using AlphaQuant.Core;
using AlphaQuant.Infrastructure;
using AlphaQuant.Desktop.ViewModels;
namespace AlphaQuant.Tests;
public sealed class ResearchWorkbenchTests:IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"aq-workbench-"+Guid.NewGuid());
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
    [Fact]public async Task Explicit_save_reopen_conflict_and_frozen_preview_do_not_overwrite_drafts()
    {
        var store=new SqliteResearchStore(Path.Combine(root,"research.db"));var vm=new ResearchWorkbenchViewModel(store,()=>[]);
        await vm.ReloadCommand.ExecuteAsync(null);vm.DocumentTitle="Initial";vm.Thesis="User thesis";await vm.SaveRevisionCommand.ExecuteAsync(null);Assert.Single(vm.Revisions);
        vm.Thesis="Unsaved private draft";await vm.ReloadCommand.ExecuteAsync(null);Assert.Equal("Unsaved private draft",vm.Thesis);
        vm.SelectedRevision=vm.Revisions[0];vm.PreviewReportCommand.Execute(null);Assert.Contains("User thesis",vm.ReportPreview);Assert.DoesNotContain("Unsaved private draft",vm.ReportPreview);
        var prior=vm.SelectedRevision.Revision;await store.SaveDocumentAsync(new("Concurrent",prior.Thesis,[],Id:prior.Id,ExpectedRevision:prior.Revision));
        await vm.SaveRevisionCommand.ExecuteAsync(null);Assert.Contains("changed",vm.Status);Assert.Equal("Unsaved private draft",vm.Thesis);
        await vm.SaveNewCopyCommand.ExecuteAsync(null);Assert.Equal(3,vm.Revisions.Count);Assert.Equal(2,vm.Revisions.Select(r=>r.Revision.Id).Distinct().Count());
    }
    [Fact]public async Task Manual_condition_reviews_reopen_with_snapshot_and_clear_without_orphaning_links()
    {
        var store=new SqliteResearchStore(Path.Combine(root,"research.db"));await store.InitializeAsync();
        var companies=new[]{new CompanyRef(1,"AAA","Synthetic A"),new CompanyRef(2,"BBB","Synthetic B")};
        var set=await store.SaveSetAsync(new("Peers",ResearchSetKind.PeerSet,companies));
        var financial=ComparisonAnalyzer.Analyze([new("Revenues","USD",100,new(2025,1,1),new(2025,12,31),"10-K",new(2026,2,1),"0000000001-26-000001")],new(2026,3,1));
        var snapshot=await store.SaveSnapshotAsync(new(set.Id,set.Revision,ComparisonAnalyzer.DefinitionVersion,companies.Select(c=>new SavedFinancialCompany(c,financial,null,false)).ToArray()));
        var vm=new ResearchWorkbenchViewModel(store,()=>[]);await vm.ReloadCommand.ExecuteAsync(null);vm.SelectedSnapshot=vm.Snapshots.Single();vm.RecordConditionsCommand.Execute(null);vm.DocumentTitle="Manual conditions";await vm.SaveRevisionCommand.ExecuteAsync(null);
        var revision=Assert.Single(await store.ListDocumentRevisionsAsync());var review=Assert.Single(revision.ConditionReviews);Assert.Equal(snapshot.Id,review.SnapshotId);Assert.Equal(2,review.Results.Count);Assert.All(review.Results,r=>Assert.Null(r.Score));
        vm.ClearLinksCommand.Execute(null);await vm.SaveRevisionCommand.ExecuteAsync(null);Assert.Empty((await store.ListDocumentRevisionsAsync())[0].ConditionReviews);
    }

    [Fact]public async Task Model_fields_start_unknown_and_invalid_models_do_not_save()
    {
        var vm=new ResearchWorkbenchViewModel(new SqliteResearchStore(Path.Combine(root,"research.db")),()=>[]);await vm.ReloadCommand.ExecuteAsync(null);
        vm.DocumentTitle="Explicit assumptions only";vm.IncludeModel=true;await vm.SaveRevisionCommand.ExecuteAsync(null);Assert.Empty(vm.Revisions);Assert.Equal("",vm.Wacc);Assert.Equal("",vm.Years[0].Tax);
    }
}
