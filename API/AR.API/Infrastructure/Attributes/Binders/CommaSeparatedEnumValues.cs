namespace AR.API.Infrastructure.Attributes.Binders;

internal sealed class CommaSeparatedEnumValues<T>  : CommaSeparatedBaseModelBinder<T>
    //added struct to skip Nullable<T> so T not nullable
    where T : struct, Enum
{
    protected override T Parse(string value) => Enum.Parse<T>(value);
}