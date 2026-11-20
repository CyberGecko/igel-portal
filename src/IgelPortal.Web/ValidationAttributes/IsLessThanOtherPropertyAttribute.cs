using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace IgelPortal.Web.ValidationAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IsLessThanOtherPropertyAttribute : ValidationAttribute
    {
        public IsLessThanOtherPropertyAttribute(String otherProperty)
        {
            this.OtherProperty = otherProperty;
        }

        public String OtherProperty { get; }

        public String? OtherPropertyDisplayName { get; internal set; }

        public override Boolean RequiresValidationContext => true;

        public override String FormatErrorMessage(String name) => String.Format(CultureInfo.CurrentCulture, base.ErrorMessageString, name, this.OtherPropertyDisplayName ?? this.OtherProperty);

        protected override ValidationResult? IsValid(Object? value, ValidationContext validationContext)
        {
            PropertyInfo? otherPropertyInfo = validationContext.ObjectType.GetRuntimeProperty(OtherProperty);
            if (otherPropertyInfo == null)
            {
                return new ValidationResult(String.Format("Could not find a property named {0}.", OtherProperty));
            }
            if (otherPropertyInfo.GetIndexParameters().Length > 0)
            {
                throw new ArgumentException(String.Format("The property {0}.{1} could not be found.", validationContext.ObjectType.FullName, OtherProperty));
            }
            if (!this.IsNumericType(otherPropertyInfo.PropertyType))
            {
                throw new ArgumentException(String.Format("The property {0}.{1} is not a numeric type.", validationContext.ObjectType.FullName, OtherProperty));
            }

            Object? otherPropertyValue = otherPropertyInfo.GetValue(validationContext.ObjectInstance, null);
            Comparer comp = new(CultureInfo.CurrentCulture);
            if (comp.Compare(value, otherPropertyValue) > 0)
            {
                OtherPropertyDisplayName ??= GetDisplayNameForProperty(otherPropertyInfo);

                string[]? memberNames = validationContext.MemberName != null ? new[] { validationContext.MemberName } : null;
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName), memberNames);
            }

            return null;
        }

        private String? GetDisplayNameForProperty(PropertyInfo property)
        {
            IEnumerable<Attribute> attributes = CustomAttributeExtensions.GetCustomAttributes(property, true);
            foreach (Attribute attribute in attributes)
            {
                if (attribute is DisplayAttribute display)
                {
                    return display.GetName();
                }
            }

            return this.OtherProperty;
        }

        public Boolean IsNumericType(Type propertyType)
        {
            switch (Type.GetTypeCode(propertyType))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}
