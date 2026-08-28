using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TradeIt.Models;

namespace TradeIt.Portfolios
{
    public partial class PortfolioEditorWindow : Window
    {
        // The redundant DataTypeComboBox was removed from the editor XAML.
        // Date/time presence is controlled only by NoDateTimeCheckBox.
        private string GetSelectedDataType() => "TseDaily";
    }
}