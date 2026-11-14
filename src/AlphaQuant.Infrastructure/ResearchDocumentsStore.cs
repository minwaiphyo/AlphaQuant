using AlphaQuant.Core;
using Microsoft.Data.Sqlite;
using System.Text;
using System.Text.Json;
namespace AlphaQuant.Infrastructure;
public sealed partial class SqliteResearchStore
{
    public Task<IReadOnlyList<ResearchDocumentRevision>> ListDocumentRevisionsAsync(CancellationToken token=default)=>AccessAsync<IReadOnlyList<ResearchDocumentRevision>>(()=>{using var db=OpenCurrent();return ReadDocuments(db,token);},token);
    public Task<ResearchDocumentRevision> SaveDocumentAsync(ResearchDocumentDraft draft,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if(draft.Id==Guid.Empty||draft.Id.HasValue!=draft.ExpectedRevision.HasValue||draft.ExpectedRevision is <=0)throw Failure(ResearchStorageFailure.Validation,"Document updates require ID and observed revision.");
        var revision=new ResearchDocumentRevision(draft.Id??Guid.NewGuid(),(draft.ExpectedRevision??0)+1,ValidName(draft.Title),draft.Thesis,draft.SnapshotIds?.ToArray()??[],draft.Model,draft.Rules,draft.Deleted,DateTimeOffset.UtcNow) { ConditionReviews = draft.ConditionReviews?.ToArray() ?? [] };
        ValidateDocument(revision);
        var json=JsonSerializer.Serialize(revision,JsonOptions);
        if(Encoding.UTF8.GetByteCount(json)>MaxSnapshotBytes)throw Failure(ResearchStorageFailure.Validation,"Research document exceeds 2 MiB; nothing saved.");
        var captured=JsonSerializer.Deserialize<ResearchDocumentRevision>(json,JsonOptions)!;
        return AccessAsync(()=>
        {
            EnsureWritable();using var db=OpenCurrent();using var tx=db.BeginTransaction();
            using(var current=db.CreateCommand())
            {
                current.Transaction=tx;current.CommandText="SELECT COALESCE(MAX(revision),0) FROM research_documents WHERE id=$id;";current.Parameters.AddWithValue("$id",captured.Id.ToString());
                if((long)current.ExecuteScalar()! != (draft.ExpectedRevision??0))throw Failure(ResearchStorageFailure.Conflict,"Research revision changed. Reload and preserve your unsaved draft before retrying.");
            }
            ValidateLinks(db,captured,tx);
            if(new FileInfo(_path).Length+Encoding.UTF8.GetByteCount(json)+16384>MaxDatabaseBytes)throw Failure(ResearchStorageFailure.Validation,"Research storage is full; preserve a backup before choosing a new file.");
            Execute(db,"INSERT INTO research_documents(id,revision,payload) VALUES($id,$revision,$payload);",tx,("$id",captured.Id.ToString()),("$revision",captured.Revision),("$payload",json));
            token.ThrowIfCancellationRequested();tx.Commit();return captured;
        },token);
    }
    private static IReadOnlyList<ResearchDocumentRevision> ReadDocuments(SqliteConnection db,CancellationToken token,SqliteTransaction? tx=null)
    {
        var result=new List<ResearchDocumentRevision>();
        using(var command=db.CreateCommand())
        {
            command.Transaction=tx;command.CommandText="SELECT id,revision,payload FROM research_documents ORDER BY id,revision DESC;";using var reader=command.ExecuteReader();
            while(reader.Read())
            {
                token.ThrowIfCancellationRequested();var json=reader.GetString(2);if(Encoding.UTF8.GetByteCount(json)>MaxSnapshotBytes)throw Failure(ResearchStorageFailure.Corrupt,"Oversized document; research preserved.");
                var item=JsonSerializer.Deserialize<ResearchDocumentRevision>(json,JsonOptions)??throw Failure(ResearchStorageFailure.Corrupt,"Empty document payload.");
                try{ValidateDocument(item);}catch(ResearchStorageException ex)when(ex.Kind==ResearchStorageFailure.Validation){throw new ResearchStorageException(ResearchStorageFailure.Corrupt,"Invalid stored research revision; preserved.",ex);}
                if(item.Id.ToString()!=reader.GetString(0)||item.Revision!=reader.GetInt64(1))throw Failure(ResearchStorageFailure.Corrupt,"Document identity mismatch; research preserved.");
                result.Add(item);
            }
        }
        foreach(var group in result.GroupBy(r=>r.Id))
            if(!group.Select(r=>r.Revision).Order().SequenceEqual(Enumerable.Range(1,group.Count()).Select(n=>(long)n)))throw Failure(ResearchStorageFailure.Corrupt,"Research revision history is incomplete; preserve the file.");
        foreach(var item in result)
            try{ValidateLinks(db,item,tx);}catch(ResearchStorageException ex)when(ex.Kind==ResearchStorageFailure.Validation){throw new ResearchStorageException(ResearchStorageFailure.Corrupt,"Stored research snapshot link is missing; file preserved.",ex);}
        return result;
    }
    private static void ValidateDocument(ResearchDocumentRevision item)
    {
        ValidName(item.Title);
        if(item.Id==Guid.Empty||item.Revision<1||item.Thesis is null||item.SnapshotIds is null||item.SnapshotIds.Count>20||item.SnapshotIds.Any(id=>id==Guid.Empty)||item.SnapshotIds.Distinct().Count()!=item.SnapshotIds.Count)throw Failure(ResearchStorageFailure.Validation,"Invalid research identity or snapshot links.");
        if(item.ConditionReviews is null||item.ConditionReviews.Count>20||item.ConditionReviews.Any(r=>r is null||!item.SnapshotIds.Contains(r.SnapshotId)||r.Results is null||r.Results.Count is <1 or >5||r.Results.Any(result=>result is null||result.Version!=ResearchScreening.Version||result.Observations is null||result.Observations.Count is <1 or >20)))throw Failure(ResearchStorageFailure.Validation,"Condition reviews require bounded results linked to a retained snapshot.");
        foreach(var review in item.ConditionReviews)foreach(var result in review.Results)
        {
            if(result.Cik<=0||result.Total!=result.Observations.Count||result.Observations.Any(o=>o is null||o.Rule is null||o.Triggered.HasValue!=o.Input.HasValue||o.Triggered.HasValue&&o.PeriodEnd is null||o.Triggered.HasValue&&o.Reason is not null))throw Failure(ResearchStorageFailure.Validation,"Malformed recorded condition inputs.");
            try{ResearchScreening.Validate(new(result.Version,result.Observations.Select(o=>o.Rule).ToArray()));}
            catch(ArgumentException ex){throw new ResearchStorageException(ResearchStorageFailure.Validation,"Invalid recorded condition rules.",ex);}
            foreach(var o in result.Observations.Where(o=>o.Input.HasValue))
            {
                var n=o.Input!.Value;var expected=o.Rule.Operator switch{">"=>n>o.Rule.Threshold,">="=>n>=o.Rule.Threshold,"<"=>n<o.Rule.Threshold,_=>n<=o.Rule.Threshold};
                if(expected!=o.Triggered)throw Failure(ResearchStorageFailure.Validation,"Recorded condition does not match its explicit threshold.");
            }
            var available=result.Observations.Count(o=>o.Triggered.HasValue);
            decimal? score=available==result.Total?result.Observations.Where(o=>o.Triggered==true).Sum(o=>o.Rule.Weight)/result.Observations.Sum(o=>o.Rule.Weight)*100:null;
            if(result.Available!=available||result.Score!=score)throw Failure(ResearchStorageFailure.Validation,"Recorded condition coverage/score is inconsistent.");
        }
        var t=item.Thesis;
        if(new[]{t.Thesis,t.VariantView,t.Catalysts,t.Risks,t.InvalidationConditions,t.Horizon,t.PeerComparability,t.Conclusion}.Any(s=>s is null||s.Length>8000||s.Any(c=>char.IsControl(c)&&c!='\n'&&c!='\r'&&c!='\t')))throw Failure(ResearchStorageFailure.Validation,"Thesis fields support up to 8000 readable characters each.");
        try
        {
            if(item.Rules is not null)ResearchScreening.Validate(item.Rules);
            if(item.Model is not null)
            {
                if(item.Model.Scenario is not("Bear" or "Base" or "Bull"))throw new ArgumentException("Choose Bear, Base or Bull; labels are not probabilities.");
                _=DcfEngine.Calculate(item.Model.Forecast,item.Model.Dcf);
                _=ResearchExploration.Replay(item.Model);
            }
        }
        catch(Exception ex)when(ex is ArgumentException or OverflowException){throw new ResearchStorageException(ResearchStorageFailure.Validation,"Research model/rules do not satisfy their versioned contract. Prior revisions preserved.",ex);}
    }
    private static void ValidateLinks(SqliteConnection db,ResearchDocumentRevision item,SqliteTransaction? tx)
    {
        foreach(var id in item.SnapshotIds)
        {
            using var c=db.CreateCommand();c.Transaction=tx;c.CommandText="SELECT COUNT(*) FROM financial_snapshots WHERE id=$id;";c.Parameters.AddWithValue("$id",id.ToString());
            if((long)c.ExecuteScalar()! !=1)throw Failure(ResearchStorageFailure.Validation,"Linked financial snapshot is missing; choose an existing immutable snapshot.");
        }
    }
}
