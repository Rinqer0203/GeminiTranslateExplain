namespace QuickExplain.Services.AiProviders
{
    internal sealed record AiProviderRequest(
        string ModelName,
        string SystemInstruction,
        IReadOnlyList<(string Role, string Text)> Messages,
        byte[]? ImageBytes = null,
        string ImageMimeType = "image/png");
}
