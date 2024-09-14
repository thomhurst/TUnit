using System.Linq.Expressions;
using TUnit.Assertions.AssertConditions.ClassMember;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface IHas<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    internal Has<TActual, TAnd, TOr> Has();
}