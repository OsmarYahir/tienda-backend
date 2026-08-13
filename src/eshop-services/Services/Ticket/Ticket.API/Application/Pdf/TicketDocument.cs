using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Ticket.API.Application.Orders;

namespace Ticket.API.Application.Pdf
{
    // Documento QuestPDF para el ticket de una orden: encabezado con "logo", info de la
    // orden, tabla de productos y totales. No hay un archivo de imagen real para el logo
    // (este proyecto no trae assets binarios) — se dibuja como una caja de color con texto,
    // que es trivial de reemplazar por `image.Image(logoBytes)` si se agrega un logo real.
    public class TicketDocument(OrderDto order) : IDocument
    {
        private static readonly CultureInfo Currency = CultureInfo.GetCultureInfo("es-MX");

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Gracias por tu compra — ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span("MiTienda").FontSize(8).SemiBold();
                });
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("MiTienda").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text("Ticket de compra").FontSize(11).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(90).Height(40)
                    .Background(Colors.Blue.Darken2)
                    .AlignCenter().AlignMiddle()
                    .Text("MT").FontColor(Colors.White).Bold().FontSize(18);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingVertical(15).Column(column =>
            {
                column.Spacing(12);

                column.Item().Element(ComposeOrderInfo);
                column.Item().Element(ComposeItemsTable);
                column.Item().Element(ComposeTotals);
            });
        }

        private void ComposeOrderInfo(IContainer container)
        {
            container.Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(t =>
                    {
                        t.Span("N° de orden: ").SemiBold();
                        t.Span(order.Id);
                    });
                    col.Item().Text(t =>
                    {
                        t.Span("Cliente: ").SemiBold();
                        t.Span(order.CustomerId);
                    });
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignRight().Text(t =>
                    {
                        t.Span("Fecha: ").SemiBold();
                        t.Span(order.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                    });
                    col.Item().AlignRight().Text(t =>
                    {
                        t.Span("Estado: ").SemiBold();
                        t.Span(order.Status);
                    });
                });
            });
        }

        private void ComposeItemsTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Producto");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Cant.");
                    header.Cell().Element(HeaderCell).AlignRight().Text("P. Unit.");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Total");

                    static IContainer HeaderCell(IContainer c) =>
                        c.DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White))
                         .Background(Colors.Blue.Darken2)
                         .Padding(5);
                });

                foreach (var item in order.Items)
                {
                    table.Cell().Element(BodyCell).Text(item.ProductName);
                    table.Cell().Element(BodyCell).AlignRight().Text(item.Quantity.ToString());
                    table.Cell().Element(BodyCell).AlignRight().Text(item.UnitPrice.ToString("C2", Currency));
                    table.Cell().Element(BodyCell).AlignRight().Text(item.LineTotal.ToString("C2", Currency));

                    static IContainer BodyCell(IContainer c) =>
                        c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(5);
                }
            });
        }

        private void ComposeTotals(IContainer container)
        {
            container.AlignRight().Width(220).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Subtotal").FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(90).AlignRight().Text(order.Subtotal.ToString("C2", Currency));
                });
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Impuestos").FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(90).AlignRight().Text(order.Tax.ToString("C2", Currency));
                });
                column.Item().PaddingTop(6).BorderTop(1).BorderColor(Colors.Black).Row(row =>
                {
                    row.RelativeItem().PaddingTop(4).Text("Total").Bold().FontSize(12);
                    row.ConstantItem(90).PaddingTop(4).AlignRight().Text(order.Total.ToString("C2", Currency)).Bold().FontSize(12);
                });
            });
        }
    }
}
