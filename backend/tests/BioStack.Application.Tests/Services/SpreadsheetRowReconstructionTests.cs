namespace BioStack.Application.Tests.Services;

using System.Diagnostics;
using System.IO.Compression;
using System.Security;
using System.Text;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Infrastructure.Knowledge;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

// BIO-ANALYZER-004: spreadsheet (CSV/XLSX) uploads are reconstructed one line per data row
// (`<name> <dose> <frequency> <duration>`) so header names never reach the parser and a row's
// dose binds to its compound. Real SpreadsheetProtocolExtractor -> real ProtocolParser with
// LocalKnowledgeSource; no mocks. Alias fixtures are limited to the five LocalKnowledgeSource
// compounds (BPC-157, TB-500, MOTS-C, NAD+, Retatrutide).
public sealed class SpreadsheetRowReconstructionTests
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string MalformedMessage = "The spreadsheet contains malformed content and could not be read.";

    // ---- T1 / T2 --------------------------------------------------------------------------

    private const string WideHeader =
        "Compound,Dose,Frequency,Duration,Vial Size,Start Date,Supplier,Qty,Price,Description";

    private static readonly string[] WideHeaderWords =
        ["Compound", "Dose", "Frequency", "Duration", "Vial Size", "Vial", "Size", "Start Date", "Start", "Date", "Supplier", "Qty", "Price", "Description"];

    private static string WideCsv() => Csv(
        WideHeader,
        "BPC-157,500mcg,daily,4 weeks,5mg,2026-10-01,Acme Labs,2,49.99,Take daily with food",
        "Retatrutide,2mg,weekly,12 weeks,10mg,2026-10-02,Peptide Co,1,89.00,Inject weekly at night");

    [Fact]
    public async Task T1_UnmappedHeaderColumns_NeverBecomeCompounds()
    {
        var outcome = await IngestCsvAsync(WideCsv());

        AssertNames(outcome, "BPC-157", "Retatrutide");
        foreach (var word in WideHeaderWords)
        {
            Assert.True(
                !outcome.Entries.Any(entry => entry.CompoundName.Contains(word, StringComparison.OrdinalIgnoreCase)),
                $"Entry named after header/unmapped cell '{word}'. Parsed: {outcome.Listing}");
        }
    }

    [Fact]
    public async Task T2_DoseBindsToTheRowsCompound()
    {
        var outcome = await IngestCsvAsync(WideCsv());

        AssertEntry(outcome, "BPC-157", 500, "mcg", "daily", "4 weeks");
        AssertEntry(outcome, "Retatrutide", 2, "mg", "weekly", "12 weeks");
    }

    // ---- T3 / T4 --------------------------------------------------------------------------

    [Fact]
    public async Task T3_UnknownCompound_IsEmittedWithItsDose()
    {
        var outcome = await IngestCsvAsync(Csv("Compound,Dose,Frequency", "Zorbatide,5mg,weekly"));

        AssertNames(outcome, "Zorbatide");
        AssertEntry(outcome, "Zorbatide", 5, "mg", "weekly");
    }

    [Fact]
    public async Task T4_UnmappedText_CannotRenameARow()
    {
        var outcome = await IngestCsvAsync(Csv("Compound,Dose,Notes", "BPC-157,500mcg,stack with TB-500"));

        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 500, "mcg");
    }

    // ---- T5 -------------------------------------------------------------------------------

    [Fact]
    public async Task T5_XlsxMissingCell_DoesNotShiftLaterColumns()
    {
        var xlsx = BuildWorkbook(new XSheet("Stack",
        [
            Cells(1, "Compound", "Dose", "Route", "Frequency"),
            // B2 is absent (writers omit empty cells): C2/D2 must stay under Route/Frequency.
            Cells(2, "BPC-157", null, "SubQ", "daily"),
        ]));

        var outcome = await IngestXlsxAsync(xlsx);

        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 0, "", "daily");
    }

    // ---- T6 -------------------------------------------------------------------------------

    [Fact]
    public async Task T6a_HeaderlessBomCsv_HasNoBomInText()
    {
        var outcome = await IngestCsvAsync("\uFEFF" + Csv("1,2,3", "4,5,6"));

        Assert.False(outcome.Ingestion.NormalizedText.Contains('\uFEFF'), "NormalizedText still contains U+FEFF.");
        Assert.Equal("Sheet: CSV\n1 | 2 | 3\n4 | 5 | 6", outcome.Text);
    }

    [Fact]
    public async Task T6b_QuotedCellBeforeMappedColumns_KeepsBindings()
    {
        var outcome = await IngestCsvAsync(
            "Compound,Notes,Dose,Frequency\nBPC-157,\"a, b\nc\",500mcg,daily\n");

        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 500, "mcg", "daily");
    }

    [Fact]
    public async Task T6c_BomPrefixedHeader_StillMaps()
    {
        var outcome = await IngestCsvAsync("\uFEFF" + Csv("Compound,Dose,Frequency", "BPC-157,500mcg,daily"));

        Assert.False(outcome.Ingestion.NormalizedText.Contains('\uFEFF'), "NormalizedText still contains U+FEFF.");
        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 500, "mcg", "daily");
    }

    // ---- T7 -------------------------------------------------------------------------------

    [Theory]
    [InlineData("Compound,Dose (mg)\nBPC-157,5", 5, "mg")]
    [InlineData("Compound,Dose,Unit\nBPC-157,250,mcg", 250, "mcg")]
    [InlineData("Compound,Dose (per injection)\nBPC-157,5", 0, "")]
    [InlineData("Compound,Dose (mg),Unit\nBPC-157,5,IU", 5, "mg")]
    [InlineData("Compound,Dose,Unit\nBPC-157,5,IU", 0, "")]
    public async Task T7_UnitHints(string csv, double expectedDose, string expectedUnit)
    {
        var outcome = await IngestCsvAsync(csv + "\n");

        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", expectedDose, expectedUnit);
    }

    // ---- T8 -------------------------------------------------------------------------------

    [Theory]
    [InlineData("Foo,Bar\nBPC-157,500mcg")]
    [InlineData("Compound Name,Dose\nBPC-157,500mcg")]
    public async Task T8_NonReconstructableTable_FallsBackToValuesOnly(string csv)
    {
        var outcome = await IngestCsvAsync(csv + "\n");

        Assert.Equal("Sheet: CSV\nBPC-157 | 500mcg", outcome.Text);
        foreach (var word in new[] { "Foo", "Bar", "Compound", "Name", "Dose" })
        {
            Assert.True(
                !outcome.Text.Contains(word, StringComparison.Ordinal),
                $"Header word '{word}' leaked into: {outcome.Text}");
        }
    }

    // ---- T9 -------------------------------------------------------------------------------

    [Fact]
    public async Task T9_SkippedRows_AreCountedInOneAggregatedWarning()
    {
        var outcome = await IngestCsvAsync(Csv("Compound,Dose", "BPC-157,500mcg", ",250mcg", ",300mcg"));

        AssertNames(outcome, "BPC-157");
        var warning = Assert.Single(outcome.Ingestion.Warnings);
        Assert.Equal("2 row(s) skipped: no compound name found.", warning);
        Assert.DoesNotContain("250", warning);
        Assert.DoesNotContain("300", warning);
    }

    [Fact]
    public async Task T9_MultiSheetXlsx_AggregatesSkippedRowsAcrossSheets()
    {
        var xlsx = BuildWorkbook(
            new XSheet("Alpha",
            [
                Cells(1, "Compound", "Dose"),
                Cells(2, "BPC-157", "500mcg"),
                Cells(3, null, "250mcg"),
            ]),
            new XSheet("Beta",
            [
                Cells(1, "Compound", "Dose"),
                Cells(2, "TB-500", "2mg"),
                Cells(3, null, "900mcg"),
            ]));

        var outcome = await IngestXlsxAsync(xlsx);

        AssertNames(outcome, "BPC-157", "TB-500");
        var warning = Assert.Single(outcome.Ingestion.Warnings);
        Assert.Equal("2 row(s) skipped: no compound name found.", warning);
        Assert.DoesNotContain("Alpha", warning);
        Assert.DoesNotContain("Beta", warning);
        Assert.DoesNotContain("250", warning);
    }

    // ---- T10 ------------------------------------------------------------------------------

    [Fact]
    public async Task T10_TrailingBlankCsvRows_AreNotCountedOrEmitted()
    {
        var outcome = await IngestCsvAsync("Compound,Dose,Frequency\nBPC-157,500mcg,daily\n,,\n  , ,\n\n");

        Assert.Empty(outcome.Ingestion.Warnings);
        AssertNames(outcome, "BPC-157");
    }

    [Fact]
    public async Task T10b_BlankCsvRows_AreNotEmittedInText()
    {
        var outcome = await IngestCsvAsync("Compound,Dose,Frequency\nBPC-157,500mcg,daily\n,,\n  , ,\n\n");

        Assert.Equal("Sheet: CSV\nBPC-157 500mcg daily", outcome.Text);
    }

    [Fact]
    public async Task T10_BlankXlsxRows_AreNotCountedOrEmitted()
    {
        var xlsx = BuildWorkbook(new XSheet("Stack",
        [
            Cells(1, "Compound", "Dose", "Frequency"),
            Cells(2, "BPC-157", "500mcg", "daily"),
            [new XCell("A3", "   ", true), new XCell("B3", null, false)],
            [],
        ]));

        var outcome = await IngestXlsxAsync(xlsx);

        Assert.Empty(outcome.Ingestion.Warnings);
        AssertNames(outcome, "BPC-157");
    }

    // ---- T11 ------------------------------------------------------------------------------

    [Theory]
    [InlineData("COMPOUND,DOSE,FREQUENCY")]
    [InlineData("Compound,Dose,Frequency")]
    public async Task T11_HeaderCaseVariants_MapIdentically(string header)
    {
        var outcome = await IngestCsvAsync(Csv(header, "BPC-157,500mcg,daily"));

        AssertEntry(outcome, "BPC-157", 500, "mcg", "daily");
    }

    // ---- T12 ------------------------------------------------------------------------------

    [Fact]
    public async Task T12_DuplicateDoseRoles_FollowPriority()
    {
        var both = await IngestCsvAsync(Csv("Compound,Strength,Dose", "BPC-157,5mg,250mcg"));
        AssertEntry(both, "BPC-157", 250, "mcg");

        var strengthOnly = await IngestCsvAsync(Csv("Compound,Strength", "BPC-157,5mg"));
        AssertEntry(strengthOnly, "BPC-157", 5, "mg");
    }

    // ---- T13 ------------------------------------------------------------------------------

    [Fact]
    public async Task T13_NameColumnWithoutLetter_FallsThroughToNextNameColumn()
    {
        var outcome = await IngestCsvAsync(Csv("Item,Compound,Dose", "1,BPC-157,500mcg"));

        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 500, "mcg");
    }

    // ---- T14 (regression lock: green before and after) ------------------------------------

    [Fact]
    public async Task T14_MultiCompoundNameCell_BindsNoDose()
    {
        var outcome = await IngestCsvAsync(Csv("Compound,Dose", "BPC-157 + TB-500,500mcg"));

        AssertNames(outcome, "BPC-157", "TB-500");
        AssertEntry(outcome, "BPC-157", 0, "");
        AssertEntry(outcome, "TB-500", 0, "");
    }

    // ---- T15 ------------------------------------------------------------------------------

    [Fact]
    public async Task T15a_LineBreakInNameCell_DoesNotFragmentTheRow()
    {
        var outcome = await IngestCsvAsync("Compound,Dose\n\"BPC-157\n(Wolverine)\",500mcg\n");

        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 500, "mcg");
    }

    [Theory]
    [InlineData("\"250mcg; 500mcg\"")]
    [InlineData("\"250mcg, 500mcg\"")]
    public async Task T15b_MultipleDosesInOneCell_KeepFirstAndDoNotSwallowFrequency(string doseCell)
    {
        var outcome = await IngestCsvAsync(Csv("Compound,Dose,Frequency", $"BPC-157,{doseCell},daily"));

        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 250, "mcg", "daily");
    }

    [Fact]
    public async Task T15c_BlendNamedRow_ParentheticalFrequencyDoesNotCreateComponent()
    {
        var outcome = await IngestCsvAsync(Csv("Compound,Dose,Frequency", "Wolverine Blend,500mcg,daily (AM)"));

        AssertNames(outcome, "Wolverine Blend");
        AssertEntry(outcome, "Wolverine Blend", 500, "mcg", "daily");
    }

    // ---- T16 ------------------------------------------------------------------------------

    [Fact]
    public async Task T16a_CsvRowsWiderThanHeader_IgnoreExtraCells()
    {
        var outcome = await IngestCsvAsync(Csv(
            "Compound,Dose,Frequency",
            "BPC-157,500mcg,daily,",
            "TB-500,2mg,weekly,Junk 9mg weekly"));

        AssertNames(outcome, "BPC-157", "TB-500");
        AssertEntry(outcome, "BPC-157", 500, "mcg", "daily");
        AssertEntry(outcome, "TB-500", 2, "mg", "weekly");
    }

    [Fact]
    public async Task T16b_XlsxCellPastHeaderWidth_IsIgnored()
    {
        var xlsx = BuildWorkbook(new XSheet("Stack",
        [
            Cells(1, "Compound", "Dose", "Frequency"),
            Cells(2, "BPC-157", "500mcg", "daily", "Junk 9mg weekly"),
        ]));

        var outcome = await IngestXlsxAsync(xlsx);

        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 500, "mcg", "daily");
    }

    // ---- T17 ------------------------------------------------------------------------------

    [Fact]
    public async Task T17_HostileCellReferences_AreBoundedAndDoNotBreakTheValidRow()
    {
        var xlsx = BuildWorkbook(new XSheet("Stack",
        [
            Cells(1, "Compound", "Dose", "Frequency"),
            [
                // Malformed (7 letters) first cell: placed at column 0, then overwritten by A2 (last wins).
                new XCell("ZZZZZZZ1", "Hostile one 9mg weekly", true),
                new XCell("A2", "BPC-157", true),
                // Duplicate reference: the last cell wins.
                new XCell("B2", "TBD", true),
                new XCell("B2", "500mcg", true),
                new XCell("C2", "daily", true),
                // Malformed `r` after C2 is placed at column 3 (past the header width): ignored.
                new XCell("1A", "Hostile two 9mg daily", true),
                // Past XFD: ignored.
                new XCell("XFE2", "Hostile three 9mg daily", true),
                new XCell("ZZZ2", "Hostile four 9mg daily", true),
                // XFD itself is a legal column but far past the header width: never materialised.
                new XCell("XFD2", "Hostile five 9mg daily", true),
            ],
        ]));

        var stopwatch = Stopwatch.StartNew();
        var outcome = await IngestXlsxAsync(xlsx);
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Ingestion took {stopwatch.Elapsed}.");
        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 500, "mcg", "daily");
        Assert.DoesNotContain("Hostile", outcome.Text);
    }

    [Fact]
    public async Task T17b_ValuesOnlyFallback_KeepsCellsUpToXfdAndDropsPastIt()
    {
        var xlsx = BuildWorkbook(new XSheet("Stack",
        [
            Cells(1, "Foo", "Bar"),
            [
                new XCell("A2", "BPC-157", true),
                new XCell("XFD2", "FarEdge", true),
                new XCell("XFE2", "PastEdge", true),
            ],
        ]));

        var outcome = await IngestXlsxAsync(xlsx);

        Assert.Equal("Sheet: Stack\nBPC-157 | FarEdge", outcome.Text);
    }

    // ---- T18 ------------------------------------------------------------------------------

    [Fact]
    public async Task T18_SparseColumnsPastZ_BindCorrectly()
    {
        var header = new List<XCell> { new("A1", "Compound", false) };
        for (var column = 1; column <= 25; column++)
        {
            header.Add(new XCell($"{ColumnLetters(column)}1", $"Filler {ColumnLetters(column)}", false));
        }

        header.Add(new XCell("AA1", "Frequency", false));
        header.Add(new XCell("AB1", "Notes", false));

        var xlsx = BuildWorkbook(new XSheet("Stack",
        [
            header.ToArray(),
            [new XCell("A2", "BPC-157", false), new XCell("AA2", "daily", false)],
        ]));

        var outcome = await IngestXlsxAsync(xlsx);

        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 0, "", "daily");
    }

    // ---- T19 ------------------------------------------------------------------------------

    [Fact]
    public async Task T19_InlineStrings_ReconstructLikeSharedStrings()
    {
        var shared = await IngestXlsxAsync(BuildWorkbook(new XSheet("Stack",
        [
            Cells(1, "Compound", "Dose", "Frequency"),
            Cells(2, "BPC-157", "500mcg", "daily"),
        ])));

        var inline = await IngestXlsxAsync(BuildWorkbook(new XSheet("Stack",
        [
            InlineCells(1, "Compound", "Dose", "Frequency"),
            InlineCells(2, "BPC-157", "500mcg", "daily"),
        ])));

        Assert.Equal("Sheet: Stack\nBPC-157 500mcg daily", shared.Text);
        Assert.Equal(shared.Text, inline.Text);
        AssertEntry(inline, "BPC-157", 500, "mcg", "daily");
    }

    // ---- T20 ------------------------------------------------------------------------------

    [Fact]
    public async Task T20a_RowBreakStyles_GiveIdenticalResults()
    {
        var rows = new[] { "Compound,Dose,Frequency", "BPC-157,500mcg,daily", "TB-500,2mg,weekly" };
        var lf = await IngestCsvAsync(string.Join("\n", rows));
        var crlf = await IngestCsvAsync(string.Join("\r\n", rows));
        var cr = await IngestCsvAsync(string.Join("\r", rows));

        Assert.Equal("Sheet: CSV\nBPC-157 500mcg daily\nTB-500 2mg weekly", lf.Text);
        Assert.Equal(lf.Text, crlf.Text);
        Assert.Equal(lf.Text, cr.Text);
    }

    [Fact]
    public async Task T20b_UnterminatedQuote_IsAFriendlyIngestionException()
    {
        var ex = await Assert.ThrowsAsync<ProtocolIngestionException>(() =>
            IngestCsvAsync("Compound,Dose\nBPC-157,\"500mcg\nTB-500,2mg\n"));

        Assert.Equal(MalformedMessage, ex.Message);
    }

    [Fact]
    public async Task T20c_SemicolonDelimitedCsv_DoesNotThrowAndFallsBackToValuesOnly()
    {
        var outcome = await IngestCsvAsync(Csv("Compound;Dose;Frequency", "BPC-157;500mcg;daily"));

        Assert.DoesNotContain("Compound", outcome.Text);
        Assert.Contains("BPC-157;500mcg;daily", outcome.Text);
    }

    [Fact]
    public async Task T20d_QuoteHandling_FollowsTheSpecifiedRules()
    {
        // Values-only table (no name role) so the raw cell text is visible.
        var outcome = await IngestCsvAsync(
            "Foo,Bar\n" +
            "\"say \"\"hi\"\"\",x\"y\n" +
            "\"abc\"def,z\n" +
            "  \"padded, comma\"  ,w\n");

        Assert.Equal("Sheet: CSV\nsay \"hi\" | x\"y\nabcdef | z\npadded, comma | w", outcome.Text);
    }

    // ---- T21 (regression lock: green before and after) -----------------------------------

    [Fact]
    public async Task T21_NoHeaderSheet_EmitsValuesOnly()
    {
        var outcome = await IngestCsvAsync(Csv("1,2,3", "4,5,6"));

        Assert.Equal("Sheet: CSV\n1 | 2 | 3\n4 | 5 | 6", outcome.Text);
    }

    [Fact]
    public async Task T21b_NoHeaderSheet_OmitsBlankCells()
    {
        var outcome = await IngestCsvAsync(Csv("1,,3", "4,5,"));

        Assert.Equal("Sheet: CSV\n1 | 3\n4 | 5", outcome.Text);
    }

    // ---- T22 ------------------------------------------------------------------------------

    [Fact]
    public async Task T22_IngestionCacheVersionBump_IgnoresStaleV1Entry()
    {
        var cache = CreateCache();
        var service = CreateIngestionService(cache);
        var bytes = Encoding.UTF8.GetBytes(Csv("Compound,Dose,Frequency", "BPC-157,500mcg,daily"));
        var request = new ProtocolIngestionRequest(ProtocolInputType.FileUpload, null, null, "protocol.csv", "text/csv", bytes);
        var fingerprints = new ProtocolFingerprintService();
        var fingerprint = fingerprints.GetIngestionFingerprint(request);

        var oldKey = $"analyzer:ingestion:fileupload:v1:{fingerprint}";
        Assert.NotEqual(oldKey, fingerprints.GetIngestionCacheKey(request, fingerprint));

        const string staleText = "Sheet: CSV\nCompound: BPC-157 | Dose: 500mcg | Frequency: daily";
        await cache.SetIngestionAsync(
            oldKey,
            new IngestionCacheDto(staleText, staleText, ProtocolInputType.FileUpload, "protocol.csv", [], [], false, fingerprint, "stale"),
            TimeSpan.FromDays(7),
            CancellationToken.None);

        var result = await service.IngestAsync(request);

        Assert.Equal("Sheet: CSV\nBPC-157 500mcg daily", result.NormalizedText.Replace("\r\n", "\n"));
    }

    // ---- T24 ------------------------------------------------------------------------------

    [Fact]
    public async Task T24a_DoseColumnWinsOverQuantityTypedIntoName()
    {
        var outcome = await IngestCsvAsync(Csv("Compound,Dose,Frequency", "BPC-157 5mg,250mcg,daily"));

        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 250, "mcg", "daily");
    }

    [Fact]
    public async Task T24b_FrequencyColumnWinsOverFrequencyTypedIntoName()
    {
        var outcome = await IngestCsvAsync(Csv("Compound,Dose,Frequency", "Retatrutide weekly,2mg,daily"));

        AssertNames(outcome, "Retatrutide");
        AssertEntry(outcome, "Retatrutide", 2, "mg", "daily");
    }

    [Fact]
    public async Task T24c_EmbeddedQuantity_ServesWhenNoDoseColumnHasAValue()
    {
        var outcome = await IngestCsvAsync(Csv("Compound,Frequency", "BPC-157 5mg,daily"));

        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 5, "mg", "daily");
    }

    // ---- T25 ------------------------------------------------------------------------------

    [Theory]
    [InlineData("250-500mcg")]
    [InlineData("250mcg-500mcg")]
    [InlineData("250 mcg - 500 mcg")]
    [InlineData("0.5 to 1 mg")]
    [InlineData("0.5mg to 1mg")]
    [InlineData("\"1,000 mcg\"")]
    public async Task T25_DoseRangesAndSeparators_AreOmitted(string subjectDose)
    {
        var outcome = await IngestCsvAsync(Csv(
            "Compound,Dose,Frequency,Duration",
            "BPC-157,500mcg,daily,12 weeks",
            $"TB-500,{subjectDose},daily,4 weeks"));

        AssertNames(outcome, "BPC-157", "TB-500");
        AssertEntry(outcome, "BPC-157", 500, "mcg", "daily", "12 weeks");
        AssertEntry(outcome, "TB-500", 0, "", "daily", "4 weeks");
    }

    [Fact]
    public async Task T25_DurationRange_IsOmitted()
    {
        var outcome = await IngestCsvAsync(Csv(
            "Compound,Dose,Frequency,Duration",
            "BPC-157,500mcg,daily,12 weeks",
            "TB-500,2mg,daily,4-6 weeks"));

        AssertNames(outcome, "BPC-157", "TB-500");
        AssertEntry(outcome, "BPC-157", 500, "mcg", "daily", "12 weeks");
        AssertEntry(outcome, "TB-500", 2, "mg", "daily", "");
    }

    // ---- T26 ------------------------------------------------------------------------------

    [Fact]
    public async Task T26_CyclePhraseInDuration_IsOmitted()
    {
        var outcome = await IngestCsvAsync(Csv(
            "Compound,Dose,Frequency,Duration",
            "BPC-157,500mcg,daily,12 weeks",
            "TB-500,2mg,daily,\"8 weeks on, 8 weeks off\""));

        AssertNames(outcome, "BPC-157", "TB-500");
        AssertEntry(outcome, "BPC-157", 500, "mcg", "daily", "12 weeks");
        AssertEntry(outcome, "TB-500", 2, "mg", "daily", "");
    }

    // ---- T27 ------------------------------------------------------------------------------

    [Fact]
    public async Task T27_DoseIsEmittedVerbatim()
    {
        var outcome = await IngestCsvAsync(Csv(
            "Compound,Dose,Frequency",
            "BPC-157,500mcg,daily",
            "TB-500,5 mg,daily"));

        Assert.Contains("BPC-157 500mcg daily", outcome.Text);
        Assert.Contains("TB-500 5 mg daily", outcome.Text);
    }

    // ---- T28 ------------------------------------------------------------------------------

    [Theory]
    [InlineData("Zorbatide (5mg)")]
    [InlineData("Zorbatide - 5mg")]
    // Regression (review F1): cleanup must run even when no embedded quantity was removed.
    [InlineData("Zorbatide ()")]
    [InlineData("Zorbatide -")]
    [InlineData("Zorbatide()")]
    public async Task T28a_NameCleanup_DoesNotDropUnknownCompound(string nameCell)
    {
        var outcome = await IngestCsvAsync(Csv("Compound,Dose,Frequency", $"{nameCell},250mcg,weekly"));

        Assert.Equal("Sheet: CSV\nZorbatide 250mcg weekly", outcome.Text);
        AssertNames(outcome, "Zorbatide");
        AssertEntry(outcome, "Zorbatide", 250, "mcg", "weekly");
    }

    [Theory]
    [InlineData("TBD")]
    [InlineData("250-500mcg")]
    public async Task T28b_OmittedDoseCell_SuppressesNameEmbeddedDose(string doseCell)
    {
        var outcome = await IngestCsvAsync(Csv("Compound,Dose", $"BPC-157 5mg,{doseCell}"));

        AssertNames(outcome, "BPC-157");
        AssertEntry(outcome, "BPC-157", 0, "");
    }

    // ---- harness --------------------------------------------------------------------------

    private sealed record Outcome(ProtocolIngestionResult Ingestion, IReadOnlyList<ProtocolEntryResponse> Entries)
    {
        public string Text => Ingestion.NormalizedText.Replace("\r\n", "\n");

        public string Listing => string.Join(" | ", Entries.Select(entry =>
            $"{entry.CompoundName} [{entry.Dose}{entry.Unit} {entry.Frequency} {entry.Duration}]".TrimEnd()));
    }

    private static Task<Outcome> IngestCsvAsync(string csv) =>
        IngestAsync("protocol.csv", "text/csv", Encoding.UTF8.GetBytes(csv));

    private static Task<Outcome> IngestXlsxAsync(byte[] xlsx) =>
        IngestAsync("protocol.xlsx", XlsxContentType, xlsx);

    private static async Task<Outcome> IngestAsync(string fileName, string contentType, byte[] bytes)
    {
        var service = CreateIngestionService(CreateCache());
        var ingestion = await service.IngestAsync(new ProtocolIngestionRequest(
            ProtocolInputType.FileUpload, null, null, fileName, contentType, bytes));

        var parser = new ProtocolParser(new LocalKnowledgeSource(), new BlendDecomposerService(), new MemoryCache(new MemoryCacheOptions()));
        var parsed = await parser.ParseAsync(ingestion.NormalizedText);
        return new Outcome(ingestion, parsed.Entries);
    }

    private static void AssertNames(Outcome outcome, params string[] expected)
    {
        var names = outcome.Entries.Select(entry => entry.CompoundName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var leaked = names.Except(expected, StringComparer.OrdinalIgnoreCase).ToList();
        var missing = expected.Except(names, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.True(
            leaked.Count == 0 && missing.Count == 0,
            $"Expected exactly {{{string.Join(", ", expected)}}}. Leaked: [{string.Join(" | ", leaked)}] Missing: [{string.Join(" | ", missing)}]. " +
            $"Parsed: {outcome.Listing}. Text: {outcome.Text.Replace("\n", " // ")}");
    }

    private static void AssertEntry(
        Outcome outcome,
        string name,
        double dose,
        string unit,
        string frequency = "",
        string duration = "")
    {
        var matches = outcome.Entries
            .Where(entry => string.Equals(entry.CompoundName, name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.True(
            matches.Count == 1,
            $"Expected exactly one entry '{name}'. Parsed: {outcome.Listing}. Text: {outcome.Text.Replace("\n", " // ")}");

        var entry = matches[0];
        var actual = $"{entry.Dose}|{entry.Unit}|{entry.Frequency}|{entry.Duration}";
        var expected = $"{dose}|{unit}|{frequency}|{duration}";
        Assert.True(
            string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase),
            $"Entry '{name}' expected dose|unit|frequency|duration = {expected} but was {actual}. Text: {outcome.Text.Replace("\n", " // ")}");
    }

    private static string Csv(params string[] lines) => string.Join("\n", lines) + "\n";

    private static ProtocolAnalysisCache CreateCache() => new(
        new MemoryCache(new MemoryCacheOptions()),
        new MemoryDistributedCache(new OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions())),
        NullLogger<ProtocolAnalysisCache>.Instance);

    private static ProtocolIngestionService CreateIngestionService(ProtocolAnalysisCache cache) => new(
        [new SpreadsheetProtocolExtractor()],
        new ProtocolNormalizationService(),
        new ProtocolFingerprintService(),
        cache,
        NullLogger<ProtocolIngestionService>.Instance);

    // ---- XLSX builder (pattern of ProtocolIngestionServiceTests.CreateMinimalXlsx / AddEntry) ----

    private readonly record struct XCell(string? Ref, string? Value, bool Inline);

    private sealed record XSheet(string Name, XCell[][] Rows);

    private static string ColumnLetters(int index)
    {
        var letters = string.Empty;
        var value = index + 1;
        while (value > 0)
        {
            value--;
            letters = (char)('A' + (value % 26)) + letters;
            value /= 26;
        }

        return letters;
    }

    // Positional shared-string cells; a null value omits the cell (as spreadsheet writers do).
    private static XCell[] Cells(int rowNumber, params string?[] values) =>
        values
            .Select((value, column) => (value, column))
            .Where(item => item.value is not null)
            .Select(item => new XCell($"{ColumnLetters(item.column)}{rowNumber}", item.value, false))
            .ToArray();

    private static XCell[] InlineCells(int rowNumber, params string?[] values) =>
        values
            .Select((value, column) => (value, column))
            .Where(item => item.value is not null)
            .Select(item => new XCell($"{ColumnLetters(item.column)}{rowNumber}", item.value, true))
            .ToArray();

    private static byte[] BuildWorkbook(params XSheet[] sheets)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                </Types>
                """);
            AddEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);

            var sheetElements = new StringBuilder();
            var relationships = new StringBuilder();
            var strings = new List<string>();
            for (var index = 0; index < sheets.Length; index++)
            {
                var number = index + 1;
                sheetElements.Append($"<sheet name=\"{SecurityElement.Escape(sheets[index].Name)}\" sheetId=\"{number}\" r:id=\"rId{number}\"/>");
                relationships.Append(
                    $"<Relationship Id=\"rId{number}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{number}.xml\"/>");
                AddEntry(archive, $"xl/worksheets/sheet{number}.xml", BuildSheetXml(sheets[index], strings));
            }

            AddEntry(archive, "xl/workbook.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                $"<sheets>{sheetElements}</sheets></workbook>");
            AddEntry(archive, "xl/_rels/workbook.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                relationships +
                "</Relationships>");
            AddEntry(archive, "xl/sharedStrings.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                string.Concat(strings.Select(value => $"<si><t xml:space=\"preserve\">{SecurityElement.Escape(value)}</t></si>")) +
                "</sst>");
        }

        return memory.ToArray();
    }

    private static string BuildSheetXml(XSheet sheet, List<string> strings)
    {
        var data = new StringBuilder();
        for (var rowIndex = 0; rowIndex < sheet.Rows.Length; rowIndex++)
        {
            data.Append($"<row r=\"{rowIndex + 1}\">");
            foreach (var cell in sheet.Rows[rowIndex])
            {
                var reference = cell.Ref is null ? string.Empty : $" r=\"{cell.Ref}\"";
                if (cell.Value is null)
                {
                    data.Append($"<c{reference}/>");
                }
                else if (cell.Inline)
                {
                    data.Append($"<c{reference} t=\"inlineStr\"><is><t xml:space=\"preserve\">{SecurityElement.Escape(cell.Value)}</t></is></c>");
                }
                else
                {
                    var stringIndex = strings.IndexOf(cell.Value);
                    if (stringIndex < 0)
                    {
                        strings.Add(cell.Value);
                        stringIndex = strings.Count - 1;
                    }

                    data.Append($"<c{reference} t=\"s\"><v>{stringIndex}</v></c>");
                }
            }

            data.Append("</row>");
        }

        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
               "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
               data +
               "</sheetData></worksheet>";
    }

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
