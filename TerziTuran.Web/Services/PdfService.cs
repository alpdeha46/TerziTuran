using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Services;

public interface IPdfService
{
    byte[] GenerateReportPdf(ReportsFilterViewModel model);
}

public class PdfService : IPdfService
{
    public byte[] GenerateReportPdf(ReportsFilterViewModel model)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.Header().Text("TerziTuran Raporu").FontSize(22).SemiBold().FontColor(Colors.Blue.Darken3);
                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text($"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}");
                    col.Item().Text($"Filtreler: Tarih {model.StartDate:dd.MM.yyyy} - {model.EndDate:dd.MM.yyyy}, Musteri: {(model.CustomerId.HasValue ? model.Customers.FirstOrDefault(x => x.Id == model.CustomerId)?.FullName : "Tum Musteriler")}, Kategori: {model.Category ?? "Tum Kategoriler"}");
                    col.Item().Text($"Toplam Siparis: {model.Rows.Count} | Toplam Tutar: {model.Rows.Sum(x => x.Price):N2} TL | Tahsilat: {model.Rows.Sum(x => x.PaidAmount):N2} TL | Kalan: {model.Rows.Sum(x => x.RemainingAmount):N2} TL");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Musteri");
                            header.Cell().Element(CellStyle).Text("Siparis");
                            header.Cell().Element(CellStyle).Text("Kategori");
                            header.Cell().Element(CellStyle).Text("Durum");
                            header.Cell().Element(CellStyle).Text("Fiyat");
                            header.Cell().Element(CellStyle).Text("Odenen");
                            header.Cell().Element(CellStyle).Text("Teslim");
                        });

                        foreach (var row in model.Rows)
                        {
                            table.Cell().Element(BodyStyle).Text(row.CustomerName);
                            table.Cell().Element(BodyStyle).Text(row.OrderTitle);
                            table.Cell().Element(BodyStyle).Text(row.Category);
                            table.Cell().Element(BodyStyle).Text(row.Status);
                            table.Cell().Element(BodyStyle).Text($"{row.Price:N0} TL");
                            table.Cell().Element(BodyStyle).Text($"{row.PaidAmount:N0} TL");
                            table.Cell().Element(BodyStyle).Text(row.DeliveryDate.ToString("dd.MM.yyyy"));
                        }
                    });
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("TerziTuran");
                    text.Span(" | Sayfa ");
                    text.CurrentPageNumber();
                });
            });
        }).GeneratePdf();

        static IContainer CellStyle(IContainer container)
            => container.Padding(4).Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1);

        static IContainer BodyStyle(IContainer container)
            => container.Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
    }
}
