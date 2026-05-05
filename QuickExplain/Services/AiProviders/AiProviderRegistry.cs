using QuickExplain.Models;

namespace QuickExplain.Services.AiProviders
{
    internal sealed class AiProviderRegistry
    {
        private readonly Dictionary<AiType, IAiProvider> _providers;

        public AiProviderRegistry(IEnumerable<IAiProvider> providers)
        {
            _providers = providers.ToDictionary(provider => provider.Type);
        }

        public bool TryGetProvider(AiType type, out IAiProvider provider)
        {
            return _providers.TryGetValue(type, out provider!);
        }
    }
}
