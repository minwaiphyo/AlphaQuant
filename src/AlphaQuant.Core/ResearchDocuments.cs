namespace AlphaQuant.Core;
public sealed record ThesisText(string Thesis,string VariantView,string Catalysts,string Risks,string InvalidationConditions,string Horizon,string PeerComparability,string Conclusion);
public sealed record ThesisConditionReview(Guid SnapshotId,DateTimeOffset EvaluatedAt,IReadOnlyList<ScreeningResult> Results);
public sealed record ResearchDocumentDraft(string Title,ThesisText Thesis,IReadOnlyList<Guid> SnapshotIds,ResearchModel? Model=null,ResearchRuleSet? Rules=null,Guid? Id=null,long? ExpectedRevision=null,bool Deleted=false)
{ public IReadOnlyList<ThesisConditionReview> ConditionReviews { get; init; } = []; }
public sealed record ResearchDocumentRevision(Guid Id,long Revision,string Title,ThesisText Thesis,IReadOnlyList<Guid> SnapshotIds,ResearchModel? Model,ResearchRuleSet? Rules,bool Deleted,DateTimeOffset SavedAt)
{ public IReadOnlyList<ThesisConditionReview> ConditionReviews { get; init; } = []; }
