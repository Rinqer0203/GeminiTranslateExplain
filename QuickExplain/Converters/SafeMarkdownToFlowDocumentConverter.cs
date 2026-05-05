using MdXaml;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Documents;

namespace QuickExplain
{
    public sealed class SafeMarkdownToFlowDocumentConverter : IValueConverter
    {
        public Markdown? Markdown { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string ?? string.Empty;

            try
            {
                return (Markdown ?? new Markdown()).Transform(text);
            }
            catch
            {
                return new FlowDocument(new Paragraph(new Run(text)));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
