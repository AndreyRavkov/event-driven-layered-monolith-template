namespace AR.API.Infrastructure.Attributes.Binders;

internal class CommaSeparatedIntValues : CommaSeparatedBaseModelBinder<int>
{
    protected override int Parse(string value) => int.Parse(value);
}