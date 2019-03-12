using System;
using System.Globalization;
using System.Windows.Data;

namespace RSMaster.UI
{
    internal class GroupValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                int.TryParse(value?.ToString(), out int groupId);
                if (groupId > 0)
                {
                    var model = MainWindow.GetGroupByIdHandler(groupId);
                    if (model != null)
                    {
                        return (parameter != null && parameter.Equals("Tooltip")) ? "Group: " + model.Name : model.Color;
                    }
                }
            }
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
