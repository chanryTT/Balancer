using BalanceSystem.Core.Models;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BalanceSystem.Infrastructure.Reporting;

public class TestReportService
{
    private readonly ILogger<TestReportService> _logger;

    static TestReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public TestReportService(ILogger<TestReportService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 生成PDF测试报告并保存到指定路径。
    /// </summary>
    /// <param name="record">测试记录</param>
    /// <param name="recipe">关联配方（可选）</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <returns>生成的PDF文件路径</returns>
    public Task<string> GenerateReportAsync(TestRecord record, Recipe? recipe, string outputPath)
    {
        return Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("SimHei"));

                    // ── Header ──
                    page.Header().Element(c =>
                    {
                        c.Column(col =>
                        {
                            col.Item().AlignCenter().Text("动平衡测试报告")
                                .FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                            col.Item().AlignCenter().Text($"报告生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        });
                    });

                    // ── Content ──
                    page.Content().Element(c =>
                    {
                        c.Column(col =>
                        {
                            col.Spacing(8);

                            // Section 1: Basic info
                            col.Item().Text("一、基本信息").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().Element(container =>
                            {
                                container.Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(); cd.RelativeColumn();
                                        cd.RelativeColumn(); cd.RelativeColumn();
                                    });
                                    void Row(string label, string value, string label2, string value2)
                                    {
                                        table.Cell().Text(label).FontSize(10).FontColor(Colors.Grey.Darken1);
                                        table.Cell().Text(value).FontSize(10).Bold();
                                        table.Cell().Text(label2).FontSize(10).FontColor(Colors.Grey.Darken1);
                                        table.Cell().Text(value2).FontSize(10).Bold();
                                    }
                                    Row("配方名称:", recipe?.Name ?? "未知", "测试时间:", record.TestTime.ToString("yyyy-MM-dd HH:mm:ss"));
                                    Row("额定转速:", $"{record.Speed} RPM", "判定结果:", record.IsPassed ? "合格 ✓" : "不合格 ✗");
                                });
                            });

                            col.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                            // Section 2: Initial vibration
                            col.Item().Text("二、初始振动数据").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().Element(container =>
                            {
                                container.Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(); cd.RelativeColumn();
                                        cd.RelativeColumn(); cd.RelativeColumn();
                                    });
                                    table.Header(header =>
                                    {
                                        header.Cell().Text("").FontSize(10);
                                        header.Cell().Text("幅值 (μm)").FontSize(10).Bold();
                                        header.Cell().Text("相位 (°)").FontSize(10).Bold();
                                        header.Cell().Text("").FontSize(10);
                                    });
                                    void PlaneRow(string plane, double amp, double phase)
                                    {
                                        table.Cell().Text(plane).FontSize(11).Bold();
                                        table.Cell().Text($"{amp:F2}").FontSize(11);
                                        table.Cell().Text($"{phase:F1}°").FontSize(11);
                                        table.Cell().Text("").FontSize(11);
                                    }
                                    PlaneRow("左面:", record.InitialLeftAmplitude, record.InitialLeftPhase);
                                    PlaneRow("右面:", record.InitialRightAmplitude, record.InitialRightPhase);
                                });
                            });

                            col.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                            // Section 3: Correction result
                            col.Item().Text("三、配重结果").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().Element(container =>
                            {
                                container.Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(); cd.RelativeColumn();
                                        cd.RelativeColumn(); cd.RelativeColumn();
                                    });
                                    table.Header(header =>
                                    {
                                        header.Cell().Text("平面").FontSize(10).Bold();
                                        header.Cell().Text("配重质量 (g)").FontSize(10).Bold();
                                        header.Cell().Text("配重角度 (°)").FontSize(10).Bold();
                                        header.Cell().Text("剩余不平衡量 (μm)").FontSize(10).Bold();
                                    });
                                    table.Cell().Text("左面").FontSize(11).Bold();
                                    table.Cell().Text($"{record.LeftCorrectionMass:F2}").FontSize(11);
                                    table.Cell().Text($"{record.LeftCorrectionAngle:F1}°").FontSize(11);
                                    table.Cell().Text($"{record.ResidualLeft:F2}").FontSize(11);

                                    table.Cell().Text("右面").FontSize(11).Bold();
                                    table.Cell().Text($"{record.RightCorrectionMass:F2}").FontSize(11);
                                    table.Cell().Text($"{record.RightCorrectionAngle:F1}°").FontSize(11);
                                    table.Cell().Text($"{record.ResidualRight:F2}").FontSize(11);
                                });
                            });

                            // Section 4: Verdict
                            col.Item().PaddingTop(12);
                            col.Item().Element(container =>
                            {
                                var (verdictText, verdictColor) = record.IsPassed
                                    ? ("判定: 合格", Colors.Green.Darken2)
                                    : ("判定: 不合格", Colors.Red.Darken2);
                                container.Background(record.IsPassed ? Colors.Green.Lighten4 : Colors.Red.Lighten4)
                                    .Padding(12)
                                    .AlignCenter()
                                    .Text(verdictText)
                                    .FontSize(18).Bold().FontColor(verdictColor);
                            });

                            if (record.Notes is not null)
                            {
                                col.Item().PaddingTop(8).Text($"备注: {record.Notes}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            }
                        });
                    });

                    // ── Footer ──
                    page.Footer().Element(c =>
                    {
                        c.AlignCenter().Text(text =>
                        {
                            text.Span("BalanceSystem — 动平衡测试系统").FontSize(8).FontColor(Colors.Grey.Medium);
                            text.Span("    ");
                            text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                            text.Span(" / ");
                            text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            }).GeneratePdf(outputPath);

            _logger.LogInformation("Test report generated: {Path}", outputPath);
            return outputPath;
        });
    }
}
