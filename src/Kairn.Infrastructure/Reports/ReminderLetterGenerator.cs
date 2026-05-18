using System.Net;
using System.Text;

namespace Kairn.Infrastructure.Reports;

public static class ReminderLetterGenerator
{
    private const string Css =
        "body{font-family:Arial,sans-serif;font-size:13px;color:#1a1a1a;margin:40px}" +
        ".recipient{margin-bottom:24px}" +
        ".meta{text-align:right;margin-bottom:32px;color:#555}" +
        ".subject{font-weight:bold;margin-bottom:24px}" +
        ".body{margin-bottom:32px;white-space:pre-wrap}" +
        "table{border-collapse:collapse;width:100%;margin-bottom:16px}" +
        "th,td{border:1px solid #ccc;padding:6px 10px;text-align:left}" +
        "th{background:#f5f5f5}" +
        ".amount{text-align:right;font-weight:bold}" +
        ".total-row td{background:#f5f5f5;font-weight:bold}" +
        ".footer{margin-top:40px}";

    public static string GenerateDefaultMessage(
        string invoiceRef, DateOnly dueDate, string currency, decimal outstanding) =>
        $"Nous nous permettons de vous rappeler que la facture {invoiceRef}, " +
        $"dont l'échéance était fixée au {dueDate:dd/MM/yyyy}, " +
        $"présente un solde dû de {currency} {outstanding:N2} qui n'a pas encore été réglé.\n\n" +
        "Nous vous remercions de bien vouloir régulariser cette situation dans les meilleurs délais " +
        "et restons à votre disposition pour tout renseignement complémentaire.";

    public static string GenerateHtml(
        string customerName, string? customerAddress,
        string invoiceRef, DateOnly dueDate,
        string currency, decimal outstanding,
        string messageText, string senderName)
    {
        var addressHtml = string.IsNullOrWhiteSpace(customerAddress)
            ? ""
            : "<br>" + Enc(customerAddress).Replace("\n", "<br>");

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"fr\"><head><meta charset=\"utf-8\">");
        sb.Append("<style>").Append(Css).Append("</style></head><body>");
        sb.Append("<div class=\"recipient\"><strong>").Append(Enc(customerName)).Append("</strong>")
          .Append(addressHtml).Append("</div>");
        sb.Append("<div class=\"meta\">Le ").Append(DateOnly.FromDateTime(DateTime.Today).ToString("dd/MM/yyyy")).Append("</div>");
        sb.Append("<div class=\"subject\">Objet : Relance de paiement – Facture ").Append(Enc(invoiceRef)).Append("</div>");
        sb.Append("<p>Madame, Monsieur,</p>");
        sb.Append("<div class=\"body\">").Append(Enc(messageText)).Append("</div>");
        sb.Append("<table><tr><th>Référence</th><th>Échéance</th><th>Solde dû</th></tr>");
        sb.Append("<tr><td>").Append(Enc(invoiceRef)).Append("</td>");
        sb.Append("<td>").Append(dueDate.ToString("dd/MM/yyyy")).Append("</td>");
        sb.Append("<td class=\"amount\">").Append(currency).Append(" ").Append(outstanding.ToString("N2")).Append("</td></tr></table>");
        sb.Append("<div class=\"footer\"><p>Cordialement,</p><p><strong>").Append(Enc(senderName)).Append("</strong></p></div>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    public static string GenerateBulkHtml(
        string customerName, string? customerAddress,
        IReadOnlyList<(string Reference, DateOnly DueDate, string Currency, decimal Outstanding)> invoices,
        string senderName)
    {
        var addressHtml = string.IsNullOrWhiteSpace(customerAddress)
            ? ""
            : "<br>" + Enc(customerAddress).Replace("\n", "<br>");

        var total = invoices.Sum(i => i.Outstanding);
        var currency = invoices.Count > 0 ? invoices[0].Currency : "EUR";
        var count = invoices.Count;
        var defaultMsg =
            $"Sauf erreur de notre part, nous constatons que {count} facture(s) " +
            "vous ayant été adressée(s) n'ont pas encore été réglées à ce jour.\n\n" +
            "Nous vous remercions de bien vouloir régulariser votre situation dans les meilleurs délais.";

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"fr\"><head><meta charset=\"utf-8\">");
        sb.Append("<style>").Append(Css).Append("</style></head><body>");
        sb.Append("<div class=\"recipient\"><strong>").Append(Enc(customerName)).Append("</strong>")
          .Append(addressHtml).Append("</div>");
        sb.Append("<div class=\"meta\">Le ").Append(DateOnly.FromDateTime(DateTime.Today).ToString("dd/MM/yyyy")).Append("</div>");
        sb.Append("<div class=\"subject\">Objet : Relance de paiement – ").Append(count).Append(" facture(s) en retard</div>");
        sb.Append("<p>Madame, Monsieur,</p>");
        sb.Append("<div class=\"body\">").Append(Enc(defaultMsg)).Append("</div>");
        sb.Append("<table><tr><th>Référence</th><th>Échéance</th><th>Solde dû</th></tr>");
        foreach (var inv in invoices)
        {
            sb.Append("<tr><td>").Append(Enc(inv.Reference)).Append("</td>");
            sb.Append("<td>").Append(inv.DueDate.ToString("dd/MM/yyyy")).Append("</td>");
            sb.Append("<td style=\"text-align:right\">").Append(inv.Currency).Append(" ").Append(inv.Outstanding.ToString("N2")).Append("</td></tr>");
        }
        sb.Append("<tr class=\"total-row\"><td colspan=\"2\">Total</td>");
        sb.Append("<td class=\"amount\">").Append(currency).Append(" ").Append(total.ToString("N2")).Append("</td></tr></table>");
        sb.Append("<div class=\"footer\"><p>Cordialement,</p><p><strong>").Append(Enc(senderName)).Append("</strong></p></div>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string Enc(string s) => WebUtility.HtmlEncode(s);
}
