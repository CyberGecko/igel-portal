using Microsoft.Extensions.Localization;

namespace IgelPortal.Web
{
    public class DataAnnotationStringLocalizer(IStringLocalizer? primaryLocalizer, IStringLocalizer? fallbackLocalizer) : IStringLocalizer
    {
        private readonly IStringLocalizer primaryLocalizer = primaryLocalizer ?? throw new ArgumentNullException(nameof(primaryLocalizer));
        private readonly IStringLocalizer fallbackLocalizer = fallbackLocalizer ?? throw new ArgumentNullException(nameof(fallbackLocalizer));

        public LocalizedString this[string name]
        {
            get
            {
                LocalizedString localizedString = primaryLocalizer[name];
                if (localizedString.ResourceNotFound)
                {
                    localizedString = fallbackLocalizer[name];
                }

                return localizedString;
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                LocalizedString localizedString = primaryLocalizer[name, arguments];
                if (localizedString.ResourceNotFound)
                {
                    localizedString = fallbackLocalizer[name, arguments];
                }

                return localizedString;
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => primaryLocalizer.GetAllStrings(includeParentCultures)
                                                                                                         .Concat(fallbackLocalizer.GetAllStrings(includeParentCultures));
    }
}
