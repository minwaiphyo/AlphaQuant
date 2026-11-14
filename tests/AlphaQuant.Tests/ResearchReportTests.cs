using AlphaQuant.Core;
using AlphaQuant.Infrastructure;
using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using PdfSharp.Pdf.IO;
namespace AlphaQuant.Tests;
public sealed class ResearchReportTests
{
    private static ResearchDocumentRevision Document()=>new(Guid.NewGuid(),1,"Synthetic α research",new("=HYPERLINK(\"bad\")","Variant","Catalysts","Risks","Conditions","3 years","Peers","User conclusion"),[],new("Base",ResearchModelTests.Input(),ResearchModelTests.Dcf()),ResearchScreening.Defaults(),false,new(2026,11,14,0,0,0,TimeSpan.Zero));
    [Fact]public void Frozen_report_is_culture_invariant_and_does_not_collect_sessions()
    {
        var document=Document();var report=ResearchReport.Capture(document,[]);var original=CultureInfo.CurrentCulture;
        try{CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("de-DE");Assert.Equal(ResearchReportExporter.Csv(report),ResearchReportExporter.Csv(ResearchReport.Capture(document,[])));Assert.Equal("de-DE",CultureInfo.CurrentCulture.Name);}
        finally{CultureInfo.CurrentCulture=original;}
        Assert.DoesNotContain("accessToken",ResearchReportExporter.Csv(report));Assert.Contains("Year 1",ResearchReportExporter.Csv(report));
    }
    [Fact]public void Csv_neutralizes_formula_injection_and_xlsx_stores_literal_unicode_text_without_formulas()
    {
        var report=ResearchReport.Capture(Document(),[]);var csv=ResearchReportExporter.Csv(report);Assert.Contains("'=HYPERLINK",csv);
        using var zip=new ZipArchive(new MemoryStream(ResearchReportExporter.Xlsx(report)));using var stream=zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();var sheet=XDocument.Load(stream);XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        Assert.Empty(sheet.Descendants(ns+"f"));Assert.Contains(sheet.Descendants(ns+"t"),t=>t.Value=="=HYPERLINK(\"bad\")");Assert.Contains(sheet.Descendants(ns+"t"),t=>t.Value=="Synthetic α research");
    }
    [Fact]public async Task Pdf_reopens_and_failed_export_preserves_user_file()
    {
        var report=ResearchReport.Capture(Document(),[]);var bytes=ResearchReportExporter.Pdf(report);using var pdf=PdfReader.Open(new MemoryStream(bytes),PdfDocumentOpenMode.Import);Assert.True(pdf.PageCount>0);Assert.Equal(report.Title,pdf.Info.Title);
        var root=Path.Combine(Path.GetTempPath(),"aq-report-"+Guid.NewGuid());Directory.CreateDirectory(root);
        try{var path=Path.Combine(root,"report.pdf");await File.WriteAllTextAsync(path,"preserve");await Assert.ThrowsAsync<IOException>(()=>ResearchReportExporter.SaveNewAsync(path,report,"PDF"));Assert.Equal("preserve",await File.ReadAllTextAsync(path));Assert.Single(Directory.GetFiles(root));}
        finally{Directory.Delete(root,true);}
    }
    [Fact]public void Saved_exploration_replays_into_reports_and_long_unicode_cells_continue_without_loss()
    {
        var document=Document();var model=document.Model!;
        var target=DcfEngine.Calculate(model.Forecast,model.Dcf).PerShare;
        model=model with{Exploration=new([.1m],[.03m],[.1m,.2m],new(target,0,.2m,.000001m))};
        var report=ResearchReport.Capture(document with{Model=model},[]);
        Assert.Contains(report.Lines,l=>l.Section=="Reverse DCF"&&l.Value.Contains("candidate"));Assert.Equal(2,report.Lines.Count(l=>l.Section=="Driver sensitivity"));
        var text=new string('α',2000);var longReport=new FrozenResearchReport("report-1","Long Unicode",document.SavedAt,[new("Thesis","Long text",text,"","","")]);
        using var zip=new ZipArchive(new MemoryStream(ResearchReportExporter.Xlsx(longReport)));using var stream=zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();var sheet=XDocument.Load(stream);XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var reconstructed=string.Concat(sheet.Descendants(ns+"row").Skip(2).Select(row=>row.Elements(ns+"c").ElementAt(2).Descendants(ns+"t").Single().Value));Assert.Equal(text,reconstructed);
    }
    [Fact]public async Task Unsupported_PDF_scripts_fail_before_writing_while_text_formats_keep_unicode()
    {
        var report=new FrozenResearchReport("report-1","မြန်မာ",DateTimeOffset.UtcNow,[new("Thesis","Unicode","日本語", "", "", "")]);
        Assert.Contains("日本語",ResearchReportExporter.Csv(report));Assert.NotEmpty(ResearchReportExporter.Xlsx(report));
        var root=Path.Combine(Path.GetTempPath(),"aq-font-"+Guid.NewGuid());Directory.CreateDirectory(root);
        try{await Assert.ThrowsAsync<ReportFontException>(()=>ResearchReportExporter.SaveNewAsync(Path.Combine(root,"report.pdf"),report,"PDF"));Assert.Empty(Directory.GetFiles(root));}
        finally{Directory.Delete(root,true);}
    }
    [Fact]public void Missing_link_prevents_falsely_complete_report()
    {var document=Document() with{SnapshotIds=[Guid.NewGuid()]};Assert.Throws<ArgumentException>(()=>ResearchReport.Capture(document,[]));}
}
