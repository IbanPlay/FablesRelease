namespace CalamityFables.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ReplacingCalamityAttribute : Attribute
    {
        public readonly string[] calamityVersions;

        public ReplacingCalamityAttribute(params string[] names)
        {
            calamityVersions = names ?? throw new ArgumentNullException(nameof(names));
        }
    }
}
