using System.Windows.Data;
using System.Windows.Markup;
using Taskloom.Infrastructure.Localization;

namespace Taskloom.Common.Markup;

/// <summary>
/// Создаёт WPF binding к локализованной строке по ключу.
/// </summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new System.Windows.Data.Binding($"[{Key}]")
        {
            Source = LocalizationManager.Source,
            Mode = BindingMode.OneWay
        }.ProvideValue(serviceProvider);
    }
}
