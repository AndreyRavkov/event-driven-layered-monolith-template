using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AR.Shared.Models;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class IdValueModel<T, TY> : IEqualityComparer<IdValueModel<T, TY>>
{
    public IdValueModel(T id, TY value)
    {
        Id = id;
        Value = value;
    }

    [JsonConstructor]
    public IdValueModel()
    {
    }

    public T Id { get; set; } = default!;
    public TY Value { get; set; } = default!;

    public bool Equals(IdValueModel<T, TY>? x, IdValueModel<T, TY>? y)
    {
        if (x is null && y is null)
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

#pragma warning disable IDE0046
        if (x.Id is null && y.Id is null)
#pragma warning restore IDE0046
        {
            return true;
        }

        return x.Id!.Equals(y.Id);
    }

    public int GetHashCode(IdValueModel<T, TY> obj) => obj.Id?.GetHashCode() ?? 0;
}