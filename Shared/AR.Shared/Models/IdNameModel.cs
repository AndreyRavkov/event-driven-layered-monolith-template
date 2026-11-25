using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AR.Shared.Models;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class IdNameModel<T, TY> : IEqualityComparer<IdNameModel<T, TY>>
{
    public IdNameModel(T id, TY name)
    {
        Id = id;
        Name = name;
    }

    [JsonConstructor]
    public IdNameModel()
    {
    }

    public T Id { get; set; } = default!;
    public TY Name { get; set; } = default!;

    public bool Equals(IdNameModel<T, TY>? x, IdNameModel<T, TY>? y)
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

    public int GetHashCode(IdNameModel<T, TY> obj) => obj.Id?.GetHashCode() ?? 0;
}