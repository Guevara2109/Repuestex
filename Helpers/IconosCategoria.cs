namespace Repuestex;

public static class IconosCategoria
{
    public static string Para(string? categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria))
            return "bi-box-seam";

        if (categoria.Contains("freno", StringComparison.OrdinalIgnoreCase))
            return "bi-disc";

        if (categoria.Contains("motor", StringComparison.OrdinalIgnoreCase))
            return "bi-gear-wide-connected";

        if (categoria.Contains("suspensi", StringComparison.OrdinalIgnoreCase) ||
            categoria.Contains("direcci", StringComparison.OrdinalIgnoreCase))
            return "bi-arrows-move";

        if (categoria.Contains("eléct", StringComparison.OrdinalIgnoreCase) ||
            categoria.Contains("elect", StringComparison.OrdinalIgnoreCase))
            return "bi-lightning-charge";

        if (categoria.Contains("carrocer", StringComparison.OrdinalIgnoreCase) ||
            categoria.Contains("exterior", StringComparison.OrdinalIgnoreCase))
            return "bi-car-front";

        if (categoria.Contains("filtro", StringComparison.OrdinalIgnoreCase))
            return "bi-funnel";

        if (categoria.Contains("aceite", StringComparison.OrdinalIgnoreCase) ||
            categoria.Contains("lubric", StringComparison.OrdinalIgnoreCase))
            return "bi-droplet";

        return "bi-box-seam";
    }
}
