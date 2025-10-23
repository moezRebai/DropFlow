namespace DropFlow.ViewModels;

/// <summary>
/// Helper pour gérer les images de produits depuis les ressources locales
/// </summary>
public static class ProductImageHelper
{
    // ═══════════════════════════════════════════════════════════════
    // IMAGES DE MEUBLES (à placer dans Assets/Images/Products/Furniture/)
    // ═══════════════════════════════════════════════════════════════
    private static readonly string[] FurnitureImages =
    [
        "/Assets/Images/Products/sofa.jpg",
        "/Assets/Images/Products/bed.jpg",
        "/Assets/Images/Products/chair.jpg",
        "/Assets/Images/Products/table.jpg",
        "/Assets/Images/Products/wardrobe.jpg",
        "/Assets/Images/Products/bookshelf.jpg",
        "/Assets/Images/Products/desk.jpg",
    ];

    // ═══════════════════════════════════════════════════════════════
    // IMAGES D'ÉLECTROMÉNAGER (à placer dans Assets/Images/Products/Appliances/)
    // ═══════════════════════════════════════════════════════════════
    private static readonly string[] ApplianceImages =
    [
        "/Assets/Images/Products/fridge.jpg",
        "/Assets/Images/Products/washing-machine.jpg",
        "/Assets/Images/Products/oven.jpg",
        "/Assets/Images/Products/dishwasher.jpg",
        "/Assets/Images/Products/tv.jpg",
    ];

    // ═══════════════════════════════════════════════════════════════
    // MÉTHODES PUBLIQUES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Retourne une image aléatoire de meuble
    /// </summary>
    public static string GetRandomFurnitureImage()
    {
        var rand = new Random();
        return FurnitureImages[rand.Next(FurnitureImages.Length)];
    }

    /// <summary>
    /// Retourne une image aléatoire d'électroménager
    /// </summary>
    public static string GetRandomApplianceImage()
    {
        var rand = new Random();
        return ApplianceImages[rand.Next(ApplianceImages.Length)];
    }

    /// <summary>
    /// Retourne une image aléatoire (meuble ou électroménager)
    /// </summary>
    public static string GetRandomProductImage()
    {
        var rand = new Random();
        var allImages = FurnitureImages.Concat(ApplianceImages).ToArray();
        return allImages[rand.Next(allImages.Length)];
    }

    /// <summary>
    /// Retourne une image spécifique par nom de client (pour avoir des images cohérentes)
    /// </summary>
    public static string GetImageForClient(string clientName)
    {
        // Utiliser le hash du nom pour toujours retourner la même image
        var hash = Math.Abs(clientName.GetHashCode());
        var allImages = FurnitureImages.Concat(ApplianceImages).ToArray();
        return allImages[hash % allImages.Length];
    }

    /// <summary>
    /// Vérifie si une image existe dans les ressources
    /// </summary>
    private static bool ImageExists(string imagePath)
    {
        try
        {
            var uri = new Uri(imagePath, UriKind.RelativeOrAbsolute);
            var resourceInfo = System.Windows.Application.GetResourceStream(uri);
            return resourceInfo != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Retourne l'image par défaut si l'image n'existe pas
    /// </summary>
    public static string GetImageOrDefault(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            return string.Empty;

        return ImageExists(imagePath) ? imagePath : string.Empty;
    }
}