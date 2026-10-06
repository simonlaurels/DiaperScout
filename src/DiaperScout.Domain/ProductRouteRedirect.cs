namespace DiaperScout.Domain;

/// <summary>Preserves a reconciled product's public identity without deleting its source record or history.</summary>
public sealed class ProductRouteRedirect : Entity
{
    private ProductRouteRedirect() { }
    public ProductRouteRedirect(Guid sourceProductId, Guid targetProductId, Guid defaultVariantId)
    {
        if (sourceProductId == Guid.Empty || targetProductId == Guid.Empty || defaultVariantId == Guid.Empty || sourceProductId == targetProductId)
            throw new ArgumentException("Distinct source and target products and a preserved variant are required.");
        SourceProductId = sourceProductId;
        TargetProductId = targetProductId;
        DefaultVariantId = defaultVariantId;
    }
    public Guid SourceProductId { get; private set; }
    public Guid TargetProductId { get; private set; }
    public Guid DefaultVariantId { get; private set; }
}
