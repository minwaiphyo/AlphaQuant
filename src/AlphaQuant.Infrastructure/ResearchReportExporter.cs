using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using AlphaQuant.Core;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
namespace AlphaQuant.Infrastructure;
public static class ResearchReportExporter
{
    static ResearchReportExporter(){if(GlobalFontSettings.FontResolver is null)GlobalFontSettings.FontResolver=new EmbeddedResearchFont();}
    private sealed class EmbeddedResearchFont:IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName,bool bold,bool italic)=>new("ResearchSans");
        public byte[] GetFont(string faceName){using var s=typeof(ResearchReportExporter).Assembly.GetManifestResourceStream("AlphaQuant.Infrastructure.Resources.DejaVuSans.ttf")??throw new InvalidOperationException("Embedded report font missing.");using var m=new MemoryStream();s.CopyTo(m);return m.ToArray();}
    }
    private static string[][] Rows(FrozenResearchReport report)=>new[]{new[]{"Section","Label","Value","Unit","Period / definition","Sources"},new[]{"Report",report.Title,report.Version,"",report.CapturedAt.ToString("O"),"Frozen local research; no current prices or credentials"}}.Concat(report.Lines.Select(l=>new[]{l.Section,l.Label,l.Value,l.Unit,l.Basis,l.Source})).ToArray();
    public static string Csv(FrozenResearchReport report)
    {
        string Cell(string value){var trimmed=new string(value.SkipWhile(c=>char.IsWhiteSpace(c)||char.IsControl(c)||char.GetUnicodeCategory(c)==UnicodeCategory.Format).ToArray());if(value.StartsWith('\t')||value.StartsWith('\r')||value.StartsWith('\n')||(trimmed.Length>0&&"=+-@".Contains(trimmed[0])))value="'"+value;return "\""+value.Replace("\"","\"\"")+"\"";}
        return string.Join("\r\n",Rows(report).Select(row=>string.Join(",",row.Select(Cell))))+"\r\n";
    }
    public static byte[] Xlsx(FrozenResearchReport report)
    {
        using var output=new MemoryStream();
        using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true))
        {
            void Entry(string path,string text){using var writer=new StreamWriter(zip.CreateEntry(path).Open(),new UTF8Encoding(false));writer.Write(text);}
            Entry("[Content_Types].xml","""<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>""");
            Entry("_rels/.rels","""<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            Entry("xl/workbook.xml","""<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Frozen research" sheetId="1" r:id="rId1"/></sheets></workbook>""");
            Entry("xl/_rels/workbook.xml.rels","""<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>""");
            Entry("xl/styles.xml","""<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="1"><font><sz val="11"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border/></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="1"><xf numFmtId="49" fontId="0" fillId="0" borderId="0" applyAlignment="1"><alignment wrapText="1" vertical="top"/></xf></cellXfs><cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles></styleSheet>""");
            XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var rows=Rows(report).SelectMany(row=>
            {
                string[] Chunks(string text){var starts=StringInfo.ParseCombiningCharacters(text);if(starts.Length==0)return [""];return Enumerable.Range(0,(starts.Length+399)/400).Select(i=>{var first=starts[i*400];var end=(i+1)*400<starts.Length?starts[(i+1)*400]:text.Length;return text[first..end];}).ToArray();}
                var cells=row.Select(Chunks).ToArray();return Enumerable.Range(0,cells.Max(c=>c.Length)).Select(i=>cells.Select(c=>i<c.Length?c[i]:"").ToArray());
            }).ToArray();
            var sheet=new XElement(ns+"worksheet",new XElement(ns+"dimension",new XAttribute("ref",$"A1:F{rows.Length}")),new XElement(ns+"cols",Enumerable.Range(1,6).Select(i=>new XElement(ns+"col",new XAttribute("min",i),new XAttribute("max",i),new XAttribute("width",i is 3 or 6?65:28),new XAttribute("customWidth",1)))),new XElement(ns+"sheetData",rows.Select((row,index)=>new XElement(ns+"row",new XAttribute("r",index+1),new XAttribute("ht",Math.Min(409,15*(1+row.Max(v=>v.Length)/24))),new XAttribute("customHeight",1),row.Select((value,column)=>new XElement(ns+"c",new XAttribute("r",$"{(char)('A'+column)}{index+1}"),new XAttribute("t","inlineStr"),new XElement(ns+"is",new XElement(ns+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value))))))));
            Entry("xl/worksheets/sheet1.xml",sheet.ToString(SaveOptions.DisableFormatting));
        }
        return output.ToArray();
    }
    public static byte[] Pdf(FrozenResearchReport report)
    {
        using var document=new PdfDocument();document.Info.Title=report.Title;
        var font=new XFont("ResearchSans",10);XGraphics? graphics=null;double y=0;var pageCount=0;
        void NewPage(){graphics?.Dispose();if(++pageCount>100)throw new ArgumentException("PDF exceeds 100 supported pages.");var page=document.AddPage();page.Size=PdfSharp.PageSize.A4;graphics=XGraphics.FromPdfPage(page);y=44;}
        void Draw(string text)
        {
            // Preserve words and break long tokens by Unicode text elements.
            foreach(var paragraph in text.Replace("\r","").Split('\n'))
            {
                void DrawLine(string line){if(y>790)NewPage();graphics!.DrawString(line,font,XBrushes.Black,new XPoint(44,y));y+=14;}
                var current="";
                foreach(var word in paragraph.Replace('\t',' ').Split(' ',StringSplitOptions.RemoveEmptyEntries))
                {
                    if(current.Length>0&&graphics!.MeasureString(current+" "+word,font).Width>505){DrawLine(current);current="";}
                    var remainder=word;
                    if(graphics!.MeasureString(remainder,font).Width>505)
                    {
                        var part=new StringBuilder();var elements=StringInfo.GetTextElementEnumerator(remainder);
                        while(elements.MoveNext()){var element=elements.GetTextElement();if(part.Length>0&&graphics.MeasureString(part.ToString()+element,font).Width>505){DrawLine(part.ToString());part.Clear();}part.Append(element);}remainder=part.ToString();
                    }
                    current=current.Length==0?remainder:current+" "+remainder;
                }
                DrawLine(current);
            }
        }
        try
        {
            NewPage();Draw(report.Title);Draw($"{report.Version} · captured {report.CapturedAt:O}");
            foreach(var line in report.Lines){Draw($"{line.Section} / {line.Label}: {line.Value} {line.Unit}");if(line.Basis.Length>0)Draw(line.Basis);if(line.Source.Length>0)Draw(line.Source);y+=5;}
            graphics?.Dispose();graphics=null;using var output=new MemoryStream();document.Save(output,false);return output.ToArray();
        }
        finally{graphics?.Dispose();}
    }
    public static async Task SaveNewAsync(string path,FrozenResearchReport report,string format,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        var data=format switch{"CSV"=>Encoding.UTF8.GetBytes(Csv(report)),"XLSX"=>Xlsx(report),"PDF"=>Pdf(report),_=>throw new ArgumentException("Choose CSV, XLSX or PDF.")};
        if(data.Length>32*1024*1024)throw new ArgumentException("Report exceeds 32 MiB.");
        var full=Path.GetFullPath(path);var expected="."+format.ToLowerInvariant();if(!full.EndsWith(expected,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Output extension must match report format.");
        var temp=full+".tmp-"+Guid.NewGuid().ToString("N");
        try{await using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,true)){await stream.WriteAsync(data,token);await stream.FlushAsync(token);stream.Flush(true);}token.ThrowIfCancellationRequested();File.Move(temp,full,false);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
