using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinBazaar.Infrastructure.MessageQueue.Camunda
{
    public class CustomDateTimeTypeConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {

            return ((DateTime?)value) ?? (DateTime?)null;
        }
    }

    [TypeConverter(typeof(CustomDateTimeTypeConverter))]
    struct AdvancedDateTime
    {
    }
}
